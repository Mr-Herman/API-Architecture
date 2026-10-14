using EAP.Models.Constants;

namespace EAP.Application.Exceptions;

public sealed class AuthenticationFailedException(string message) : BusinessException(message, BusinessCode.JwtInvalid);
