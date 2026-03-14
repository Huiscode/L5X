using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml;
using L5XInspector.Core.Models;

namespace L5XInspector.Core;

public static class L5xStreamingParser
{
    public static ProjectIr ParseProject(string l5xPath)
    {
        using var fs = File.OpenRead(l5xPath);
        using var reader = XmlReader.Create(fs, new XmlReaderSettings
        {
            IgnoreComments = true,
            IgnoreProcessingInstructions = true,
            IgnoreWhitespace = true,
            DtdProcessing = DtdProcessing.Ignore,
        });

        string projectName = "";
        string softwareRevision = "";
        string? targetType = null;
        DateTimeOffset? exportDate = null;

        var dataTypes = new List<DataTypeIr>();
        var aois = new List<AoiIr>();
        var controllerTags = new List<TagIr>();
        var programs = new List<ProgramIr>();

        while (reader.Read())
        {
            if (reader.NodeType != XmlNodeType.Element)
                continue;

            if (reader.Name == "RSLogix5000Content")
            {
                softwareRevision = reader.GetAttribute("SoftwareRevision") ?? "";
                projectName = reader.GetAttribute("TargetName") ?? "";
                targetType = reader.GetAttribute("TargetType");

                var exportDateStr = reader.GetAttribute("ExportDate");
                if (!string.IsNullOrWhiteSpace(exportDateStr) &&
                    DateTimeOffset.TryParse(exportDateStr, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dt))
                {
                    exportDate = dt;
                }

                continue;
            }

            if (reader.Name == "Controller")
            {
                ReadController(reader, dataTypes, aois, controllerTags, programs);
                continue;
            }
        }

        if (programs.Count == 0)
        {
            programs.AddRange(ReadProgramsFallback(l5xPath));
        }

        return new ProjectIr(
            Name: projectName,
            SoftwareRevision: softwareRevision,
            ExportDate: exportDate,
            TargetType: targetType,
            DataTypes: dataTypes,
            Aois: aois,
            ControllerTags: controllerTags,
            Programs: programs);
    }

    private static List<ProgramIr> ReadProgramsFallback(string l5xPath)
    {
        var fallbackPrograms = new List<ProgramIr>();
        using var fs = File.OpenRead(l5xPath);
        using var reader = XmlReader.Create(fs, new XmlReaderSettings
        {
            IgnoreComments = true,
            IgnoreProcessingInstructions = true,
            IgnoreWhitespace = true,
            DtdProcessing = DtdProcessing.Ignore,
        });

        while (reader.Read())
        {
            if (reader.NodeType != XmlNodeType.Element)
                continue;

            if (reader.Name == "Program")
            {
                fallbackPrograms.Add(ReadProgram(reader));
            }
        }

        return fallbackPrograms;
    }


    private static void ReadController(
        XmlReader reader,
        List<DataTypeIr> dataTypes,
        List<AoiIr> aois,
        List<TagIr> controllerTags,
        List<ProgramIr> programs)
    {
        if (reader.IsEmptyElement)
            return;

        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "Controller")
                break;

            if (reader.NodeType != XmlNodeType.Element)
                continue;

            switch (reader.Name)
            {
                case "DataTypes":
                    ReadDataTypes(reader, dataTypes);
                    break;
                case "AddOnInstructionDefinitions":
                    ReadAois(reader, aois);
                    break;
                case "Tags":
                    ReadTags(reader, controllerTags, scope: "Controller");
                    break;
                case "Programs":
                    ReadPrograms(reader, programs);
                    break;
                case "Program":
                    programs.Add(ReadProgram(reader));
                    break;
            }
        }
    }

    private static void ReadDataTypes(XmlReader reader, List<DataTypeIr> output)
    {
        if (reader.IsEmptyElement)
            return;

        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "DataTypes")
                break;

            if (reader.NodeType != XmlNodeType.Element)
                continue;

            if (reader.Name == "DataType")
            {
                output.Add(ReadDataType(reader));
            }
        }
    }

    private static void ReadAois(XmlReader reader, List<AoiIr> output)
    {
        if (reader.IsEmptyElement)
            return;

        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "AddOnInstructionDefinitions")
                break;

            if (reader.NodeType != XmlNodeType.Element)
                continue;

            if (reader.Name == "AddOnInstructionDefinition")
            {
                output.Add(ReadAoi(reader));
            }
        }
    }

    private static AoiIr ReadAoi(XmlReader reader)
    {
        var name = reader.GetAttribute("Name") ?? "";
        string? description = null;
        var parameters = new List<AoiParameterIr>();
        var localTags = new List<TagIr>();
        var routines = new List<RoutineIr>();

        if (reader.IsEmptyElement)
            return new AoiIr(name, description, parameters, localTags, routines);

        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "AddOnInstructionDefinition")
                break;

            if (reader.NodeType != XmlNodeType.Element)
                continue;

            switch (reader.Name)
            {
                case "Description":
                    description = reader.ReadInnerXml();
                    break;
                case "Parameters":
                    ReadAoiParameters(reader, parameters);
                    break;
                case "LocalTags":
                    ReadTags(reader, localTags, scope: $"AOI:{name}");
                    break;
                case "Routines":
                    ReadRoutines(reader, routines);
                    break;
            }
        }

        return new AoiIr(name, description, parameters, localTags, routines);
    }

    private static void ReadAoiParameters(XmlReader reader, List<AoiParameterIr> output)
    {
        if (reader.IsEmptyElement)
            return;

        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "Parameters")
                break;

            if (reader.NodeType != XmlNodeType.Element)
                continue;

            if (reader.Name == "Parameter")
            {
                var name = reader.GetAttribute("Name") ?? "";
                var dataType = reader.GetAttribute("DataType") ?? "";
                var direction = reader.GetAttribute("Usage") ?? "";
                string? description = null;

                if (!reader.IsEmptyElement)
                {
                    var paramSubtree = reader.ReadSubtree();
                    while (paramSubtree.Read())
                    {
                        if (paramSubtree.NodeType == XmlNodeType.Element && paramSubtree.Name == "Description")
                        {
                            description = paramSubtree.ReadInnerXml();
                            break;
                        }
                    }
                }

                output.Add(new AoiParameterIr(name, dataType, direction, description));
            }
        }
    }

    private static DataTypeIr ReadDataType(XmlReader reader)
    {
        var name = reader.GetAttribute("Name") ?? "";
        var @class = reader.GetAttribute("Class") ?? "";

        string? description = null;
        var members = new List<DataTypeMemberIr>();
        var deps = new List<string>();

        if (reader.IsEmptyElement)
            return new DataTypeIr(name, @class, members, deps, description);

        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "DataType")
                break;

            if (reader.NodeType != XmlNodeType.Element)
                continue;

            switch (reader.Name)
            {
                case "Description":
                    description = reader.ReadInnerXml();
                    break;
                case "Members":
                    ReadDataTypeMembers(reader, members);
                    break;
                case "Dependencies":
                    ReadDependencies(reader, deps);
                    break;
            }
        }

        return new DataTypeIr(name, @class, members, deps, description);
    }

    private static void ReadDataTypeMembers(XmlReader reader, List<DataTypeMemberIr> members)
    {
        if (reader.IsEmptyElement)
            return;

        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "Members")
                break;

            if (reader.NodeType != XmlNodeType.Element)
                continue;

            if (reader.Name == "Member")
            {
                var name = reader.GetAttribute("Name") ?? "";
                var dataType = reader.GetAttribute("DataType") ?? "";
                var dimStr = reader.GetAttribute("Dimension");
                int? dim = int.TryParse(dimStr, out var d) ? d : null;
                var hidden = string.Equals(reader.GetAttribute("Hidden"), "true", StringComparison.OrdinalIgnoreCase);

                members.Add(new DataTypeMemberIr(name, dataType, dim, hidden));

                // consume subtree if needed
                if (!reader.IsEmptyElement)
                    reader.Skip();
            }
        }
    }

    private static void ReadDependencies(XmlReader reader, List<string> deps)
    {
        if (reader.IsEmptyElement)
            return;

        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "Dependencies")
                break;

            if (reader.NodeType != XmlNodeType.Element)
                continue;

            if (reader.Name == "Dependency")
            {
                var name = reader.GetAttribute("Name");
                if (!string.IsNullOrWhiteSpace(name))
                    deps.Add(name);

                if (!reader.IsEmptyElement)
                    reader.Skip();
            }
        }
    }

    private static void ReadTags(XmlReader reader, List<TagIr> tags, string scope)
    {
        if (reader.IsEmptyElement)
            return;

        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "Tags")
                break;

            if (reader.NodeType != XmlNodeType.Element)
                continue;

            if (reader.Name == "Tag")
            {
                var name = reader.GetAttribute("Name") ?? "";
                var dataType = reader.GetAttribute("DataType") ?? "";
                var dimStr = reader.GetAttribute("Dimensions");
                int? dims = int.TryParse(dimStr, out var d) ? d : null;

                string? desc = null;
                if (!reader.IsEmptyElement)
                {
                    // Look for optional <Description>
                    var tagSubtree = reader.ReadSubtree();
                    while (tagSubtree.Read())
                    {
                        if (tagSubtree.NodeType == XmlNodeType.Element && tagSubtree.Name == "Description")
                        {
                            desc = tagSubtree.ReadInnerXml();
                            break;
                        }
                    }
                }

                tags.Add(new TagIr(name, dataType, scope, dims, desc));
            }
        }
    }

    private static void ReadPrograms(XmlReader reader, List<ProgramIr> output)
    {
        if (reader.IsEmptyElement)
            return;

        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "Programs")
                break;

            if (reader.NodeType != XmlNodeType.Element)
                continue;

            if (reader.Name == "Program")
            {
                output.Add(ReadProgram(reader));
            }
        }
    }

    private static ProgramIr ReadProgram(XmlReader reader)
    {
        var name = reader.GetAttribute("Name") ?? "";
        var tags = new List<TagIr>();
        var routines = new List<RoutineIr>();

        if (reader.IsEmptyElement)
            return new ProgramIr(name, tags, routines);

        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "Program")
                break;

            if (reader.NodeType != XmlNodeType.Element)
                continue;

            switch (reader.Name)
            {
                case "Tags":
                    ReadTags(reader, tags, scope: $"Program:{name}");
                    break;
                case "Routines":
                    ReadRoutines(reader, routines);
                    break;
            }
        }

        return new ProgramIr(name, tags, routines);
    }

    private static void ReadRoutines(XmlReader reader, List<RoutineIr> output)
    {
        if (reader.IsEmptyElement)
            return;

        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "Routines")
                break;

            if (reader.NodeType != XmlNodeType.Element)
                continue;

            if (reader.Name == "Routine")
            {
                output.Add(ReadRoutine(reader));
                continue;
            }

            if (reader.Name == "EncodedData" && string.Equals(reader.GetAttribute("EncodedType"), "Routine", StringComparison.OrdinalIgnoreCase))
            {
                // Encrypted routine: keep metadata but no logic.
                var rName = reader.GetAttribute("Name") ?? "";
                output.Add(new RoutineIr(rName, Type: "Encoded", IsEncrypted: true, LogicText: null, ReadTags: Array.Empty<string>(), WriteTags: Array.Empty<string>(), AoiCalls: Array.Empty<AoiCallIr>()));
                if (!reader.IsEmptyElement)
                    reader.Skip();
            }
        }
    }

    private static RoutineIr ReadRoutine(XmlReader reader)
    {
        var name = reader.GetAttribute("Name") ?? "";
        var type = reader.GetAttribute("Type") ?? "";
        string? logicText = null;

        if (reader.IsEmptyElement)
            return new RoutineIr(name, type, IsEncrypted: false, LogicText: logicText, ReadTags: Array.Empty<string>(), WriteTags: Array.Empty<string>(), AoiCalls: Array.Empty<AoiCallIr>());

        // Heuristic: capture inner XML of the routine (later we can specialize per type)
        // Keep size bounded in future if needed.
        logicText = reader.ReadInnerXml();

        var (reads, writes) = TagExtraction.ExtractTags(type, logicText);
        var aoiCalls = TagExtraction.ExtractAoiCalls(logicText);
        return new RoutineIr(name, type, IsEncrypted: false, LogicText: logicText, ReadTags: reads, WriteTags: writes, AoiCalls: aoiCalls);
    }

}

internal static class TagExtraction
{
    private static readonly Regex StAssignmentRegex = new(
        @"(?<left>[A-Za-z_][A-Za-z0-9_\.\[\]]*)\s*(:=|=)\s*(?<right>[^;]+)",
        RegexOptions.Compiled);

    private static readonly Regex TokenRegex = new(
        @"[A-Za-z_][A-Za-z0-9_\.\[\]]*",
        RegexOptions.Compiled);

    private static readonly HashSet<string> StKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "IF","THEN","ELSE","ELSIF","END_IF","FOR","TO","DO","END_FOR",
        "WHILE","END_WHILE","CASE","OF","END_CASE","RETURN","AND","OR","NOT",
        "TRUE","FALSE","XIC","XIO","OTE","MOV"
    };

    public static (IReadOnlyList<string> Reads, IReadOnlyList<string> Writes) ExtractTags(string routineType, string? logicText)
    {
        if (string.IsNullOrWhiteSpace(logicText))
            return (Array.Empty<string>(), Array.Empty<string>());

        if (string.Equals(routineType, "ST", StringComparison.OrdinalIgnoreCase))
            return ExtractStructuredText(logicText);

        if (string.Equals(routineType, "RLL", StringComparison.OrdinalIgnoreCase))
            return ExtractLadder(logicText);

        return (Array.Empty<string>(), Array.Empty<string>());
    }

    private static (IReadOnlyList<string> Reads, IReadOnlyList<string> Writes) ExtractStructuredText(string logicText)
    {
        var reads = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var writes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (Match match in StAssignmentRegex.Matches(logicText))
        {
            var left = match.Groups["left"].Value;
            if (IsTagToken(left))
                writes.Add(Normalize(left));

            var right = match.Groups["right"].Value;
            foreach (Match token in TokenRegex.Matches(right))
            {
                var candidate = token.Value;
                if (IsTagToken(candidate))
                    reads.Add(Normalize(candidate));
            }
        }

        return (reads.ToList(), writes.ToList());
    }

    private static readonly Regex AoiCallRegex = new(
        @"<Instruction[^>]*Name=""(?<name>[A-Za-z_][A-Za-z0-9_]*)""[^>]*\bType=""ADD_ON_INSTRUCTION""[^>]*>(?<body>.*?)</Instruction>",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

    private static readonly Regex AoiInstanceRegex = new(
        @"<AoiName>(?<name>[^<]+)</AoiName>|<AddOnInstruction[^>]*Name=""(?<name2>[^""]+)""",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static IReadOnlyList<AoiCallIr> ExtractAoiCalls(string logicText)
    {
        var calls = new List<AoiCallIr>();

        foreach (Match match in AoiCallRegex.Matches(logicText))
        {
            var aoiName = match.Groups["name"].Value;
            string? instance = null;

            var body = match.Groups["body"].Value;
            var instanceMatch = AoiInstanceRegex.Match(body);
            if (instanceMatch.Success)
                instance = instanceMatch.Groups["name"].Success ? instanceMatch.Groups["name"].Value : instanceMatch.Groups["name2"].Value;

            if (!string.IsNullOrWhiteSpace(aoiName))
                calls.Add(new AoiCallIr(aoiName, instance));
        }

        return calls;
    }

    private static readonly Regex LadderInstructionRegex = new(
        @"(?<mnemonic>XIC|XIO|OTE|OTL|OTU|MOV|ADD|SUB|MUL|DIV|COP|CPS|TON|TOF|RTO|EQU|NEQ|GRT|GEQ|LES|LEQ|LIM)\s*\((?<args>[^\)]*)\)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static (IReadOnlyList<string> Reads, IReadOnlyList<string> Writes) ExtractLadder(string logicText)
    {
        var reads = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var writes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (Match match in LadderInstructionRegex.Matches(logicText))
        {
            var mnemonic = match.Groups["mnemonic"].Value.ToUpperInvariant();
            var args = match.Groups["args"].Value
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (args.Length == 0)
                continue;

            switch (mnemonic)
            {
                case "XIC":
                case "XIO":
                    if (IsTagToken(args[0]))
                        reads.Add(Normalize(args[0]));
                    break;
                case "OTE":
                case "OTL":
                case "OTU":
                    if (IsTagToken(args[0]))
                        writes.Add(Normalize(args[0]));
                    break;
                case "MOV":
                    if (args.Length >= 1 && IsTagToken(args[0]))
                        reads.Add(Normalize(args[0]));
                    if (args.Length >= 2 && IsTagToken(args[1]))
                        writes.Add(Normalize(args[1]));
                    break;
                case "ADD":
                case "SUB":
                case "MUL":
                case "DIV":
                    if (args.Length >= 1 && IsTagToken(args[0]))
                        reads.Add(Normalize(args[0]));
                    if (args.Length >= 2 && IsTagToken(args[1]))
                        reads.Add(Normalize(args[1]));
                    if (args.Length >= 3 && IsTagToken(args[2]))
                        writes.Add(Normalize(args[2]));
                    break;
                case "COP":
                case "CPS":
                    if (args.Length >= 1 && IsTagToken(args[0]))
                        reads.Add(Normalize(args[0]));
                    if (args.Length >= 2 && IsTagToken(args[1]))
                        writes.Add(Normalize(args[1]));
                    break;
                case "TON":
                case "TOF":
                case "RTO":
                    if (args.Length >= 1 && IsTagToken(args[0]))
                        reads.Add(Normalize(args[0]));
                    if (args.Length >= 2 && IsTagToken(args[1]))
                        writes.Add(Normalize(args[1]));
                    break;
                case "EQU":
                case "NEQ":
                case "GRT":
                case "GEQ":
                case "LES":
                case "LEQ":
                    if (args.Length >= 1 && IsTagToken(args[0]))
                        reads.Add(Normalize(args[0]));
                    if (args.Length >= 2 && IsTagToken(args[1]))
                        reads.Add(Normalize(args[1]));
                    break;
                case "LIM":
                    if (args.Length >= 1 && IsTagToken(args[0]))
                        reads.Add(Normalize(args[0]));
                    if (args.Length >= 2 && IsTagToken(args[1]))
                        reads.Add(Normalize(args[1]));
                    if (args.Length >= 3 && IsTagToken(args[2]))
                        reads.Add(Normalize(args[2]));
                    break;
            }
        }

        return (reads.ToList(), writes.ToList());
    }

    private static bool IsTagToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return false;

        return !StKeywords.Contains(token);
    }

    private static string Normalize(string token)
    {
        return token.Replace(" ", string.Empty);
    }
}
