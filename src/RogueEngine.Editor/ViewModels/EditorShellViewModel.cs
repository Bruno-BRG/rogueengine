using System.Collections.ObjectModel;
using System.Text.Json;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RogueEngine.Editor.Models;
using RogueEngine.Editor.Services;
using RogueEngine.Editor.Views;
using RogueEngine.Engine.Core;
using RogueEngine.Engine.VisualScripting;
using RogueEngine.Toolkit.ProcGen;

namespace RogueEngine.Editor.ViewModels;

public partial class EditorShellViewModel : ViewModelBase
{
    private readonly EditorNavigationService _navigation;
    private readonly TemplateService _templateService = new();
    private readonly ProjectService _projectService = new();
    private readonly PlaytestService _playtestService = new();
    private readonly BuildService _buildService = new();
    private readonly VisualScriptService _visualScriptService = new();
    private readonly ResourceTreeService _resourceTreeService = new();
    private readonly MapPreviewService _mapPreviewService = new();
    private readonly OutputLogViewModel _outputLog = new();
    private readonly EditorLogSink _buildLogSink;

    private EditorProject? _project;
    private EditorActor? _selectedActor;
    private EditorItem? _selectedItem;
    private EditorScene? _selectedScene;
    private EditorVisualGraph? _selectedVisualGraph;
    private EditorGraphNode? _selectedVisualNode;
    private EditorScriptFile? _selectedScript;

    public EditorShellViewModel(EditorNavigationService navigation, string reprojPath)
    {
        _navigation = navigation;
        _buildLogSink = new EditorLogSink(_outputLog);
        _outputLog.Changed += () => OnPropertyChanged(nameof(OutputText));
        _playtestService.RunningStateChanged += OnPlaytestRunningStateChanged;

        Actors = new ObservableCollection<EditorActor>();
        Items = new ObservableCollection<EditorItem>();
        Scenes = new ObservableCollection<EditorScene>();
        VisualGraphs = new ObservableCollection<EditorVisualGraph>();
        VisualNodes = new ObservableCollection<EditorGraphNode>();
        ResourceTree = new ObservableCollection<EditorResourceNode>();
        OpenDocuments = new ObservableCollection<EditorDocumentTab>();
        GeneratorAlgorithms = new ObservableCollection<string>(GeneratorRegistry.List());
        OverworldCells = new ObservableCollection<EditorOverworldCell>();
        OverworldConnections = new ObservableCollection<EditorOverworldConnection>();
        VisualNodeTypes =
        [
            NodeType.OnTurn,
            NodeType.IsPlayerAdjacent,
            NodeType.HasHpBelow,
            NodeType.MoveTowardPlayer,
            NodeType.AttackAtPlayer,
            NodeType.Log
        ];

        try
        {
            LoadProject(_projectService.Open(reprojPath));
            _outputLog.AddSuccess($"Opened project: {reprojPath}");
            OpenWelcomeDocument();
        }
        catch (Exception ex)
        {
            _outputLog.AddError(ex.Message);
            StatusText = "Failed to open project.";
        }
    }

    public ObservableCollection<EditorActor> Actors { get; }
    public ObservableCollection<EditorItem> Items { get; }
    public ObservableCollection<EditorScene> Scenes { get; }
    public ObservableCollection<EditorVisualGraph> VisualGraphs { get; }
    public ObservableCollection<EditorGraphNode> VisualNodes { get; }
    public ObservableCollection<EditorResourceNode> ResourceTree { get; }
    public ObservableCollection<EditorDocumentTab> OpenDocuments { get; }
    public ObservableCollection<string> GeneratorAlgorithms { get; }
    public ObservableCollection<EditorOverworldCell> OverworldCells { get; }
    public ObservableCollection<EditorOverworldConnection> OverworldConnections { get; }
    public IReadOnlyList<string> VisualNodeTypes { get; }
    public OutputLogViewModel OutputLog => _outputLog;
    public string OutputText => _outputLog.Text;

    [ObservableProperty] private string _windowTitle = "RogueEngine Editor";
    [ObservableProperty] private string _statusText = "Ready";
    [ObservableProperty] private string _projectName = string.Empty;
    [ObservableProperty] private int _mapWidth = 80;
    [ObservableProperty] private int _mapHeight = 22;
    [ObservableProperty] private int _messagePanelHeight = 3;
    [ObservableProperty] private int _minEnemies = 3;
    [ObservableProperty] private int _maxEnemies = 5;
    [ObservableProperty] private string _generatorId = "main_dungeon";
    [ObservableProperty] private string _generatorAlgorithm = "rooms_corridors";
    [ObservableProperty] private int _generatorWidth = 80;
    [ObservableProperty] private int _generatorHeight = 22;
    [ObservableProperty] private string _generatorSeedText = string.Empty;
    [ObservableProperty] private string _actorId = string.Empty;
    [ObservableProperty] private string _actorGlyph = "?";
    [ObservableProperty] private int _actorColorR;
    [ObservableProperty] private int _actorColorG;
    [ObservableProperty] private int _actorColorB;
    [ObservableProperty] private int _actorMaxHp = 1;
    [ObservableProperty] private bool _actorIsPlayer;
    [ObservableProperty] private bool _actorBlocksMovement;
    [ObservableProperty] private bool _actorHasChaseAI;
    [ObservableProperty] private string _actorBehavior = string.Empty;
    [ObservableProperty] private string _itemId = string.Empty;
    [ObservableProperty] private string _itemName = string.Empty;
    [ObservableProperty] private string _itemGlyph = "!";
    [ObservableProperty] private int _itemColorR = 200;
    [ObservableProperty] private int _itemColorG = 50;
    [ObservableProperty] private int _itemColorB = 50;
    [ObservableProperty] private string _itemKind = "consumable";
    [ObservableProperty] private int _itemMaxStack = 1;
    [ObservableProperty] private string _itemEquipSlot = string.Empty;
    [ObservableProperty] private int _itemHealOnUse;
    [ObservableProperty] private string _overworldId = string.Empty;
    [ObservableProperty] private int _genFillPercent = 45;
    [ObservableProperty] private int _genSmoothPasses = 5;
    [ObservableProperty] private int _genWallThreshold = 4;
    [ObservableProperty] private int _genTargetFloorCount = 1200;
    [ObservableProperty] private int _genWalkerCount = 4;
    [ObservableProperty] private int _genMinRoomSize = 4;
    [ObservableProperty] private int _genMaxRoomSize = 10;
    [ObservableProperty] private int _genSplitDepth = 4;
    [ObservableProperty] private string _genTilesetPath = string.Empty;
    [ObservableProperty] private string _sceneId = string.Empty;
    [ObservableProperty] private string _sceneName = string.Empty;
    [ObservableProperty] private string _sceneGenerator = string.Empty;
    [ObservableProperty] private int _sceneWidth = 80;
    [ObservableProperty] private int _sceneHeight = 22;
    [ObservableProperty] private int _sceneSeed;
    [ObservableProperty] private TileMap? _scenePreviewMap;
    [ObservableProperty] private bool _isPlaytestRunning;
    [ObservableProperty] private string _codeScriptContent = string.Empty;
    [ObservableProperty] private string _codeScriptFileName = string.Empty;
    [ObservableProperty] private string _visualGraphId = string.Empty;
    [ObservableProperty] private string _visualGraphEntryNodeId = string.Empty;
    [ObservableProperty] private string _visualNodeId = string.Empty;
    [ObservableProperty] private string _visualNodeType = NodeType.OnTurn;
    [ObservableProperty] private string _visualNodeNext = string.Empty;
    [ObservableProperty] private string _visualNodeTrueBranch = string.Empty;
    [ObservableProperty] private string _visualNodeFalseBranch = string.Empty;
    [ObservableProperty] private string _visualNodeMessage = string.Empty;
    [ObservableProperty] private int _visualNodeThreshold = 1;
    [ObservableProperty] private string _visualScriptPreview = string.Empty;

    partial void OnSelectedDocumentChanged(EditorDocumentTab? value)
    {
        OnPropertyChanged(nameof(ActiveDocumentKind));
        OnPropertyChanged(nameof(IsSettingsDocument));
        OnPropertyChanged(nameof(IsActorDocument));
        OnPropertyChanged(nameof(IsSceneDocument));
        OnPropertyChanged(nameof(IsGeneratorDocument));
        OnPropertyChanged(nameof(IsItemDocument));
        OnPropertyChanged(nameof(IsOverworldDocument));
        OnPropertyChanged(nameof(IsVisualScriptDocument));
        OnPropertyChanged(nameof(IsCodeScriptDocument));
        OnPropertyChanged(nameof(IsWelcomeDocument));
        OnPropertyChanged(nameof(IsInteractionDocument));
        OnPropertyChanged(nameof(IsClassDocument));
        OnPropertyChanged(nameof(IsQuestDocument));
        OnPropertyChanged(nameof(InspectorTitle));
        SyncSelectionFromDocument(value);
    }

    [ObservableProperty]
    private EditorDocumentTab? _selectedDocument;

    public EditorDocumentKind ActiveDocumentKind => SelectedDocument?.Kind ?? EditorDocumentKind.Welcome;
    public bool IsWelcomeDocument => ActiveDocumentKind == EditorDocumentKind.Welcome;
    public bool IsSettingsDocument => ActiveDocumentKind == EditorDocumentKind.ProjectSettings;
    public bool IsActorDocument => ActiveDocumentKind == EditorDocumentKind.Actor;
    public bool IsSceneDocument => ActiveDocumentKind == EditorDocumentKind.Scene;
    public bool IsGeneratorDocument => ActiveDocumentKind == EditorDocumentKind.Generator;
    public bool IsItemDocument => ActiveDocumentKind == EditorDocumentKind.Item;
    public bool IsOverworldDocument => ActiveDocumentKind == EditorDocumentKind.Overworld;
    public bool IsVisualScriptDocument => ActiveDocumentKind == EditorDocumentKind.VisualScript;
    public bool IsCodeScriptDocument => ActiveDocumentKind == EditorDocumentKind.CodeScript;

    public string InspectorTitle => SelectedDocument?.Title ?? "No document open";

    public int VisualNodeCount => VisualNodes.Count;

    public int SceneEntityCount => SelectedScene?.Entities.Count ?? 0;

    public int SceneItemCount => SelectedScene?.ItemPlacements.Count ?? 0;

    public bool CanPlaytest() => HasProject && !IsPlaytestRunning;

    public bool CanStopPlaytest() => IsPlaytestRunning;

    public EditorActor? SelectedActor
    {
        get => _selectedActor;
        set
        {
            if (SetProperty(ref _selectedActor, value))
            {
                LoadSelectedActorIntoForm();
            }
        }
    }

    public EditorItem? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (SetProperty(ref _selectedItem, value))
            {
                LoadSelectedItemIntoForm();
            }
        }
    }

    public EditorScene? SelectedScene
    {
        get => _selectedScene;
        set
        {
            if (SetProperty(ref _selectedScene, value))
            {
                LoadSelectedSceneIntoForm();
            }
        }
    }

    public EditorVisualGraph? SelectedVisualGraph
    {
        get => _selectedVisualGraph;
        set
        {
            if (SetProperty(ref _selectedVisualGraph, value))
            {
                LoadSelectedVisualGraphIntoForm();
            }
        }
    }

    public EditorGraphNode? SelectedVisualNode
    {
        get => _selectedVisualNode;
        set
        {
            if (SetProperty(ref _selectedVisualNode, value))
            {
                LoadSelectedVisualNodeIntoForm();
            }
        }
    }

    public bool HasProject => _project is not null;

    [RelayCommand]
    private void ReturnToLauncher()
    {
        _playtestService.Stop();
        _navigation.ShowLauncher();
    }

    [RelayCommand]
    private void OpenResource(EditorResourceNode? node)
    {
        if (node is null || node.IsPlaceholder || !node.IsSelectable)
        {
            return;
        }

        switch (node.Kind)
        {
            case EditorResourceKind.ProjectSettings:
                OpenDocument(EditorDocumentKind.ProjectSettings, "settings", "Project Settings", "[.]");
                break;
            case EditorResourceKind.Scene when node.Payload is not null:
                SelectedScene = Scenes.FirstOrDefault(scene => scene.Id == node.Payload);
                OpenDocument(EditorDocumentKind.Scene, node.Payload, SelectedScene?.Name ?? node.Payload, "[S]");
                break;
            case EditorResourceKind.Actor when node.Payload is not null:
                SelectedActor = Actors.FirstOrDefault(actor => actor.Id == node.Payload);
                OpenDocument(EditorDocumentKind.Actor, node.Payload, node.Payload, node.Icon);
                break;
            case EditorResourceKind.Item when node.Payload is not null:
                SelectedItem = Items.FirstOrDefault(item => item.Id == node.Payload);
                OpenDocument(EditorDocumentKind.Item, node.Payload, SelectedItem?.Name ?? node.Payload, node.Icon);
                break;
            case EditorResourceKind.Overworld:
                OpenDocument(EditorDocumentKind.Overworld, "overworld", _project?.Overworld?.Id ?? "Overworld", "[W]");
                LoadOverworldIntoForm();
                break;
            case EditorResourceKind.Generator:
                OpenDocument(EditorDocumentKind.Generator, "generator", GeneratorId, "[G]");
                break;
            case EditorResourceKind.VisualScript when node.Payload is not null:
                SelectedVisualGraph = VisualGraphs.FirstOrDefault(graph => graph.Id == node.Payload);
                OpenDocument(EditorDocumentKind.VisualScript, node.Payload, node.Payload, "[V]");
                break;
            case EditorResourceKind.CodeScript when node.Payload is not null:
                _selectedScript = _project?.ScriptFiles.FirstOrDefault(script => script.FullPath == node.Payload);
                CodeScriptFileName = _selectedScript?.FileName ?? string.Empty;
                CodeScriptContent = _selectedScript?.Content ?? string.Empty;
                OpenDocument(EditorDocumentKind.CodeScript, node.Payload, CodeScriptFileName, "C#");
                break;
            case EditorResourceKind.Interaction:
            case EditorResourceKind.Class:
            case EditorResourceKind.Quest:
                OpenGameRulesResource(node);
                break;
            case EditorResourceKind.AddNew:
                HandleAddNew(node.Id);
                break;
        }
    }

    [RelayCommand]
    private void SelectDocument(EditorDocumentTab? tab)
    {
        if (tab is null)
        {
            return;
        }

        SelectedDocument = tab;
        SyncSelectionFromDocument(tab);
    }

    [RelayCommand]
    private void CloseDocument(EditorDocumentTab? tab)
    {
        if (tab is null)
        {
            return;
        }

        var index = OpenDocuments.IndexOf(tab);
        if (index < 0)
        {
            return;
        }

        OpenDocuments.RemoveAt(index);
        if (OpenDocuments.Count == 0)
        {
            OpenWelcomeDocument();
            return;
        }

        SelectedDocument = OpenDocuments[Math.Min(index, OpenDocuments.Count - 1)];
        SyncSelectionFromDocument(SelectedDocument);
    }

    [RelayCommand(CanExecute = nameof(HasProject))]
    private void SaveProject()
    {
        if (_project is null) return;
        ApplyFormToProject();
        var errors = _projectService.Save(_project);
        if (errors.Count > 0)
        {
            foreach (var error in errors) _outputLog.AddError(error);
            StatusText = "Save failed.";
            return;
        }

        foreach (var tab in OpenDocuments) tab.IsModified = false;
        _outputLog.AddSuccess("Project saved.");
        StatusText = $"Saved {_project.Name}";
        OnPropertyChanged(nameof(WindowTitle));
    }

    [RelayCommand(CanExecute = nameof(HasProject))]
    private void ValidateProject()
    {
        if (_project is null) return;
        ApplyFormToProject();
        var saveErrors = _projectService.Save(_project);
        if (saveErrors.Count > 0)
        {
            foreach (var error in saveErrors) _outputLog.AddError(error);
            StatusText = "Validation failed before save.";
            return;
        }

        var result = _projectService.Validate(_project.ReprojPath);
        _outputLog.Clear();
        if (result.Success)
        {
            _outputLog.AddSuccess("Validation passed.");
            StatusText = "Validation passed.";
        }
        else
        {
            foreach (var error in result.Errors) _outputLog.AddError(error);
            StatusText = "Validation failed.";
        }
    }

    [RelayCommand(CanExecute = nameof(CanPlaytest))]
    private void Playtest()
    {
        if (_project is null) return;
        ApplyFormToProject();
        var saveErrors = _projectService.Save(_project);
        if (saveErrors.Count > 0)
        {
            foreach (var error in saveErrors) _outputLog.AddError(error);
            StatusText = "Cannot playtest until save errors are fixed.";
            return;
        }

        try
        {
            _playtestService.Start(_project.ReprojPath);
            _outputLog.AddInfo("Playtest started.");
            StatusText = "Running…";
        }
        catch (Exception ex)
        {
            _outputLog.AddError(ex.Message);
            StatusText = "Playtest failed to start.";
        }
    }

    [RelayCommand(CanExecute = nameof(CanStopPlaytest))]
    private void StopPlaytest()
    {
        _playtestService.Stop();
        _outputLog.AddInfo("Playtest stopped.");
        StatusText = "Ready";
    }

    public void OnSceneViewportChanged()
    {
        MarkDirty();
        OnPropertyChanged(nameof(SceneEntityCount));
        OnPropertyChanged(nameof(SceneItemCount));
        OnPropertyChanged(nameof(SceneInteractionCount));
    }

    public void RefreshSceneMapPreview() => RefreshScenePreview();

    [RelayCommand]
    private void RegenerateScenePreview()
    {
        if (SelectedScene is null || _project is null)
        {
            return;
        }

        var result = _mapPreviewService.Generate(SelectedScene, _project.Generator, Random.Shared.Next());
        SelectedScene.Seed = result.Seed;
        SceneSeed = result.Seed;
        ScenePreviewMap = result.Map;
        MarkDirty();
    }

    private void OnPlaytestRunningStateChanged()
    {
        IsPlaytestRunning = _playtestService.IsRunning;
        PlaytestCommand.NotifyCanExecuteChanged();
        StopPlaytestCommand.NotifyCanExecuteChanged();
        if (!IsPlaytestRunning && StatusText == "Running…")
        {
            StatusText = "Ready";
        }
    }

    [RelayCommand(CanExecute = nameof(HasProject))]
    private void BuildProject()
    {
        if (_project is null) return;
        ApplyFormToProject();
        var saveErrors = _projectService.Save(_project);
        if (saveErrors.Count > 0)
        {
            foreach (var error in saveErrors) _outputLog.AddError(error);
            StatusText = "Cannot build until save errors are fixed.";
            return;
        }

        _outputLog.Clear();
        var result = _buildService.Build(_project.ReprojPath, _buildLogSink);
        if (result.Success)
        {
            _outputLog.AddSuccess($"Build output: {result.OutputDirectory}");
            if (!string.IsNullOrWhiteSpace(result.ZipPath)) _outputLog.AddSuccess($"ZIP: {result.ZipPath}");
            StatusText = "Build succeeded.";
        }
        else StatusText = "Build failed.";
    }

    [RelayCommand(CanExecute = nameof(HasProject))]
    private void AddItem()
    {
        if (_project is null) return;
        var item = new EditorItem
        {
            Id = $"item{Items.Count + 1}",
            SourceFileName = $"item{Items.Count + 1}.json",
            Name = $"Item {Items.Count + 1}",
            Glyph = '!',
            ColorR = 200,
            ColorG = 50,
            ColorB = 50,
            Kind = "consumable",
            MaxStack = 5,
            HealOnUse = 5
        };
        Items.Add(item);
        _project.Items = Items.ToList();
        RebuildResourceTree();
        SelectedItem = item;
        OpenDocument(EditorDocumentKind.Item, item.Id, item.Name, item.Glyph.ToString());
        MarkDirty();
    }

    [RelayCommand(CanExecute = nameof(HasProject))]
    private void RemoveItem()
    {
        if (_project is null || SelectedItem is null) return;
        var index = Items.IndexOf(SelectedItem);
        if (index < 0) return;
        var itemId = SelectedItem.Id;
        Items.RemoveAt(index);
        _project.Items = Items.ToList();
        CloseDocumentsForResource(itemId);
        RebuildResourceTree();
        SelectedItem = Items.Count > 0 ? Items[Math.Min(index, Items.Count - 1)] : null;
        MarkDirty();
    }

    [RelayCommand(CanExecute = nameof(HasProject))]
    private void AddOverworldCell()
    {
        if (_project?.Overworld is null) return;
        var cell = new EditorOverworldCell
        {
            Id = $"cell{_project.Overworld.Cells.Count + 1}",
            X = _project.Overworld.Cells.Count,
            Y = 0,
            Biome = "cave",
            LocalGenerator = _project.DefaultGeneratorPath
        };
        _project.Overworld.Cells.Add(cell);
        OverworldCells.Add(cell);
        MarkDirty();
    }

    [RelayCommand(CanExecute = nameof(HasProject))]
    private void AddOverworldConnection()
    {
        if (_project?.Overworld is null || _project.Overworld.Cells.Count < 2) return;
        var connection = new EditorOverworldConnection
        {
            From = _project.Overworld.Cells[0].Id,
            To = _project.Overworld.Cells[1].Id,
            Type = "road"
        };
        _project.Overworld.Connections.Add(connection);
        OverworldConnections.Add(connection);
        MarkDirty();
    }

    [RelayCommand(CanExecute = nameof(HasProject))]
    private void AddActor()
    {
        if (_project is null) return;
        var actor = new EditorActor
        {
            Id = $"actor{Actors.Count + 1}",
            SourceFileName = $"actor{Actors.Count + 1}.json",
            Glyph = '?',
            ColorR = 200, ColorG = 200, ColorB = 200,
            MaxHp = 5
        };
        Actors.Add(actor);
        _project.Actors = Actors.ToList();
        RebuildResourceTree();
        SelectedActor = actor;
        OpenDocument(EditorDocumentKind.Actor, actor.Id, actor.Id, actor.Glyph.ToString());
        MarkDirty();
    }

    [RelayCommand(CanExecute = nameof(HasProject))]
    private void RemoveActor()
    {
        if (_project is null || SelectedActor is null) return;
        var index = Actors.IndexOf(SelectedActor);
        if (index < 0) return;
        var actorId = SelectedActor.Id;
        Actors.RemoveAt(index);
        _project.Actors = Actors.ToList();
        CloseDocumentsForResource(actorId);
        RebuildResourceTree();
        SelectedActor = Actors.Count > 0 ? Actors[Math.Min(index, Actors.Count - 1)] : null;
        MarkDirty();
    }

    [RelayCommand(CanExecute = nameof(HasProject))]
    private void AddScene()
    {
        if (_project is null) return;
        var scene = new EditorScene
        {
            Id = $"scene{Scenes.Count + 1}",
            SourceFileName = $"scene{Scenes.Count + 1}.scene.json",
            Name = $"Scene {Scenes.Count + 1}",
            Generator = _project.DefaultGeneratorPath,
            Width = _project.Settings.MapWidth,
            Height = _project.Settings.MapHeight
        };
        Scenes.Add(scene);
        _project.Scenes = Scenes.ToList();
        RebuildResourceTree();
        SelectedScene = scene;
        OpenDocument(EditorDocumentKind.Scene, scene.Id, scene.Name, "[S]");
        MarkDirty();
    }

    [RelayCommand(CanExecute = nameof(HasProject))]
    private void AddVisualGraph()
    {
        if (_project is null) return;
        var graph = new EditorVisualGraph
        {
            Id = $"graph{VisualGraphs.Count + 1}",
            SourceFileName = $"graph{VisualGraphs.Count + 1}.graph.json",
            EntryNodeId = "n1",
            Nodes =
            [
                new EditorGraphNode { Id = "n1", Type = NodeType.OnTurn, Next = "n2" },
                new EditorGraphNode { Id = "n2", Type = NodeType.Log, Next = null, Message = "Hello from visual script." }
            ]
        };
        VisualGraphs.Add(graph);
        _project.VisualGraphs = VisualGraphs.ToList();
        RebuildResourceTree();
        SelectedVisualGraph = graph;
        OpenDocument(EditorDocumentKind.VisualScript, graph.Id, graph.Id, "[V]");
        MarkDirty();
    }

    [RelayCommand(CanExecute = nameof(HasProject))]
    private void RemoveVisualGraph()
    {
        if (_project is null || SelectedVisualGraph is null) return;
        var index = VisualGraphs.IndexOf(SelectedVisualGraph);
        if (index < 0) return;
        var graphId = SelectedVisualGraph.Id;
        VisualGraphs.RemoveAt(index);
        _project.VisualGraphs = VisualGraphs.ToList();
        CloseDocumentsForResource(graphId);
        RebuildResourceTree();
        SelectedVisualGraph = VisualGraphs.Count > 0 ? VisualGraphs[Math.Min(index, VisualGraphs.Count - 1)] : null;
        MarkDirty();
    }

    [RelayCommand(CanExecute = nameof(HasProject))]
    private void AddVisualNode()
    {
        if (SelectedVisualGraph is null) return;
        var node = new EditorGraphNode { Id = $"n{SelectedVisualGraph.Nodes.Count + 1}", Type = NodeType.Log, Message = "Log message" };
        SelectedVisualGraph.Nodes.Add(node);
        VisualNodes.Add(node);
        SelectedVisualNode = node;
        NotifyVisualNodeCount();
        MarkDirty();
    }

    [RelayCommand(CanExecute = nameof(HasProject))]
    private void RemoveVisualNode()
    {
        if (SelectedVisualGraph is null || SelectedVisualNode is null) return;
        var index = VisualNodes.IndexOf(SelectedVisualNode);
        if (index < 0) return;
        SelectedVisualGraph.Nodes.Remove(SelectedVisualNode);
        VisualNodes.RemoveAt(index);
        SelectedVisualNode = VisualNodes.Count > 0 ? VisualNodes[Math.Min(index, VisualNodes.Count - 1)] : null;
        NotifyVisualNodeCount();
        MarkDirty();
    }

    [RelayCommand(CanExecute = nameof(HasProject))]
    private void PreviewVisualScript()
    {
        if (SelectedVisualGraph is null) return;
        ApplyVisualNodeFormToSelection();
        var result = _visualScriptService.GeneratePreview(SelectedVisualGraph);
        if (result.Success)
        {
            VisualScriptPreview = result.Source;
            _outputLog.AddSuccess($"Generated preview for '{SelectedVisualGraph.Id}'.");
        }
        else
        {
            VisualScriptPreview = string.Empty;
            foreach (var error in result.Errors) _outputLog.AddError(error);
        }
    }

    partial void OnProjectNameChanged(string value) => MarkDirty();
    partial void OnMapWidthChanged(int value) => MarkDirty();
    partial void OnMapHeightChanged(int value) => MarkDirty();
    partial void OnMessagePanelHeightChanged(int value) => MarkDirty();
    partial void OnMinEnemiesChanged(int value) => MarkDirty();
    partial void OnMaxEnemiesChanged(int value) => MarkDirty();
    partial void OnGeneratorIdChanged(string value) => MarkDirty();
    partial void OnGeneratorAlgorithmChanged(string value) => MarkDirty();
    partial void OnGeneratorWidthChanged(int value) => MarkDirty();
    partial void OnGeneratorHeightChanged(int value) => MarkDirty();
    partial void OnGeneratorSeedTextChanged(string value) => MarkDirty();
    partial void OnActorIdChanged(string value) => ApplyActorFormToSelection();
    partial void OnActorGlyphChanged(string value) => ApplyActorFormToSelection();
    partial void OnActorColorRChanged(int value) => ApplyActorFormToSelection();
    partial void OnActorColorGChanged(int value) => ApplyActorFormToSelection();
    partial void OnActorColorBChanged(int value) => ApplyActorFormToSelection();
    partial void OnActorMaxHpChanged(int value) => ApplyActorFormToSelection();
    partial void OnActorIsPlayerChanged(bool value) => ApplyActorFormToSelection();
    partial void OnActorBlocksMovementChanged(bool value) => ApplyActorFormToSelection();
    partial void OnActorHasChaseAIChanged(bool value) => ApplyActorFormToSelection();
    partial void OnActorBehaviorChanged(string value) => ApplyActorFormToSelection();
    partial void OnItemIdChanged(string value) => ApplyItemFormToSelection();
    partial void OnItemNameChanged(string value) => ApplyItemFormToSelection();
    partial void OnItemGlyphChanged(string value) => ApplyItemFormToSelection();
    partial void OnItemColorRChanged(int value) => ApplyItemFormToSelection();
    partial void OnItemColorGChanged(int value) => ApplyItemFormToSelection();
    partial void OnItemColorBChanged(int value) => ApplyItemFormToSelection();
    partial void OnItemKindChanged(string value) => ApplyItemFormToSelection();
    partial void OnItemMaxStackChanged(int value) => ApplyItemFormToSelection();
    partial void OnItemEquipSlotChanged(string value) => ApplyItemFormToSelection();
    partial void OnItemHealOnUseChanged(int value) => ApplyItemFormToSelection();
    partial void OnOverworldIdChanged(string value) => ApplyOverworldFormToProject();
    partial void OnGenFillPercentChanged(int value) => ApplyGeneratorParametersToProject();
    partial void OnGenSmoothPassesChanged(int value) => ApplyGeneratorParametersToProject();
    partial void OnGenWallThresholdChanged(int value) => ApplyGeneratorParametersToProject();
    partial void OnGenTargetFloorCountChanged(int value) => ApplyGeneratorParametersToProject();
    partial void OnGenWalkerCountChanged(int value) => ApplyGeneratorParametersToProject();
    partial void OnGenMinRoomSizeChanged(int value) => ApplyGeneratorParametersToProject();
    partial void OnGenMaxRoomSizeChanged(int value) => ApplyGeneratorParametersToProject();
    partial void OnGenSplitDepthChanged(int value) => ApplyGeneratorParametersToProject();
    partial void OnGenTilesetPathChanged(string value) => ApplyGeneratorParametersToProject();
    partial void OnSceneIdChanged(string value) => ApplySceneFormToSelection();
    partial void OnSceneNameChanged(string value) => ApplySceneFormToSelection();
    partial void OnSceneGeneratorChanged(string value) => ApplySceneFormToSelection();
    partial void OnSceneWidthChanged(int value)
    {
        ApplySceneFormToSelection();
        RefreshScenePreview();
    }

    partial void OnSceneHeightChanged(int value)
    {
        ApplySceneFormToSelection();
        RefreshScenePreview();
    }

    partial void OnSceneSeedChanged(int value)
    {
        if (SelectedScene is not null)
        {
            SelectedScene.Seed = value;
            MarkDirty();
        }
    }
    partial void OnVisualGraphIdChanged(string value) => ApplyVisualGraphFormToSelection();
    partial void OnVisualGraphEntryNodeIdChanged(string value) => ApplyVisualGraphFormToSelection();
    partial void OnVisualNodeIdChanged(string value) => ApplyVisualNodeFormToSelection();
    partial void OnVisualNodeTypeChanged(string value) => ApplyVisualNodeFormToSelection();
    partial void OnVisualNodeNextChanged(string value) => ApplyVisualNodeFormToSelection();
    partial void OnVisualNodeTrueBranchChanged(string value) => ApplyVisualNodeFormToSelection();
    partial void OnVisualNodeFalseBranchChanged(string value) => ApplyVisualNodeFormToSelection();
    partial void OnVisualNodeMessageChanged(string value) => ApplyVisualNodeFormToSelection();
    partial void OnVisualNodeThresholdChanged(int value) => ApplyVisualNodeFormToSelection();

    private void HandleAddNew(string nodeId)
    {
        switch (nodeId)
        {
            case "add-actor": AddActor(); break;
            case "add-scene": AddScene(); break;
            case "add-item": AddItem(); break;
            case "add-visual": AddVisualGraph(); break;
            case "add-interaction":
            case "add-class":
            case "add-quest":
                HandleAddNewGameRules(nodeId);
                break;
        }
    }

    private void OpenWelcomeDocument()
    {
        var tab = new EditorDocumentTab
        {
            TabKey = "welcome",
            Kind = EditorDocumentKind.Welcome,
            Title = "Welcome",
            Icon = "—"
        };
        OpenDocuments.Add(tab);
        SelectedDocument = tab;
    }

    private void OpenDocument(EditorDocumentKind kind, string resourceId, string title, string icon)
    {
        var tabKey = $"{kind}:{resourceId}";
        var existing = OpenDocuments.FirstOrDefault(tab => tab.TabKey == tabKey);
        if (existing is not null)
        {
            SelectedDocument = existing;
            return;
        }

        var tab = new EditorDocumentTab
        {
            TabKey = tabKey,
            Kind = kind,
            ResourceId = resourceId,
            Title = title,
            Icon = icon,
            IsModified = false
        };
        OpenDocuments.Add(tab);
        SelectedDocument = tab;
    }

    private void SyncSelectionFromDocument(EditorDocumentTab? tab)
    {
        if (tab is null) return;
        switch (tab.Kind)
        {
            case EditorDocumentKind.Actor:
                SelectedActor = Actors.FirstOrDefault(actor => actor.Id == tab.ResourceId);
                break;
            case EditorDocumentKind.Item:
                SelectedItem = Items.FirstOrDefault(item => item.Id == tab.ResourceId);
                break;
            case EditorDocumentKind.Overworld:
                LoadOverworldIntoForm();
                break;
            case EditorDocumentKind.Scene:
                SelectedScene = Scenes.FirstOrDefault(scene => scene.Id == tab.ResourceId);
                break;
            case EditorDocumentKind.VisualScript:
                SelectedVisualGraph = VisualGraphs.FirstOrDefault(graph => graph.Id == tab.ResourceId);
                break;
            case EditorDocumentKind.CodeScript:
                _selectedScript = _project?.ScriptFiles.FirstOrDefault(script => script.FullPath == tab.ResourceId);
                CodeScriptFileName = _selectedScript?.FileName ?? string.Empty;
                CodeScriptContent = _selectedScript?.Content ?? string.Empty;
                break;
            default:
                SyncGameRulesSelectionFromDocument(tab);
                break;
        }
    }

    private void CloseDocumentsForResource(string resourceId)
    {
        var toClose = OpenDocuments.Where(tab => tab.ResourceId == resourceId).ToList();
        foreach (var tab in toClose) OpenDocuments.Remove(tab);
        if (SelectedDocument is not null && toClose.Contains(SelectedDocument))
        {
            SelectedDocument = OpenDocuments.LastOrDefault();
        }
    }

    private void RebuildResourceTree()
    {
        if (_project is null) return;
        ResourceTree.Clear();
        foreach (var node in _resourceTreeService.BuildTree(_project)) ResourceTree.Add(node);
    }

    private void LoadProject(EditorProject project)
    {
        _project = project;
        ProjectName = project.Name;
        MapWidth = project.Settings.MapWidth;
        MapHeight = project.Settings.MapHeight;
        MessagePanelHeight = project.Settings.MessagePanelHeight;
        MinEnemies = project.Settings.MinEnemies;
        MaxEnemies = project.Settings.MaxEnemies;
        GeneratorId = project.Generator.Id;
        GeneratorAlgorithm = project.Generator.Algorithm;
        GeneratorWidth = project.Generator.Width;
        GeneratorHeight = project.Generator.Height;
        GeneratorSeedText = project.Generator.Seed?.ToString() ?? string.Empty;
        LoadGeneratorParametersFromProject(project.Generator);

        Actors.Clear();
        foreach (var actor in project.Actors) Actors.Add(actor);
        SelectedActor = Actors.FirstOrDefault();

        Items.Clear();
        foreach (var item in project.Items) Items.Add(item);
        SelectedItem = Items.FirstOrDefault();

        LoadGameRulesCollections(project);

        Scenes.Clear();
        foreach (var scene in project.Scenes) Scenes.Add(scene);
        SelectedScene = Scenes.FirstOrDefault();

        VisualGraphs.Clear();
        VisualNodes.Clear();
        foreach (var graph in project.VisualGraphs) VisualGraphs.Add(graph);
        SelectedVisualGraph = VisualGraphs.FirstOrDefault();

        RebuildResourceTree();
        WindowTitle = $"RogueEngine — {project.Name}";
        StatusText = $"Editing {project.Name}";
        OnPropertyChanged(nameof(HasProject));
        RefreshCommands();
    }

    private void ApplyFormToProject()
    {
        if (_project is null) return;
        ApplyActorFormToSelection();
        ApplyItemFormToSelection();
        ApplySceneFormToSelection();
        ApplyVisualGraphFormToSelection();
        ApplyVisualNodeFormToSelection();
        ApplyOverworldFormToProject();
        ApplyGeneratorParametersToProject();
        ApplyGameRulesFormsToProject();

        _project.Name = ProjectName;
        _project.Settings.MapWidth = MapWidth;
        _project.Settings.MapHeight = MapHeight;
        _project.Settings.MessagePanelHeight = MessagePanelHeight;
        _project.Settings.MinEnemies = MinEnemies;
        _project.Settings.MaxEnemies = MaxEnemies;
        _project.Generator.Id = GeneratorId;
        _project.Generator.Algorithm = GeneratorAlgorithm;
        _project.Generator.Width = GeneratorWidth;
        _project.Generator.Height = GeneratorHeight;
        _project.Generator.Seed = int.TryParse(GeneratorSeedText, out var seed) ? seed : null;
        _project.Actors = Actors.ToList();
        _project.Items = Items.ToList();
        _project.Scenes = Scenes.ToList();
        _project.VisualGraphs = VisualGraphs.ToList();
        _project.IsDirty = true;
    }

    private void LoadSelectedSceneIntoForm()
    {
        if (SelectedScene is null)
        {
            SceneId = string.Empty;
            SceneName = string.Empty;
            SceneGenerator = string.Empty;
            SceneWidth = _project?.Settings.MapWidth ?? 80;
            SceneHeight = _project?.Settings.MapHeight ?? 22;
            SceneSeed = 0;
            ScenePreviewMap = null;
            OnPropertyChanged(nameof(SceneEntityCount));
            OnPropertyChanged(nameof(SceneItemCount));
            OnPropertyChanged(nameof(SceneInteractionCount));
            return;
        }

        SceneId = SelectedScene.Id;
        SceneName = SelectedScene.Name;
        SceneGenerator = SelectedScene.Generator ?? string.Empty;
        SceneWidth = SelectedScene.Width ?? _project?.Settings.MapWidth ?? 80;
        SceneHeight = SelectedScene.Height ?? _project?.Settings.MapHeight ?? 22;
        SceneSeed = SelectedScene.Seed ?? 0;
        RefreshScenePreview();
        OnPropertyChanged(nameof(SceneEntityCount));
        OnPropertyChanged(nameof(SceneItemCount));
        OnPropertyChanged(nameof(SceneInteractionCount));
    }

    private void RefreshScenePreview()
    {
        if (SelectedScene is null || _project is null)
        {
            ScenePreviewMap = null;
            return;
        }

        ApplySceneFormToSelection();
        var result = _mapPreviewService.Generate(SelectedScene, _project.Generator);
        SelectedScene.Seed = result.Seed;
        SceneSeed = result.Seed;
        ScenePreviewMap = result.Map;
    }

    private void ApplySceneFormToSelection()
    {
        if (SelectedScene is null) return;
        SelectedScene.Id = SceneId;
        SelectedScene.Name = SceneName;
        SelectedScene.Generator = string.IsNullOrWhiteSpace(SceneGenerator) ? null : SceneGenerator;
        SelectedScene.Width = SceneWidth;
        SelectedScene.Height = SceneHeight;
        SelectedScene.Seed = SceneSeed;
        MarkDirty();
    }

    private void LoadSelectedVisualGraphIntoForm()
    {
        VisualNodes.Clear();
        if (SelectedVisualGraph is null)
        {
            VisualGraphId = string.Empty;
            VisualGraphEntryNodeId = string.Empty;
            SelectedVisualNode = null;
            VisualScriptPreview = string.Empty;
            NotifyVisualNodeCount();
            return;
        }

        VisualGraphId = SelectedVisualGraph.Id;
        VisualGraphEntryNodeId = SelectedVisualGraph.EntryNodeId;
        foreach (var node in SelectedVisualGraph.Nodes) VisualNodes.Add(node);
        SelectedVisualNode = VisualNodes.FirstOrDefault();
        NotifyVisualNodeCount();
    }

    private void LoadSelectedVisualNodeIntoForm()
    {
        if (SelectedVisualNode is null)
        {
            VisualNodeId = string.Empty;
            VisualNodeType = NodeType.OnTurn;
            VisualNodeNext = string.Empty;
            VisualNodeTrueBranch = string.Empty;
            VisualNodeFalseBranch = string.Empty;
            VisualNodeMessage = string.Empty;
            VisualNodeThreshold = 1;
            return;
        }

        VisualNodeId = SelectedVisualNode.Id;
        VisualNodeType = SelectedVisualNode.Type;
        VisualNodeNext = SelectedVisualNode.Next ?? string.Empty;
        VisualNodeTrueBranch = SelectedVisualNode.TrueBranch ?? string.Empty;
        VisualNodeFalseBranch = SelectedVisualNode.FalseBranch ?? string.Empty;
        VisualNodeMessage = SelectedVisualNode.Message;
        VisualNodeThreshold = SelectedVisualNode.Threshold;
    }

    private void ApplyVisualGraphFormToSelection()
    {
        if (SelectedVisualGraph is null) return;
        SelectedVisualGraph.Id = VisualGraphId;
        SelectedVisualGraph.EntryNodeId = VisualGraphEntryNodeId;
        MarkDirty();
    }

    private void ApplyVisualNodeFormToSelection()
    {
        if (SelectedVisualNode is null) return;
        SelectedVisualNode.Id = VisualNodeId;
        SelectedVisualNode.Type = VisualNodeType;
        SelectedVisualNode.Next = string.IsNullOrWhiteSpace(VisualNodeNext) ? null : VisualNodeNext;
        SelectedVisualNode.TrueBranch = string.IsNullOrWhiteSpace(VisualNodeTrueBranch) ? null : VisualNodeTrueBranch;
        SelectedVisualNode.FalseBranch = string.IsNullOrWhiteSpace(VisualNodeFalseBranch) ? null : VisualNodeFalseBranch;
        SelectedVisualNode.Message = VisualNodeMessage;
        SelectedVisualNode.Threshold = VisualNodeThreshold;
        MarkDirty();
    }

    private void LoadSelectedActorIntoForm()
    {
        if (SelectedActor is null)
        {
            ActorId = string.Empty;
            ActorGlyph = "?";
            ActorColorR = ActorColorG = ActorColorB = 0;
            ActorMaxHp = 1;
            ActorIsPlayer = ActorBlocksMovement = ActorHasChaseAI = false;
            ActorBehavior = string.Empty;
            return;
        }

        ActorId = SelectedActor.Id;
        ActorGlyph = SelectedActor.Glyph.ToString();
        ActorColorR = SelectedActor.ColorR;
        ActorColorG = SelectedActor.ColorG;
        ActorColorB = SelectedActor.ColorB;
        ActorMaxHp = SelectedActor.MaxHp;
        ActorIsPlayer = SelectedActor.IsPlayer;
        ActorBlocksMovement = SelectedActor.BlocksMovement;
        ActorHasChaseAI = SelectedActor.HasChaseAI;
        ActorBehavior = SelectedActor.Behavior ?? string.Empty;
    }

    private void ApplyActorFormToSelection()
    {
        if (SelectedActor is null) return;
        SelectedActor.Id = ActorId;
        SelectedActor.Glyph = ActorGlyph.Length > 0 ? ActorGlyph[0] : '?';
        SelectedActor.ColorR = (byte)Math.Clamp(ActorColorR, 0, 255);
        SelectedActor.ColorG = (byte)Math.Clamp(ActorColorG, 0, 255);
        SelectedActor.ColorB = (byte)Math.Clamp(ActorColorB, 0, 255);
        SelectedActor.MaxHp = ActorMaxHp;
        SelectedActor.IsPlayer = ActorIsPlayer;
        SelectedActor.BlocksMovement = ActorBlocksMovement;
        SelectedActor.HasChaseAI = ActorHasChaseAI;
        SelectedActor.Behavior = string.IsNullOrWhiteSpace(ActorBehavior) ? null : ActorBehavior;
        MarkDirty();
    }

    private void LoadSelectedItemIntoForm()
    {
        if (SelectedItem is null)
        {
            ItemId = string.Empty;
            ItemName = string.Empty;
            ItemGlyph = "!";
            ItemColorR = 200;
            ItemColorG = 50;
            ItemColorB = 50;
            ItemKind = "consumable";
            ItemMaxStack = 1;
            ItemEquipSlot = string.Empty;
            ItemHealOnUse = 0;
            return;
        }

        ItemId = SelectedItem.Id;
        ItemName = SelectedItem.Name;
        ItemGlyph = SelectedItem.Glyph.ToString();
        ItemColorR = SelectedItem.ColorR;
        ItemColorG = SelectedItem.ColorG;
        ItemColorB = SelectedItem.ColorB;
        ItemKind = SelectedItem.Kind;
        ItemMaxStack = SelectedItem.MaxStack;
        ItemEquipSlot = SelectedItem.EquipSlot ?? string.Empty;
        ItemHealOnUse = SelectedItem.HealOnUse;
        LoadSelectedItemIntoFormGameRules();
    }

    private void ApplyItemFormToSelection()
    {
        if (SelectedItem is null) return;
        SelectedItem.Id = ItemId;
        SelectedItem.Name = ItemName;
        SelectedItem.Glyph = ItemGlyph.Length > 0 ? ItemGlyph[0] : '!';
        SelectedItem.ColorR = (byte)Math.Clamp(ItemColorR, 0, 255);
        SelectedItem.ColorG = (byte)Math.Clamp(ItemColorG, 0, 255);
        SelectedItem.ColorB = (byte)Math.Clamp(ItemColorB, 0, 255);
        SelectedItem.Kind = ItemKind;
        SelectedItem.MaxStack = ItemMaxStack;
        SelectedItem.EquipSlot = string.IsNullOrWhiteSpace(ItemEquipSlot) ? null : ItemEquipSlot;
        SelectedItem.HealOnUse = ItemHealOnUse;
        ApplyItemFormToSelectionGameRules();
        MarkDirty();
    }

    private void LoadOverworldIntoForm()
    {
        OverworldCells.Clear();
        OverworldConnections.Clear();
        if (_project?.Overworld is null)
        {
            OverworldId = string.Empty;
            return;
        }

        OverworldId = _project.Overworld.Id;
        foreach (var cell in _project.Overworld.Cells) OverworldCells.Add(cell);
        foreach (var connection in _project.Overworld.Connections) OverworldConnections.Add(connection);
    }

    private void ApplyOverworldFormToProject()
    {
        if (_project?.Overworld is null) return;
        _project.Overworld.Id = OverworldId;
        MarkDirty();
    }

    private void LoadGeneratorParametersFromProject(EditorGenerator generator)
    {
        GenFillPercent = GetIntParam(generator, "fillPercent", 45);
        GenSmoothPasses = GetIntParam(generator, "smoothPasses", 5);
        GenWallThreshold = GetIntParam(generator, "wallThreshold", 4);
        GenTargetFloorCount = GetIntParam(generator, "targetFloorCount", 1200);
        GenWalkerCount = GetIntParam(generator, "walkerCount", 4);
        GenMinRoomSize = GetIntParam(generator, "minRoomSize", 4);
        GenMaxRoomSize = GetIntParam(generator, "maxRoomSize", 10);
        GenSplitDepth = GetIntParam(generator, "splitDepth", 4);
        GenTilesetPath = generator.Parameters.TryGetValue("tileset", out var tileset)
            ? tileset?.ToString() ?? string.Empty
            : string.Empty;
    }

    private void ApplyGeneratorParametersToProject()
    {
        if (_project is null) return;
        _project.Generator.Parameters["fillPercent"] = GenFillPercent;
        _project.Generator.Parameters["smoothPasses"] = GenSmoothPasses;
        _project.Generator.Parameters["wallThreshold"] = GenWallThreshold;
        _project.Generator.Parameters["targetFloorCount"] = GenTargetFloorCount;
        _project.Generator.Parameters["walkerCount"] = GenWalkerCount;
        _project.Generator.Parameters["minRoomSize"] = GenMinRoomSize;
        _project.Generator.Parameters["maxRoomSize"] = GenMaxRoomSize;
        _project.Generator.Parameters["splitDepth"] = GenSplitDepth;
        if (string.IsNullOrWhiteSpace(GenTilesetPath))
        {
            _project.Generator.Parameters.Remove("tileset");
        }
        else
        {
            _project.Generator.Parameters["tileset"] = GenTilesetPath;
        }

        MarkDirty();
    }

    private static int GetIntParam(EditorGenerator generator, string key, int fallback)
    {
        if (!generator.Parameters.TryGetValue(key, out var value))
        {
            return fallback;
        }

        return value switch
        {
            int i => i,
            long l => (int)l,
            double d => (int)d,
            JsonElement element when element.ValueKind == JsonValueKind.Number => element.GetInt32(),
            _ => int.TryParse(value?.ToString(), out var parsed) ? parsed : fallback
        };
    }

    private void NotifyVisualNodeCount() => OnPropertyChanged(nameof(VisualNodeCount));

    private void MarkDirty()
    {
        if (_project is not null) _project.IsDirty = true;
        if (SelectedDocument is not null) SelectedDocument.IsModified = true;
    }

    private void RefreshCommands()
    {
        SaveProjectCommand.NotifyCanExecuteChanged();
        ValidateProjectCommand.NotifyCanExecuteChanged();
        PlaytestCommand.NotifyCanExecuteChanged();
        StopPlaytestCommand.NotifyCanExecuteChanged();
        BuildProjectCommand.NotifyCanExecuteChanged();
        AddActorCommand.NotifyCanExecuteChanged();
        RemoveActorCommand.NotifyCanExecuteChanged();
        AddItemCommand.NotifyCanExecuteChanged();
        RemoveItemCommand.NotifyCanExecuteChanged();
        AddSceneCommand.NotifyCanExecuteChanged();
        AddVisualGraphCommand.NotifyCanExecuteChanged();
        RemoveVisualGraphCommand.NotifyCanExecuteChanged();
        AddVisualNodeCommand.NotifyCanExecuteChanged();
        RemoveVisualNodeCommand.NotifyCanExecuteChanged();
        PreviewVisualScriptCommand.NotifyCanExecuteChanged();
    }
}
