using AionLightning.Commons.Events;
using AionLightning.Game.Model;

namespace AionLightning.Game.Events;

public sealed record PlayerMovedEvent(Player Player, Position OldPosition, Position NewPosition) : IGameEvent;
