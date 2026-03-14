# Technical Specification (SPEC)

## Architecture Overview
- Windows desktop app (WinUI 3) with a local analysis engine.
- Ingestion pipeline: L5X parser → normalized IR (intermediate representation) → graph index.
- Core services:
  - Project Indexer: tasks/programs/routines/AOIs/UDTs/tags
  - Dependency Graph Builder: call hierarchy, read/write edges, station groupings
  - Trace Engine: forward/backward tag traversal
  - Doc Generator: AOI/UDT summaries and learning mode narratives
- UI modules: Navigator, Station View, Trace Explorer, AOI/UDT docs, Impact Lens, Learning Mode.

## Key Flows
- Import L5X → parse XML → build IR → index entities → build graphs → render navigator.
- Station definition: user provides naming patterns → station groups computed → summaries built.
- Tag trace: select tag → compute read/write locations → traverse graph forward/backward.
- Change impact: select routine or AOI → compute affected tags → downstream station map.
- Learning mode: choose template → gather key routines + tags → generate walkthrough.

## L5X Parser Extraction Targets (v30–v35)
### Phase 1 (Core Structure)
- Controller: name, software revision, export metadata
- Tasks: task name/type/rate → program references
- Programs: name, routines list, program tags
- Routines: name/type, rungs/logic text

### Phase 2 (Types and Tags)
- DataTypes (UDTs): members, descriptions, dependencies
- AOIs: parameters, local tags, routines, logic text
- Controller Tags + Program Tags: data types, dimensions, descriptions

### Phase 3 (References)
- Tag references in routines: read/write detection from ladder/ST
- Calls: routine-to-routine and routine-to-AOI instance calls
- AOI instance bindings (parameters and mapped tags)

### Mapping Notes
- Use streaming XML reader (XmlReader) for large files
- Preserve source text snippets for trace preview
- Track Location references: Routine → Rung → Instruction line/pos

## Sample L5X Observations (UN01_FPP_1_Program.L5X)
- File is Program-targeted export (TargetType="Program") with nested Controller content.
- DataTypes appear early under `<DataTypes>` with `<Members>` and `<Dependencies>`.
- AOIs exist under `<AddOnInstructionDefinitions>`.
- Controller-level tags appear under `<Tags Use="Context">`.
- Programs are under `<Program Name="...">` with nested `<Tags>` and `<Routines>`.
- Routines have `Type="RLL"` or `Type="ST"` and include `<LocalTags>` sections.
- Some routines are `<EncodedData EncodedType="Routine" ...>` (encrypted); parser should record as encoded and skip logic extraction.

## Parser Design (M2)
- Use a single-pass streaming XML reader (XmlReader) with a state machine.
- Capture global metadata from `RSLogix5000Content` and `Controller` attributes.
- On entering a known section, route to a scoped handler:
  - `DataTypes` → UDT parser (members, dependencies)
  - `AddOnInstructionDefinitions` → AOI parser (parameters, local tags, routines)
  - `Tags` (controller/program) → Tag parser
  - `Programs`/`Program` → program parser (routines, local tags)
  - `Routines`/`Routine` → routine parser (type, logic text, local tags)
- Maintain a stack of scope (Controller → Program → Routine) for tag scope and location.
- Emit IR entities incrementally to an indexer, not stored as raw XML.
- Record `EncodedData` routines with a placeholder logic marker and `IsEncrypted=true`.
- Preserve snippets of `RLLContent` or `ST` bodies for later read/write extraction.

## Data Models / Schemas
### Normalized IR (Intermediate Representation)
- Project
  - Id, Name, SoftwareRevision, ExportDate, Language
  - Tasks[], Programs[], Routines[], AOIs[], UDTs[], Tags[], DataTypes[]
- Task
  - Id, Name, Type, Rate, Programs[]
- Program
  - Id, Name, Routines[], Tags[], AOIInstances[]
- Routine
  - Id, Name, Type (Ladder/ST/FBD), LogicText, Rungs[]
  - ReadTags[], WriteTags[], CalledAOIs[], CalledRoutines[]
- AOI
  - Id, Name, Parameters[], LocalTags[], LogicText, Routines[], Instances[]
- UDT
  - Id, Name, Members[], Dependencies[]
- Tag
  - Id, Name, DataType, Scope (Controller/Program), Dimensions, Description
- TagRef
  - TagId, TagName, Direction (Read/Write), Location (Routine/Rung/Line), SourceText
- Station
  - Id, Name, PatternRules[], Routines[], TagsIn[], TagsOut[]

### Graph Index
- Nodes: Task, Program, Routine, AOI, UDT, Tag, Station
- Edge Types:
  - Contains (Task→Program, Program→Routine, AOI→Routine)
  - Calls (Routine→Routine/AOI)
  - Reads (Routine→Tag)
  - Writes (Routine→Tag)
  - UsesType (Tag→UDT)
  - DependsOn (UDT→UDT)
  - BelongsToStation (Routine/Program→Station)
- Builder (M3):
  - Adds Program → Routine contains edges
  - Adds Routine → Tag read/write edges from extraction
  - Adds Routine → AOI calls edges with instance metadata
  - Adds Tag → UDT uses edges for controller and program tags
  - Adds UDT → UDT dependency edges

### Graph Data Structures (M3)
- GraphNode
  - Id (string), Kind (enum), Name (string)
- GraphEdge
  - Kind (enum), FromId, ToId, Metadata (optional)
- DependencyGraph
  - Nodes (dictionary by Id)
  - Edges (adjacency list by FromId)
  - Methods: AddNode, AddEdge, GetOutgoing, GetIncoming
- StationRule
  - Name, Patterns (list of regex or wildcard patterns)

## APIs / Endpoints
- Local service interfaces (in-process), no external endpoints in v1.
- Optional: export endpoints for HTML/PDF/CSV if requested.

## Dependencies
- XML parsing library (e.g., System.Xml or fast SAX-style parser for huge files)
- Graph library for traversal (custom adjacency lists or QuickGraph)
- UI toolkit (WPF + MVVM or WinUI 3)

## Security / Compliance
- Offline local processing; no data leaves workstation.
- Support redaction of tag names for export if needed.

## Risk Areas
- Performance on very large L5X files
- L5X schema variability between Studio 5000 versions
- Accurate read/write detection in complex ladder/structured text

## UX Wireframes (text)
### Global Layout
- Top app bar: Project name, import button, search, settings
- Left rail: Navigator, Stations, Trace, AOI/UDT, Impact, Learning
- Main canvas: content view with split panes as needed
- Right inspector: selected node details, tags, calls, references

### Program Navigator + Dependency Map
- Split view: left tree (Tasks > Programs > Routines > AOIs/UDTs), right graph map
- Graph toolbar: filter by station, toggle read/write edges, zoom controls
- Selection shows call chain and cross-references in inspector

### Station-Centric View
- Station list on left, summary cards in main
- Summary: inputs, outputs, key routines, AOIs, alarms
- “What changes this station” flow: inputs → logic → outputs pipeline

### Signal Trace / I/O Explorer
- Tag search and history on left
- Main: read/write locations table + forward/backward trace graph
- Toggle: ladder vs ST snippet preview

### Change Impact Lens
- Impact summary at top (tags, stations, AOIs affected)
- Downstream paths graph with severity color scale
- Export impact report button

### AOI/UDT Explainer
- AOI/UDT list with filters
- Main: description, parameters, internal tags, instances, usage locations

### Learning Mode
- Scenario selector (Start/Stop, Safety, Station Cycle)
- Step-by-step timeline with linked routines and tags

## Open Questions
- Should learning mode use deterministic templates or optional AI summaries?
- Do we need incremental re-indexing when L5X changes?
- Export format priority (PDF vs HTML vs CSV)?
