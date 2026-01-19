using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Parameters;

namespace AionLightning.LoginServer.Network.Ncrypt
{
    public class BlowfishCipher
    {
        private readonly BlowfishEngine _cipher;

        public BlowfishCipher(byte[] key)
        {
            _cipher = new BlowfishEngine();
            var keyParam = new KeyParameter(key);
            _cipher.Init(true, keyParam);
        }

        public void Decrypt(byte[] data, int offset, int length)
        {
            for (int i = offset; i < offset + length; i += 8)
            {
                _cipher.ProcessBlock(data, i, data, i);
            }
        }

        public void Encrypt(byte[] data, int offset, int length)
        {
            for (int i = offset; i < offset + length; i += 8)
            {
                _cipher.ProcessBlock(data, i, data, i);
            }
        }
    }
}
