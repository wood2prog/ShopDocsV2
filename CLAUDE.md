# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

ShopDocsV2 is a Windows Forms desktop app for a cabinet shop to capture per-room job specs (cabinets, countertops, sinks, appliances, etc.), print/export them, and export a `.ordx` XML interchange file for the shop's CV cabinet-design software. It is a WinForms rebuild of an earlier web/Electron-style app ("the original app") — several places in the code call out behavior intentionally ported or intentionally changed from that original; read those comments before changing formatting/export logic, since they encode product decisions, not just implementation notes.

## Commands

Build and test from the repo root (`ShopDocsV2.slnx`):

```
dotnet build
dotnet test
dotnet test --filter "FullyQualifiedName~OrdxExportServiceTests"   # single test class
dotnet run --project src/ShopDocsV2.WinForms
```

There is one test project, `test/ShopDocsV2.Application.Tests` (xUnit), covering `ShopDocsV2.Application` and `ShopDocsV2.Infrastructure`. WinForms UI code has no automated tests.

## Architecture

Four projects, clean-architecture style, referenced in one direction only (WinForms → Infrastructure/Application → Domain):

- **`ShopDocsV2.Domain`** — plain POCOs, no dependencies: `Job` (has `List<Room>`), `Room` (answers keyed by `QuestionDef.Id`), `QuestionSet`/`QuestionDef` (the dynamic form schema), `Catalog*` types (dropdown option sources). `AnswerValue` is a tagged union (Text/Number/Bool) with `DisplayText`/`IsBlank` helpers used throughout formatting and export.
- **`ShopDocsV2.Application`** — interfaces + the implementations that are pure logic (no I/O): `ISpecFormattingService`/`SpecFormattingService` (ports the original app's `isAnswered`/`getRoomSpecSections`/`buildRoomText` — the single source of truth for "what counts as answered" and section rendering, shared by print, clipboard copy, and text export), `IJobPrintContentBuilder`, `IOrdxExportService`/`OrdxExportService`. Interfaces for repositories and the question-set/paint-lookup providers live here; their implementations live in Infrastructure.
- **`ShopDocsV2.Infrastructure`** — SQLite persistence (`Sqlite/`: `SqliteConnectionFactory` — database lives in `Documents\ShopDocsV2` (alongside `window.json` and `settings.json`) via `Files/AppDataPaths`, which copies it over from the pre-1.0.11 `%LocalAppData%\ShopDocsV2` location on first run (and moves a pre-1.0.28 `settings.json` from there), `SchemaInitializer` which runs the embedded `Schema.sql` and seeds `SeedData/catalog.seed.json` on first run, `JobRepository`, `CatalogRepository`, all via Dapper), the `questions.json` loader (`Files/QuestionSetFileProvider` — remembers the last explicitly-picked path in `settings.json` in the data folder so a custom question-set file survives restarts), and `Apis/CompositePaintColorClient` (one-shot hex color lookup against compositepaint.com, used only when the user clicks "Lookup Hex" in the Catalog Manager — not per-keystroke like the original app).
- **`ShopDocsV2.WinForms`** — the UI shell. `Program.cs` wires DI manually with `Microsoft.Extensions.DependencyInjection` (no ASP.NET host) and runs `SchemaInitializer.Initialize()` before showing `MainForm`. `MainForm` owns the open `Job`, a 1s debounce autosave timer (flushed on `FormClosing` before the window is allowed to actually close — see the comment on `MainForm_FormClosing`), and menu actions for print/export. `RoomsTabControl` hosts one tab per `Room`, dynamically building fields from the loaded `QuestionSet` via `RoomFormBuilder`. `CatalogManagerForm` edits the SQLite-backed catalog lists live (edits are committed immediately by its grids, not batched).

## Data flow specifics worth knowing before changing things

- The room/job form is **schema-driven**: `questions.json` (shipped next to the exe, copied via `CopyToOutputDirectory`) defines sections → questions → (for `list`-type questions) item fields. `QuestionDef.CatalogSource` points a dropdown at a catalog table (`materials`/`finishes`/`countertops`/`pulls`/`hardware_colors`/`hinges`/`guides`); `CatalogFilterBy` narrows it by a sibling field's value (only `countertops.color` filtered by `material` today). Changing question behavior usually means editing `questions.json`, not C# — the UI (`RoomFormBuilder`) interprets the schema generically.
- `CatalogCountertopColor.MaterialName` and the `CatalogList.Materials` catalog are unrelated concepts that happen to share the word "material" — see the doc comment on `CatalogCountertopColor` in `Catalog.cs`.
- `ISpecFormattingService.IsAnswered` has type-specific rules (checkbox: only explicit `true` counts; list: at least one item with at least one non-blank field) — reuse it rather than re-deriving "is this filled in" logic elsewhere.
- `JobRepository.SaveJobAsync` is a full upsert of the whole job graph (job + rooms + answers + list items) and is used by both autosave and explicit Save — there's no separate partial-update path.
- New Job does **not** write to the database: `MainForm` holds it unpersisted and `SaveCurrentJobAsync` inserts it (`CreateJobAsync`) on the first save where `Job.HasContent` is true, so abandoned blank jobs never show up in the job list. `SaveJobAsync` only UPDATEs the job row, so it must not be called for an unpersisted job.
