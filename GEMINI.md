# GEMINI.md - Project Rules & RTK Proxy Instructions

## RTK (Rust Token Killer) Usage Directive

All CLI terminal commands in this repository MUST be executed using `rtk` (Rust Token Killer) or proxy wrappers to optimize token efficiency and save context window overhead.

### RTK Proxy Rules
1. **Primary Tool Execution**: Prefix terminal commands with `rtk` (e.g., `rtk git status`, `rtk dotnet build`, `rtk cargo`).
2. **Meta Commands**:
   - `rtk gain`: Analytics on token savings.
   - `rtk gain --history`: View command history and savings metrics.
   - `rtk discover`: Analyze command patterns.
   - `rtk proxy <cmd>`: Run raw command without output filtering when full unformatted output is explicitly required for debugging.
3. **Verification**: Always verify `rtk --version` when initializing new environments.

### Project Architecture & Rules
- **Target Project**: Persona 5 Royal (v1.0.4.0) Archipelago Randomizer Mod (`ArchipelagoP5RMod`).
- **Build System**: .NET 8 C# with Reloaded-II Mod Loader Framework.
- **Native Interop Rules**:
  - Memory layouts in `GameTypes/` (such as `FlowCommandData`) must match native C/C++ struct field alignment (e.g., `fixed byte` for 1-byte char arrays).
  - Bit flag mutations should call native direct engine functions (`DirectSetBit` at `0x1405C1730`) instead of flowscript VM opcode handlers to prevent out-of-context execution crashes.
  - Disk logging must force immediate unbuffered flushing (`sw.Flush()`, `fs.Flush(true)`) to prevent log data loss on native crashes.
  - **Script Organization**: All Python diagnostic/analysis scripts must be created in `scripts/` directory for reuse.
  - **Git Operations**: `rtk git add .` is authorized to stage all modified and untracked repository files when committing.
- **New Game Intro Event Sequence (`AP_Methods.flow` / `NewGameSetupSdl`)**:
  1. `CALL_EVENT(102, 1)`: Casino Prologue VERY FIRST (Stained glass jump, fight shadow mob).
  2. `CALL_EVENT(105, 1)`: Police Interrogation Room.
  3. `CALL_EVENT(101, 1)`: Select Difficulty & Name Input.
  4. `CALL_EVENT(105, 2)`: Sae Interrogation Dialogue.
  5. `CALL_EVENT(104, 1)`: Blue Butterfly / Velvet Room.
  6. `CALL_EVENT(106, 1)`: Cinematic with Shido.
  7. Transition to Day 21 (April 22) -> `WarpToLeblanc` (`CALL_FIELD(150, 2, 0, 0)`) at Yongen-Jaya / Leblanc.

