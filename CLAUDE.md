# fluent-assertion-migration

This repository is a **development and testing workbench** for a single Claude Code
skill: `migrate-to-awesome-assertions`. It is not a normal application repo — the
"product" here is the skill itself, and everything else exists to test and improve it.

## What the skill does

Migrates .NET test projects from FluentAssertions (which became commercially licensed
in v8, January 2025) to AwesomeAssertions, the free Apache-2.0 community fork.
It covers migration from any FluentAssertions version, including the hard parts:
the v7→v8 extensibility rewrite (`Execute.Assertion` → `AssertionChain`) and the
AwesomeAssertions 9.0 namespace rename (`FluentAssertions` → `AwesomeAssertions`).

## Repository layout

```
skills/migrate-to-awesome-assertions/   The skill under development (the deliverable)
├── SKILL.md                            Entry point: workflow + triggering description
└── references/
    ├── breaking-changes.md             User-level API changes (FA ≤7 → AA 8/9)
    └── extensibility-migration.md      Custom-assertion rewrite patterns (verified to compile)

evals/                                  COMMITTED — durable eval assets
├── evals.json                          Test-case definitions with grading assertions
├── fixtures/                           Real .NET projects used as migration inputs;
│   ├── basic-v6/                       all are GREEN on their old FluentAssertions
│   ├── custom-assertions-v7/           version before migration (verify after editing!)
│   └── fa8-central-packages/
└── scripts/grade_run.py                Programmatic grader for eval runs

test/                                   GIT-IGNORED — everything generated or local-only
├── workspace/iteration-N/              Eval run results (migrated copies, grading,
│                                       benchmark.json/md, review.html)
└── test1/, test2/                      Local real-world .NET solutions for large-scale
                                        skill testing (not part of the repo)
```

**Convention: `evals/` holds only what the repo keeps (test cases, fixtures,
scripts); every run output and local test codebase goes under `test/`, which is
git-ignored. Never write run outputs under `skills/` or `evals/`.**

## Working on this repo

- The skill is iterated with the **skill-creator** workflow: run each eval prompt with
  and without the skill, grade against the assertions in `evals/evals.json`, review,
  improve, repeat.
- **All test/eval output ALWAYS goes to `test/`** (e.g. `test/workspace/iteration-N/`
  for eval runs, `test/<scratch-name>/` for ad-hoc experiments). `test/` is
  git-ignored. Never write generated output into `evals/`, `skills/`, or the repo
  root — if a tool defaults to another location (skill-creator defaults to a
  `<skill-name>-workspace/` sibling of the skill), override it to `test/`.
- The fixtures must always pass their tests on the *original* FluentAssertions version
  before being used as eval inputs (`dotnet test` in each fixture project). If you edit
  a fixture, re-verify and remove `bin/`/`obj/` afterwards.
- Facts in the skill's reference files were verified against the live
  awesomeassertions.org docs, GitHub release notes, and by compiling samples against
  AwesomeAssertions 9.4.0 (June 2026). If AwesomeAssertions ships a new major version,
  re-verify before editing.
- Requires a local .NET SDK (8.0+) and NuGet access to run the evals.

See `README.md` for a human-oriented description of the skill and `INSTALL.md` for how
to install it into Claude Code.
