# Publishing the editor in Releases

## Current readiness

The editor is a Windows WPF application targeting `net10.0-windows`. Build it from the **private game repository**, not this community scaffold. This document is a release procedure, not a statement that a standalone public distribution is ready.

The current startup searches for `GoblinStronghold.slnx`. `ContentWorkspace` expects data below `src/GoblinStronghold.Simulation/Content`, `Localization`, and `src/GoblinStronghold.Godot/AssetRecipes`; spreadsheet authoring can also require `content/authoring`. Map defaults point to `src/GoblinStronghold.Godot/Maps`. Publishing the EXE alone is insufficient.

Before the first public release, implement a standalone workspace entry point and assemble an explicitly approved authoring-data bundle. Review runtime file accesses, asset recipes, source catalogs and redistribution rights. Do not ship the private repository or a fake empty solution file as a substitute. This is a release blocker until verified on a machine without the private checkout.

## Build and package

1. Select a tested private revision and matching game version. Record source revision privately; public notes need the product version and compatibility, not a private repository link.
2. On Windows with the matching .NET 10 SDK, from the private repository root, use a fresh output directory:

   ```powershell
   dotnet publish src/GoblinStronghold.Editor/GoblinStronghold.Editor.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -p:PublishTrimmed=false -p:TutorialEditorProbe=false -p:UserMapEditorProbe=false -o artifacts/editor-release/win-x64
   ```

   The product version currently comes from `project.godot` through `Directory.Build.targets`. Keep the release tag consistent with it. Explicitly disabling probe compilation avoids shipping test entry points.
3. Stage the complete publish output, approved authoring data, user instructions, license texts and third-party notices. Inspect the staged file list; exclude private credentials, internal documents, saves and debug-only fixtures. Do not blindly zip the repository.
4. Apply the production signing process to the final distributable binaries and verify their signatures. Existing per-user development signing is not proof of public trust; do not distribute private signing material.
5. Test the staged directory on a clean supported Windows installation without an installed .NET runtime or source checkout: launch, load catalogs, create/edit/save/reopen a `.gmap`, undo/redo a brush, import a sample mod, and switch English/Polish. Check paths containing spaces and non-ASCII characters. Record actual supported OS and architecture in release notes.
6. Zip the verified directory as `GoblinStronghold-Editor-<version>-win-x64.zip`. Compute SHA-256 **after signing and packaging**:

   ```powershell
   Get-FileHash -LiteralPath 'GoblinStronghold-Editor-<version>-win-x64.zip' -Algorithm SHA256
   ```

   Replace `<version>` before running. Include the filename and lowercase hash in `SHA256SUMS.txt`.

## Draft and publish

In the public repository's Releases tab, create a **draft** with tag `editor-v<version>`, targeting the reviewed public documentation commit. Tags here describe public distribution, not the private game's Git history. Use a prerelease for preview builds.

Attach the editor ZIP, `SHA256SUMS.txt`, and applicable notices. Use [the release note template](RELEASE_TEMPLATE.md). Official maps may be separate release assets with their own versions and hashes. Never include Steam credentials or private source. Review the draft, then publish manually. No publishing workflow or upload credentials are included in this scaffold.

Download the published assets again and verify their hashes and launch behavior. Add catalog listings only for actual tested downloads. Treat released bytes as immutable: fixes receive a new version. If a release is broken, clearly mark it and point to the replacement.

GitHub reference: [managing releases](https://docs.github.com/en/repositories/releasing-projects-on-github/managing-releases-in-a-repository).
