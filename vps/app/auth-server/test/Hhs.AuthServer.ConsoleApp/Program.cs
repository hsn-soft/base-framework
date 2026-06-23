using System.Security.Cryptography;

namespace Hhs.AuthServer.ConsoleApp;

public static class Program
{
    private static async Task Main()
    {
        await CreatePrivatePublicPemAsync();
    }

    private static async Task CreatePrivatePublicPemAsync()
    {
        using var rsa = RSA.Create();
        Console.WriteLine($"-----Private key-----{Environment.NewLine}{Convert.ToBase64String(rsa.ExportRSAPrivateKey())}{Environment.NewLine}-----Private key-----");
        Console.WriteLine($"-----Public key-----{Environment.NewLine}{Convert.ToBase64String(rsa.ExportRSAPublicKey())}-----Public key-----");

        await Task.Delay(100);
    }
}