# transkit-tool

Independent .NET 10 CLI for community localization. No game checkout, game assemblies, NuGet packages, macros or network service is required. The v1 format is a **new public tool contract**; loading it in the game/editor is a separate roadmap task.

[Roadmap (PL)](../../docs/transkit-roadmap.md) · [Format](FORMAT.md)

## Build and run

```powershell
dotnet build tools/transkit-tool/Transkit -c Release
dotnet tools/transkit-tool/Transkit/bin/Release/net10.0/transkit.dll --help
dotnet build tools/transkit-tool/Transkit.Tests
dotnet tools/transkit-tool/Transkit.Tests/bin/Debug/net10.0/Transkit.Tests.dll
```

Use the built DLL with `dotnet` on any supported .NET 10 platform, or publish a platform-specific executable. Corporate application-control policies may require local signing of built binaries; no private certificate or game signing configuration is part of this project.

## First translation

Run from the repository root; use fresh output names:

```powershell
dotnet tools/transkit-tool/Transkit/bin/Release/net10.0/transkit.dll pack --input tools/transkit-tool/examples/en-1.0.0.json --out artifacts/transkit/en-1.0.0.langpack
dotnet tools/transkit-tool/Transkit/bin/Release/net10.0/transkit.dll init --source artifacts/transkit/en-1.0.0.langpack --locale pl --author "Example team" --title "Polski" --out artifacts/transkit/pl-base.langpack --kit artifacts/transkit/initial.json
```

This creates an incomplete base package plus `initial.json` and `initial.xlsx`. Edit **Translation** and **Reviewed** (`true`/`false`) in XLSX. All other columns describe the immutable operation plan. Source cells are protected against accidents; validation is independent of that protection.

```powershell
dotnet tools/transkit-tool/Transkit/bin/Release/net10.0/transkit.dll validate --kit artifacts/transkit/initial.xlsx
dotnet tools/transkit-tool/Transkit/bin/Release/net10.0/transkit.dll export --kit artifacts/transkit/initial.xlsx --out artifacts/transkit/translated.json
dotnet tools/transkit-tool/Transkit/bin/Release/net10.0/transkit.dll apply --base artifacts/transkit/pl-base.langpack --kit artifacts/transkit/initial.xlsx --out artifacts/transkit/pl-1.0.0.1.langpack
```

`validate --kit` checks structure and reviewed rows; it does **not** assert completeness. `apply` requires complete reviewed translations by default. Use `--allow-incomplete` explicitly for an experimental draft. Nonempty translations must preserve source tokens even in draft mode.

JSON and XLSX are two representations of one transkit, not automatically synchronized files. Choose one to edit, then `export` it to a **new** JSON/XLSX filename for review. Do not independently edit both and assume changes will merge. In JSON only change `changes[].translation` and `changes[].reviewed`; changes to source/base/operation structure are rejected.

## Updating

```powershell
dotnet tools/transkit-tool/Transkit/bin/Release/net10.0/transkit.dll pack --input tools/transkit-tool/examples/en-1.1.0.json --out artifacts/transkit/en-1.1.0.langpack
dotnet tools/transkit-tool/Transkit/bin/Release/net10.0/transkit.dll diff --from artifacts/transkit/en-1.0.0.langpack --to artifacts/transkit/en-1.1.0.langpack --base artifacts/transkit/pl-1.0.0.1.langpack --kit artifacts/transkit/update.json
# Translate update.xlsx, then:
dotnet tools/transkit-tool/Transkit/bin/Release/net10.0/transkit.dll apply --base artifacts/transkit/pl-1.0.0.1.langpack --kit artifacts/transkit/update.xlsx --out artifacts/transkit/pl-1.1.0.1.langpack
dotnet tools/transkit-tool/Transkit/bin/Release/net10.0/transkit.dll validate --package artifacts/transkit/pl-1.1.0.1.langpack --source artifacts/transkit/en-1.1.0.langpack
dotnet tools/transkit-tool/Transkit/bin/Release/net10.0/transkit.dll status --package artifacts/transkit/pl-1.1.0.1.langpack
```

Declared renames preserve translations; source/context changes require review. Removed keys disappear from output but remain in the transkit's audit information. The exact base package GUID/hash must match. Local corrections or a competing author's package are conflicts, not an invitation to silently overwrite.

Another `init` creates a distinct translation project (`translationId`) even for the same English source and locale. Updates retain that project ID but create a new edition GUID. The future game's selector can use project ID, author and title to offer competing translations.

Exit codes: 0 = success, 2 = invalid input, validation failure, conflict or I/O failure. Existing output files are never overwritten.

XLSX uses plain OpenXML with hidden provenance metadata; formulas are rejected on import. Text over the XLSX cell limit (32767 characters) must use JSON. Real Excel/LibreOffice save compatibility remains an explicit acceptance task; current automated checks cover XML edits and package roundtrips.
