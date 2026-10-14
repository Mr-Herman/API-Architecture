namespace EAP.Application.Interfaces;

public interface ILegacyPasswordEncoder
{
    string Encode(string value);
}
