using RogueEngine.Engine.Core;

namespace RogueEngine.Engine.Rules;

public abstract record GameEvent;

public sealed record EntityKilledEvent(Entity Victim, Entity? Killer) : GameEvent;

public sealed record ItemPickedUpEvent(Entity Entity, string ItemId, int Count) : GameEvent;

public sealed record ItemUsedEvent(Entity Entity, string ItemId) : GameEvent;

public sealed record InteractionTriggeredEvent(Entity Entity, string InteractionId, Position Position) : GameEvent;

public sealed record QuestCompletedEvent(string QuestId) : GameEvent;

public sealed record EntityMovedEvent(Entity Entity, Position From, Position To) : GameEvent;
