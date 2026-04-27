using System.Security.Cryptography;
using Microsoft.Extensions.Logging;

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
}
