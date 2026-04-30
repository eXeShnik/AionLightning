using AionLightning.Commons.Network;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Item;

namespace AionLightning.Game.Network.Aion.ServerPackets;

public sealed class SM_CHARACTER_LIST : AionServerPacket
{
    private readonly int _playOk2;
    private readonly IReadOnlyList<(Player Player, PlayerAppearance Appearance, IReadOnlyList<Item> Equipment, bool HasUnread)> _characters;

    public SM_CHARACTER_LIST(int playOk2,
        IReadOnlyList<(Player Player, PlayerAppearance Appearance, IReadOnlyList<Item> Equipment, bool HasUnread)> characters)
        : base(0xC8)
    {
        _playOk2 = playOk2;
        _characters = characters;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteD(_playOk2);
        w.WriteC((byte)_characters.Count);

        foreach (var (p, a, equip, hasUnread) in _characters)
        {
            WritePlayerInfo(ref w, p, a, equip);

            w.WriteD(p.DisplaySettings); // display helmet
            w.WriteD(0);
            w.WriteD(0);
            w.WriteD(hasUnread ? 1 : 0); // unread mail indicator
            w.WriteD(0);
            w.WriteD(0);
            w.WriteQ(0);      // broker collected money
            w.WriteD(0);
            w.WriteD(0);
            w.WriteD(0);
            w.WriteD(0);
            w.WriteB(new byte[88]); // unk 4.5.0.18
            w.WriteD(0);      // ban info
            w.WriteD(0);
            w.WriteD(0);
            w.WriteH(0);
        }
    }

    internal static void WritePlayerInfo(ref PacketWriter w, Player p, PlayerAppearance a, IReadOnlyList<Item>? equipment = null)
    {
        int raceId   = (int)p.Race;
        int genderId = (int)p.Gender;

        w.WriteD(p.ObjectId);
        w.WriteS(p.Name, 52);
        w.WriteD(genderId);
        w.WriteD(raceId);
        w.WriteD((int)p.PlayerClass);
        w.WriteD(a.Voice);
        w.WriteD(a.SkinRgb);
        w.WriteD(a.HairRgb);
        w.WriteD(a.EyeRgb);
        w.WriteD(a.LipRgb);
        w.WriteC((byte)a.Face);
        w.WriteC((byte)a.Hair);
        w.WriteC((byte)a.Deco);
        w.WriteC((byte)a.Tattoo);
        w.WriteC((byte)a.FaceContour);
        w.WriteC((byte)a.Expression);
        w.WriteC(4);           // always 4
        w.WriteC((byte)a.JawLine);
        w.WriteC((byte)a.Forehead);
        w.WriteC((byte)a.EyeHeight);
        w.WriteC((byte)a.EyeSpace);
        w.WriteC((byte)a.EyeWidth);
        w.WriteC((byte)a.EyeSize);
        w.WriteC((byte)a.EyeShape);
        w.WriteC((byte)a.EyeAngle);
        w.WriteC((byte)a.BrowHeight);
        w.WriteC((byte)a.BrowAngle);
        w.WriteC((byte)a.BrowShape);
        w.WriteC((byte)a.Nose);
        w.WriteC((byte)a.NoseBridge);
        w.WriteC((byte)a.NoseWidth);
        w.WriteC((byte)a.NoseTip);
        w.WriteC((byte)a.Cheek);
        w.WriteC((byte)a.LipHeight);
        w.WriteC((byte)a.MouthSize);
        w.WriteC((byte)a.LipSize);
        w.WriteC((byte)a.Smile);
        w.WriteC((byte)a.LipShape);
        w.WriteC((byte)a.JawHeight);
        w.WriteC((byte)a.ChinJut);
        w.WriteC((byte)a.EarShape);
        w.WriteC((byte)a.HeadSize);
        w.WriteC((byte)a.Neck);
        w.WriteC((byte)a.NeckLength);
        w.WriteC((byte)a.ShoulderSize);
        w.WriteC((byte)a.Torso);
        w.WriteC((byte)a.Chest);
        w.WriteC((byte)a.Waist);
        w.WriteC((byte)a.Hips);
        w.WriteC((byte)a.ArmThickness);
        w.WriteC((byte)a.HandSize);
        w.WriteC((byte)a.LegThickness);
        w.WriteC((byte)a.FootSize);
        w.WriteC((byte)a.FacialRate);
        w.WriteC(0x00);
        w.WriteC((byte)a.ArmLength);
        w.WriteC((byte)a.LegLength);
        w.WriteC((byte)a.Shoulders);
        w.WriteC((byte)a.FaceShape);
        w.WriteC(0x00);
        w.WriteC(0x00);
        w.WriteC(0x00);
        w.WriteF(a.Height);

        int raceSex = 100000 + raceId * 2 + genderId;
        w.WriteD(raceSex);
        w.WriteD(p.Position.WorldId);
        w.WriteF(p.Position.X);
        w.WriteF(p.Position.Y);
        w.WriteF(p.Position.Z);
        w.WriteD(p.Position.Heading);
        w.WriteH(p.Level);
        w.WriteC(0);
        w.WriteC(64);
        w.WriteD(p.TitleId);

        // No legion
        w.WriteB(new byte[86]);
        w.WriteH(0x00); // not in legion

        w.WriteD(p.LastOnline.HasValue
            ? (int)new DateTimeOffset(p.LastOnline.Value).ToUnixTimeSeconds()
            : 0);

        // Equipment slot buffer: up to 208 bytes; 13 bytes per visible item (slot ≤ PANTS=4096)
        int written = 0;
        if (equipment is not null)
        {
            foreach (var item in equipment)
            {
                if (written + 13 > 208) break;
                byte indicator = (item.Slot == 2 || item.Slot == 64 || item.Slot == 256) ? (byte)2 : (byte)1;
                w.WriteC(indicator);
                w.WriteD(item.ItemId);
                w.WriteD(0); // no godstone
                w.WriteD(0); // no dye color
                written += 13;
            }
        }
        w.WriteB(new byte[208 - written]);
        // Deletion timer (Unix seconds); 0 = character is active
        w.WriteD(p.DeletionTime);
    }
}
