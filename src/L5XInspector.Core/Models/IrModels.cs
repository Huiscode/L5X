namespace L5XInspector.Core.Models;

public sealed record ProjectIr(
    string Name,
    string SoftwareRevision,
    DateTimeOffset? ExportDate,
    string? TargetType,
    IReadOnlyList<DataTypeIr> DataTypes,
    IReadOnlyList<AoiIr> Aois,
    IReadOnlyList<TagIr> ControllerTags,
    IReadOnlyList<ProgramIr> Programs);

public sealed record ProgramIr(
    string Name,
    IReadOnlyList<TagIr> ProgramTags,
    IReadOnlyList<RoutineIr> Routines);

public sealed record RoutineIr(
    string Name,
    string Type,
    bool IsEncrypted,
    string? LogicText,
    IReadOnlyList<string> ReadTags,
    IReadOnlyList<string> WriteTags,
    IReadOnlyList<AoiCallIr> AoiCalls);

public sealed record TagIr(
    string Name,
    string DataType,
    string Scope,
    int? Dimensions,
    string? Description);

public sealed record DataTypeIr(
    string Name,
    string Class,
    IReadOnlyList<DataTypeMemberIr> Members,
    IReadOnlyList<string> Dependencies,
    string? Description);

public sealed record DataTypeMemberIr(
    string Name,
    string DataType,
    int? Dimension,
    bool Hidden);

public sealed record AoiIr(
    string Name,
    string? Description,
    IReadOnlyList<AoiParameterIr> Parameters,
    IReadOnlyList<TagIr> LocalTags,
    IReadOnlyList<RoutineIr> Routines);

public sealed record AoiParameterIr(
    string Name,
    string DataType,
    string Direction,
    string? Description);

public sealed record AoiCallIr(
    string AoiName,
    string? InstanceName);
