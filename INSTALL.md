# Installing the skill in Claude Code

The skill is the folder `skills/migrate-to-awesome-assertions/` (a `SKILL.md` plus its
`references/` directory). Installing means putting that folder where Claude Code looks
for skills. Claude discovers it automatically at the next session start — no
configuration beyond copying.

## Option 1: Globally (available in every project)

Copy the skill folder into your user-level Claude directory:

**Linux / macOS / WSL**

```bash
mkdir -p ~/.claude/skills
cp -r skills/migrate-to-awesome-assertions ~/.claude/skills/
```

**Windows (PowerShell)**

```powershell
New-Item -ItemType Directory -Force "$env:USERPROFILE\.claude\skills" | Out-Null
Copy-Item -Recurse skills\migrate-to-awesome-assertions "$env:USERPROFILE\.claude\skills\"
```

## Option 2: Per project (shared with your team via the repo)

Copy the skill folder into the target repository's `.claude/skills/` directory:

```bash
mkdir -p <your-repo>/.claude/skills
cp -r skills/migrate-to-awesome-assertions <your-repo>/.claude/skills/
```

Commit it; everyone using Claude Code in that repository gets the skill.

## Tip for skill development: symlink instead of copy

While iterating on the skill in this repo, a symlink keeps the installed copy in sync
with your edits:

```bash
ln -s "$(pwd)/skills/migrate-to-awesome-assertions" ~/.claude/skills/migrate-to-awesome-assertions
```

(On Windows, use `New-Item -ItemType SymbolicLink` in an elevated PowerShell, or just
re-copy after editing.)

## Verify the installation

Start a new Claude Code session and ask:

> Which skills do you have available?

`migrate-to-awesome-assertions` should be listed. Then try it on a real project:

> Migrate this solution from FluentAssertions to AwesomeAssertions.

## Uninstall

Delete the folder (or symlink) from `~/.claude/skills/` or `<repo>/.claude/skills/`.
