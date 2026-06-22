using RogueEngine.Engine.Commands;
using RogueEngine.Engine.Components;
using RogueEngine.Engine.Core;
using RogueEngine.Engine.Data;
using RogueEngine.Engine.Scripting;

namespace RogueEngine.Engine.Rules;

public sealed class QuestService
{
    private readonly IReadOnlyDictionary<string, QuestDefinition> _quests;
    private readonly ScriptAssembly? _scripts;

    public QuestService(LoadedProject project, ScriptAssembly? scripts)
    {
        ArgumentNullException.ThrowIfNull(project);
        _quests = project.Quests;
        _scripts = scripts;
    }

    public void StartQuest(Entity player, string questId)
    {
        if (!player.TryGetComponent<QuestLogComponent>(out var questLog) || questLog is null)
        {
            return;
        }

        if (questLog.CompletedQuestIds.Contains(questId) ||
            questLog.ActiveQuests.Any(entry => entry.QuestId == questId))
        {
            return;
        }

        if (!_quests.TryGetValue(questId, out var quest))
        {
            return;
        }

        questLog.ActiveQuests.Add(new QuestProgressEntry
        {
            QuestId = questId,
            ObjectiveProgress = quest.Objectives.Select(_ => 0).ToList()
        });
    }

    public void HandleEvent(World world, GameRulesContext rules, GameEvent gameEvent)
    {
        var player = world.GetPlayer();
        if (player is null ||
            !player.TryGetComponent<QuestLogComponent>(out var questLog) ||
            questLog is null)
        {
            return;
        }

        switch (gameEvent)
        {
            case QuestCompletedEvent:
                return;
            case EntityKilledEvent killed:
                HandleKill(world, player, questLog, killed);
                break;
            case ItemPickedUpEvent pickedUp:
                HandleCollect(world, player, questLog, pickedUp);
                break;
            case EntityMovedEvent moved when moved.Entity.HasComponent<IsPlayerComponent>():
                HandleReachCell(world, player, questLog, moved.To);
                break;
        }

        CheckCompletions(world, player, questLog, rules);
    }

    private void HandleKill(World world, Entity player, QuestLogComponent questLog, EntityKilledEvent killed)
    {
        if (killed.Victim is null ||
            !killed.Victim.TryGetComponent<ActorIdComponent>(out var actorId) ||
            actorId is null)
        {
            return;
        }

        foreach (var entry in questLog.ActiveQuests)
        {
            if (!_quests.TryGetValue(entry.QuestId, out var quest))
            {
                continue;
            }

            for (var i = 0; i < quest.Objectives.Count; i++)
            {
                var objective = quest.Objectives[i];
                if (!string.Equals(objective.Type, "kill", StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(objective.ActorId, actorId.ActorId, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                entry.ObjectiveProgress[i]++;
            }
        }
    }

    private void HandleCollect(World world, Entity player, QuestLogComponent questLog, ItemPickedUpEvent pickedUp)
    {
        foreach (var entry in questLog.ActiveQuests)
        {
            if (!_quests.TryGetValue(entry.QuestId, out var quest))
            {
                continue;
            }

            for (var i = 0; i < quest.Objectives.Count; i++)
            {
                var objective = quest.Objectives[i];
                if (!string.Equals(objective.Type, "collect", StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(objective.ItemId, pickedUp.ItemId, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                entry.ObjectiveProgress[i] += pickedUp.Count;
            }
        }
    }

    private void HandleReachCell(World world, Entity player, QuestLogComponent questLog, Position position)
    {
        foreach (var entry in questLog.ActiveQuests)
        {
            if (!_quests.TryGetValue(entry.QuestId, out var quest))
            {
                continue;
            }

            for (var i = 0; i < quest.Objectives.Count; i++)
            {
                var objective = quest.Objectives[i];
                if (!string.Equals(objective.Type, "reach_cell", StringComparison.OrdinalIgnoreCase) ||
                    objective.X != position.X ||
                    objective.Y != position.Y)
                {
                    continue;
                }

                entry.ObjectiveProgress[i] = Math.Max(entry.ObjectiveProgress[i], 1);
            }
        }
    }

    private void CheckCompletions(World world, Entity player, QuestLogComponent questLog, GameRulesContext rules)
    {
        foreach (var entry in questLog.ActiveQuests.ToList())
        {
            if (!_quests.TryGetValue(entry.QuestId, out var quest))
            {
                continue;
            }

            if (!IsQuestComplete(world, player, quest, entry))
            {
                continue;
            }

            questLog.ActiveQuests.Remove(entry);
            questLog.CompletedQuestIds.Add(entry.QuestId);
            GrantRewards(world, player, quest, rules);
            world.Log.Add($"Quest complete: {quest.Title}");
            world.Raise(new QuestCompletedEvent(quest.Id));
        }
    }

    private bool IsQuestComplete(World world, Entity player, QuestDefinition quest, QuestProgressEntry entry)
    {
        for (var i = 0; i < quest.Objectives.Count; i++)
        {
            var objective = quest.Objectives[i];
            var progress = i < entry.ObjectiveProgress.Count ? entry.ObjectiveProgress[i] : 0;

            if (string.Equals(objective.Type, "script", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(objective.Script))
                {
                    return false;
                }

                var checker = _scripts?.CreateQuestObjectiveChecker(objective.Script);
                if (checker is null)
                {
                    return false;
                }

                var context = new QuestObjectiveContext(world, player, quest, objective, progress);
                if (!checker.IsComplete(context))
                {
                    return false;
                }

                continue;
            }

            var required = Math.Max(1, objective.Count);
            if (progress < required)
            {
                return false;
            }
        }

        return quest.Objectives.Count > 0;
    }

    private static void GrantRewards(World world, Entity player, QuestDefinition quest, GameRulesContext rules)
    {
        if (!player.TryGetComponent<InventoryComponent>(out var inventory) || inventory is null)
        {
            return;
        }

        foreach (var reward in quest.Rewards)
        {
            PickupCommand.AddToInventory(inventory, reward.ItemId, reward.Count);
            if (rules.Project.Items.TryGetValue(reward.ItemId, out var item))
            {
                world.Log.Add($"Received {reward.Count}x {item.Name}.");
            }
        }
    }
}
