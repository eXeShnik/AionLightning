using AionLightning.Commons.Network;
using AionLightning.Game.Model;

namespace AionLightning.Game.Network.Aion.ServerPackets;

public sealed class SM_CHARACTER_LIST : AionServerPacket
{
    private readonly int _playOk2;
    private readonly IReadOnlyList<(Player Player, PlayerAppearance Appearance)> _characters;

    public SM_CHARACTER_LIST(int playOk2,
        IReadOnlyList<(Player Player, PlayerAppearance Appearance)> characters)
        : base(0xC8)
    {
        _playOk2 = playOk2;
        _characters = characters;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteD(_playOk2);
        w.WriteC((byte)_characters.Count);

        foreach (var (p, a) in _characters)
        {
            WritePlayerInfo(ref w, p, a);

            w.WriteD(0);      // display settings
            w.WriteD(0);
            w.WriteD(0);
            w.WriteD(0);      // unread mail
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

    private static void WritePlayerInfo(ref PacketWriter w, Player p, PlayerAppearance a)
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

        // No equipped items — write 0 items header
        w.WriteH(0); // 0 items
    }
}
