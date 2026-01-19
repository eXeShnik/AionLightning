namespace AionLightning.LoginServer.Network.Ncrypt
{
    public class CryptEngine
    {
        private readonly byte[] _key =
        {
            (byte) 0x6b, (byte) 0x60, (byte) 0xcb, (byte) 0x5b,
            (byte) 0x82, (byte) 0xce, (byte) 0x90, (byte) 0xb1,
            (byte) 0xcc, (byte) 0x2b, (byte) 0x6c, (byte) 0x55,
            (byte) 0x6c, (byte) 0x6c, (byte) 0x6c, (byte) 0x6c
        };

        private bool _updatedKey;
        private BlowfishCipher _cipher;

        public CryptEngine()
        {
            _cipher = new BlowfishCipher(_key);
        }

        public CryptEngine(byte[] key)
        {
            _cipher = new BlowfishCipher(key);
            _updatedKey = true;
        }

        public bool Decrypt(byte[] data, int offset, int length)
        {
            _cipher.Decrypt(data, offset, length);
            return Checksum(data, offset, length);
        }

        public void Encrypt(byte[] data, int offset, int length)
        {
            AppendChecksum(data, offset, length);
            _cipher.Encrypt(data, offset, length);
        }

        private bool Checksum(byte[] raw, int offset, int size)
        {
            if ((size & 3) != 0 || size <= 4)
            {
                return false;
            }

            long chksum = 0;
            int count = size - 4;
            long check = -1;
            int i;

            for (i = offset; i < count; i += 4)
            {
                check = raw[i] & 0xff;
                check |= (raw[i + 1] << 8) & 0xff00;
                check |= (raw[i + 2] << 16) & 0xff0000;
                check |= (raw[i + 3] << 24) & 0xff000000;
                chksum ^= check;
            }

            check = raw[i] & 0xff;
            check |= (raw[i + 1] << 8) & 0xff00;
            check |= (raw[i + 2] << 16) & 0xff0000;
            check |= (raw[i + 3] << 24) & 0xff000000;

            return check == chksum;
        }

        private void AppendChecksum(byte[] raw, int offset, int size)
        {
            long chksum = 0;
            int count = size - 4;
            int i;

            for (i = offset; i < count; i += 4)
            {
                chksum ^= (raw[i] & 0xFF) | ((raw[i + 1] << 8) & 0xFF00) | ((raw[i + 2] << 16) & 0xFF0000) | ((raw[i + 3] << 24) & 0xFF000000);
            }

            raw[i] = (byte)(chksum & 0xff);
            raw[i + 1] = (byte)((chksum >> 8) & 0xff);
            raw[i + 2] = (byte)((chksum >> 16) & 0xff);
            raw[i + 3] = (byte)((chksum >> 24) & 0xff);
        }
    }
}
