using EAP.Application.Interfaces;
using EAP.DataAccess.Base;
using EAP.DataAccess.Database;
using EAP.Models.Entities.Quality;

namespace EAP.DataAccess.Repositories.Quality;

public sealed class SpcInspectionRepository : RepositoryBase, ISpcInspectionRepository
{
    private const string DatabaseName = "SpcOracle";

    public SpcInspectionRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory, DatabaseName)
    {
    }

    public Task<string?> GetProcessCodeAsync(string project, string process, CancellationToken cancellationToken)
    {
        string sql = @"
            SELECT fis_process_code
            FROM eap_fis_process1
            WHERE fis_product = :Project
              AND fis_process_id = :Process
            FETCH FIRST 1 ROW ONLY
            ";

        return QueryFirstOrDefaultAsync<string>(
            sql,
            new { Project = project, Process = process },
            cancellationToken);
    }

    public async Task<bool> IsEquipmentConfiguredAsync(string processCode, string equipmentId, CancellationToken cancellationToken)
    {
        string sql = @"
            SELECT COUNT(1)
            FROM eap_fisprocesscode_equipid4
            WHERE fis_process_code = :ProcessCode
              AND equipid = :EquipmentId
            ";

        var count = await QueryFirstOrDefaultAsync<int>(
            sql,
            new { ProcessCode = processCode, EquipmentId = equipmentId },
            cancellationToken);

        return count > 0;
    }

    public Task<SpcInspectionEntity?> GetInspectionAsync(string equipmentId, string processCode, CancellationToken cancellationToken)
    {
        string sql = @"
            WITH chart_list AS
            (
                SELECT
                    chart.chart_id,
                    chart.usl,
                    chart.lsl,
                    cycle.cycle_time,
                    cycle.delay_time
                FROM spc_chart_list chart
                INNER JOIN eap_equip_cycle_time3 cycle
                    ON chart.chart_id = cycle.qa_id
                INNER JOIN eap_fis_qa2 quality
                    ON cycle.qa_id = quality.qa_id
                WHERE quality.fis_process_code = :ProcessCode
            ),
            latest_data AS
            (
                SELECT
                    data.id5,
                    data.chart_id,
                    data.data_seq,
                    data.update_time,
                    ROW_NUMBER() OVER
                    (
                        PARTITION BY data.chart_id
                        ORDER BY data.update_time DESC, data.data_seq DESC
                    ) AS row_number
                FROM spc_variable_data data
                INNER JOIN chart_list chart
                    ON chart.chart_id = data.chart_id
                WHERE data.id5 = :EquipmentId
            ),
            latest_chart AS
            (
                SELECT
                    id5,
                    chart_id,
                    data_seq,
                    update_time
                FROM latest_data
                WHERE row_number = 1
            ),
            chart_status AS
            (
                SELECT
                    chart.chart_id,
                    latest.id5,
                    latest.data_seq,
                    latest.update_time,
                    chart.cycle_time,
                    chart.delay_time,
                    chart.usl,
                    chart.lsl
                FROM chart_list chart
                LEFT JOIN latest_chart latest
                    ON latest.chart_id = chart.chart_id
            ),
            time_check AS
            (
                SELECT
                    COUNT(*) AS required_chart_count,
                    COUNT(status.data_seq) AS actual_chart_count,
                    COUNT(DISTINCT status.update_time) AS time_count,
                    MIN(status.update_time) AS min_update_time,
                    MAX(status.update_time) AS max_update_time
                FROM chart_status status
            ),
            measure_result AS
            (
                SELECT
                    latest.id5,
                    latest.chart_id,
                    latest.data_seq,
                    latest.update_time,
                    chart.cycle_time,
                    latest.update_time
                        + NUMTODSINTERVAL(chart.cycle_time + chart.delay_time, 'HOUR') AS valid_until,
                    CASE
                        WHEN latest.update_time
                            + NUMTODSINTERVAL(chart.cycle_time + chart.delay_time, 'HOUR') >= SYSDATE
                        THEN 1
                        ELSE 0
                    END AS time_pass,
                    measurement.value,
                    chart.usl,
                    chart.lsl,
                    CASE
                        WHEN (chart.usl IS NULL OR measurement.value <= chart.usl)
                         AND (chart.lsl IS NULL OR measurement.value >= chart.lsl)
                        THEN 1
                        ELSE 0
                    END AS value_pass
                FROM latest_chart latest
                INNER JOIN spc_variable_value measurement
                    ON measurement.chart_id = latest.chart_id
                    AND measurement.data_seq = latest.data_seq
                INNER JOIN chart_list chart
                    ON chart.chart_id = latest.chart_id
            ),
            fail_summary AS
            (
                SELECT
                    id5,
                    SUM(
                        CASE
                            WHEN time_pass = 0 OR value_pass = 0 THEN 1
                            ELSE 0
                        END
                    ) AS fail_count,
                    LISTAGG(
                        CASE
                            WHEN time_pass = 0
                            THEN chart_id
                                || ': Data has expired, Update Time='
                                || TO_CHAR(update_time, 'YYYY-MM-DD HH24:MI:SS')
                                || ', Cycle Time='
                                || TO_CHAR(cycle_time)
                                || ' Hours, Deadline='
                                || TO_CHAR(valid_until, 'YYYY-MM-DD HH24:MI:SS')
                            WHEN usl IS NOT NULL AND value > usl
                            THEN chart_id
                                || ': value='
                                || TO_CHAR(value)
                                || ' > USL='
                                || TO_CHAR(usl)
                            WHEN lsl IS NOT NULL AND value < lsl
                            THEN chart_id
                                || ': value='
                                || TO_CHAR(value)
                                || ' < LSL='
                                || TO_CHAR(lsl)
                            ELSE NULL
                        END,
                        '; '
                    ) WITHIN GROUP (ORDER BY chart_id, data_seq) AS problem_detail
                FROM measure_result
                WHERE time_pass = 0 OR value_pass = 0
                GROUP BY id5
            )
            SELECT
                CASE
                    WHEN time_check.required_chart_count = 0 THEN 'FAIL'
                    WHEN time_check.actual_chart_count < time_check.required_chart_count THEN 'FAIL'
                    WHEN time_check.time_count > 1 THEN 'FAIL'
                    WHEN NVL(fail_summary.fail_count, 0) > 0 THEN 'FAIL'
                    ELSE 'PASS'
                END AS Result,
                CASE
                    WHEN time_check.required_chart_count = 0
                    THEN 'No SPC chart is configured'
                    WHEN time_check.actual_chart_count < time_check.required_chart_count
                    THEN 'Chart data is incomplete. Required='
                        || TO_CHAR(time_check.required_chart_count)
                        || ', Actual='
                        || TO_CHAR(time_check.actual_chart_count)
                    WHEN time_check.time_count > 1
                    THEN 'Chart update times are inconsistent. Earliest='
                        || TO_CHAR(time_check.min_update_time, 'YYYY-MM-DD HH24:MI:SS')
                        || ', Latest='
                        || TO_CHAR(time_check.max_update_time, 'YYYY-MM-DD HH24:MI:SS')
                    WHEN fail_summary.fail_count > 0
                    THEN fail_summary.problem_detail
                    ELSE NULL
                END AS ProblemMessage
            FROM time_check
            LEFT JOIN fail_summary
                ON 1 = 1
            ";

        return QueryFirstOrDefaultAsync<SpcInspectionEntity>(
            sql,
            new { EquipmentId = equipmentId, ProcessCode = processCode },
            cancellationToken);
    }
}
