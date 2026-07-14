using System.Collections.Generic;
// WardenSuramaAI2 — Java ai/instance/esoterrace/WardenSuramaAI2.java. Boss that spawns a ring of
// geysers at 50%/25%/5% HP thresholds.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("wardensurama")]
public sealed class WardenSuramaAI2 : AggressiveNpcAI2
{
    private readonly List<int> _percents = new();

    public override void OnSpawned()
    {
        base.OnSpawned();
        AddPercent();
    }

    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        CheckPercentage(Owner.HpPercentage);
    }

    private void CheckPercentage(int hpPercentage)
    {
        foreach (int percent in _percents)
        {
            if (hpPercentage <= percent)
            {
                if (percent is 50 or 25 or 5) SpawnGeysers();
                _percents.Remove(percent);
                break;
            }
        }
    }

    private void AddPercent()
    {
        _percents.Clear();
        _percents.AddRange(new[] { 50, 25, 5 });
    }

    private void SpawnGeysers()
    {
        UseSkill(19332, 50);
        Spawn(282425, 1305.310059f, 1159.337769f, 53.203529f);
        Spawn(282173, 1316.953979f, 1196.861328f, 53.203529f);
        Spawn(282428, 1305.083130f, 1182.424927f, 53.203529f);
        Spawn(282427, 1328.613770f, 1182.369873f, 53.203529f);
        Spawn(282172, 1343.426147f, 1170.675293f, 53.203529f);
        Spawn(282171, 1317.097656f, 1145.419556f, 53.203529f);
        Spawn(282426, 1328.446289f, 1159.062500f, 53.203529f);
        Spawn(282174, 1290.778442f, 1170.730957f, 53.203529f);
        // note: Java also broadcast SM_SYSTEM_MESSAGEs 1400997/1400998 to every online known-list
        // player (KnownList/PacketSendUtility not exposed here) and, after 13s, despawned each geyser
        // NPC by instance lookup (WorldMapInstance.getNpc + Npc.getController().onDelete) — not
        // exposed either.
    }

    public override void OnBackHome()
    {
        base.OnBackHome();
        AddPercent();
    }
}
