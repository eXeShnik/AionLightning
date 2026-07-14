using System.Linq;
using System.Collections.Generic;
using System;
// QueenMosquaAI2 — Java ai/instance/tallocsHollow/QueenMosquaAI2.java. Talloc's Hollow boss: closes
// an instance door on aggro, and on death replaces a marker NPC and releases players' bound summons.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("queenmosqua")]
public sealed class QueenMosquaAI2 : SummonerAI2
{
    private bool _isHome = true;

    public override void OnCreatureAggro(Creature creature)
    {
        base.OnCreatureAggro(creature);
        if (_isHome)
        {
            _isHome = false;
            // note: Java closed instance door 7 via WorldMapInstance.getDoors(); instance-door state
            // isn't exposed to scripts yet.
        }
    }

    public override void OnBackHome()
    {
        _isHome = true;
        // note: Java re-opened instance door 7 via WorldMapInstance.getDoors(); instance-door state
        // isn't exposed to scripts yet.
        base.OnBackHome();
    }

    public override void OnDied()
    {
        base.OnDied();
        // note: Java re-opened instance door 7 here too; instance-door state isn't exposed to scripts yet.
        var marker = GetNpc(700738);
        if (marker is not null)
        {
            Spawn(700739, marker.HomePosition.X, marker.HomePosition.Y, marker.HomePosition.Z, (byte)marker.HomePosition.Heading);
            // note: Java's spawn call also passed a trailing "11" argument (spawn-group/flag) that
            // Spawn's signature has no slot for, and it's dropped here. Java then broadcast
            // SM_SYSTEM_MESSAGE(1400476) to the marker's known-list, released
            // any bound summon (799500/799501) via SummonsService.doMode, and sent SM_PLAY_MOVIE(435) to
            // its owner, before deleting the marker via getController().onDelete(). Known-list iteration,
            // SummonsService, PacketSendUtility, and scripted NPC removal aren't exposed to scripts yet.
        }
    }
}
