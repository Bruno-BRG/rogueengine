# Extending RogueEngine — JSON first, scripts when needed

RogueEngine v1.0 adds **data-driven game rules** with optional C# escape hatches. Built-in registries cover common RPG mechanics; anything else can be a script class referenced from JSON.

## Pattern

```
Data JSON  →  built-in registry  →  commands / systems
     ↓
"script": "MyClass"  →  Scripts/*.cs  →  same systems
```

Game events (`EntityKilled`, `ItemPickedUp`, `ItemUsed`, `InteractionTriggered`, `QuestCompleted`, `EntityMoved`) flow through `GameRulesContext` so quests and future hooks stay decoupled from individual commands.

## Built-in hooks

| JSON field | Interface | Method | Example |
|------------|-----------|--------|---------|
| Item `onUse.script` | `IItemEffect` | `Apply(IItemEffectContext)` | Custom potion behavior |
| Interaction `script` | `IInteractionHandler` | `TryInteract(IInteractionContext)` | Puzzle lever |
| Quest objective `type: "script"` | `IQuestObjectiveChecker` | `IsComplete(IQuestObjectiveContext)` | Weird objective |
| Actor `behavior` | `IBehavior` | turn hooks (existing) | Goblin AI |

Place script classes in `Scripts/` and reference the **class name** (not file name) in JSON.

## Items (`Data/items/*.json`)

```json
{
  "id": "rusty_sword",
  "kind": "equipment",
  "equipSlot": "weapon",
  "stats": { "attack": 2 }
}
```

Consumables:

```json
"onUse": { "effect": "heal", "amount": 5 }
```

Built-in effects: `heal`, `damage`, `buff` (log MVP).

Escape hatch:

```json
"onUse": { "script": "ChaosPotionEffect" }
```

Keys use `kind: "key"` and `keyId` (doors reference `requiredKeyId`, not item id).

Equip bonuses feed `StatResolver` — bump-attacks use equipped weapon + class stats.

## Interactions (`Data/interactions/*.json`)

Kinds: `door`, `stairs`, `use`, or `script`.

Place on scenes:

```json
"interactions": [{ "interactionId": "iron_door", "x": 5, "y": 3 }]
```

Runtime: **Space** on or adjacent to the interaction tile (`InteractCommand`).

## Classes (`Data/classes/*.json`)

Optional convenience — set `defaultClass` in `game.reproj`. Applies `baseMaxHp`, `statBonuses`, and `startItems` on player spawn. Add `ClassComponent` for save/load and stat resolution.

## Quests (`Data/quests/*.json`)

Objectives: `kill`, `collect`, `reach_cell`, `talk` (log MVP), or `script`.

List quest ids in `startQuests` on `game.reproj` to auto-start at new game.

`QuestService` listens to game events and grants `rewards` to player inventory on completion.

## Sample project

See [`templates/RpgDemoProject/`](../templates/RpgDemoProject/): warrior class, sword + potion, iron door + key, goblin hunt quest, stairs interaction.

Run:

```bash
dotnet run --project src/RogueEngine.Runtime -- templates/RpgDemoProject/game.reproj
```

Controls: move, **G** pickup, **U**+1–9 use, **E**+1–9 equip, **Space** interact.

## When to use scripts

| Use JSON when… | Use C# when… |
|----------------|--------------|
| Heal potion, keyed door, kill quest | Procedural puzzle state |
| Equipment stat bonuses | Multi-step scripted cutscene |
| Stairs message / scene hint | Custom win condition |

Keep scripts thin — call into `IItemEffectContext.World`, inventory, and log APIs rather than duplicating engine logic.
