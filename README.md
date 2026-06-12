# migrate-to-awesome-assertions

A [Claude Code skill](https://docs.anthropic.com/en/docs/claude-code) that migrates
.NET test projects from **FluentAssertions** to **AwesomeAssertions**.

## Why

In January 2025, FluentAssertions 8.0 moved to a paid Xceed license for commercial
use. **AwesomeAssertions** is the community fork that continues the library under the
original free Apache-2.0 license, tracking the FluentAssertions 8 API.

Migrating is mostly mechanical, but there are real traps:

- **AwesomeAssertions 9.0 renamed everything** — namespaces and assembly are now
  `AwesomeAssertions`, not `FluentAssertions`, so it is no longer a pure package swap.
- **FluentAssertions 7→8 had breaking changes** that flow into the migration when you
  come from v7 or earlier: renamed methods (`HaveCountGreaterOrEqualTo` →
  `HaveCountGreaterThanOrEqualTo`, …), renamed equivalency types
  (`EquivalencyAssertionOptions` → `EquivalencyOptions`), removed static configuration
  (`AssertionOptions` → `AssertionConfiguration.Current`), and a few **behavioral**
  changes that compile fine but pass/fail differently.
- **Custom assertions need a real rewrite.** The static `Execute.Assertion` entry
  point was replaced by the `AssertionChain` API — every custom assertion class,
  extension method, value formatter registration, and equivalency step is affected.
- **Companion packages** (`FluentAssertions.Json`, `.Web`, `.Analyzers`, `.DataSets`)
  have AwesomeAssertions counterparts that must be swapped together, and third-party
  packages can keep pulling FluentAssertions in transitively.

This skill gives Claude a verified, step-by-step playbook for all of that — the code
templates in it were compile-checked against AwesomeAssertions 9.4.0.

## What it handles

| Coming from | What the migration involves |
|---|---|
| FluentAssertions 8.x | Package swap + namespace rename (easiest path) |
| FluentAssertions 7.x | Plus API breaking changes + custom-assertion rewrite |
| FluentAssertions ≤6.x | Same as 7.x, plus removed obsolete members |

In all cases the skill finishes by building, running the tests, comparing test counts
against the pre-migration state, and verifying no FluentAssertions reference (direct
or transitive) is left behind.

## Use

Install the skill (see [INSTALL.md](INSTALL.md)), then just ask Claude Code things
like:

> Migrate this solution from FluentAssertions to AwesomeAssertions.

> We can't use FluentAssertions 8 because of the Xceed license — get us onto the free
> fork. We have custom assertion classes in tests/TestKit.

> Our custom FluentAssertions extensions broke — `Execute.Assertion` doesn't exist
> anymore. Fix them for AwesomeAssertions 9.

Claude picks the right migration path based on the FluentAssertions version it finds
in your project files.

## Repository contents

This repo is the development workbench for the skill: the skill itself lives in
[`skills/migrate-to-awesome-assertions/`](skills/migrate-to-awesome-assertions/),
`evals/` contains real .NET fixture projects (on FluentAssertions 6, 7 and 8) used to
test the skill end-to-end, and the workspace directory holds evaluation results.
See [CLAUDE.md](CLAUDE.md) for the layout and development workflow.
