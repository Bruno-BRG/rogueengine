using RogueEngine.Engine.Data;

namespace RogueEngine.Editor.Models;

public sealed class EditorItem
{
    public string Id { get; set; } = string.Empty;
    public string SourceFileName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public char Glyph { get; set; } = '!';
    public byte ColorR { get; set; } = 200;
    public byte ColorG { get; set; } = 50;
    public byte ColorB { get; set; } = 50;
    public string Kind { get; set; } = "consumable";
    public int MaxStack { get; set; } = 1;
    public string? EquipSlot { get; set; }
    public int HealOnUse { get; set; }

    public static EditorItem FromEngine(ItemDefinition item) => new()
    {
        Id = item.Id,
        SourceFileName = $"{item.Id}.json",
        Name = item.Name,
        Glyph = item.Glyph,
        ColorR = item.Color.R,
        ColorG = item.Color.G,
        ColorB = item.Color.B,
        Kind = item.Kind,
        MaxStack = item.MaxStack,
        EquipSlot = item.EquipSlot,
        HealOnUse = item.OnUse.Heal
    };

    public ItemDefinition ToEngine() => new()
    {
        Id = Id,
        Name = Name,
        Glyph = Glyph,
        Color = new ColorData { R = ColorR, G = ColorG, B = ColorB },
        Kind = Kind,
        MaxStack = MaxStack,
        EquipSlot = string.IsNullOrWhiteSpace(EquipSlot) ? null : EquipSlot,
        OnUse = new ItemUseEffect { Heal = HealOnUse }
    };
}

public sealed class EditorSceneItem
{
    public string ItemId { get; set; } = string.Empty;
    public int X { get; set; }
    public int Y { get; set; }
    public int Count { get; set; } = 1;
}

public sealed class EditorOverworld
{
    public string Id { get; set; } = "world";
    public string SourceFileName { get; set; } = "world.json";
    public List<EditorOverworldCell> Cells { get; set; } = [];
    public List<EditorOverworldConnection> Connections { get; set; } = [];

    public static EditorOverworld FromEngine(OverworldDefinition definition, string fileName) => new()
    {
        Id = definition.Id,
        SourceFileName = fileName,
        Cells = definition.Cells.Select(cell => new EditorOverworldCell
        {
            Id = cell.Id,
            X = cell.X,
            Y = cell.Y,
            Biome = cell.Biome,
            LocalGenerator = cell.LocalGenerator
        }).ToList(),
        Connections = definition.Connections.Select(connection => new EditorOverworldConnection
        {
            From = connection.From,
            To = connection.To,
            Type = connection.Type
        }).ToList()
    };

    public OverworldDefinition ToEngine() => new()
    {
        Id = Id,
        Cells = Cells.Select(cell => new OverworldCellDefinition
        {
            Id = cell.Id,
            X = cell.X,
            Y = cell.Y,
            Biome = cell.Biome,
            LocalGenerator = cell.LocalGenerator
        }).ToList(),
        Connections = Connections.Select(connection => new OverworldConnectionDefinition
        {
            From = connection.From,
            To = connection.To,
            Type = connection.Type
        }).ToList()
    };
}

public sealed class EditorOverworldCell
{
    public string Id { get; set; } = string.Empty;
    public int X { get; set; }
    public int Y { get; set; }
    public string Biome { get; set; } = string.Empty;
    public string LocalGenerator { get; set; } = string.Empty;
}

public sealed class EditorOverworldConnection
{
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public string Type { get; set; } = "road";
}
