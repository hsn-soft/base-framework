using System.Security.Cryptography;
using System.Text;

namespace Hhs.Shared.Helper.Utils;

public class Hash
{
    private readonly HashAlgorithm _cryptoService;
    private string _salt;

    public Hash()
    {
        // Default Hash algorithm
        _cryptoService = new SHA1Managed();
    }

    public Hash(string serviceProviderName)
    {
        // Set Hash algorithm
        _cryptoService = (HashAlgorithm)CryptoConfig.CreateFromName(
            serviceProviderName.ToUpper());
    }

    public Hash(HashServiceProviderEnum serviceProvider)
    {
        // Select hash algorithm
        switch (serviceProvider)
        {
            case HashServiceProviderEnum.MD5:
                _cryptoService = new MD5CryptoServiceProvider();
                break;
            case HashServiceProviderEnum.SHA1:
                _cryptoService = new SHA1Managed();
                break;
            case HashServiceProviderEnum.SHA256:
                _cryptoService = new SHA256Managed();
                break;
            case HashServiceProviderEnum.SHA384:
                _cryptoService = new SHA384Managed();
                break;
            case HashServiceProviderEnum.SHA512:
                _cryptoService = new SHA512Managed();
                break;
        }
    }

    public virtual string HashPassword(string plainText)
    {
        byte[] cryptoByte = _cryptoService.ComputeHash(
            Encoding.ASCII.GetBytes(plainText + _salt));

        return Convert.ToBase64String(cryptoByte, 0, cryptoByte.Length);
    }

    public virtual bool Verify(string plainText, string hash)
    {
        return HashPassword(plainText).Equals(hash);
    }

    public string Salt
    {
        get { return _salt; }
        set => _salt = value;
    }
}

public enum HashServiceProviderEnum
{
    SHA1,
    SHA256,
    SHA384,
    SHA512,
    MD5
}