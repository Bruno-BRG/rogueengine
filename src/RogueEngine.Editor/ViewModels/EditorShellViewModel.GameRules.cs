using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RogueEngine.Editor.Models;

namespace RogueEngine.Editor.ViewModels;

public partial class EditorShellViewModel
{
    private EditorInteraction? _selectedInteraction;
    private EditorClass? _selectedClass;
    private EditorQuest? _selectedQuest;
    private EditorQuestObjective? _selectedQuestObjective;
    private EditorQuestReward? _selectedQuestReward;
    private EditorClassStartItem? _selectedClassStartItem;

    public ObservableCollection<EditorInteraction> Interactions { get; } = [];
    public ObservableCollection<EditorClass> Classes { get; } = [];
    public ObservableCollection<EditorQuest> Quests { get; } = [];
    public ObservableCollection<EditorQuestObjective> QuestObjectives { get; } = [];
    public ObservableCollection<EditorQuestReward> QuestRewards { get; } = [];
    public ObservableCollection<EditorClassStartItem> ClassStartItems { get; } = [];

    [ObservableProperty] private string _defaultClassId = string.Empty;
    [ObservableProperty] private string _startQuestsText = string.Empty;
    [ObservableProperty] private string _interactionId = string.Empty;
    [ObservableProperty] private string _interactionKind = "use";
    [ObservableProperty] private string _interactionRequiredKeyId = string.Empty;
    [ObservableProperty] private string _interactionTargetScene = string.Empty;
    [ObservableProperty] private string _interactionMessage = string.Empty;
    [ObservableProperty] private string _interactionScript = string.Empty;
    [ObservableProperty] private string _classId = string.Empty;
    [ObservableProperty] private string _className = string.Empty;
    [ObservableProperty] private int _classBaseMaxHp;
    [ObservableProperty] private string _classPlayerActorId = string.Empty;
    [ObservableProperty] private int _classStatAttack;
    [ObservableProperty] private int _classStatDefense;
    [ObservableProperty] private int _classStatMaxHp;
    [ObservableProperty] private string _questId = string.Empty;
    [ObservableProperty] private string _questTitle = string.Empty;
    [ObservableProperty] private string _questOnCompleteScript = string.Empty;
    [ObservableProperty] private string _questObjectiveType = "kill";
    [ObservableProperty] private string _questObjectiveActorId = string.Empty;
    [ObservableProperty] private string _questObjectiveItemId = string.Empty;
    [ObservableProperty] private int _questObjectiveCount = 1;
    [ObservableProperty] private int _questObjectiveX;
    [ObservableProperty] private int _questObjectiveY;
    [ObservableProperty] private string _questObjectiveScript = string.Empty;
    [ObservableProperty] private string _questRewardItemId = string.Empty;
    [ObservableProperty] private int _questRewardCount = 1;
    [ObservableProperty] private string _classStartItemId = string.Empty;
    [ObservableProperty] private int _classStartItemCount = 1;
    [ObservableProperty] private int _itemStatAttack;
    [ObservableProperty] private int _itemStatDefense;
    [ObservableProperty] private string _itemKeyId = string.Empty;
    [ObservableProperty] private string _itemOnUseEffect = string.Empty;
    [ObservableProperty] private int _itemOnUseAmount;
    [ObservableProperty] private string _itemOnUseScript = string.Empty;

    public bool IsInteractionDocument => ActiveDocumentKind == EditorDocumentKind.Interaction;
    public bool IsClassDocument => ActiveDocumentKind == EditorDocumentKind.Class;
    public bool IsQuestDocument => ActiveDocumentKind == EditorDocumentKind.Quest;
    public int SceneInteractionCount => SelectedScene?.InteractionPlacements.Count ?? 0;

    public EditorInteraction? SelectedInteraction
    {
        get => _selectedInteraction;
        set
        {
            if (SetProperty(ref _selectedInteraction, value))
            {
                LoadSelectedInteractionIntoForm();
            }
        }
    }

    public EditorClass? SelectedClass
    {
        get => _selectedClass;
        set
        {
            if (SetProperty(ref _selectedClass, value))
            {
                LoadSelectedClassIntoForm();
            }
        }
    }

    public EditorQuest? SelectedQuest
    {
        get => _selectedQuest;
        set
        {
            if (SetProperty(ref _selectedQuest, value))
            {
                LoadSelectedQuestIntoForm();
            }
        }
    }

    public EditorQuestObjective? SelectedQuestObjective
    {
        get => _selectedQuestObjective;
        set
        {
            if (SetProperty(ref _selectedQuestObjective, value))
            {
                LoadSelectedQuestObjectiveIntoForm();
            }
        }
    }

    public EditorQuestReward? SelectedQuestReward
    {
        get => _selectedQuestReward;
        set
        {
            if (SetProperty(ref _selectedQuestReward, value))
            {
                LoadSelectedQuestRewardIntoForm();
            }
        }
    }

    public EditorClassStartItem? SelectedClassStartItem
    {
        get => _selectedClassStartItem;
        set
        {
            if (SetProperty(ref _selectedClassStartItem, value))
            {
                LoadSelectedClassStartItemIntoForm();
            }
        }
    }

    private void OpenGameRulesResource(EditorResourceNode node)
    {
        switch (node.Kind)
        {
            case EditorResourceKind.Interaction when node.Payload is not null:
                SelectedInteraction = Interactions.FirstOrDefault(interaction => interaction.Id == node.Payload);
                OpenDocument(EditorDocumentKind.Interaction, node.Payload, SelectedInteraction?.Id ?? node.Payload, "+");
                break;
            case EditorResourceKind.Class when node.Payload is not null:
                SelectedClass = Classes.FirstOrDefault(classDef => classDef.Id == node.Payload);
                OpenDocument(EditorDocumentKind.Class, node.Payload, SelectedClass?.Name ?? node.Payload, "[C]");
                break;
            case EditorResourceKind.Quest when node.Payload is not null:
                SelectedQuest = Quests.FirstOrDefault(quest => quest.Id == node.Payload);
                OpenDocument(EditorDocumentKind.Quest, node.Payload, SelectedQuest?.Title ?? node.Payload, "[Q]");
                break;
        }
    }

    private void HandleAddNewGameRules(string nodeId)
    {
        switch (nodeId)
        {
            case "add-interaction": AddInteraction(); break;
            case "add-class": AddClass(); break;
            case "add-quest": AddQuest(); break;
        }
    }

    private void SyncGameRulesSelectionFromDocument(EditorDocumentTab tab)
    {
        switch (tab.Kind)
        {
            case EditorDocumentKind.Interaction:
                SelectedInteraction = Interactions.FirstOrDefault(interaction => interaction.Id == tab.ResourceId);
                break;
            case EditorDocumentKind.Class:
                SelectedClass = Classes.FirstOrDefault(classDef => classDef.Id == tab.ResourceId);
                break;
            case EditorDocumentKind.Quest:
                SelectedQuest = Quests.FirstOrDefault(quest => quest.Id == tab.ResourceId);
                break;
        }
    }

    private void LoadGameRulesCollections(EditorProject project)
    {
        DefaultClassId = project.DefaultClassId ?? string.Empty;
        StartQuestsText = project.StartQuestsText;

        Interactions.Clear();
        foreach (var interaction in project.Interactions) Interactions.Add(interaction);
        SelectedInteraction = Interactions.FirstOrDefault();

        Classes.Clear();
        foreach (var classDef in project.Classes) Classes.Add(classDef);
        SelectedClass = Classes.FirstOrDefault();

        Quests.Clear();
        foreach (var quest in project.Quests) Quests.Add(quest);
        SelectedQuest = Quests.FirstOrDefault();
    }

    private void ApplyGameRulesFormsToProject()
    {
        ApplyInteractionFormToSelection();
        ApplyClassFormToSelection();
        ApplyQuestFormToSelection();

        if (_project is null)
        {
            return;
        }

        _project.DefaultClassId = string.IsNullOrWhiteSpace(DefaultClassId) ? null : DefaultClassId;
        _project.StartQuestsText = StartQuestsText;
        _project.Interactions = Interactions.ToList();
        _project.Classes = Classes.ToList();
        _project.Quests = Quests.ToList();
    }

    [RelayCommand]
    private void AddInteraction()
    {
        if (_project is null) return;
        var interaction = new EditorInteraction
        {
            Id = $"interaction{Interactions.Count + 1}",
            SourceFileName = $"interaction{Interactions.Count + 1}.json",
            Kind = "use",
            Message = "Nothing happens."
        };
        Interactions.Add(interaction);
        _project.Interactions = Interactions.ToList();
        MarkDirty();
        SelectedInteraction = interaction;
        OpenDocument(EditorDocumentKind.Interaction, interaction.Id, interaction.Id, "+");
        RebuildResourceTree();
    }

    [RelayCommand]
    private void RemoveInteraction()
    {
        if (_project is null || SelectedInteraction is null) return;
        var index = Interactions.IndexOf(SelectedInteraction);
        if (index < 0) return;
        var id = SelectedInteraction.Id;
        Interactions.RemoveAt(index);
        _project.Interactions = Interactions.ToList();
        MarkDirty();
        CloseDocumentsForResource(id);
        SelectedInteraction = Interactions.Count > 0 ? Interactions[Math.Min(index, Interactions.Count - 1)] : null;
        RebuildResourceTree();
    }

    [RelayCommand]
    private void AddClass()
    {
        if (_project is null) return;
        var classDef = new EditorClass
        {
            Id = $"class{Classes.Count + 1}",
            SourceFileName = $"class{Classes.Count + 1}.json",
            Name = $"Class {Classes.Count + 1}",
            BaseMaxHp = 20
        };
        Classes.Add(classDef);
        _project.Classes = Classes.ToList();
        MarkDirty();
        SelectedClass = classDef;
        OpenDocument(EditorDocumentKind.Class, classDef.Id, classDef.Name, "[C]");
        RebuildResourceTree();
    }

    [RelayCommand]
    private void RemoveClass()
    {
        if (_project is null || SelectedClass is null) return;
        var index = Classes.IndexOf(SelectedClass);
        if (index < 0) return;
        var id = SelectedClass.Id;
        Classes.RemoveAt(index);
        _project.Classes = Classes.ToList();
        MarkDirty();
        CloseDocumentsForResource(id);
        SelectedClass = Classes.Count > 0 ? Classes[Math.Min(index, Classes.Count - 1)] : null;
        RebuildResourceTree();
    }

    [RelayCommand]
    private void AddQuest()
    {
        if (_project is null) return;
        var quest = new EditorQuest
        {
            Id = $"quest{Quests.Count + 1}",
            SourceFileName = $"quest{Quests.Count + 1}.json",
            Title = $"Quest {Quests.Count + 1}"
        };
        Quests.Add(quest);
        _project.Quests = Quests.ToList();
        MarkDirty();
        SelectedQuest = quest;
        OpenDocument(EditorDocumentKind.Quest, quest.Id, quest.Title, "[Q]");
        RebuildResourceTree();
    }

    [RelayCommand]
    private void RemoveQuest()
    {
        if (_project is null || SelectedQuest is null) return;
        var index = Quests.IndexOf(SelectedQuest);
        if (index < 0) return;
        var id = SelectedQuest.Id;
        Quests.RemoveAt(index);
        _project.Quests = Quests.ToList();
        MarkDirty();
        CloseDocumentsForResource(id);
        SelectedQuest = Quests.Count > 0 ? Quests[Math.Min(index, Quests.Count - 1)] : null;
        RebuildResourceTree();
    }

    [RelayCommand]
    private void AddQuestObjective()
    {
        if (SelectedQuest is null) return;
        var objective = new EditorQuestObjective { Type = "kill", Count = 1 };
        SelectedQuest.Objectives.Add(objective);
        QuestObjectives.Add(objective);
        SelectedQuestObjective = objective;
        MarkDirty();
    }

    [RelayCommand]
    private void RemoveQuestObjective()
    {
        if (SelectedQuest is null || SelectedQuestObjective is null) return;
        SelectedQuest.Objectives.Remove(SelectedQuestObjective);
        QuestObjectives.Remove(SelectedQuestObjective);
        SelectedQuestObjective = QuestObjectives.FirstOrDefault();
        MarkDirty();
    }

    [RelayCommand]
    private void AddQuestReward()
    {
        if (SelectedQuest is null) return;
        var reward = new EditorQuestReward { Count = 1 };
        SelectedQuest.Rewards.Add(reward);
        QuestRewards.Add(reward);
        SelectedQuestReward = reward;
        MarkDirty();
    }

    [RelayCommand]
    private void RemoveQuestReward()
    {
        if (SelectedQuest is null || SelectedQuestReward is null) return;
        SelectedQuest.Rewards.Remove(SelectedQuestReward);
        QuestRewards.Remove(SelectedQuestReward);
        SelectedQuestReward = QuestRewards.FirstOrDefault();
        MarkDirty();
    }

    [RelayCommand]
    private void AddClassStartItem()
    {
        if (SelectedClass is null) return;
        var startItem = new EditorClassStartItem { Count = 1 };
        SelectedClass.StartItems.Add(startItem);
        ClassStartItems.Add(startItem);
        SelectedClassStartItem = startItem;
        MarkDirty();
    }

    [RelayCommand]
    private void RemoveClassStartItem()
    {
        if (SelectedClass is null || SelectedClassStartItem is null) return;
        SelectedClass.StartItems.Remove(SelectedClassStartItem);
        ClassStartItems.Remove(SelectedClassStartItem);
        SelectedClassStartItem = ClassStartItems.FirstOrDefault();
        MarkDirty();
    }

    partial void OnDefaultClassIdChanged(string value) => MarkDirty();
    partial void OnStartQuestsTextChanged(string value) => MarkDirty();
    partial void OnInteractionIdChanged(string value) => ApplyInteractionFormToSelection();
    partial void OnInteractionKindChanged(string value) => ApplyInteractionFormToSelection();
    partial void OnInteractionRequiredKeyIdChanged(string value) => ApplyInteractionFormToSelection();
    partial void OnInteractionTargetSceneChanged(string value) => ApplyInteractionFormToSelection();
    partial void OnInteractionMessageChanged(string value) => ApplyInteractionFormToSelection();
    partial void OnInteractionScriptChanged(string value) => ApplyInteractionFormToSelection();
    partial void OnClassIdChanged(string value) => ApplyClassFormToSelection();
    partial void OnClassNameChanged(string value) => ApplyClassFormToSelection();
    partial void OnClassBaseMaxHpChanged(int value) => ApplyClassFormToSelection();
    partial void OnClassPlayerActorIdChanged(string value) => ApplyClassFormToSelection();
    partial void OnClassStatAttackChanged(int value) => ApplyClassFormToSelection();
    partial void OnClassStatDefenseChanged(int value) => ApplyClassFormToSelection();
    partial void OnClassStatMaxHpChanged(int value) => ApplyClassFormToSelection();
    partial void OnQuestIdChanged(string value) => ApplyQuestFormToSelection();
    partial void OnQuestTitleChanged(string value) => ApplyQuestFormToSelection();
    partial void OnQuestOnCompleteScriptChanged(string value) => ApplyQuestFormToSelection();
    partial void OnQuestObjectiveTypeChanged(string value) => ApplyQuestObjectiveFormToSelection();
    partial void OnQuestObjectiveActorIdChanged(string value) => ApplyQuestObjectiveFormToSelection();
    partial void OnQuestObjectiveItemIdChanged(string value) => ApplyQuestObjectiveFormToSelection();
    partial void OnQuestObjectiveCountChanged(int value) => ApplyQuestObjectiveFormToSelection();
    partial void OnQuestObjectiveXChanged(int value) => ApplyQuestObjectiveFormToSelection();
    partial void OnQuestObjectiveYChanged(int value) => ApplyQuestObjectiveFormToSelection();
    partial void OnQuestObjectiveScriptChanged(string value) => ApplyQuestObjectiveFormToSelection();
    partial void OnQuestRewardItemIdChanged(string value) => ApplyQuestRewardFormToSelection();
    partial void OnQuestRewardCountChanged(int value) => ApplyQuestRewardFormToSelection();
    partial void OnClassStartItemIdChanged(string value) => ApplyClassStartItemFormToSelection();
    partial void OnClassStartItemCountChanged(int value) => ApplyClassStartItemFormToSelection();
    partial void OnItemStatAttackChanged(int value) => ApplyItemFormToSelection();
    partial void OnItemStatDefenseChanged(int value) => ApplyItemFormToSelection();
    partial void OnItemKeyIdChanged(string value) => ApplyItemFormToSelection();
    partial void OnItemOnUseEffectChanged(string value) => ApplyItemFormToSelection();
    partial void OnItemOnUseAmountChanged(int value) => ApplyItemFormToSelection();
    partial void OnItemOnUseScriptChanged(string value) => ApplyItemFormToSelection();

    private void LoadSelectedInteractionIntoForm()
    {
        if (SelectedInteraction is null)
        {
            InteractionId = string.Empty;
            InteractionKind = "use";
            InteractionRequiredKeyId = string.Empty;
            InteractionTargetScene = string.Empty;
            InteractionMessage = string.Empty;
            InteractionScript = string.Empty;
            return;
        }

        InteractionId = SelectedInteraction.Id;
        InteractionKind = SelectedInteraction.Kind;
        InteractionRequiredKeyId = SelectedInteraction.RequiredKeyId ?? string.Empty;
        InteractionTargetScene = SelectedInteraction.TargetScene ?? string.Empty;
        InteractionMessage = SelectedInteraction.Message ?? string.Empty;
        InteractionScript = SelectedInteraction.Script ?? string.Empty;
    }

    private void ApplyInteractionFormToSelection()
    {
        if (SelectedInteraction is null) return;
        SelectedInteraction.Id = InteractionId;
        SelectedInteraction.Kind = InteractionKind;
        SelectedInteraction.RequiredKeyId = string.IsNullOrWhiteSpace(InteractionRequiredKeyId) ? null : InteractionRequiredKeyId;
        SelectedInteraction.TargetScene = string.IsNullOrWhiteSpace(InteractionTargetScene) ? null : InteractionTargetScene;
        SelectedInteraction.Message = string.IsNullOrWhiteSpace(InteractionMessage) ? null : InteractionMessage;
        SelectedInteraction.Script = string.IsNullOrWhiteSpace(InteractionScript) ? null : InteractionScript;
        MarkDirty();
    }

    private void LoadSelectedClassIntoForm()
    {
        ClassStartItems.Clear();
        if (SelectedClass is null)
        {
            ClassId = string.Empty;
            ClassName = string.Empty;
            ClassBaseMaxHp = 0;
            ClassPlayerActorId = string.Empty;
            ClassStatAttack = ClassStatDefense = ClassStatMaxHp = 0;
            return;
        }

        ClassId = SelectedClass.Id;
        ClassName = SelectedClass.Name;
        ClassBaseMaxHp = SelectedClass.BaseMaxHp;
        ClassPlayerActorId = SelectedClass.PlayerActorId ?? string.Empty;
        ClassStatAttack = SelectedClass.StatAttack;
        ClassStatDefense = SelectedClass.StatDefense;
        ClassStatMaxHp = SelectedClass.StatMaxHp;
        foreach (var startItem in SelectedClass.StartItems) ClassStartItems.Add(startItem);
        SelectedClassStartItem = ClassStartItems.FirstOrDefault();
    }

    private void ApplyClassFormToSelection()
    {
        if (SelectedClass is null) return;
        SelectedClass.Id = ClassId;
        SelectedClass.Name = ClassName;
        SelectedClass.BaseMaxHp = ClassBaseMaxHp;
        SelectedClass.PlayerActorId = string.IsNullOrWhiteSpace(ClassPlayerActorId) ? null : ClassPlayerActorId;
        SelectedClass.StatAttack = ClassStatAttack;
        SelectedClass.StatDefense = ClassStatDefense;
        SelectedClass.StatMaxHp = ClassStatMaxHp;
        MarkDirty();
    }

    private void LoadSelectedQuestIntoForm()
    {
        QuestObjectives.Clear();
        QuestRewards.Clear();
        if (SelectedQuest is null)
        {
            QuestId = string.Empty;
            QuestTitle = string.Empty;
            QuestOnCompleteScript = string.Empty;
            return;
        }

        QuestId = SelectedQuest.Id;
        QuestTitle = SelectedQuest.Title;
        QuestOnCompleteScript = SelectedQuest.OnCompleteScript ?? string.Empty;
        foreach (var objective in SelectedQuest.Objectives) QuestObjectives.Add(objective);
        foreach (var reward in SelectedQuest.Rewards) QuestRewards.Add(reward);
        SelectedQuestObjective = QuestObjectives.FirstOrDefault();
        SelectedQuestReward = QuestRewards.FirstOrDefault();
    }

    private void ApplyQuestFormToSelection()
    {
        if (SelectedQuest is null) return;
        SelectedQuest.Id = QuestId;
        SelectedQuest.Title = QuestTitle;
        SelectedQuest.OnCompleteScript = string.IsNullOrWhiteSpace(QuestOnCompleteScript) ? null : QuestOnCompleteScript;
        MarkDirty();
    }

    private void LoadSelectedQuestObjectiveIntoForm()
    {
        if (SelectedQuestObjective is null)
        {
            QuestObjectiveType = "kill";
            QuestObjectiveActorId = string.Empty;
            QuestObjectiveItemId = string.Empty;
            QuestObjectiveCount = 1;
            QuestObjectiveX = QuestObjectiveY = 0;
            QuestObjectiveScript = string.Empty;
            return;
        }

        QuestObjectiveType = SelectedQuestObjective.Type;
        QuestObjectiveActorId = SelectedQuestObjective.ActorId ?? string.Empty;
        QuestObjectiveItemId = SelectedQuestObjective.ItemId ?? string.Empty;
        QuestObjectiveCount = SelectedQuestObjective.Count;
        QuestObjectiveX = SelectedQuestObjective.X ?? 0;
        QuestObjectiveY = SelectedQuestObjective.Y ?? 0;
        QuestObjectiveScript = SelectedQuestObjective.Script ?? string.Empty;
    }

    private void ApplyQuestObjectiveFormToSelection()
    {
        if (SelectedQuestObjective is null) return;
        SelectedQuestObjective.Type = QuestObjectiveType;
        SelectedQuestObjective.ActorId = string.IsNullOrWhiteSpace(QuestObjectiveActorId) ? null : QuestObjectiveActorId;
        SelectedQuestObjective.ItemId = string.IsNullOrWhiteSpace(QuestObjectiveItemId) ? null : QuestObjectiveItemId;
        SelectedQuestObjective.Count = QuestObjectiveCount;
        SelectedQuestObjective.X = QuestObjectiveX;
        SelectedQuestObjective.Y = QuestObjectiveY;
        SelectedQuestObjective.Script = string.IsNullOrWhiteSpace(QuestObjectiveScript) ? null : QuestObjectiveScript;
        MarkDirty();
    }

    private void LoadSelectedQuestRewardIntoForm()
    {
        if (SelectedQuestReward is null)
        {
            QuestRewardItemId = string.Empty;
            QuestRewardCount = 1;
            return;
        }

        QuestRewardItemId = SelectedQuestReward.ItemId;
        QuestRewardCount = SelectedQuestReward.Count;
    }

    private void ApplyQuestRewardFormToSelection()
    {
        if (SelectedQuestReward is null) return;
        SelectedQuestReward.ItemId = QuestRewardItemId;
        SelectedQuestReward.Count = QuestRewardCount;
        MarkDirty();
    }

    private void LoadSelectedClassStartItemIntoForm()
    {
        if (SelectedClassStartItem is null)
        {
            ClassStartItemId = string.Empty;
            ClassStartItemCount = 1;
            return;
        }

        ClassStartItemId = SelectedClassStartItem.ItemId;
        ClassStartItemCount = SelectedClassStartItem.Count;
    }

    private void ApplyClassStartItemFormToSelection()
    {
        if (SelectedClassStartItem is null) return;
        SelectedClassStartItem.ItemId = ClassStartItemId;
        SelectedClassStartItem.Count = ClassStartItemCount;
        MarkDirty();
    }

    private void LoadSelectedItemIntoFormGameRules()
    {
        if (SelectedItem is null)
        {
            ItemStatAttack = ItemStatDefense = 0;
            ItemKeyId = string.Empty;
            ItemOnUseEffect = string.Empty;
            ItemOnUseAmount = 0;
            ItemOnUseScript = string.Empty;
            return;
        }

        ItemStatAttack = SelectedItem.StatAttack;
        ItemStatDefense = SelectedItem.StatDefense;
        ItemKeyId = SelectedItem.KeyId ?? string.Empty;
        ItemOnUseEffect = SelectedItem.OnUseEffect ?? string.Empty;
        ItemOnUseAmount = SelectedItem.OnUseAmount;
        ItemOnUseScript = SelectedItem.OnUseScript ?? string.Empty;
    }

    private void ApplyItemFormToSelectionGameRules()
    {
        if (SelectedItem is null) return;
        SelectedItem.StatAttack = ItemStatAttack;
        SelectedItem.StatDefense = ItemStatDefense;
        SelectedItem.KeyId = string.IsNullOrWhiteSpace(ItemKeyId) ? null : ItemKeyId;
        SelectedItem.OnUseEffect = string.IsNullOrWhiteSpace(ItemOnUseEffect) ? null : ItemOnUseEffect;
        SelectedItem.OnUseAmount = ItemOnUseAmount;
        SelectedItem.OnUseScript = string.IsNullOrWhiteSpace(ItemOnUseScript) ? null : ItemOnUseScript;
    }
}
