# ShotLens Windows Repository Migration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Move all Windows source history, versions, releases, tags, and build automation into `qcsidios/ShotLens-Windows`, then restore `qcsidios/ShotLens` to the macOS `v0.8.7` state.

**Architecture:** Create a Windows-only repository by filtering the current linear history down to Windows paths. Preserve the historical Windows code states, add version/repository-only release commits for `v0.1.0–v0.1.5`, and rebuild every installer in the new repository before deleting anything from the old repository.

**Tech Stack:** Git, GitHub CLI, GitHub Actions, .NET 8, WPF, Inno Setup, Bash/PowerShell.

---

### Task 1: Create complete migration backups

**Files:**
- Create: `/Users/chenyilin/Vibe Coding/backups/ShotLens-2026-06-29.bundle`
- Create: `/Users/chenyilin/Vibe Coding/backups/ShotLens-releases-2026-06-29.json`
- Create: `/Users/chenyilin/Vibe Coding/backups/ShotLens-Windows-assets-2026-06-29/`

- [ ] Create the backup directory.
- [ ] Export all local branches and tags to a Git Bundle.
- [ ] Export the original GitHub Release JSON.
- [ ] Download Windows Setup.exe assets from `v0.8.7–v0.8.12`.
- [ ] Create a SHA-256 manifest for downloaded assets.
- [ ] Verify the Git Bundle and all six files.

Expected: six installers, one checksum manifest, one Release JSON file, and a valid Git Bundle exist before any remote mutation.

### Task 2: Create the filtered local Windows repository

**Files:**
- Create repository: `/Users/chenyilin/Vibe Coding/ShotLens-Windows`
- Source paths: `ShotLens.Windows/`, `.github/workflows/windows-release.yml`, `scripts/build-windows.ps1`, `VERSION`

- [ ] Clone the current local `main` into `ShotLens-Windows` without tags.
- [ ] Filter history to the Windows paths only and prune empty commits.
- [ ] Verify the first retained commit is `feat: add Windows ShotLens build`.
- [ ] Verify the requirements and repository split documents are retained.
- [ ] Record filtered commit IDs for the six historical source commits by commit subject.

Expected: the new repository history contains Windows commits only and has no Swift, Xcode, DMG, or macOS source files.

### Task 3: Create remapped historical Windows tags

**Version map:**

| Source subject | New tag |
| --- | --- |
| `fix: use default Inno Setup language` | `v0.1.0` |
| `fix: include Windows installer icon` | `v0.1.1` |
| `fix: import IO for Windows smoke check` | `v0.1.2` |
| `fix: avoid blocking window smoke in CI` | `v0.1.3` |
| `fix: exit window smoke deterministically` | `v0.1.4` |
| `fix: disambiguate WPF shutdown` | `v0.1.5` |

- [ ] For each source commit, create a detached release adaptation commit.
- [ ] Replace only the matching old version with the new version in tracked text files.
- [ ] Replace `qcsidios/ShotLens` repository URLs with `qcsidios/ShotLens-Windows`.
- [ ] Create the corresponding annotated `v0.1.x` tag.
- [ ] Verify each tagged tree has the expected `VERSION`, updater URL, installer URL, and build workflow.

Expected: six tags exist locally; each contains unchanged historical business code plus only version/repository wiring changes.

### Task 4: Adapt the current Windows main branch to an independent repository

**Files:**
- Move: `ShotLens.Windows/src/` → `src/`
- Move: `ShotLens.Windows/tests/` → `tests/`
- Move: `ShotLens.Windows/installer/` → `installer/`
- Move: `ShotLens.Windows/Directory.Build.props` → `Directory.Build.props`
- Move: `ShotLens.Windows/REQUIREMENTS.md` → `REQUIREMENTS.md`
- Move: `ShotLens.Windows/REPOSITORY_SPLIT.md` → `REPOSITORY_SPLIT.md`
- Move: `ShotLens.Windows/MIGRATION_PLAN.md` → `MIGRATION_PLAN.md`
- Modify: `scripts/build-windows.ps1`
- Modify: `.github/workflows/windows-release.yml`
- Create: `README.md`
- Create: `.gitignore`
- Create: `LICENSE`
- Modify: `VERSION`

- [ ] Move Windows project contents to the repository root.
- [ ] Update build paths for the root layout.
- [ ] Update `Directory.Build.props` to read root `VERSION`.
- [ ] Set current version to `v0.1.5`.
- [ ] Point updater, installer, support, and release links to the new repository.
- [ ] Rewrite README as Windows-only documentation.
- [ ] Update requirements to make `v0.2.0` the next development version.
- [ ] Add MIT license and Windows-appropriate ignore rules.
- [ ] Verify no macOS path or old repository URL remains.
- [ ] Commit the independent repository adaptation.

Expected: current `main` is a standalone Windows repository at `v0.1.5`; no new product functionality is added.

### Task 5: Create and push the new GitHub repository

- [ ] Create public repository `qcsidios/ShotLens-Windows`.
- [ ] Set the local Windows `origin` to the new repository.
- [ ] Push `main`.
- [ ] Push `v0.1.0–v0.1.5`.
- [ ] Verify the default branch, visibility, description, source tree, and tags.

Expected: the new public GitHub repository exists and contains the filtered Windows history.

### Task 6: Rebuild and publish historical Windows releases

**Release assets:**

- `ShotLens-Windows-v0.1.0-Setup.exe`
- `ShotLens-Windows-v0.1.1-Setup.exe`
- `ShotLens-Windows-v0.1.2-Setup.exe`
- `ShotLens-Windows-v0.1.3-Setup.exe`
- `ShotLens-Windows-v0.1.4-Setup.exe`
- `ShotLens-Windows-v0.1.5-Setup.exe`

- [ ] Wait for the six tag-triggered Windows Release workflows.
- [ ] If any run fails, diagnose only repository/version wiring and rerun; do not modify old product behavior.
- [ ] Edit each Release body to contain only that iteration’s changes.
- [ ] Verify each Release has exactly one correctly named installer.
- [ ] Verify each installer is non-empty and record its size.
- [ ] Verify the new repository updater endpoint can discover `v0.1.5`.

Expected: six successful releases exist in order and all installers are downloadable.

### Task 7: Update workspace metadata

**Files:**
- Modify: `/Users/chenyilin/Vibe Coding/AGENTS.md`

- [ ] Add `ShotLens-Windows/` to the workspace directory structure.
- [ ] Change the `ShotLens/` description to macOS-only.
- [ ] Verify both local repositories are listed once.

### Task 8: Restore and clean the macOS repository

**Remote repository:** `qcsidios/ShotLens`

- [ ] Re-check that Task 6 is fully complete.
- [ ] Force-push commit `ea0fdc6` to remote `main` using `--force-with-lease`.
- [ ] Remove the Windows installer asset from the retained `v0.8.7` Release.
- [ ] Delete Releases `v0.8.8–v0.8.12`.
- [ ] Delete tags `v0.8.8–v0.8.12`.
- [ ] Delete remote branch `codex/windows-version`.
- [ ] Delete all `Windows Release` workflow run records from the old repository.
- [ ] Reset the local macOS repository to the restored remote `main`.

Expected: the old repository’s latest branch, tag, and Release are macOS `v0.8.7`, and the retained Release contains only the DMG.

### Task 9: Final verification

- [ ] Verify local repository roots and remotes are different.
- [ ] Verify local macOS HEAD equals `ea0fdc6`.
- [ ] Verify local Windows HEAD has `VERSION=v0.1.5`.
- [ ] Verify the new repository has only Windows files and six remapped tags/releases.
- [ ] Verify the old repository has no Windows assets, Windows-only tags/releases, Windows workflow, or Windows branch.
- [ ] Verify backup files remain intact.
- [ ] Run `git status --short` in both repositories and require clean output.
- [ ] Report URLs, final versions, release counts, and backup location.

Expected: all migration acceptance criteria in `REPOSITORY_SPLIT.md` pass.
