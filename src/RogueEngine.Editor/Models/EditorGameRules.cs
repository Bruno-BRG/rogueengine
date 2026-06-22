using RogueEngine.Engine.Data;

namespace RogueEngine.Editor.Models;

public sealed class EditorInteraction
{
    public string Id { get; set; } = string.Empty;
    public string SourceFileName { get; set; } = string.Empty;
    public string Kind { get; set; } = "use";
    public string? RequiredKeyId { get; set; }
    public string? TargetScene { get; set; }
    public string? Message { get; set; }
    public string? Script { get; set; }

    public static EditorInteraction FromEngine(InteractionDefinition interaction) => new()
    {
        Id = interaction.Id,
        SourceFileName = $"{interaction.Id}.json",
        Kind = interaction.Kind,
        RequiredKeyId = interaction.RequiredKeyId,
        TargetScene = interaction.TargetScene,
        Message = interaction.Message,
        Script = interaction.Script
    };

    public InteractionDefinition ToEngine() => new()
    {
        Id = Id,
        Kind = Kind,
        RequiredKeyId = string.IsNullOrWhiteSpace(RequiredKeyId) ? null : RequiredKeyId,
        TargetScene = string.IsNullOrWhiteSpace(TargetScene) ? null : TargetScene,
        Message = string.IsNullOrWhiteSpace(Message) ? null : Message,
        Script = string.IsNullOrWhiteSpace(Script) ? null : Script
    };
}

public sealed class EditorClass
{
    public string Id { get; set; } = string.Empty;
    public string SourceFileName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int BaseMaxHp { get; set; }
    public string? PlayerActorId { get; set; }
    public int StatAttack { get; set; }
    public int StatDefense { get; set; }
    public int StatMaxHp { get; set; }
    public List<EditorClassStartItem> StartItems { get; set; } = [];

    public static EditorClass FromEngine(ClassDefinition classDef) => new()
    {
        Id = classDef.Id,
        SourceFileName = $"{classDef.Id}.json",
        Name = classDef.Name,
        BaseMaxHp = classDef.BaseMaxHp,
        PlayerActorId = classDef.PlayerActorId,
        StatAttack = classDef.StatBonuses.Attack,
        StatDefense = classDef.StatBonuses.Defense,
        StatMaxHp = classDef.StatBonuses.MaxHp,
        StartItems = classDef.StartItems
            .Select(item => new EditorClassStartItem { ItemId = item.ItemId, Count = item.Count })
            .ToList()
    };

    public ClassDefinition ToEngine() => new()
    {
        Id = Id,
        Name = Name,
        BaseMaxHp = BaseMaxHp,
        PlayerActorId = string.IsNullOrWhiteSpace(PlayerActorId) ? null : PlayerActorId,
        StatBonuses = new ClassStatBonuses
        {
            Attack = StatAttack,
            Defense = StatDefense,
            MaxHp = StatMaxHp
        },
        StartItems = StartItems
            .Select(item => new ClassStartItem { ItemId = item.ItemId, Count = item.Count })
            .ToList()
    };
}

public sealed class EditorClassStartItem
{
    public string ItemId { get; set; } = string.Empty;
    public int Count { get; set; } = 1;
}

public sealed class EditorQuest
{
    public string Id { get; set; } = string.Empty;
    public string SourceFileName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public List<EditorQuestObjective> Objectives { get; set; } = [];
    public List<EditorQuestReward> Rewards { get; set; } = [];
    public string? OnCompleteScript { get; set; }

    public static EditorQuest FromEngine(QuestDefinition quest) => new()
    {
        Id = quest.Id,
        SourceFileName = $"{quest.Id}.json",
        Title = quest.Title,
        Objectives = quest.Objectives
            .Select(objective => new EditorQuestObjective
            {
                Type = objective.Type,
                ActorId = objective.ActorId,
                ItemId = objective.ItemId,
                Count = objective.Count,
                X = objective.X,
                Y = objective.Y,
                Message = objective.Message,
                Script = objective.Script
            })
            .ToList(),
        Rewards = quest.Rewards
            .Select(reward => new EditorQuestReward { ItemId = reward.ItemId, Count = reward.Count })
            .ToList(),
        OnCompleteScript = quest.OnComplete?.Script
    };

    public QuestDefinition ToEngine() => new()
    {
        Id = Id,
        Title = Title,
        Objectives = Objectives
            .Select(objective => new QuestObjectiveDefinition
            {
                Type = objective.Type,
                ActorId = string.IsNullOrWhiteSpace(objective.ActorId) ? null : objective.ActorId,
                ItemId = string.IsNullOrWhiteSpace(objective.ItemId) ? null : objective.ItemId,
                Count = objective.Count,
                X = objective.X,
                Y = objective.Y,
                Message = string.IsNullOrWhiteSpace(objective.Message) ? null : objective.Message,
                Script = string.IsNullOrWhiteSpace(objective.Script) ? null : objective.Script
            })
            .ToList(),
        Rewards = Rewards
            .Select(reward => new QuestRewardDefinition { ItemId = reward.ItemId, Count = reward.Count })
            .ToList(),
        OnComplete = string.IsNullOrWhiteSpace(OnCompleteScript)
            ? null
            : new QuestCompletionScript { Script = OnCompleteScript }
    };
}

public sealed class EditorQuestObjective
{
    public string Type { get; set; } = "kill";
    public string? ActorId { get; set; }
    public string? ItemId { get; set; }
    public int Count { get; set; } = 1;
    public int? X { get; set; }
    public int? Y { get; set; }
    public string? Message { get; set; }
    public string? Script { get; set; }
}

public sealed class EditorQuestReward
{
    public string ItemId { get; set; } = string.Empty;
    public int Count { get; set; } = 1;
}

public sealed class EditorSceneInteraction
{
    public string InteractionId { get; set; } = string.Empty;
    public int X { get; set; }
    public int Y { get; set; }
}
