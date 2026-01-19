using System.Collections.Generic;
using AionLightning.LoginServer.Controller;
using AionLightning.LoginServer.Model.Base;
using AionLightning.LoginServer.Network.Gameserver;

namespace AionLightning.LoginServer.Network.Gameserver.Serverpackets
{
    public class SM_MACBAN_LIST : GsServerPacket
    {
        private readonly Dictionary<string, BannedMacEntry> _bannedList;

        public SM_MACBAN_LIST()
        {
            _bannedList = BannedMacManager.GetInstance().GetMap();
        }

        protected override void WriteImpl(GsConnection con)
        {
            WriteC(9);
            WriteD(_bannedList.Count);

            foreach (var entry in _bannedList.Values)
            {
                WriteS(entry.Mac);
                WriteQ(new System.DateTimeOffset(entry.TimeEnd).ToUnixTimeMilliseconds());
                WriteS(entry.Details);
            }
        }
    }
}
