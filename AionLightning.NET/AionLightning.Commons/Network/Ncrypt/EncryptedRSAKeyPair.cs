using System.Security.Cryptography;

namespace AionLightning.Commons.Network.Ncrypt;

public sealed class EncryptedRSAKeyPair
{
    public RSAParameters RsaKeyPair { get; }
    public byte[] PublicKey { get; }

    public EncryptedRSAKeyPair(RSAParameters rsaKeyPair)
    {
        RsaKeyPair = rsaKeyPair;
        PublicKey = ScrambleModulus(rsaKeyPair.Modulus
            ?? throw new ArgumentException("RSA modulus is null", nameof(rsaKeyPair)));
    }

    private static byte[] ScrambleModulus(byte[] modulus)
    {
        if (modulus.Length != 128)
            throw new ArgumentException("RSA modulus must be 128 bytes");

        var scrambled = (byte[])modulus.Clone();

        for (int i = 0; i < 4; i++)
        {
            (scrambled[i], scrambled[i + 77]) = (scrambled[i + 77], scrambled[i]);
        }

        for (int i = 0; i < 64; i++)
            scrambled[i] = (byte)(scrambled[i] ^ scrambled[i + 64]);

        for (int i = 0; i < 4; i++)
            scrambled[i + 13] = (byte)(scrambled[i + 13] ^ scrambled[i + 52]);

        for (int i = 0; i < 64; i++)
            scrambled[i + 64] = (byte)(scrambled[i + 64] ^ scrambled[i]);

        return scrambled;
    }
}
