namespace EAP.Application.Exceptions;

public abstract class BusinessException : Exception
{
    protected BusinessException(string message, int code) : base(message)
    {
        Code = code;
    }

    public int Code { get; }
}
