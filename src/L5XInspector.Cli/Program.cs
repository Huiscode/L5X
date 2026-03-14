using System.Globalization;
using L5XInspector.Core;

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
