SET XACT_ABORT ON;
GO

IF OBJECT_ID(N'dbo.SN_DEFECT_DET_API', N'U') IS NULL
    THROW 50002, 'dbo.SN_DEFECT_DET_API does not exist.', 1;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.SN_DEFECT_DET_API')
      AND name = N'IX_SN_DEFECT_DET_API_ProcessRelation'
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_SN_DEFECT_DET_API_ProcessRelation
        ON dbo.SN_DEFECT_DET_API
        (
            PROJ_CODE,
            PRODUCT_CODE,
            PROCESS_NAME,
            LOT_NO
        )
        INCLUDE
        (
            SN,
            DEFECT_CODE,
            DEFECT,
            DEFECT_QTY,
            START_TIME,
            TEST_TIME
        );
END;
GO
