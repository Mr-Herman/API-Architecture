using EAP.Models.Constants;

namespace EAP.Application.Exceptions;

public sealed class ResourceNotFoundException(string message) : BusinessException(message, BusinessCode.NotFound);
