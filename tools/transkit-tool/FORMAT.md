# Localization format v1

## Package

A `.langpack` is a ZIP containing exactly `langpack.json`. Raw JSON input/output is also supported. Paths are never extracted by the CLI. A package is limited to 32 MiB expanded. UTF-8 JSON has unique property names and rejects unknown fields. Keys are ordinal, case-sensitive, nonempty identifiers.

Required metadata:

| Field | Meaning |
| --- | --- |
| formatVersion | 1 |
| identity | domain + stable source ID |
| familyId | Stable GUID shared by a source family and all its translations |
| packageId | Unique GUID for this exact edition |
| sourcePackageId | English edition GUID; empty GUID in EN itself |
| translationId | Stable author/project GUID; empty GUID in EN |
| author, title, description | Display metadata; target author/title must be nonempty |
| locale | EN uses `en`; targets use a language tag such as `pl` or `pt-BR` |
| sourceVersion | Three numeric components, e.g. `1.4.0` |
| revision | EN = 0; initial target = 0; subsequent target editions increment it |
| sourceHash | SHA-256 of canonical EN package JSON; empty in EN |
| entries | Map of stable keys to text, reviewed flag and context |
| renames | EN rename history: since, from, to; empty in built target packages |

Domains: `game-ui`, `editor-ui`, `core`, `mod`, `map`, `tutorial`. They must not be conflated even where text is shared. Distinct families should use distinct family GUIDs.

A target version is `sourceVersion.revision`. Source upgrades start target revision 1. Every apply generates a new package GUID and preserves translation project GUID. A repeat build of an unchanged source JSON preserves its GUID. Changing EN content requires a new source version and package GUID; never reuse an edition GUID for different bytes. The CLI checks identity/hash at update boundaries; a future registry must detect globally reused GUIDs.

Hashes are calculated over the tool's UTF-8, indented JSON serialization, with ordinal entry ordering and rename records sorted by since/from. They include package metadata. The archive compression, ZIP timestamps and input JSON whitespace do not affect the semantic hash. `sourceHash` is not a signature, and self-declared authorship is not proof of official acceptance.

## Rename history

The new English package carries cumulative rename history, for example:

```json
"renames": [
  { "since": "1.1.0", "from": "menu.play", "to": "menu.new-game" }
]
```

The key is removed under its old name and present under its new name. Migrations after the old source version are applied in version order. Multi-version rename chains are supported; missing old keys, destination collisions and duplicate declarations are errors. Avoid chained renames within one source version; record the final old-to-new mapping. Never infer renames from similar English text. If source text/context is unchanged, a reviewed translation remains reviewed after rename.

## Transkit JSON / XLSX

The transkit embeds immutable old/new source information, the exact base translation, their hashes, and explicit operations. Initial kits have no previous source. Diff operations are add/change/rename/delete; unchanged translated entries live in the base snapshot. Unreviewed old entries reappear for review.

Only proposed translation and reviewed status may change. `apply` rebuilds the expected operation plan and rejects omitted/modified operations. It requires the supplied base package's exact hash, which includes its GUID and translation lineage. Deletions occur before insertions. Input files are not modified.

XLSX stores this model in the very-hidden `_metadata` sheet and visible rows in `Translations`. Only columns F/G are unlocked. Sorting complete rows is supported; removed, duplicate or unknown rows and modified source cells are rejected. Shared-string and inline-string cells are accepted. Formulas, DTDs, oversized archives and external worksheet targets are rejected. Sheet protection prevents accidents, not malicious editing.

## Current validation limits

Technical validation checks domain/version/GUID references, exact key sets, completeness/review, argument/action placeholder token multiplicity, basic HTML/bracket markup token preservation and nesting of common formatting tags. It does not assess translation quality and is not yet the complete game rich-text/plural parser. English text and context are supplied by authors; semantic source correctness still needs source-export validation.

Experimental drafts may be incomplete only when explicitly requested. The future runtime must provide EN fallback for missing/unreviewed text and let players select a translation project. This CLI does not install packs, choose a winning translation, upload releases or mark a package official.
