namespace AionLightning.Game.Network.Aion;

public sealed class GsCrypt
{
    private static readonly byte[] StaticKey =
        "nKO/WctQ0AVLbpzfBkS6NevDYT8ourG5CRlmdjyJ72aswx4EPq1UgZhFMXH?3iI9"u8.ToArray();

    private const byte StaticServerPacketCode = 0x43;
    private const byte StaticClientPacketCode = 0x65;

    private byte[] _serverKey = null!;
    private byte[] _clientKey = null!;
    private bool _encryptEnabled;

    // Returns the "false key" to send via SM_KEY.
    // After this call encryption is primed (next Encrypt call will actually encrypt).
    public int EnableKey()
    {
        int baseKey = Random.Shared.Next();

        _serverKey = new byte[8];
        _serverKey[0] = (byte)(baseKey & 0xFF);
        _serverKey[1] = (byte)((baseKey >> 8)  & 0xFF);
        _serverKey[2] = (byte)((baseKey >> 16) & 0xFF);
        _serverKey[3] = (byte)((baseKey >> 24) & 0xFF);
        _serverKey[4] = 0xa1;
        _serverKey[5] = 0x6c;
        _serverKey[6] = 0x54;
        _serverKey[7] = 0x87;

        _clientKey = (byte[])_serverKey.Clone();

        // First call to Encrypt (for SM_KEY) will skip encryption but set the flag.
        _encryptEnabled = false;

        return unchecked((baseKey ^ (int)0xCD92E4DDu) + (int)0x3FF2CCCFu);
    }

    // Encrypts data in-place. First call skips encryption (SM_KEY is unencrypted).
    public void Encrypt(byte[] data, int offset, int size)
    {
        if (!_encryptEnabled)
        {
            _encryptEnabled = true;
            return;
        }

        data[offset] ^= _serverKey[0];
        int prev = data[offset++];

        for (int i = 1; i < size; i++, offset++)
        {
            data[offset] ^= (byte)(StaticKey[i & 63] ^ _serverKey[i & 7] ^ prev);
            prev = data[offset];
        }

        long oldKey = KeyAsLong(_serverKey);
        oldKey += size;
        LongToKey(oldKey, _serverKey);
    }

    // Decrypts data in-place. Returns false if checksum/validation fails.
    public bool Decrypt(byte[] data, int offset, int size)
    {
        if (!_encryptEnabled) return true;

        int prev = data[offset];
        data[offset++] ^= _clientKey[0];

        for (int i = 1; i < size; i++, offset++)
        {
            int curr = data[offset];
            data[offset] ^= (byte)(StaticKey[i & 63] ^ _clientKey[i & 7] ^ prev);
            prev = curr;
        }

        long oldKey = KeyAsLong(_clientKey);
        oldKey += size;

        if (!ValidateClientPacket(data, size))
            return false;

        LongToKey(oldKey, _clientKey);
        return true;
    }

    private static bool ValidateClientPacket(byte[] data, int size)
    {
        if (size < 5) return false;
        ushort opcode    = (ushort)(data[0] | (data[1] << 8));
        byte   code      = data[2];
        ushort notOpcode = (ushort)(data[3] | (data[4] << 8));
        return code == StaticClientPacketCode && (ushort)(~opcode) == notOpcode;
    }

    public static ushort EncodeOpcode(ushort opcode)
        => (ushort)((opcode + 0xCC) ^ 0xDD);

    private static long KeyAsLong(byte[] key)
    {
        long v = 0;
        for (int i = 0; i < 8; i++) v |= ((long)(key[i] & 0xFF)) << (i * 8);
        return v;
    }

    private static void LongToKey(long val, byte[] key)
    {
        for (int i = 0; i < 8; i++) key[i] = (byte)((val >> (i * 8)) & 0xFF);
    }
}
