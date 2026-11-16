# GoblinStronghold Community

Community hub for Goblin Stronghold: editor releases, maps, mods, documentation and bug reports.

**Local scaffold — no public release is available from this repository yet.**
English and Polish reports are welcome. Zgłoszenia po polsku są mile widziane.

- Download published tools and official packs from the repository's **Releases** tab. Choose the named release asset, not GitHub's source-code archive.
- Report reproducible bugs through **Issues** after reading [reporting guidelines](CONTRIBUTING.md).
- Use **Discussions** for questions, ideas and showcasing community creations (once enabled).
- Browse [maps](catalog/maps/README.md), [mods](catalog/mods/README.md) and [asset packs](catalog/assets/README.md).

This public repository does not contain the game's private source code. A listing is not an endorsement or a promise of compatibility. Each package declares its author, license, dependencies and tested game versions.

## Layout

```text
.github/ISSUE_TEMPLATE/   Game, editor and package issue forms
catalog/maps/            Published map listings
catalog/mods/            Published mod listings, including future map collections
catalog/assets/          Published asset-pack listings
examples/                Unpublished catalog entry examples
docs/                    Catalog, release and moderation procedures
```

Archives belong in Releases or the author's release hosting, not Git history. The catalog is currently documentation; it is not an in-game downloader or an implemented mod manifest format.

Maintainers: see [initial setup](docs/SETUP.md), [editor publishing](docs/PUBLISHING_EDITOR.md), [triage](docs/TRIAGE.md) and [licensing](LICENSES.md).

## Translation tools

[transkit-tool CLI](tools/transkit-tool/README.md) provides versioned EN/translation packages, XLSX and JSON transkits, migration and validation. See the [localization roadmap](docs/transkit-roadmap.md). This is an independent tool; game/editor integration is planned separately.
