using AionLightning.LoginServer.Model;
using AionLightning.LoginServer.Network.Gameserver;

namespace AionLightning.LoginServer.Network.Gameserver.Serverpackets
{
    public class SM_ACCOUNT_AUTH_RESPONSE : GsServerPacket
    {
        private readonly int _accountId;
        private readonly bool _ok;
        private readonly string _accountName;
        private readonly byte _accessLevel;
        private readonly byte _membership;
        private readonly long _toll;

        public SM_ACCOUNT_AUTH_RESPONSE(int accountId, bool ok)
        {
            _accountId = accountId;
            _ok = ok;

            if (ok)
            {
                var account = Controller.AccountController.GetAccount(accountId, null);
                _accountName = account.Name;
                _accessLevel = account.AccessLevel;
                _membership = account.Membership;
                _toll = account.Toll ?? 0;
            }
        }

        protected override void WriteImpl(GsConnection con)
        {
            WriteC(1);
            WriteD(_accountId);
            WriteC(_ok ? 1 : 0);
            if (_ok)
            {
                WriteS(_accountName);
                WriteC(_accessLevel);
                WriteC(_membership);
                WriteQ(_toll);
            }
        }
    }
}
