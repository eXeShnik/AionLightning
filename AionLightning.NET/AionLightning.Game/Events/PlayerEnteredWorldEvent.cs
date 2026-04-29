using AionLightning.Commons.Events;
using AionLightning.Game.Model;

namespace AionLightning.Game.Events;

public sealed record PlayerEnteredWorldEvent(Player Player) : IGameEvent;
