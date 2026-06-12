#!/usr/bin/env python3
"""Programmatic grading for migrate-to-awesome-assertions eval runs.

Usage: python3 grade_run.py <run_dir> <eval_id>

<run_dir> is e.g. .../iteration-1/eval-0-basic-v6-migration/with_skill
It must contain project/ (the migrated project).

Writes grading.json into <run_dir> with the viewer schema:
  {"expectations": [{"text": ..., "passed": ..., "evidence": ...}]}

Assertions that need human/LLM judgment (semantics preserved, minimal diff) are
emitted with passed=null so a grader agent can fill them in.
"""
import json
import re
import subprocess
import sys
from pathlib import Path

RUN_DIR = Path(sys.argv[1]).resolve()
EVAL_ID = int(sys.argv[2])
PROJECT = RUN_DIR / "project"

TEST_PROJECT = {
    0: "InventoryApp.Tests",
    1: "ShippingService.Tests",
    2: "ApiClient.Tests",
    3: "LabelKit.Tests",
    4: "SensorFeed.Tests",
    5: "PricingEngine.Tests",
    6: "RouteKit.Tests",
}[EVAL_ID]
EXPECTED_TESTS = {0: 9, 1: 9, 2: 5, 3: 6, 4: 6, 5: 14, 6: 13}[EVAL_ID]


def sh(cmd, cwd=None, timeout=600):
    r = subprocess.run(cmd, shell=True, cwd=cwd, capture_output=True, text=True, timeout=timeout)
    return r.returncode, r.stdout + r.stderr


def source_files(root: Path, exts=(".cs", ".csproj", ".props")):
    for p in root.rglob("*"):
        if p.suffix in exts and "bin" not in p.parts and "obj" not in p.parts:
            yield p


def grep(pattern, root: Path, exts=(".cs",)):
    rx = re.compile(pattern)
    hits = []
    for p in source_files(root, exts):
        for i, line in enumerate(p.read_text(encoding="utf-8", errors="replace").splitlines(), 1):
            if rx.search(line):
                hits.append(f"{p.relative_to(root)}:{i}: {line.strip()[:120]}")
    return hits


def expectation(text, passed, evidence):
    return {"text": text, "passed": passed, "evidence": evidence[:1000] if evidence else ""}


exps = []

# --- package references ---
pkg_fa = grep(r"FluentAssertions", PROJECT, exts=(".csproj", ".props"))
pkg_aa = grep(r"AwesomeAssertions", PROJECT, exts=(".csproj", ".props"))

if EVAL_ID == 2:
    exps.append(expectation(
        "Directory.Packages.props references AwesomeAssertions (latest) and contains no FluentAssertions entry",
        bool(pkg_aa) and not pkg_fa,
        f"FluentAssertions refs: {pkg_fa or 'none'} | AwesomeAssertions refs: {pkg_aa or 'none'}"))
elif EVAL_ID in (5, 6):
    # classic-assertion conversion: AA gets added, the test framework packages must remain
    versions = re.findall(r'AwesomeAssertions"\s+Version="(\d+)', "\n".join(
        p.read_text() for p in source_files(PROJECT, (".csproj", ".props"))))
    aa9 = any(int(v) >= 9 for v in versions)
    fw_pkg = (grep(r'Include="NUnit"', PROJECT, exts=(".csproj", ".props"))
              if EVAL_ID == 5 else
              grep(r'Include="xunit"', PROJECT, exts=(".csproj", ".props")))
    exps.append(expectation(
        "Project references AwesomeAssertions major version 9+ and keeps the "
        + ("NUnit framework and test-adapter packages" if EVAL_ID == 5 else "xunit packages"),
        bool(versions) and aa9 and bool(fw_pkg),
        f"AA versions: {versions} | framework pkg refs: {fw_pkg or 'MISSING'}"))
else:
    exps.append(expectation(
        "No FluentAssertions package reference remains in any project file",
        not pkg_fa,
        "\n".join(pkg_fa) or "clean"))
    # AA major version: look in csproj/props, fall back to assets file
    versions = re.findall(r'AwesomeAssertions"\s+Version="(\d+)', "\n".join(
        p.read_text() for p in source_files(PROJECT, (".csproj", ".props"))))
    aa9 = any(int(v) >= 9 for v in versions)
    exps.append(expectation(
        "Project references the AwesomeAssertions NuGet package, major version 9 or later",
        bool(versions) and aa9,
        f"versions found: {versions}"))

# --- no FluentAssertions identifier in sources (FA-migration evals only) ---
if EVAL_ID not in (5, 6):
    src_fa = grep(r"FluentAssertions", PROJECT, exts=(".cs",))
    exps.append(expectation(
        "No FluentAssertions identifier remains in the C# sources",
        not src_fa,
        "\n".join(src_fa[:10]) or "clean"))

# --- build ---
build_dirs = [PROJECT / TEST_PROJECT]
if EVAL_ID == 2:
    build_dirs.insert(0, PROJECT / "TestKit")
build_ok, build_log = True, ""
for d in build_dirs:
    rc, out = sh("dotnet build -v q --nologo", cwd=d)
    build_ok &= rc == 0
    tail = "\n".join(out.splitlines()[-6:])
    build_log += f"[{d.name}] rc={rc}\n{tail}\n"
exps.append(expectation("dotnet build succeeds with 0 errors", build_ok, build_log))

# --- tests ---
rc, out = sh("dotnet test -v q --nologo", cwd=PROJECT / TEST_PROJECT)
m = re.search(r"Failed:\s*(\d+),\s*Passed:\s*(\d+),\s*Skipped:\s*(\d+),\s*Total:\s*(\d+)", out)
if m:
    failed, passed, skipped, total = map(int, m.groups())
    tests_ok = failed == 0 and skipped == 0 and passed == EXPECTED_TESTS and total == EXPECTED_TESTS
    ev = m.group(0)
else:
    tests_ok, ev = False, "\n".join(out.splitlines()[-8:])
exps.append(expectation(
    f"dotnet test discovers {EXPECTED_TESTS} tests and all {EXPECTED_TESTS} pass",
    tests_ok, ev))

# --- eval-specific source checks ---
if EVAL_ID == 0:
    old = grep(r"HaveCountGreaterOrEqualTo|HaveCountLessOrEqualTo|BeGreaterOrEqualTo|BeLessOrEqualTo"
               r"|EquivalencyAssertionOptions|RespectingRuntimeTypes|ExcludingNestedObjects"
               r"|\bAssertionOptions\b", PROJECT)
    exps.append(expectation(
        "Removed v7 API names are gone (HaveCountGreaterOrEqualTo, BeLessOrEqualTo, "
        "EquivalencyAssertionOptions, RespectingRuntimeTypes, ExcludingNestedObjects, AssertionOptions, ...)",
        not old, "\n".join(old[:10]) or "clean"))

if EVAL_ID == 1:
    execs = grep(r"Execute\s*\.\s*Assertion", PROJECT)
    exps.append(expectation("No Execute.Assertion calls remain anywhere",
                            not execs, "\n".join(execs[:10]) or "clean"))

    chain_should = grep(r"AssertionChain\.GetOrCreate", PROJECT)
    base_chain = grep(r"base\([^)]*,\s*\w*[Cc]hain\)", PROJECT)
    exps.append(expectation(
        "Custom assertion classes thread AssertionChain: Should() calls AssertionChain.GetOrCreate() "
        "and the constructor passes the chain to base",
        bool(chain_should) and bool(base_chain),
        f"GetOrCreate: {chain_should[:3]} | base(..., chain): {base_chain[:3]}"))

    clear = grep(r"ClearExpectation", PROJECT)
    withexp = grep(r"WithExpectation", PROJECT)
    exps.append(expectation(
        "WithExpectation uses the nested-lambda form and ClearExpectation is gone",
        bool(withexp) and not clear,
        f"ClearExpectation: {clear or 'none'} | WithExpectation: {withexp[:2]}"))

    # AndWhichConstraint constructed with >= 3 args (subject, value, chain[, postfix])
    text = "\n".join(p.read_text() for p in source_files(PROJECT, (".cs",)))
    ctors = re.findall(r"new AndWhichConstraint<[^>]+>\s*\(([^;]*?)\);", text, re.S)
    with_chain = [c for c in ctors if c.count(",") >= 2]
    exps.append(expectation(
        "ContainItem returns AndWhichConstraint constructed WITH the assertion chain (not the 2-argument v7 form)",
        bool(ctors) and len(with_chain) == len(ctors),
        f"ctor args found: {[c.replace(chr(10), ' ')[:80] for c in ctors]}"))

    validate = grep(r"IValidateChildNodeEquivalency", PROJECT)
    proven = grep(r"EquivalencyProven", PROJECT)
    # both AssertionConfiguration.Current and AssertionEngine.Configuration are valid public hosts
    plan = grep(r"(AssertionConfiguration\.Current|AssertionEngine\.Configuration)\.Equivalency\.Plan", PROJECT)
    exps.append(expectation(
        "Equivalency step uses IValidateChildNodeEquivalency and EquivalencyResult.EquivalencyProven, "
        "registered via the global equivalency plan (AssertionConfiguration.Current or AssertionEngine.Configuration)",
        bool(validate) and bool(proven) and bool(plan),
        f"IValidateChildNodeEquivalency: {bool(validate)}, EquivalencyProven: {bool(proven)}, Plan reg: {bool(plan)}"))

if EVAL_ID == 2:
    # minimal-diff check: fixture with s/FluentAssertions/AwesomeAssertions/g should equal project
    fixture = Path(__file__).resolve().parents[1] / "fixtures" / "fa8-central-packages"
    diffs = []
    for fp in source_files(fixture, (".cs",)):
        rel = fp.relative_to(fixture)
        mp = PROJECT / rel
        if not mp.exists():
            diffs.append(f"missing: {rel}")
            continue
        want = fp.read_text().replace("FluentAssertions", "AwesomeAssertions")
        if want.strip() != mp.read_text().strip():
            diffs.append(f"structural change: {rel}")
    exps.append(expectation(
        "Migration is minimal: the v8-style AssertionChain code was only namespace-renamed, not structurally rewritten",
        not diffs, "\n".join(diffs) or "sources identical modulo namespace rename"))

if EVAL_ID in (3, 4):
    legacy_tfm = grep(r"<TargetFramework>net5\.?0?</TargetFramework>", PROJECT, exts=(".csproj",))
    net8 = grep(r"<TargetFramework>net8\.0</TargetFramework>", PROJECT, exts=(".csproj",))
    exps.append(expectation(
        "Legacy TFMs are gone: projects retargeted from net5.0/net50 to net8.0",
        not legacy_tfm and bool(net8),
        f"legacy TFMs: {legacy_tfm or 'none'} | net8.0 targets: {len(net8)}"))

    execs = grep(r"Execute\s*\.\s*Assertion", PROJECT)
    exps.append(expectation("No Execute.Assertion calls remain anywhere",
                            not execs, "\n".join(execs[:10]) or "clean"))

    old_cmp = grep(r"\bBeGreaterOrEqualTo\b|\bBeLessOrEqualTo\b", PROJECT)
    exps.append(expectation("Renamed APIs adopted: no BeGreaterOrEqualTo / BeLessOrEqualTo remain",
                            not old_cmp, "\n".join(old_cmp[:10]) or "clean"))

if EVAL_ID == 3:
    chain_should = grep(r"AssertionChain\.GetOrCreate", PROJECT)
    base_chain = grep(r"base\([^)]*,\s*\w*[Cc]hain\)", PROJECT)
    subject_assign = grep(r"^\s*Subject\s*=", PROJECT)
    exps.append(expectation(
        "LabelTokenAssertions threads AssertionChain: GetOrCreate in Should(), chain passed to base ctor "
        "(no v5-style 'Subject = ...' assignment)",
        bool(chain_should) and bool(base_chain) and not subject_assign,
        f"GetOrCreate: {bool(chain_should)}, base(..., chain): {bool(base_chain)}, "
        f"Subject assignment leftovers: {subject_assign or 'none'}"))

    current_chain = grep(r"CurrentAssertionChain", PROJECT / "TestSupport")
    exps.append(expectation(
        "The shared ObjectAssertions extension obtains the chain via CurrentAssertionChain (or equivalent)",
        bool(current_chain),
        "\n".join(current_chain[:3]) or "CurrentAssertionChain not found in TestSupport"))

if EVAL_ID == 4:
    wc = PROJECT / TEST_PROJECT / "WindowCheck.cs"
    wc_text = wc.read_text() if wc.exists() else ""
    exps.append(expectation(
        "WindowCheck uses AssertionChain instead of Execute.Assertion",
        "AssertionChain" in wc_text and "Execute" not in wc_text,
        (wc_text[:400] if wc_text else "WindowCheck.cs not found (renamed or deleted?)")))

if EVAL_ID == 5:
    nunit_asserts = grep(
        r"Assert\.(That|AreEqual|AreNotEqual|AreSame|AreNotSame|IsTrue|IsFalse|True\b|False\b"
        r"|IsNull|IsNotNull|Null\b|NotNull\b|IsEmpty|IsNotEmpty|Greater|GreaterOrEqual|Less"
        r"|LessOrEqual|Positive|Negative|Zero|NotZero|IsInstanceOf|IsNotInstanceOf|Contains"
        r"|Throws|Catch|DoesNotThrow|ThrowsAsync|DoesNotThrowAsync|Multiple)"
        r"|CollectionAssert\.|StringAssert\.|ClassicAssert\.", PROJECT)
    exps.append(expectation(
        "No NUnit assertion calls remain: no Assert.That / classic Assert.* comparisons, "
        "CollectionAssert, or StringAssert in the sources",
        not nunit_asserts, "\n".join(nunit_asserts[:10]) or "clean"))

    attrs = grep(r"\[(Test|TestFixture)\]", PROJECT)
    using_nunit = grep(r"using NUnit\.Framework;", PROJECT)
    exps.append(expectation(
        "NUnit remains the runner: [Test]/[TestFixture] attributes and using NUnit.Framework retained",
        bool(attrs) and bool(using_nunit),
        f"[Test]/[TestFixture]: {len(attrs)} | using NUnit.Framework: {len(using_nunit)}"))

    exact = grep(r"ThrowExactly(Async)?<(FormatException|ArgumentException|InvalidOperationException)>", PROJECT)
    derived = grep(r"\bThrow<", PROJECT)  # plain Throw< (Assert.Catch / Throws.InstanceOf sites)
    exps.append(expectation(
        "Exception strictness preserved: exact-type asserts (Assert.Throws, Throws.TypeOf, "
        "Throws.ArgumentException) became ThrowExactly while derived-allowed asserts "
        "(Assert.Catch, Throws.InstanceOf) became Throw",
        bool(exact) and bool(derived),
        f"ThrowExactly sites: {len(exact)} | plain Throw< sites: {len(derived)}"))

    scope = grep(r"AssertionScope", PROJECT)
    exps.append(expectation(
        "Assert.Multiple blocks converted to AssertionScope",
        bool(scope), "\n".join(scope[:3]) or "AssertionScope not found"))

    approx = grep(r"BeApproximately", PROJECT)
    exps.append(expectation(
        "Numeric tolerance asserts (AreEqual with delta, Is.EqualTo().Within()) became BeApproximately",
        bool(approx), "\n".join(approx[:5]) or "BeApproximately not found"))

if EVAL_ID == 6:
    xunit_asserts = grep(
        r"Assert\.(Equal|NotEqual|StrictEqual|True\b|False\b|Null\b|NotNull\b|Same|NotSame"
        r"|Empty|NotEmpty|Single|Contains|DoesNotContain|StartsWith|EndsWith|Matches"
        r"|DoesNotMatch|InRange|NotInRange|IsType|IsNotType|IsAssignableFrom|Throws|ThrowsAny"
        r"|ThrowsAsync|ThrowsAnyAsync|All\b|Collection|Equivalent|Distinct|Subset|Superset|Raises)",
        PROJECT)
    exps.append(expectation(
        "No xUnit Assert.* assertion calls remain in the sources",
        not xunit_asserts, "\n".join(xunit_asserts[:10]) or "clean"))

    attrs = grep(r"\[(Fact|Theory|InlineData)", PROJECT)
    using_xunit = grep(r"using Xunit;", PROJECT)
    exps.append(expectation(
        "xUnit remains the runner: [Fact]/[Theory]/[InlineData] attributes and using Xunit retained",
        bool(attrs) and bool(using_xunit),
        f"[Fact]/[Theory]/[InlineData]: {len(attrs)} | using Xunit: {len(using_xunit)}"))

    exact = grep(r"ThrowExactly<ArgumentException>", PROJECT)
    exact_async = grep(r"await .*ThrowExactlyAsync<InvalidOperationException>", PROJECT)
    any_derived = grep(r"\bThrow<", PROJECT)  # Assert.ThrowsAny site
    exps.append(expectation(
        "Exception strictness preserved: Assert.Throws/ThrowsAsync became ThrowExactly/"
        "ThrowExactlyAsync (awaited), Assert.ThrowsAny became Throw",
        bool(exact) and bool(exact_async) and bool(any_derived),
        f"ThrowExactly: {len(exact)} | awaited ThrowExactlyAsync: {len(exact_async)} | plain Throw<: {len(any_derived)}"))

    satisfy = grep(r"SatisfyRespectively", PROJECT)
    allsat = grep(r"AllSatisfy", PROJECT)
    single = grep(r"ContainSingle", PROJECT)
    exps.append(expectation(
        "Structured asserts converted faithfully: Assert.Collection -> SatisfyRespectively, "
        "Assert.All -> AllSatisfy, Assert.Single -> ContainSingle",
        bool(satisfy) and bool(allsat) and bool(single),
        f"SatisfyRespectively: {len(satisfy)} | AllSatisfy: {len(allsat)} | ContainSingle: {len(single)}"))

# --- judgment assertion placeholder ---
exps.append(expectation(
    "Test semantics preserved: every original assertion still checks the same condition "
    "(nothing deleted or weakened to force a pass)",
    None, "NEEDS GRADER JUDGMENT: diff the test files against the original fixture"))

out_file = RUN_DIR / "grading.json"
out_file.write_text(json.dumps({"expectations": exps}, indent=2))
done = sum(1 for e in exps if e["passed"] is True)
print(f"{RUN_DIR.name}: {done}/{len(exps)} passed programmatically "
      f"({sum(1 for e in exps if e['passed'] is None)} need judgment) -> {out_file}")
