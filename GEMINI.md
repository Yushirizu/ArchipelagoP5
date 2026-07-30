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

### New Game Intro Event Sequence (`AP_Methods.flow` / `NewGameSetupSdl`)
Entry point: `CALL_EVENT(102, 1)` in `NewGameSetupSdl`.
Vanilla P5R event scripting naturally chains through:
1. `CALL_EVENT(102, 1)`: Casino Prologue.
2. `E105_001`: Police Interrogation / Sae.
3. `E101_001`: Select Difficulty & Name Input.
4. `E104_001`: Blue Butterfly / Velvet Room.
5. `E106_001`: Cinematic with Shido.
6. Transition to Day 21 (April 22) -> `WarpToLeblanc` (`CALL_FIELD(150, 2, 0, 0)`) at Yongen-Jaya / Leblanc.

DO NOT stack multiple `CALL_EVENT` calls in `NewGameSetupSdl`! `CALL_EVENT` is asynchronous and queues events on top of each other, causing out-of-order execution (Sae -> Sojiro -> Police -> infinite loading).

---

## FlowScript / Native Engine Knowledge (learned from session)

### CALL_EVENT is Asynchronous
- `CALL_EVENT(major, minor)` in `AP_Methods.flow` queues the event and returns **immediately** to C# — it does NOT block until the event finishes.
- Never queue multiple story `CALL_EVENT` calls in one procedure — native event scripts chain themselves. Stacking `CALL_EVENT` calls causes event collision and infinite loading freezes.
- Do NOT call `CALL_FIELD` inside `NewGameSetupSdl` — it crashes the game (`0xFFFFFFFFFFFFFFFF` Access Violation) because the field manager context is not ready when called from inside a schedule hook.

### Safe Map Warp & Schedule Transition Rules
- NEVER call `CallCustomFlowFunction(CustomApMethodsIndexes.WarpToLeblanc)` (or any procedure executing `CALL_FIELD`) inside `RunScheduleForDayImpl` hook or `NewGameSetupSdl`. Calling `CALL_FIELD` while inside a schedule execution context dereferences invalid/null field manager memory, causing `0xFFFFFFFFFFFFFFFF` Access Violation crash.
- To transition to Day 21 (April 22 Leblanc), update `dateInfo` (`currTotalDays = 21`, `nextTotalDays = 21`, `currTime = 0`, `nextTime = 0`), set `newMonth = 4; newDay = 22;`, and pass them to `_runScheduleForDayHook.OriginalFunction(newMonth, newDay, time)`. Native schedule manager will natively load April 22 schedule at Leblanc cleanly.

### Setup Completion Transition Strategy
- `_hasRunNewGameSetup` bool in `ScheduleManipulator` guards one-time execution of `NewGameSetupSdl`.
- On setup day hit (4/7 time 4): update `dateInfo` to Day 21 (April 22), run `FirstTimeSetup.Setup()` (`IsSetupComplete = true`), and execute `NewGameSetupSdl`.
- Updating `dateInfo` to 21 when `NewGameSetupSdl` runs prevents `DateManipulator` from resetting `nextTotalDays` back to 6 during the 4/9 Metaverse App sequence, preventing desync native access violations.
- On subsequent setup day hits (4/7 time 5+): update `dateInfo` to Day 21 (April 22) and pass `(4, 22)` to native `OriginalFunction`.

---

## User Preferences
- Build manually in IDE each time — do NOT auto-rebuild unless asked.
- Always use `rtk git add .` to stage all files.
- Always push to `origin/main` after commits.
- Caveman response style: terse, drop articles/filler, exact technical terms.
