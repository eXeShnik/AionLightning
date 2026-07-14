using System.Collections.Generic;
// DaliaCharlandsAI2 — Java ai/instance/esoterrace/DaliaCharlandsAI2.java. Boss that spawns three
// helper NPCs at 75%/50%/25% HP thresholds.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("dalia_charlands")]
public sealed class DaliaCharlandsAI2 : AggressiveNpcAI2
{
    private readonly List<int> _percents = new();

    public override void OnSpawned()
    {
        AddPercent();
        base.OnSpawned();
    }

    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        CheckPercentage(Owner.HpPercentage);
    }

    private void CheckPercentage(int hpPercentage)
    {
        if (hpPercentage > 80 && _percents.Count < 3) AddPercent();
        foreach (int percent in _percents)
        {
            if (hpPercentage <= percent)
            {
                if (percent is 75 or 50 or 25) SpawnHelpers();
                _percents.Remove(percent);
                break;
            }
        }
    }

    private void AddPercent()
    {
        _percents.Clear();
        _percents.AddRange(new[] { 75, 50, 25 });
    }

    private void SpawnHelpers()
    {
        Spawn(282177, 1173.68f, 674.11f, 297.5f);
        Spawn(282176, 1174.44f, 669.64f, 297.5f);
        Spawn(282178, 1176.2f, 677.32f, 297.5f);
        // note: Java also assigned a walker id (npc_walker.xml route) to each spawned helper and
        // started it via WalkManager, set a visible-state flag, and broadcast an
        // SM_EMOTION(START_EMOTE2). Walker routes/WalkManager and PacketSendUtility aren't exposed to
        // the script layer yet.
    }

    public override void OnDespawned()
    {
        _percents.Clear();
        base.OnDespawned();
    }

    public override void OnDied()
    {
        _percents.Clear();
        base.OnDied();
    }

    public override void OnBackHome()
    {
        AddPercent();
        base.OnBackHome();
    }
}
