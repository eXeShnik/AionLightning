// SummonerAI2 — Java ai/SummonerAI2.java. Summons helper NPCs at configured HP-percentage
// thresholds and despawns them on death/despawn/back-home.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("summoner")]
public sealed class SummonerAI2 : AggressiveNpcAI2
{
    public override void OnSpawned()
    {
        base.OnSpawned();
        // note: Java loaded this npc's HP-percentage summon thresholds (AI_DATA Percentage/SummonGroup
        // templates); those templates aren't in IDataManager yet.
    }

    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        // note: Java checked the owner's HP percentage against the loaded thresholds to cast a skill
        // and/or spawn helper NPCs (individually or in scheduled groups) around itself via SpawnEngine.
    }

    public override void OnDespawned()
    {
        base.OnDespawned();
        // note: Java despawned every helper NPC it had spawned (tracked by objectId); helper-spawn
        // tracking isn't ported yet.
    }

    public override void OnBackHome()
    {
        base.OnBackHome();
        // note: same helper-despawn as OnDespawned, plus resetting the percentage-threshold cursor.
    }

    public override void OnDied()
    {
        base.OnDied();
        // note: same helper-despawn as OnDespawned.
    }
}
