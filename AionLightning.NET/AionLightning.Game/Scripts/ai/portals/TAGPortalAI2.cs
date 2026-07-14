// TAGPortalAI2 — Java ai/portals/TAGPortalAI2.java. Auto-group recruit portal for the three
// Tower-of-Eternity/instance worlds.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("t_a_g_portal")]
public sealed class TAGPortalAI2 : PortalDialogAI2
{
    // note: Java's onDialogSelect mapped dialogId (10000/10001/10002) to a worldId, resolved an
    // AutoGroupType.getAutoGroupByWorld and opened it via SM_AUTO_GROUP, falling back to the base
    // PortalDialogAI2 dialog handling for questId != 0. onDialogSelect has no NpcAi2 hook to override,
    // and AutoGroupType/SM_AUTO_GROUP aren't reachable from the script layer.
}
