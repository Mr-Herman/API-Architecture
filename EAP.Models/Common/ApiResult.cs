using EAP.Models.Constants;

namespace EAP.Models.Common;

public sealed class ApiResult<T>
{
    public int Code { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }

    public static ApiResult<T> SuccessResult(T? data, string message = "success", int code = BusinessCode.Success)
    {
        return new ApiResult<T>
        {
            Success = true,
            Code = code,
            Message = message,
            Data = data
        };
    }

    public static ApiResult<T> FailResult(string message, int code)
    {
        return new ApiResult<T>
        {
            Success = false,
            Code = code,
            Message = message,
            Data = default
        };
    }
}
