# Canonical English source packs

The first independent English baselines are available as local exports in version **1.0.0**. They use the public transkit v1 format and are not yet installed by the current game/editor runtime. Source export is implemented; runtime language-pack selection remains separate.

| Domain | Entries | Family GUID | English edition GUID |
| --- | ---: | --- | --- |
| core | 21357 | c27a7cf8-bf61-4e8a-92d6-50d29789b0ab | e933d0e3-1620-4519-9ac3-5e3f0e418a00 |
| editor-ui | 1693 | c721c581-e486-427c-b48b-e819e5192379 | aa9daa81-7210-4299-8d13-89b9b25bb9c7 |
| game-ui | 3150 | 0454f057-8054-47ee-8703-524bfc9c00c1 | 66c6b539-910e-4fb3-86e6-1eb53b42e2ae |

Files are named `<domain>-en-1.0.0.langpack`, with corresponding `.json` files for inspection and Git-based review. They are generated assets, not game source code. No GitHub release is published by the export operation. Maintainers should attach the actual files to a release before adding download links here.

UI packs are independent. Core contains shared resource, equipment, recipe and workshop names and other content terminology. Maps, tutorials and third-party mods will have separate source families; these initial packs do not replace their existing localization formats.

## Translate

Use [transkit-tool](../tools/transkit-tool/README.md) with one of the exported source packages:

```powershell
dotnet tools/transkit-tool/Transkit/bin/Release/net10.0/transkit.dll validate --package game-ui-en-1.0.0.langpack
dotnet tools/transkit-tool/Transkit/bin/Release/net10.0/transkit.dll init --source game-ui-en-1.0.0.langpack --locale pl --author "Your team" --title "Your translation" --out pl-base.langpack --kit initial.json
```

Each author/team starts its own translation project. The exact English edition GUID is copied to `sourcePackageId`; a translation gets its own `translationId` and edition `packageId`. UI and Core source versions need not advance together.

Keys preserve the original three-part localization address: `section/subsection/key`. Within components, `~` becomes `~0` and `/` becomes `~1`. Case matters. Do not rename these identifiers in a translation.

The source producer owns all future key renames and includes their cumulative history in English metadata. The initial baseline has no invented historical renames. Use `diff` against exact old/new EN editions when an update arrives; do not manually transplant an older package's GUID to claim compatibility.

The initial exports were validated with the independent CLI, including canonical hash/GUID agreement and generation/import of XLSX transkits for all three domains. This verifies technical compatibility, not translation quality or in-game installation support.
