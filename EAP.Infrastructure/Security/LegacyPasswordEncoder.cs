using System.Text;
using EAP.Application.Interfaces;

namespace EAP.Infrastructure.Security;

public sealed class LegacyPasswordEncoder : ILegacyPasswordEncoder
{
    private const int BoxLength = 255;
    private const string PasswordKey = "#4c#2@!";

    public string Encode(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var box = CreateBox(PasswordKey);
        var input = Encoding.Unicode.GetBytes(value);
        var output = new byte[input.Length];
        var i = 0;
        var j = 0;

        for (var offset = 0; offset < input.Length; offset++)
        {
            i = (i + 1) % BoxLength;
            j = (j + box[i]) % BoxLength;
            (box[i], box[j]) = (box[j], box[i]);
            output[offset] = (byte)(input[offset] ^ box[(box[i] + box[j]) % BoxLength]);
        }

        return Convert.ToHexString(output);
    }

    private static byte[] CreateBox(string key)
    {
        var box = new byte[BoxLength];
        var keyBytes = Encoding.ASCII.GetBytes(key);
        var index = 0;

        for (var count = 0; count < BoxLength; count++)
        {
            box[count] = (byte)count;
        }

        for (var count = 0; count < BoxLength; count++)
        {
            index = (index + box[count] + keyBytes[count % keyBytes.Length]) % BoxLength;
            (box[count], box[index]) = (box[index], box[count]);
        }

        return box;
    }
}
