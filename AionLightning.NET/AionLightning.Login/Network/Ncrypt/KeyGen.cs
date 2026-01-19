using System.Security.Cryptography;
using Microsoft.Extensions.Logging;

namespace AionLightning.LoginServer.Network.Ncrypt
{
    public class KeyGen
    {
        private static readonly ILogger<KeyGen> _log = new LoggerFactory().CreateLogger<KeyGen>();
        private static EncryptedRSAKeyPair[] _encryptedRSAKeyPairs;
        private static readonly int _keyPairs = 10;

        public static void Init()
        {
            try
            {
                _encryptedRSAKeyPairs = new EncryptedRSAKeyPair[_keyPairs];
                for (int i = 0; i < _keyPairs; i++)
                {
                    _encryptedRSAKeyPairs[i] = new EncryptedRSAKeyPair(new RSACryptoServiceProvider(1024).ExportParameters(true));
                }
                _log.LogInformation("Cached 10 RSA key pairs for clients.");
            }
            catch (System.Exception e)
            {
                _log.LogError(e, "Failed to generate RSA key pairs.");
                throw;
            }
        }

        public static EncryptedRSAKeyPair GetEncryptedRSAKeyPair()
        {
            return _encryptedRSAKeyPairs[new System.Random().Next(_keyPairs)];
        }
    }
}
