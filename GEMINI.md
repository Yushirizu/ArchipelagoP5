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
Exact order — do NOT change, do NOT add `CALL_EVENT(105, 2)` (file `E105_002.ECS` does not exist):
1. `CALL_EVENT(102, 1)`: Casino Prologue VERY FIRST (Stained glass jump, fight shadow mob).
2. `CALL_EVENT(105, 1)`: Police Interrogation Room (contains Sae dialogue).
3. `CALL_EVENT(101, 1)`: Select Difficulty & Name Input.
4. `CALL_EVENT(104, 1)`: Blue Butterfly / Velvet Room.
5. `CALL_EVENT(106, 1)`: Cinematic with Shido.
6. Transition to Day 21 (April 22) -> `WarpToLeblanc` (`CALL_FIELD(150, 2, 0, 0)`) at Yongen-Jaya / Leblanc.

---

## FlowScript / Native Engine Knowledge (learned from session)

### CALL_EVENT is Asynchronous
- `CALL_EVENT(major, minor)` in `AP_Methods.flow` queues the event and returns **immediately** to C# — it does NOT block until the event finishes.
- Never assume the event has completed when `CallCustomFlowFunction` returns.
- Do NOT call `CALL_FIELD` inside `NewGameSetupSdl` — it crashes the game (`0xFFFFFFFFFFFFFFFF` Access Violation) because the field manager context is not ready when called from inside a schedule hook.

### Safe Map Warp (CALL_FIELD) Rules
- `CALL_FIELD(150, 2, 0, 0)` = warp to Yongen-Jaya / Leblanc (Field 150, minor 2, entrance 0).
- `CALL_FIELD` MUST only be called from a dedicated `WarpToLeblanc` procedure (index 8 in `AP_Methods.flow`), invoked from C# via `FlowFunctionWrapper.CallCustomFlowFunction(CustomApMethodsIndexes.WarpToLeblanc)`.
- NEVER call `CALL_FIELD` from inside `NewGameSetupSdl` or any other procedure that is itself called while `RunScheduleForDay` is still executing. This crashes the native field manager.

### Setup Day Infinite Loop Cause & Fix
- **Cause**: `ScheduleManipulator.RunScheduleForDayImpl` fires every frame on Day 6 (April 7, `time == 4`). If `NewGameSetupSdl` returns but the date pointer (`currTotalDays`) stays at 6, the engine re-runs the setup cutscene every frame (Police → Butterfly → Police loop).
- **Fix**: After the first execution of `NewGameSetupSdl` (`_hasRunNewGameSetup = true`), subsequent setup-day calls immediately update the date pointer to Day 21 (`currTotalDays = 21`, `nextTotalDays = 21`, `currTime = 0`, `nextTime = 0`) and invoke `WarpToLeblanc`.
- **NEVER mutate `dateInfo->currTotalDays` BEFORE calling `NewGameSetupSdl`** — the setup FlowScript runs in the context of Day 6 and dereferencing a moved date pointer crashes the engine.

### Unsafe Pointer Access
- Any method in C# that dereferences `DateInfo*` or any native pointer must be declared `unsafe`.
- `RunScheduleForDayImpl` must carry the `unsafe` keyword because it accesses `DateManipulator.DateInfoAddress`.

### Missing Event Files (E105_002)
- `CALL_EVENT(105, 2)` references `EVENT\E100\E100\E105_002.ECS` which does NOT exist in the game files.
- Using `CALL_EVENT(105, 2)` produces CRI file-not-found errors and causes a freeze/loop.
- The Sae interrogation dialogue is already included inside `CALL_EVENT(105, 1)`. Do not add a second `105` call.

### Setup Completion Transition Strategy
- `_hasRunNewGameSetup` bool in `ScheduleManipulator` guards one-time execution of `NewGameSetupSdl`.
- On first setup day hit: run `NewGameSetupSdl` and return its result to the engine.
- On subsequent setup day hits (engine still on Day 6 before CALL_EVENT has advanced the date): advance `dateInfo` to Day 21 and call `WarpToLeblanc` from C#.
- `DateManipulator.IsSetupComplete = true` is set in `FirstTimeSetup.Setup()` to allow `ManipulateInGameDate` to advance beyond `SETUP_TOTAL_DAY` (6).

---

## User Preferences
- Build manually in IDE each time — do NOT auto-rebuild unless asked.
- Always use `rtk git add .` to stage all files.
- Always push to `origin/main` after commits.
- Caveman response style: terse, drop articles/filler, exact technical terms.
