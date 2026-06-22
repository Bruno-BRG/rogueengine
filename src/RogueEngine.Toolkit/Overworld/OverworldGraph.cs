using RogueEngine.Engine.Data;

namespace RogueEngine.Toolkit.Overworld;

public sealed class OverworldGraph
{
    public OverworldDefinition Definition { get; }
    private readonly Dictionary<string, OverworldCellDefinition> _cellsById;

    public OverworldGraph(OverworldDefinition definition)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        _cellsById = definition.Cells.ToDictionary(cell => cell.Id, StringComparer.OrdinalIgnoreCase);
    }

    public OverworldCellDefinition? GetCell(string id) =>
        _cellsById.TryGetValue(id, out var cell) ? cell : null;

    public IReadOnlyList<OverworldCellDefinition> GetNeighbors(string cellId)
    {
        var neighbors = new List<OverworldCellDefinition>();
        foreach (var connection in Definition.Connections)
        {
            if (string.Equals(connection.From, cellId, StringComparison.OrdinalIgnoreCase) &&
                _cellsById.TryGetValue(connection.To, out var to))
            {
                neighbors.Add(to);
            }
            else if (string.Equals(connection.To, cellId, StringComparison.OrdinalIgnoreCase) &&
                     _cellsById.TryGetValue(connection.From, out var from))
            {
                neighbors.Add(from);
            }
        }

        return neighbors;
    }
}
