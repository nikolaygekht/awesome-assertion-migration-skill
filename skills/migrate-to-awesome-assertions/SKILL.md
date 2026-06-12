---
name: migrate-to-awesome-assertions
description: >
  Migrate .NET test projects from FluentAssertions to AwesomeAssertions, the free
  Apache-2.0 community fork. Use this skill whenever the user mentions FluentAssertions
  licensing/Xceed costs, AwesomeAssertions, replacing or removing FluentAssertions,
  upgrading assertion libraries in C#/.NET test code, or fixing custom assertion
  extensions that broke (e.g. Execute.Assertion no longer exists, AssertionChain errors).
  Covers migration from any FluentAssertions version (v5/v6/v7 and v8) including the
  extensibility API rewrite (Execute.Assertion → AssertionChain) and the v9 namespace
  rename (FluentAssertions → AwesomeAssertions).
---

# Migrate from FluentAssertions to AwesomeAssertions

FluentAssertions 8.0 (January 2025) moved to a paid Xceed license for commercial use.
AwesomeAssertions is the community fork that continues the library under the free
Apache-2.0 license. It tracks the FluentAssertions 8 API, so a migration is mostly
mechanical — but the amount of work depends on which FluentAssertions version the
project is coming from, and whether it defines custom assertions.

Target the **latest AwesomeAssertions 9.x** unless the user says otherwise. Since 9.0,
everything is named `AwesomeAssertions` (namespaces, assembly); 9.0 was deliberately a
rename-only release with no functional changes, so there is no reason to stop at 8.x.
AwesomeAssertions 9.x targets `net47`, `net6.0`, `net8.0`, `netstandard2.0/2.1` — the
same reach as FluentAssertions 6/7 except .NET Core 2.x/3.x and .NET 5 (which still
work via the netstandard targets, just without a dedicated one). Projects on
end-of-life targets (`netcoreapp3.1`, `net5.0` — sometimes written as the legacy
`net50` form) often combine this migration with a TFM bump; do that only when the user
asks for it, and expect the test infrastructure packages (`Microsoft.NET.Test.Sdk`,
runners) to need a version bump along with the TFM.

## Step 1: Inventory the project

Find out what you are migrating before changing anything:

```bash
# Which FluentAssertions packages and versions are referenced (incl. companion packages)?
grep -rn --include="*.csproj" --include="*.props" --include="packages.config" -i "fluentassertions" .

# Where is the library used in code?
grep -rln --include="*.cs" "FluentAssertions" .

# Does the project define custom assertions (the part that needs real rewriting)?
grep -rln --include="*.cs" -E "Execute\.Assertion|ReferenceTypeAssertions|IValueFormatter|IEquivalencyStep|AssertionOptions\." .
```

Record three facts; they determine the migration path:

1. **Source version** (from the package reference):
   - **v8.x** → easiest path: AwesomeAssertions 8.x is API-identical to FluentAssertions 8.x,
     so migrating to 9.x is just the package swap (Step 2) + namespace rename (Step 3). Done.
   - **v7.x or earlier** → also apply the API breaking changes (Step 4) and, if the
     project has custom assertions, the extensibility rewrite (Step 5).
2. **Custom assertions present?** Any hit on `Execute.Assertion` or assertion base
   classes means Step 5 applies.
3. **Companion packages** — they must be swapped together with the main package (Step 2),
   and watch for *transitive* FluentAssertions references from third-party test libraries
   (see Step 6).

## Step 2: Swap the NuGet packages

Replace in `.csproj`, `Directory.Packages.props` (central package management), or
`packages.config`:

| Remove | Add instead |
|---|---|
| `FluentAssertions` | `AwesomeAssertions` |
| `FluentAssertions.Json` | `AwesomeAssertions.Json` |
| `FluentAssertions.Web` | `AwesomeAssertions.Web` |
| `FluentAssertions.Analyzers` | `AwesomeAssertions.Analyzers` |
| `FluentAssertions.DataSets` | `AwesomeAssertions.DataSets` |

Use the latest stable version of each (check with
`dotnet package search AwesomeAssertions --exact-match`). For other third-party
extensions built on FluentAssertions (e.g. `FluentAssertions.AspNetCore.Mvc`,
NodaTime or Moq helpers), search NuGet for an `AwesomeAssertions`-based fork; several
exist (`WireMock.Net.AwesomeAssertions`, `Heavendata.AwesomeAssertions.NodaTime`, …).
If none exists, that package will keep pulling FluentAssertions transitively — flag
this to the user, because mixing both libraries in one project causes ambiguous-type
compile errors (see Step 6).

`FluentAssertions.DataSets` only exists for v8+; if the source project is v6/v7 and
asserts on `DataSet`/`DataTable`/`DataRow`/`DataColumn`, those assertions moved out of
the core package — add `AwesomeAssertions.DataSets`.

## Step 3: Rename the namespaces

Since AwesomeAssertions 9.0, the root namespace is `AwesomeAssertions`. In virtually
all code a plain text replacement of `FluentAssertions` → `AwesomeAssertions` is
correct and complete, because the namespace structure below the root is unchanged
(`.Execution`, `.Extensions`, `.Equivalency`, `.Formatting`, `.Primitives`, …).

Apply it to:

- `using FluentAssertions;` and friends (`FluentAssertions.Execution`, etc.)
- `global using` directives in `GlobalUsings.cs` *and* `<Using Include="FluentAssertions" />`
  items in project files
- Fully-qualified references in code, e.g. `FluentAssertions.Execution.AssertionScope`
- One rename that is not just the root: the `[AssertionEngineInitializer]` attribute
  lives in `AwesomeAssertions.Extensibility`

Don't replace occurrences that refer to the *product* in comments/docs where the
distinction matters, and never touch unrelated strings that merely contain the word.

## Step 4: Apply API breaking changes (only when coming from v7 or earlier)

FluentAssertions 7→8 (and therefore AwesomeAssertions) renamed and removed a number of
user-facing APIs. The full grep-ready catalog is in
[references/breaking-changes.md](references/breaking-changes.md) — read it whenever the
source project is v7 or earlier. The highlights you will hit most often:

- `EquivalencyAssertionOptions<T>` → `EquivalencyOptions<T>` (breaks every reusable
  equivalency-config helper method signature)
- `HaveCountGreaterOrEqualTo`/`HaveCountLessOrEqualTo` → `...GreaterThanOrEqualTo`/`...LessThanOrEqualTo`;
  same `...OrEqualTo` → `...ThanOrEqualTo` pattern on comparable, TimeSpan and execution-time assertions
- `RespectingRuntimeTypes` → `PreferringRuntimeMemberTypes`,
  `ExcludingNestedObjects` → `WithoutRecursing`
- Static configuration: `AssertionOptions.*` is gone → `AssertionConfiguration.Current.*`
- `HttpResponseMessage` assertions removed from core → `AwesomeAssertions.Web`

Also check the **behavioral changes** section of that reference — a few assertions
(`AllSatisfy`/`OnlyContain` on empty collections, `BeLowerCased`/`BeUpperCased`)
compile unchanged but pass/fail differently, which can silently weaken a test.

## Step 5: Rewrite custom assertions (only when coming from v7 or earlier)

The extensibility model was redesigned: the static `Execute.Assertion` entry point no
longer exists; assertions are built on an `AssertionChain` instance that flows from
`Should()` through the whole fluent statement. Every custom assertion class, extension
method, value formatter, and equivalency step needs a mechanical-but-precise rewrite.

Read [references/extensibility-migration.md](references/extensibility-migration.md)
for verified before/after templates covering:

- assertion classes deriving from `ReferenceTypeAssertions<TSubject, TAssertions>`
- extension methods on built-in assertion classes (`CurrentAssertionChain`)
- `WithExpectation`/`ClearExpectation` → nested-lambda `WithExpectation`
- chaining with `AndWhichConstraint` and caller-identifier postfixes
- `AssertionScope` (what stayed, what moved to `AssertionChain`)
- `IValueFormatter`, `IEquivalencyStep`, and global configuration hooks

## Step 6: Build, test, and verify nothing FluentAssertions remains

Iterate until clean:

```bash
dotnet build   # fix remaining compile errors; unknown ones are usually in breaking-changes.md
dotnet test    # all tests must pass; investigate failures against the behavioral-changes list
```

Then verify the dependency tree — a third-party package may still pull FluentAssertions
transitively, which at best wastes the migration and at worst creates ambiguous
references:

```bash
dotnet list package --include-transitive | grep -i fluentassertions
grep -rn -i "fluentassertions" --include="*.cs" --include="*.csproj" --include="*.props" . | grep -v bin/ | grep -v obj/
```

Both should come back empty. If a transitive reference remains, either find an
AwesomeAssertions-based fork of the offending package or report it to the user as a
known leftover with the licensing implication spelled out.

A test count comparison is a cheap, strong signal: the number of discovered and passed
tests after migration must equal the number before. If tests fail after migration,
check the behavioral-changes section in breaking-changes.md before assuming a real bug.
