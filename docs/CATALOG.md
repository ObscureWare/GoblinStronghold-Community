# Community catalog v1 (proposed)

This is a listing format for this repository, **not** the game's content-pack manifest. The current game does not automatically consume this catalog. Map collections in mods still require engine discovery support.

Keep one immutable listing per release:

```text
catalog/maps/<stable-id>/<version>.json
catalog/mods/<stable-id>/<version>.json
catalog/assets/<stable-id>/<version>.json
```

Required fields are demonstrated in `examples/`. `schemaVersion` is 1; `kind` is `map`, `mod` or `assets`; `id` is a stable author-qualified identifier. `titles` and `descriptions` contain language codes with mandatory English fallback and optional Polish/other translations. `version` is the package version, independent of game/editor versions.

`compatibility.testedGameVersions` lists exact verified versions, not an untested promise about future versions. `dependencies` lists required package IDs and exact supported versions. Empty means no additional packs. `download` holds the immutable HTTPS asset URL, filename, byte size and 64-character lowercase SHA-256 of the downloaded archive. `license` identifies and links the package's full license. `sourceUrl` identifies the author/project.

Map listings also carry `mapGuid`, matching the GUID inside `.gmap`. A map collection is a `mod` with `includedMapGuids`; it is not a new game-readable package type introduced by this document. Resource packs use `assets` and declare the same licensing and compatibility information.

Before accepting a listing:

- Download and verify size/hash; open the archive with the appropriate validator.
- Check metadata identity, dependencies, author permission and license links.
- Test loading in the declared game/editor version; record limits such as a rendering stress test or high memory use in localized descriptions.
- Keep older versions available; never replace the bytes behind an existing version.

Examples deliberately contain `example.invalid` URLs and dummy checksums; they are outside `catalog/` and must not be treated as published downloads.
