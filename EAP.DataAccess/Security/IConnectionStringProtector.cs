namespace EAP.DataAccess.Security;

public interface IConnectionStringProtector
{
    string Encrypt(string connectionString);

    string Decrypt(string protectedConnectionString);
}
