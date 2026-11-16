# Publishing the standalone editor

The Windows x64 editor distribution includes the .NET runtime, versioned authoring data, source workbook and schema, localization, recipe definitions and their source artwork, sample maps and tutorials. Users extract the full archive and run `GoblinStronghold.Editor.exe`; no private checkout, SDK or Godot installation is needed for authoring.

See [the user guide](EDITOR_USER_GUIDE.md) for installation, supported features and offline diagnostics.

## Build and verify (maintainers)

Build sources and packaging tooling remain in the development repository. Do not copy their history, signing settings or credentials into this community repository.

1. Select a tested editor/game version. Run `tools/pack-editor.ps1` from the development checkout with a fresh output directory. For a public release use `-RequireReleaseSigning`; local development candidates are explicitly marked in the distribution manifest.
2. The packer builds a self-contained Release application, signs the final published apphost, includes approved data and runtime notices, and creates a ZIP, SHA-256 sidecar and payload manifest. Never zip the entire checkout or publish only the EXE.
3. Run `tools/test-editor-distribution.ps1 -Archive <zip>`. It extracts the exact ZIP outside the checkout, verifies hashes and runs the included offline diagnostic. Retain its report and screenshots as release evidence; do not publish local-machine paths from reports.
4. Also test on a clean supported Windows machine without an SDK/runtime or source checkout. Verify map editing and brushes, undo/redo, save/reopen, content-pack editing, tutorial export and English/Polish UI. An outside-checkout test on a development machine is not a clean-OS test.
5. Review redistribution rights and all license notices. Verify the final binary signatures and exact archive hash. A Development signing profile does not establish public trust.

## Current scope

Standalone authoring includes content catalogs, supported mod-pack editing, map generation/editing/previews, undo/redo and tutorial editing/export. Developer Godot artwork rendering/atlas baking and launching live tutorial gameplay from the editor are unavailable in this edition. Export a package and use a compatible game for playtesting. Do not advertise the standalone edition as containing the game runtime.

## GitHub release

Create a draft release tagged `editor-v<version>` against the reviewed public documentation commit. Use a prerelease for development/preview versions. These tags track public distributions, not private source history.

Attach:

- `GoblinStronghold-Editor-<version>-win-x64.zip`;
- the matching `.sha256` sidecar;
- additional license notices if required beyond those included in the ZIP.

Use [the release template](RELEASE_TEMPLATE.md), stating exact game compatibility, tested Windows versions and current limitations. Verify all draft assets, then publish manually. No upload credentials or automatic publishing workflow are stored here.

Download the published archive and verify its checksum again. Never replace the bytes of an existing released version; issue a new version for fixes. Add catalog entries only for real, tested public downloads. Official map packs may be published independently with their own versions and hashes.

Reference: [GitHub release management](https://docs.github.com/en/repositories/releasing-projects-on-github/managing-releases-in-a-repository).