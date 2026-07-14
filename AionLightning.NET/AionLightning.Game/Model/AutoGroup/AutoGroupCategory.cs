namespace AionLightning.Game.Model.AutoGroup;

/// <summary>
/// Collapses Java <c>AutoGroupType</c>'s per-constant <c>isDredgion()/isKamar()/isPvPFFAArena()/...</c>
/// switch methods into a single enum. <see cref="General"/> covers every plain co-op dungeon queue
/// (the vast majority of <c>auto_group.xml</c> entries) — this port does not model the per-category
/// team-composition nuances Java's <c>AutoDredgionInstance</c>/<c>AutoPvPFFAInstance</c>/
/// <c>AutoHarmonyInstance</c>/<c>AutoKamarBattlefieldInstance</c>/<c>AutoOphidanBridgeWarInstance</c>/
/// <c>AutoEternalBastionWarInstance</c> implement (race-balanced FFA/solo-duel/3v3 team slots) — every
/// queue is filled generically by <see cref="Services.AutoGroupService"/> regardless of category. The
/// category is retained only to drive the client's "entry available" minimap icon toggle
/// (SM_AUTO_GROUP windowId 6), matching Java's isDredgion()/isKamar()/isOphidan()/isIronWall() checks
/// for that one packet.
/// </summary>
public enum AutoGroupCategory
{
    General,
    Dredgion,
    Kamar,
    Ophidan,
    IronWall,
    PvpFfa,
    PvpSolo,
    Harmony,
    Glory,
}
