# Status Log

## 2026-03-14
- Locked WinUI 3 + Studio 5000 v30–v35 scope.
- Added UX wireframes to spec.
- Parser test (UN01_FPP_1_Program.L5X): DataTypes=234, Programs=118, Program Tags=5430, Routines=696, Controller Tags=0 (Program export).
- AOI parse test: 0 AOIs in sample (as reported by CLI).
- Read/write extraction test: Reads=20177, Writes=10395 (UN01_FPP_1_Program.L5X).
- AOI call detection test: AOI calls=0 (UN01_FPP_1_Program.L5X).

## Current Focus
- Milestone 2 parsing: tag read/write extraction and AOI coverage.

## Next Steps
- Validate AOI parsing against a sample with AOIs.
- Improve ladder (RLL) extraction coverage beyond XIC/XIO/OTE/OTL/OTU/MOV.

## Fix Notes (Programs/Routines were zero)
- Root cause: parsing within `<Controller>` was skipping `<Programs>` when using `ReadSubtree` + `Skip`.
- Fix: parse `<Programs>` directly with the main `XmlReader` (no subtree) and allow direct `<Program>` elements under `<Controller>`.
- Added a fallback scan for `<Program>` elements if no programs are found (safety net for program-targeted exports).

## Risks / Blocks
- L5X schema variability could slow parser development.
