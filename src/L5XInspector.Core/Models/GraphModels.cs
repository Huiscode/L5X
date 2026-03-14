namespace L5XInspector.Core.Models;

public enum GraphNodeKind
{
    Task,
    Program,
    Routine,
    Aoi,
    Udt,
    Tag,
    Station
}

public enum GraphEdgeKind
{
    Contains,
    Calls,
    Reads,
    Writes,
    UsesType,
    DependsOn,
    BelongsToStation
}

public sealed record GraphNode(string Id, GraphNodeKind Kind, string Name);

public sealed record GraphEdge(GraphEdgeKind Kind, string FromId, string ToId, string? Metadata = null);

public sealed class DependencyGraph
{
    private readonly Dictionary<string, GraphNode> _nodes = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<GraphEdge>> _outgoing = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<GraphEdge>> _incoming = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, GraphNode> Nodes => _nodes;
    public IReadOnlyList<GraphEdge> Edges => _outgoing.Values.SelectMany(list => list).ToList();
    public int EdgeCount => _outgoing.Values.Sum(list => list.Count);

    public void AddNode(GraphNode node)
    {
        if (!_nodes.ContainsKey(node.Id))
            _nodes[node.Id] = node;
    }

    public void AddEdge(GraphEdge edge)
    {
        AddNode(new GraphNode(edge.FromId, GraphNodeKind.Tag, edge.FromId));
        AddNode(new GraphNode(edge.ToId, GraphNodeKind.Tag, edge.ToId));

        if (!_outgoing.TryGetValue(edge.FromId, out var outgoing))
        {
            outgoing = new List<GraphEdge>();
            _outgoing[edge.FromId] = outgoing;
        }

        if (!_incoming.TryGetValue(edge.ToId, out var incoming))
        {
            incoming = new List<GraphEdge>();
            _incoming[edge.ToId] = incoming;
        }

        outgoing.Add(edge);
        incoming.Add(edge);
    }

    public IReadOnlyList<GraphEdge> GetOutgoing(string nodeId)
        => _outgoing.TryGetValue(nodeId, out var list) ? list : Array.Empty<GraphEdge>();

    public IReadOnlyList<GraphEdge> GetIncoming(string nodeId)
        => _incoming.TryGetValue(nodeId, out var list) ? list : Array.Empty<GraphEdge>();
}

public sealed record StationRule(string Name, IReadOnlyList<string> Patterns);
