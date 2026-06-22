using RogueEngine.Engine.Core;
using RogueEngine.Engine.Data;
using RogueEngine.Engine.ProcGen;
using RogueEngine.Toolkit.ProcGen;

namespace RogueEngine.Toolkit.Overworld;

public sealed class OverworldService
{
    public OverworldGraph Graph { get; }

    public OverworldService(OverworldDefinition definition) =>
        Graph = new OverworldGraph(definition);

    public TileMap BuildOverworldMap()
    {
        var cells = Graph.Definition.Cells;
        var maxX = cells.Max(cell => cell.X) + 3;
        var maxY = cells.Max(cell => cell.Y) + 3;
        var map = new TileMap(Math.Max(11, maxX), Math.Max(11, maxY));

        for (var x = 0; x < map.Width; x++)
        {
            for (var y = 0; y < map.Height; y++)
            {
                map.SetTile(x, y, Tile.Floor);
            }
        }

        foreach (var cell in cells)
        {
            var x = cell.X * 2 + 2;
            var y = cell.Y * 2 + 2;
            if (map.IsInBounds(x, y))
            {
                map.SetTile(x, y, new Tile(true, true, cell.Id.Length > 0 ? char.ToUpper(cell.Id[0]) : '?'));
            }
        }

        return map;
    }

    public DungeonGenerationResult EnterCell(
        OverworldCellDefinition cell,
        LoadedProject project,
        Random random)
    {
        ArgumentNullException.ThrowIfNull(cell);
        ArgumentNullException.ThrowIfNull(project);

        var generatorPath = Path.Combine(project.DataDirectory, cell.LocalGenerator);
        var definition = GeneratorLoader.Load(generatorPath);
        return MapGeneratorService.Generate(definition, random);
    }

    public OverworldCellDefinition? GetCellAt(TileMap map, Position position)
    {
        foreach (var cell in Graph.Definition.Cells)
        {
            var x = cell.X * 2 + 2;
            var y = cell.Y * 2 + 2;
            if (position.X == x && position.Y == y)
            {
                return cell;
            }
        }

        return null;
    }
}
