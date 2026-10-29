using System.Security.Cryptography;

namespace Edx.Shared.Efatura.Envelope;

/// <summary>The hash the center checks an envelope with: the MD5 of the zip, in lowercase hex (the center's choice, not ours).</summary>
public static class Hashing
{
#pragma warning disable CA5351 // MD5 is what the GİB service uses to check an envelope; it is a checksum here, not a security measure
    public static string Md5Hex(byte[] bytes) => Convert.ToHexStringLower(MD5.HashData(bytes));
#pragma warning restore CA5351
}
