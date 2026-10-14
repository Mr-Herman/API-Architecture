using EAP.Models.Common;
using EAP.Models.Requests.Quality;
using EAP.Models.Responses.Quality;

namespace EAP.Application.Interfaces;

public interface IQualityInspectionService
{
    Task<ApiResult<QualityInspectionResponse>> ValidateInspectionAsync(QualityInspectionRequest request, CancellationToken cancellationToken);
}
