using System.Text.RegularExpressions;
using L5XInspector.Core.Models;

namespace L5XInspector.Core;

public static class DependencyGraphBuilder
{
    public static DependencyGraph Build(ProjectIr project, IReadOnlyList<StationRule>? stationRules = null)
    {
        var graph = new DependencyGraph();

        foreach (var program in project.Programs)
        {
            var programId = BuildProgramId(program.Name);
            graph.AddNode(new GraphNode(programId, GraphNodeKind.Program, program.Name));

            foreach (var routine in program.Routines)
            {
                var routineId = BuildRoutineId(program.Name, routine.Name);
                graph.AddNode(new GraphNode(routineId, GraphNodeKind.Routine, routine.Name));
                graph.AddEdge(new GraphEdge(GraphEdgeKind.Contains, programId, routineId));

                foreach (var readTag in routine.ReadTags)
                {
                    var tagId = BuildTagId(readTag, routine.Name);
                    graph.AddNode(new GraphNode(tagId, GraphNodeKind.Tag, readTag));
                    graph.AddEdge(new GraphEdge(GraphEdgeKind.Reads, routineId, tagId));
                }

                foreach (var writeTag in routine.WriteTags)
                {
                    var tagId = BuildTagId(writeTag, routine.Name);
                    graph.AddNode(new GraphNode(tagId, GraphNodeKind.Tag, writeTag));
                    graph.AddEdge(new GraphEdge(GraphEdgeKind.Writes, routineId, tagId));
                }

                foreach (var aoiCall in routine.AoiCalls)
                {
                    var aoiId = BuildAoiId(aoiCall.AoiName);
                    graph.AddNode(new GraphNode(aoiId, GraphNodeKind.Aoi, aoiCall.AoiName));
                    graph.AddEdge(new GraphEdge(GraphEdgeKind.Calls, routineId, aoiId, aoiCall.InstanceName));
                }
            }
        }

        foreach (var aoi in project.Aois)
        {
            var aoiId = BuildAoiId(aoi.Name);
            graph.AddNode(new GraphNode(aoiId, GraphNodeKind.Aoi, aoi.Name));

            foreach (var routine in aoi.Routines)
            {
                var routineId = BuildRoutineId(aoi.Name, routine.Name, isAoi: true);
                graph.AddNode(new GraphNode(routineId, GraphNodeKind.Routine, routine.Name));
                graph.AddEdge(new GraphEdge(GraphEdgeKind.Contains, aoiId, routineId));
            }
        }

        foreach (var dataType in project.DataTypes)
        {
            var udtId = BuildUdtId(dataType.Name);
            graph.AddNode(new GraphNode(udtId, GraphNodeKind.Udt, dataType.Name));

            foreach (var dependency in dataType.Dependencies)
            {
                var depId = BuildUdtId(dependency);
                graph.AddNode(new GraphNode(depId, GraphNodeKind.Udt, dependency));
                graph.AddEdge(new GraphEdge(GraphEdgeKind.DependsOn, udtId, depId));
            }
        }

        foreach (var tag in project.ControllerTags)
        {
            var tagId = BuildTagId(tag.Name, tag.Scope);
            graph.AddNode(new GraphNode(tagId, GraphNodeKind.Tag, tag.Name));

            if (!string.IsNullOrWhiteSpace(tag.DataType))
            {
                var udtId = BuildUdtId(tag.DataType);
                graph.AddNode(new GraphNode(udtId, GraphNodeKind.Udt, tag.DataType));
                graph.AddEdge(new GraphEdge(GraphEdgeKind.UsesType, tagId, udtId));
            }
        }

        foreach (var program in project.Programs)
        {
            foreach (var tag in program.ProgramTags)
            {
                var tagId = BuildTagId(tag.Name, tag.Scope);
                graph.AddNode(new GraphNode(tagId, GraphNodeKind.Tag, tag.Name));

                if (!string.IsNullOrWhiteSpace(tag.DataType))
                {
                    var udtId = BuildUdtId(tag.DataType);
                    graph.AddNode(new GraphNode(udtId, GraphNodeKind.Udt, tag.DataType));
                    graph.AddEdge(new GraphEdge(GraphEdgeKind.UsesType, tagId, udtId));
                }
            }
        }

        if (stationRules is { Count: > 0 })
        {
            ApplyStationRules(graph, project, stationRules);
        }

        return graph;
    }

    private static void ApplyStationRules(DependencyGraph graph, ProjectIr project, IReadOnlyList<StationRule> rules)
    {
        var compiled = rules
            .Select(rule => (rule, patterns: rule.Patterns.Select(BuildRegex).ToList()))
            .ToList();

        foreach (var rule in compiled)
        {
            var stationId = BuildStationId(rule.rule.Name);
            graph.AddNode(new GraphNode(stationId, GraphNodeKind.Station, rule.rule.Name));

            foreach (var program in project.Programs)
            {
                var programId = BuildProgramId(program.Name);
                var programMatched = rule.patterns.Any(rx => rx.IsMatch(program.Name));
                if (programMatched)
                {
                    graph.AddEdge(new GraphEdge(GraphEdgeKind.BelongsToStation, programId, stationId));
                }

                foreach (var routine in program.Routines)
                {
                    var routineId = BuildRoutineId(program.Name, routine.Name);
                    if (programMatched || rule.patterns.Any(rx => rx.IsMatch(routine.Name)))
                    {
                        graph.AddEdge(new GraphEdge(GraphEdgeKind.BelongsToStation, routineId, stationId));
                    }
                }
            }
        }
    }

    private static Regex BuildRegex(string pattern)
    {
        var wildcard = Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".");
        return new Regex($"^{wildcard}$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    }

    private static string BuildProgramId(string programName) => $"Program::{programName}";

    private static string BuildRoutineId(string ownerName, string routineName, bool isAoi = false)
        => isAoi ? $"AOI::{ownerName}::Routine::{routineName}" : $"Program::{ownerName}::Routine::{routineName}";

    private static string BuildTagId(string tagName, string scope)
        => $"Tag::{scope}::{tagName}";

    private static string BuildAoiId(string aoiName) => $"AOI::{aoiName}";

    private static string BuildUdtId(string udtName) => $"UDT::{udtName}";

    private static string BuildStationId(string stationName) => $"Station::{stationName}";
}
