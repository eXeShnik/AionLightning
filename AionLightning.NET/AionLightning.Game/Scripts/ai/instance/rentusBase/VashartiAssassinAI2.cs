// VashartiAssassinAI2 — Java ai/instance/rentusBase/VashartiAssassinAI2.java. Rentus Base add: casts a
// stealth skill shortly after spawning and re-applies it on return-home.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("vasharti_assassin")]
public sealed class VashartiAssassinAI2 : AggressiveNpcAI2
{
    private bool _isHome = true;

    public override void OnCreatureAggro(Creature creature)
    {
        if (_isHome)
        {
            _isHome = false;
            var p = Owner.Position;
            Spawn(282465, p.X, p.Y, p.Z, (byte)p.Heading);
            // note: Java deleted this "smoke" marker npc immediately after spawning it (NpcActions.delete)
            // — scripted NPC delete isn't exposed to scripts yet.
        }
        base.OnCreatureAggro(creature);
    }

    public override void OnSpawned()
    {
        base.OnSpawned();
        ScheduleTask(() => UseSkill(19915), 2000);
    }

    public override void OnBackHome()
    {
        _isHome = true;
        base.OnBackHome();
        Owner.RemoveEffectBySkillId(19915);
        Owner.RemoveEffectBySkillId(19916);
        UseSkill(19915);
    }
}
