# Reporting and contributing

English or Polish is fine. Please keep one problem per issue and use a descriptive title.

1. Search open and closed issues for the symptom and error message. Add new evidence to an existing report when it matches.
2. Select the game, editor or package form. Include exact versions, enabled mods and minimal reproduction steps.
3. Explain expected and actual behavior. For crashes, include the relevant error and a small log excerpt.
4. Attach a minimal save or map when useful and permitted. Check attachments for personal paths, usernames, credentials and private content first. Public issues and attachments are public.
5. Test without optional mods if practical; do not destroy your original save to do so. Say when you cannot reproduce on a clean setup.

Do not post credentials, account/payment issues or security exploit details publicly. Use the repository's private vulnerability reporting channel when enabled; see [security policy](SECURITY.md). Maintainers must configure a private support channel before launch.

Suggestions and questions belong in Discussions. Bugs in third-party packs normally belong with their author; report suspected engine defects here with the pack version and a minimal example.

## Catalog contributions

Submit a pull request adding one versioned listing under `catalog/<kind>/<stable-id>/<version>.json`. Follow [the catalog contract](docs/CATALOG.md) and copy an example from `examples/`. Include a working download, checksum, author permission and compatibility evidence. Do not upload game binaries, private source or assets you cannot redistribute.

Existing stable IDs must not change. A changed archive requires a new version and checksum. Maintainers review listings; submission does not automatically publish or execute a package.

Be constructive and discuss behavior rather than people. No harassment, spam or impersonation. Maintainers may hide abusive content and restrict participation. Duplicate bug reports are linked and closed, not deleted merely for being duplicates.
