using AionLightning.Commons.Network;
using AionLightning.Game.Model.House;
using AionLightning.Game.Model.Legion;
using AionLightning.Game.Model.Templates.Housing;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// Shared tail section written identically by SM_HOUSE_RENDER and SM_HOUSE_UPDATE (Java's two classes are
/// near-verbatim copies of each other from the building-id field onward — factored out here once instead
/// of duplicating it). Callers must call <see cref="House.FixBuildingStates"/> and
/// <see cref="House.NormalizePermissions"/> before invoking this (Java's getPermissions()/getDoorState()
/// normalize on every read; this port does it once, explicitly, at the call site — see House's doc
/// comments).
/// </summary>
internal static class HouseRenderPacketBody
{
    /// <summary>Java BuildingType.PERSONAL_FIELD/PERSONAL_INS's packet-level numeric id (2/1) — distinct
    /// from this port's plain BuildingType enum ordinal, which is not wire-compatible.</summary>
    public static int BuildingTypeId(BuildingType? type) => type == BuildingType.PERSONAL_INS ? 1 : 2;

    public static void Write(ref PacketWriter w, House house, Building building, Legion? legion)
    {
        w.WriteD(building.Id);
        w.WriteC(house.HouseOwnerInfoFlags);
        w.WriteC(HousePermissions.GetPacketValue(house.DoorState));

        // note: Java overlays the manager NPC's masterName into this 52-byte block when a butler is
        // spawned (shrinking the zero-padding by (name.length()+1)*2 bytes). This port doesn't spawn
        // house manager/teleport/sign NPCs yet (see HousingService.SpawnHouse), so the block is always
        // the full 52 zero bytes.
        w.WriteZero(52);

        w.WriteD(legion?.LegionId ?? 0);
        w.WriteC(HousePermissions.GetPacketValue(house.NoticeState));

        var notice = house.SignNotice;
        int written = Math.Min(notice.Length, House.NoticeLength);
        w.WriteB(notice.AsSpan(0, written));
        w.WriteZero(House.NoticeLength - written);

        bool isPersonal = building.Type == BuildingType.PERSONAL_INS;
        WritePart(ref w, house, PartType.ROOF, 0, isPersonal, skipPersonal: true);
        WritePart(ref w, house, PartType.OUTWALL, 0, isPersonal, skipPersonal: true);
        WritePart(ref w, house, PartType.FRAME, 0, isPersonal, skipPersonal: true);
        WritePart(ref w, house, PartType.DOOR, 0, isPersonal, skipPersonal: true);
        WritePart(ref w, house, PartType.GARDEN, 0, isPersonal, skipPersonal: true);
        WritePart(ref w, house, PartType.FENCE, 0, isPersonal, skipPersonal: true);

        for (int floor = 0; floor < 6; floor++)
            WritePart(ref w, house, PartType.INWALL_ANY, floor, isPersonal, skipPersonal: floor > 0);

        for (int floor = 0; floor < 6; floor++)
            WritePart(ref w, house, PartType.INFLOOR_ANY, floor, isPersonal, skipPersonal: floor > 0);

        WritePart(ref w, house, PartType.ADDON, 0, isPersonal, skipPersonal: true);
        w.WriteD(0);
        w.WriteD(0);
        w.WriteC(0);

        // Legion emblem + color (Java: null emblem == 6 zero bytes, matching the non-null branch's byte count).
        if (legion is null)
        {
            w.WriteC(0);
            w.WriteC(0);
            w.WriteD(0);
        }
        else
        {
            w.WriteC(legion.EmblemId);
            w.WriteC(legion.EmblemType);
            w.WriteC(legion.EmblemType == 0 ? (byte)0x00 : (byte)0xFF); // alpha: 0=default emblem, 0xFF=custom
            w.WriteC(legion.EmblemR);
            w.WriteC(legion.EmblemG);
            w.WriteC(legion.EmblemB);
        }
    }

    private static void WritePart(ref PacketWriter w, House house, PartType type, int floor, bool isPersonal, bool skipPersonal)
    {
        if (skipPersonal && isPersonal) { w.WriteD(0); return; }
        var deco = house.Registry.GetRenderPart(type, floor);
        w.WriteD(deco?.PartId ?? 0);
    }
}
