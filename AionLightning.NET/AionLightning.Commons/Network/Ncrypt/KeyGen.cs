using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;

namespace AionLightning.Commons.Network.Ncrypt;

public static class KeyGen
{
    private const int KeyPairCount = 10;
    private static EncryptedRSAKeyPair[]? _keyPairs;

    public static void Init(ILogger? log = null)
    {
        _keyPairs = new EncryptedRSAKeyPair[KeyPairCount];
        for (int i = 0; i < KeyPairCount; i++)
        {
            using var rsa = RSA.Create(1024);
            _keyPairs[i] = new EncryptedRSAKeyPair(rsa.ExportParameters(true));
        }
        log?.LogInformation("KeyGen: cached {Count} RSA key pairs.", KeyPairCount);
    }

    public static EncryptedRSAKeyPair GetEncryptedRSAKeyPair()
    {
        if (_keyPairs is null)
            throw new InvalidOperationException("KeyGen.Init() must be called before GetEncryptedRSAKeyPair().");
        return _keyPairs[Random.Shared.Next(KeyPairCount)];
    }

    public static byte[] DecryptRSA(RSAParameters rsaParams, byte[] encrypted)
    {
        var engine = new RsaEngine();
        engine.Init(false, new RsaPrivateCrtKeyParameters(
            new BigInteger(1, rsaParams.Modulus!),
            new BigInteger(1, rsaParams.Exponent!),
            new BigInteger(1, rsaParams.D!),
            new BigInteger(1, rsaParams.P!),
            new BigInteger(1, rsaParams.Q!),
            new BigInteger(1, rsaParams.DP!),
            new BigInteger(1, rsaParams.DQ!),
            new BigInteger(1, rsaParams.InverseQ!)
        ));
        var result = engine.ProcessBlock(encrypted, 0, encrypted.Length);
        int blockSize = rsaParams.Modulus!.Length; // 128 bytes for 1024-bit key
        if (result.Length == blockSize)
            return result;
        var padded = new byte[blockSize];
        result.CopyTo(padded, blockSize - result.Length);
        return padded;
    }
}
