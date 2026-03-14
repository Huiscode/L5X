using System.Globalization;
using L5XInspector.Core;
using L5XInspector.Core.Models;

var l5xPath = args.Length > 0 ? args[0] : "UN01_FPP_1_Program.L5X";
if (!File.Exists(l5xPath))
{
    Console.WriteLine($"L5X file not found: {l5xPath}");
    return;
}

var project = L5xStreamingParser.ParseProject(l5xPath);

Console.WriteLine("L5X parse summary");
Console.WriteLine($"Project: {project.Name} (v{project.SoftwareRevision})");
Console.WriteLine($"Target: {project.TargetType}");
Console.WriteLine($"Export: {project.ExportDate?.ToString("u", CultureInfo.InvariantCulture) ?? "unknown"}");
Console.WriteLine($"UDTs (DataTypes): {project.DataTypes.Count}");
Console.WriteLine($"Controller Tags: {project.ControllerTags.Count}");
Console.WriteLine($"Programs: {project.Programs.Count}");
Console.WriteLine($"Program Tags: {project.Programs.Sum(p => p.ProgramTags.Count)}");
var routines = project.Programs.SelectMany(p => p.Routines).ToList();
Console.WriteLine($"Routines: {routines.Count}");
Console.WriteLine($"ST Read Tags: {routines.Sum(r => r.ReadTags.Count)}");
Console.WriteLine($"ST Write Tags: {routines.Sum(r => r.WriteTags.Count)}");
Console.WriteLine($"AOIs: {project.Aois.Count}");

var stationRules = StationRuleLoader.Load("station-rules.sample.json");

var graph = DependencyGraphBuilder.Build(project, stationRules);
Console.WriteLine($"Graph Nodes: {graph.Nodes.Count}");
Console.WriteLine($"Graph Edges: {graph.EdgeCount}");

var nodeCounts = graph.Nodes.Values
    .GroupBy(n => n.Kind)
    .Select(g => $"{g.Key}:{g.Count()}")
    .OrderBy(s => s);
Console.WriteLine("Node Types: " + string.Join(", ", nodeCounts));

var edgeCounts = graph.Edges
    .GroupBy(e => e.Kind)
    .Select(g => $"{g.Key}:{g.Count()}")
    .OrderBy(s => s);
Console.WriteLine("Edge Types: " + string.Join(", ", edgeCounts));

var stationEdges = graph.Edges.Count(e => e.Kind == GraphEdgeKind.BelongsToStation);
Console.WriteLine($"BelongsToStation Edges: {stationEdges}");
