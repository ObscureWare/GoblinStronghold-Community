# Preparing the public repository

This directory is a standalone scaffold, not a nested Git repository. No remote, account or public resource has been created.

1. Copy this directory, including `.github`, into a separate working directory. Review all files before initializing its own Git history. Do not publish the private game's history.
2. Choose documentation/example and binary licenses; complete `LICENSES.md` and prepare third-party notices.
3. Create `GoblinStronghold-Community` under the chosen owner. Enable Issues, Discussions and private vulnerability reporting. Configure a private support contact.
4. Set the default branch to `main`; issue forms must be on the default branch. Create Discussions categories: Help, Ideas, Maps & Mods, Announcements.
5. Add real Discussions and support URLs to the issue chooser's `contact_links` once the owner and channels exist. No placeholder URLs are wired into the current forms.
6. Create labels from `TRIAGE.md`, choose maintainers, protect the main branch and review contributions before merging. Forms deliberately do not depend on pre-existing labels.
7. Validate form rendering with a test draft for each form. Verify public downloads from a signed-out browser and private report visibility separately.
8. Publish the first editor only after the standalone acceptance checks in `PUBLISHING_EDITOR.md` pass. Remove the README's local-scaffold notice when a real release is available.

Optional later work: catalog validation CI, signed catalog snapshots, map-pack discovery in the game, scheduled issue triage, and an explicit YouTrack integration. None is enabled by this scaffold.

GitHub documentation: [issue form syntax](https://docs.github.com/en/communities/using-templates-to-encourage-useful-issues-and-pull-requests/syntax-for-issue-forms), [template configuration](https://docs.github.com/en/communities/using-templates-to-encourage-useful-issues-and-pull-requests/configuring-issue-templates-for-your-repository).
