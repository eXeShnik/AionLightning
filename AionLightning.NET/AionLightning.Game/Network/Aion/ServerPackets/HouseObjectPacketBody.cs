using AionLightning.Commons.Network;
using AionLightning.Game.Model.GameObjects;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// Shared tail sections written identically by SM_HOUSE_OBJECT, SM_HOUSE_EDIT and SM_HOUSE_REGISTRY (Java
/// repeats both blocks verbatim across all three classes) — factored out here once instead of duplicating.
/// </summary>
internal static class HouseObjectPacketBody
{
    /// <summary>Java's repeated "is dyed" 4-byte block: 1 + RGB when a dye color is set, else 4 zero bytes.</summary>
    public static void WriteColor(ref PacketWriter w, int? color)
    {
        if (color is > 0)
        {
            w.WriteC(1);
            w.WriteC((byte)((color.Value & 0xFF0000) >> 16));
            w.WriteC((byte)((color.Value & 0xFF00) >> 8));
            w.WriteC((byte)(color.Value & 0xFF));
        }
        else
        {
            w.WriteC(0);
            w.WriteC(0);
            w.WriteC(0);
            w.WriteC(0);
        }
    }

    /// <summary>Java UseableItemObject.writeUsageData — only meaningful for <see cref="HouseObject.TypeId"/> 1
    /// (Java's UseableItemObject). Reward/consume wiring (final_reward_id etc.) is not ported in this phase
    /// (see CM_USE_HOUSE_OBJECT's note); only the two wire fields the client actually needs are written.</summary>
    public static void WriteUseItemUsageData(ref PacketWriter w, HouseObject obj)
    {
        w.WriteD(obj.Template?.UseCount is not null ? obj.OwnerUsedCount + obj.VisitorUsedCount : 0);
        w.WriteC((byte)(obj.Template?.Action?.CheckType ?? 0));
    }
}
