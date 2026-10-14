namespace EAP.Application.Localization;

public interface IMessageLocalizer
{
    string this[string key] { get; }
}
