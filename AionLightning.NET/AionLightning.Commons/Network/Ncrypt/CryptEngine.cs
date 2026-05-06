namespace AionLightning.Commons.Network.Ncrypt;

public sealed class CryptEngine
{
    private static readonly byte[] DefaultKey =
    [
        0x6b, 0x60, 0xcb, 0x5b,
        0x82, 0xce, 0x90, 0xb1,
        0xcc, 0x2b, 0x6c, 0x55,
        0x6c, 0x6c, 0x6c, 0x6c
    ];

    private BlowfishCipher _cipher;
    private bool _firstPacket = true;

    public CryptEngine() : this(DefaultKey) { }

    public CryptEngine(byte[] key)
    {
        _cipher = new BlowfishCipher(key);
    }

    public void UpdateKey(byte[] key)
    {
        _cipher = new BlowfishCipher(key);
    }

    public bool Decrypt(byte[] data, int offset, int length)
    {
        _cipher.Decrypt(data, offset, length);
        return Checksum(data, offset, length);
    }

    // First server packet (SM_INIT) uses encXORPass; all subsequent use appendChecksum.
    // This matches Java CryptEngine.encrypt(!updatedKey / updatedKey paths).
    public void Encrypt(byte[] data, int offset, int length)
    {
        if (_firstPacket)
        {
            _firstPacket = false;
            EncXorPass(data, offset, length, Random.Shared.Next());
        }
        else
        {
            AppendChecksum(data, offset, length);
        }
        _cipher.Encrypt(data, offset, length);
    }

    private static void EncXorPass(byte[] data, int offset, int length, int key)
    {
        int stop = length - 8;
        int pos = 4 + offset;
        int ecx = key;

        while (pos < stop)
        {
            int edx = (data[pos] & 0xFF)
                      | (data[pos + 1] & 0xFF) << 8
                      | (data[pos + 2] & 0xFF) << 16
                      | (data[pos + 3] & 0xFF) << 24;
            ecx += edx;
            edx ^= ecx;
            data[pos++] = (byte)(edx & 0xFF);
            data[pos++] = (byte)(edx >> 8 & 0xFF);
            data[pos++] = (byte)(edx >> 16 & 0xFF);
            data[pos++] = (byte)(edx >> 24 & 0xFF);
        }

        data[pos++] = (byte)(ecx & 0xFF);
        data[pos++] = (byte)(ecx >> 8 & 0xFF);
        data[pos++] = (byte)(ecx >> 16 & 0xFF);
        data[pos]   = (byte)(ecx >> 24 & 0xFF);
    }

    private static bool Checksum(byte[] raw, int offset, int size)
    {
        if ((size & 3) != 0 || size <= 4)
            return false;

        long chksum = 0;
        int count = size - 4;
        long check = -1;
        int i;

        for (i = offset; i < count; i += 4)
        {
            check = raw[i] & 0xff;
            check |= (long)((raw[i + 1] << 8) & 0xff00);
            check |= (long)((raw[i + 2] << 16) & 0xff0000);
            check |= (long)(raw[i + 3] << 24) & 0xff000000L;
            chksum ^= check;
        }

        check = raw[i] & 0xff;
        check |= (long)((raw[i + 1] << 8) & 0xff00);
        check |= (long)((raw[i + 2] << 16) & 0xff0000);
        check |= (long)(raw[i + 3] << 24) & 0xff000000L;

        return chksum == 0;
    }

    private static void AppendChecksum(byte[] raw, int offset, int size)
    {
        long chksum = 0;
        int count = size - 4;
        int i;

        for (i = offset; i < count; i += 4)
        {
            chksum ^= (long)(raw[i] & 0xFF)
                      | (long)((raw[i + 1] << 8) & 0xFF00)
                      | (long)((raw[i + 2] << 16) & 0xFF0000)
                      | ((long)(raw[i + 3] << 24) & 0xFF000000L);
        }

        raw[i] = (byte)(chksum & 0xff);
        raw[i + 1] = (byte)((chksum >> 8) & 0xff);
        raw[i + 2] = (byte)((chksum >> 16) & 0xff);
        raw[i + 3] = (byte)((chksum >> 24) & 0xff);
    }
}
