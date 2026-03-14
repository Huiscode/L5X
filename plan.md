# Execution Plan (PLAN)

## Milestones
1. Project discovery and UX wireframes (WinUI 3, Studio 5000 v30–v35)
2. L5X parsing + IR model
3. Dependency graph + station grouping
4. Core UI views (Navigator, Station, Trace)
5. AOI/UDT docs + Impact Lens
6. Learning Mode + export
7. Performance tuning + beta readiness

## Step-by-Step (follows milestones)
### Milestone 1: Project discovery and UX wireframes
- ✅ Confirm UI stack and packaging (WinUI 3, MSIX)
- ✅ Gather sample L5X files and target Studio 5000 versions (v30–v35)
- ✅ Create UX wireframes for Navigator, Station, Trace, and Impact

### Milestone 2: L5X parsing + IR model
- ✅ Define IR schema and graph edge types
- ✅ Build streaming L5X parser and entity indexer
- ✅ Implement tag read/write extraction (ladder + ST)

### Milestone 3: Dependency graph + station grouping
- ✅ Build dependency graph (calls, reads, writes)
- ✅ Implement station grouping rules engine
- ✅ Validate graph output on sample projects

### Milestone 4: Core UI views (Navigator, Station, Trace)
- ✅ Implement Program Navigator + dependency map (shell)
- ✅ Implement Station-Centric View with summaries (shell)
- ✅ Implement Signal Trace / I/O Explorer (shell)

### Milestone 5: AOI/UDT docs + Impact Lens
- Implement AOI/UDT Explainer and usage lists
- Implement Change Impact Lens

### Milestone 6: Learning Mode + export
- Implement Learning Mode walkthrough generator
- Add export/reporting (if required)

### Milestone 7: Performance tuning + beta readiness
- Performance profiling and optimizations
- Package app for internal beta

## Task List
- [x] Confirm UI stack and packaging (WinUI 3, MSIX)
- [x] Define IR schema and graph edges
- [x] Build streaming L5X parser and entity indexer
- [x] Implement tag read/write extraction (ladder + ST)
- [x] Build dependency graph and station grouping
- [x] Implement Program Navigator + dependency map (shell)
- [x] Implement Station-Centric View with summaries (shell)
- [x] Implement Signal Trace / I/O Explorer (shell)
- [ ] Implement AOI/UDT Explainer and usage lists
- [ ] Implement Change Impact Lens
- [ ] Implement Learning Mode walkthrough generator
- [ ] Add export/reporting (if required)
- [ ] Performance profiling and optimizations

## Dependencies
- L5X schema samples for multiple Studio 5000 versions
- Decision on UI framework

## Acceptance Criteria
- Import and parse a 200MB L5X within 2 minutes on reference machine
- Dependency map is navigable and accurate for tasks/programs/routines/AOIs/UDTs
- Station definitions correctly group logic by naming patterns
- Tag trace shows read/write locations with forward/backward traversal
- AOI/UDT docs generated with instances and summary
- Change Impact Lens highlights affected tags and stations
- Learning Mode can generate at least 3 walkthrough templates
