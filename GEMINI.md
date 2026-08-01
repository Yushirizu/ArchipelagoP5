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
Official sequence written down and enforced in `NewGameSetupSdl`:
1. `CALL_EVENT(102, 1)`: Casino Prologue (Very First).
2. `CALL_EVENT(105, 1)`: Police Interrogation.
3. `CALL_EVENT(101, 1)`: Select Difficulty & Name Input.
4. `CALL_EVENT(105, 1)`: Sae Interrogation Dialogue.
5. `CALL_EVENT(104, 1)`: Blue Butterfly / Velvet Room.
6. `CALL_EVENT(106, 1)`: Cinematic with Shido.
7. `CALL_EVENT(107, 1)`: Metro station / Ginza line transfer.
8. Transition to Day 21 (April 22 Leblanc free-roam).

DO NOT run `FirstTimeSetup.Setup()` before `NewGameSetupSdl` completes! `FirstTimeSetup` mutates bit `6144` (`DbgScript_150_000`) and story count `145` (`40100`), which causes native P5R to skip the intro cutscenes and jump to Sojiro (`104`) mid-sequence. `FirstTimeSetup` MUST be deferred to the Day 21 schedule transition.

---

## FlowScript / Native Engine Knowledge (learned from session)

### CALL_EVENT is Asynchronous
- `CALL_EVENT(major, minor)` in `AP_Methods.flow` queues the event and returns **immediately** to C# — it does NOT block until the event finishes.
- Never queue multiple story `CALL_EVENT` calls in one procedure — native event scripts chain themselves (`102` → `105` → `101` → `104` → `106` → `107`). Stacking `CALL_EVENT` calls in one procedure causes event collision and out-of-order execution.
- Do NOT call `CALL_FIELD` inside `NewGameSetupSdl` — it crashes the game (`0xFFFFFFFFFFFFFFFF` Access Violation) because the field manager context is not ready when called from inside a schedule hook.

### Safe Map Warp & Schedule Transition Rules
- NEVER call `CallCustomFlowFunction(CustomApMethodsIndexes.WarpToLeblanc)` (or any procedure executing `CALL_FIELD`) inside `RunScheduleForDayImpl` hook or `NewGameSetupSdl`. Calling `CALL_FIELD` while inside a schedule execution context dereferences invalid/null field manager memory, causing `0xFFFFFFFFFFFFFFFF` Access Violation crash.
- To transition to Day 21 (April 22 Leblanc), update `dateInfo` (`currTotalDays = 21`, `nextTotalDays = 21`, `currTime = 0`, `nextTime = 0`), set `newMonth = 4; newDay = 22;`, and pass them to `_runScheduleForDayHook.OriginalFunction(newMonth, newDay, time)`. Native schedule manager will natively load April 22 schedule at Leblanc cleanly.

### Setup Completion Transition Strategy
- `_hasRunNewGameSetup` bool in `ScheduleManipulator` guards one-time execution of `NewGameSetupSdl`.
- On initial setup day hit (4/7 time 4): run `NewGameSetupSdl` (`CALL_EVENT(102, 1)`) and return its result to engine to kick off the native intro sequence (`102` Casino -> `105` Police -> `101` Difficulty/Name -> `105` Sae -> `104` Velvet -> `106` Shido -> `107` Metro).
- During subsequent 4/7 hits: allow native engine to execute cutscenes naturally without intercepting or jumping dates.
- On schedule advance past Day 7 (`month == 4 && day > 7 && day < 22`): update `dateInfo` to Day 21 (April 22), pass `(4, 22)` to native `OriginalFunction`, and defer `FirstTimeSetup.Setup()` asynchronously via `Task.Run` with 200ms delay.
- **CRITICAL**: Never invoke `FirstTimeSetup.Setup()` synchronously inside `RunScheduleForDayImpl` hook! Mutating bit flags (`6144`) and story progress counter `145` (`40100`) while inside a native schedule hook callback invalidates schedule manager memory and triggers an immediate `0xFFFFFFFFFFFFFFFF` native access violation crash. Deferring execution asynchronously allows native schedule manager to return cleanly.

---

## User Preferences
- Build manually in IDE each time — do NOT auto-rebuild unless asked.
- Always use `rtk git add .` to stage all files.
- Always push to `origin/main` after commits.
- Caveman response style: terse, drop articles/filler, exact technical terms.
