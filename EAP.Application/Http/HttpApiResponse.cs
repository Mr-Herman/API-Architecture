namespace EAP.Application.Http;

public sealed record HttpApiResponse(bool IsSuccess, int StatusCode, string Content);
