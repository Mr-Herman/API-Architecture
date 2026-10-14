using EAP.Models.Constants;

namespace EAP.Application.Exceptions;

public sealed class RequestValidationException : BusinessException
{
    public RequestValidationException(string message) : base(message, BusinessCode.ParamError)
    {
    }
}
