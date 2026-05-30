using System.Security.Cryptography;
using System.Text;

namespace Hhs.Shared.Helper.Utils;

public static class Cryptography
{
    #region Settings

    private const int Iterations = 2;
    private const int KeySize = 256;

    private const string Salt = "xx1ssfi32dd201l21"; // Random
    private const string Vector = "92af32sz1wl24la1"; // Random
    private const string DefaultPassword = "aila42a2daabab4ss2kaal98llos2b226x";

    #endregion

    public static string Encrypt(string value)
    {
        return string.IsNullOrEmpty(value) ? null : Encrypt(value, DefaultPassword);
    }

    public static string Encrypt(string value, string password)
    {
        byte[] vectorBytes = Encoding.ASCII.GetBytes(Vector);
        byte[] saltBytes = Encoding.ASCII.GetBytes(Salt);
        byte[] valueBytes = Encoding.UTF8.GetBytes(value);

        byte[] encrypted;
        using (Aes cipher = Aes.Create())
        {
            Rfc2898DeriveBytes passwordDeriveBytes =
                new Rfc2898DeriveBytes(password, saltBytes, Iterations, HashAlgorithmName.SHA1);
            byte[] keyBytes = passwordDeriveBytes.GetBytes(KeySize / 8);
            cipher.Mode = CipherMode.CBC;

            using (ICryptoTransform encryptor = cipher.CreateEncryptor(keyBytes, vectorBytes))
            {
                using (MemoryStream to = new MemoryStream())
                {
                    using (CryptoStream writer = new CryptoStream(to, encryptor, CryptoStreamMode.Write))
                    {
                        writer.Write(valueBytes, 0, valueBytes.Length);
                        writer.FlushFinalBlock();
                        encrypted = to.ToArray();
                    }
                }
            }

            cipher.Clear();
        }

        return Convert.ToBase64String(encrypted);
    }

    public static string Decrypt(string value)
    {
        return string.IsNullOrEmpty(value) ? null : Decrypt(value, DefaultPassword);
    }

    public static string Decrypt(string value, string password)
    {
        byte[] vectorBytes = Encoding.ASCII.GetBytes(Vector);
        byte[] saltBytes = Encoding.ASCII.GetBytes(Salt);
        byte[] valueBytes = Convert.FromBase64String(value);

        using (Aes cipher = Aes.Create())
        {
            Rfc2898DeriveBytes passwordDeriveBytes =
                new Rfc2898DeriveBytes(password, saltBytes, Iterations, HashAlgorithmName.SHA1);
            byte[] keyBytes = passwordDeriveBytes.GetBytes(KeySize / 8);
            cipher.Mode = CipherMode.CBC;

            try
            {
                using (ICryptoTransform decryptor = cipher.CreateDecryptor(keyBytes, vectorBytes))
                {
                    using (MemoryStream from = new MemoryStream(valueBytes))
                    {
                        using (CryptoStream reader = new CryptoStream(from, decryptor, CryptoStreamMode.Read))
                        {
                            cipher.Clear();
                            return new StreamReader(reader).ReadToEnd();
                        }
                    }
                }
            }
            catch (Exception)
            {
                cipher.Clear();
                return String.Empty;
            }
        }
    }
}