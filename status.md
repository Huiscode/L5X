# Status Log

## 2026-03-14
- Locked WinUI 3 + Studio 5000 v30–v35 scope.
- Added UX wireframes to spec.
- Parser test (UN01_FPP_1_Program.L5X): DataTypes=234, Programs=118, Program Tags=5430, Routines=696, Controller Tags=0 (Program export).
- AOI parse test: 0 AOIs in sample (as reported by CLI).
- Read/write extraction test: Reads=20177, Writes=10395 (UN01_FPP_1_Program.L5X).
- AOI call detection test: AOI calls=0 (UN01_FPP_1_Program.L5X).
- Graph validation (M3): Nodes=20919, Edges=36193; Node types Program=118, Routine=660, Station=2, Tag=19864, Udt=275; Edge types Contains=696, DependsOn=311, Reads=20177, UsesType=4614, Writes=10395.
- Station rules test: BelongsToStation edges=0 (patterns did not match sample naming).

## Current Focus
- Milestone 4 complete; ready to begin AOI/UDT docs and Impact views (M5).

## Next Steps
- Start M5: wire AOI/UDT explorer to parsed models.
- Start Impact Lens data scaffolding.

## Fix Notes (Programs/Routines were zero)
- Root cause: parsing within `<Controller>` was skipping `<Programs>` when using `ReadSubtree` + `Skip`.
- Fix: parse `<Programs>` directly with the main `XmlReader` (no subtree) and allow direct `<Program>` elements under `<Controller>`.
- Added a fallback scan for `<Program>` elements if no programs are found (safety net for program-targeted exports).

## Risks / Blocks
- L5X schema variability could slow parser development.
