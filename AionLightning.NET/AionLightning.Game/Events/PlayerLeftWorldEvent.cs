using AionLightning.Commons.Events;
using AionLightning.Game.Model;

namespace AionLightning.Game.Events;

public sealed record PlayerLeftWorldEvent(Player Player) : IGameEvent;
