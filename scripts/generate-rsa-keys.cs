// Makes an RSA key pair for the tokens of a sample system: the private key signs (only the auth server has it), the public key checks (every host has it).
//
//   dotnet run scripts/generate-rsa-keys.cs -- <private-key-file> <public-key-file>
//   dotnet run scripts/generate-rsa-keys.cs -- samples/abc/app/Abc.AuthServer.Host/private_key.xml samples/abc/shared/Abc.Shared.Hosting/public_key.xml
//
// Both files are the XML of RSA.ToXmlString (2048 bits). It refuses to overwrite a file that exists. The private key is a secret: git ignores `private_key.xml`, never commit one.
// Runs with only the .NET SDK, so it is the same on every platform.
using System.Security.Cryptography;

if (args.Length != 2)
{
    Console.Error.WriteLine("Usage: dotnet run scripts/generate-rsa-keys.cs -- <private-key-file> <public-key-file>");
    return 2;
}

foreach (string file in args.Where(File.Exists))
{
    Console.Error.WriteLine($"'{file}' exists: it is not overwritten (delete it first to make a new pair).");
    return 1;
}

using RSA rsa = RSA.Create(2048);
File.WriteAllText(args[0], rsa.ToXmlString(includePrivateParameters: true));
File.WriteAllText(args[1], rsa.ToXmlString(includePrivateParameters: false));
Console.WriteLine($"private key: {args[0]}");
Console.WriteLine($"public key:  {args[1]}");
return 0;
