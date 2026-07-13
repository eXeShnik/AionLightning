using AionLightning.Game.Model;
using AionLightning.Game.World;

namespace AionLightning.Game.Instance;

/// <summary>
/// Per-map instance script contract (Java <c>instance/handlers/InstanceHandler</c>). One fresh
/// handler instance is created per live channel and bound to its <see cref="WorldMapInstance"/>.
/// <see cref="GeneralInstanceHandler"/> supplies no-op defaults; concrete scripts override the
/// hooks they need. The Java <c>onOpenDoor(int)</c> hook is intentionally omitted — it is declared
/// but never invoked anywhere in the Java server.
/// </summary>
public interface IInstanceHandler
{
    void OnInstanceCreate(WorldMapInstance instance);
    void OnInstanceDestroy();

    void OnPlayerLogin(Player player);
    void OnPlayerLogOut(Player player);
    void OnEnterInstance(Player player);
    void OnLeaveInstance(Player player);
    void OnExitInstance(Player player);

    void OnEnterZone(Player player, string zoneName);
    void OnLeaveZone(Player player, string zoneName);

    void OnPlayMovieEnd(Player player, int movieId);
    bool OnReviveEvent(Player player);
    bool OnDie(Player player, Creature? lastAttacker);
    void OnDie(Npc npc);
    void OnGather(Player player, Gatherable gatherable);
    bool OnPassFlyingRing(Player player, string flyingRing);
    void HandleUseItemFinish(Player player, Npc npc);
}
