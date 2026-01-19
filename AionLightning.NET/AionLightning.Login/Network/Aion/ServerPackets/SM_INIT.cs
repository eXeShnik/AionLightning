using AionLightning.LoginServer.Network.Aion;
using AionLightning.LoginServer.Network.Ncrypt;

namespace AionLightning.LoginServer.Network.Aion.Serverpackets
{
    public class SM_INIT : AionServerPacket
    {
        private readonly int _sessionId;
        private readonly byte[] _publicRsaKey;
        private readonly byte[] _blowfishKey;

        public SM_INIT(LoginConnection client) : base(0x00)
        {
            _sessionId = client.SessionId;
            _publicRsaKey = client.GetEncryptedRSAKeyPair().PublicKey;
            _blowfishKey = new byte[16];
            new System.Random().NextBytes(_blowfishKey);
            client.SetCrypt(new CryptEngine(_blowfishKey));
        }

        protected override void WriteImpl(LoginConnection con)
        {
            WriteD(_sessionId);
            WriteD(0x0000c621);
            WriteB(_publicRsaKey);
            WriteD(0x01);
            WriteD(0x00);
            WriteD(0x00);
            WriteD(0x00);
            WriteB(_blowfishKey);
            WriteC(0x00);
        }
    }
}
