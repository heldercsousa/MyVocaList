#!/usr/bin/env python3
"""check_dx.py -- DevExpress guard for the UI component library migration.

While DevExpress and Syncfusion coexist:
  * an Sf copy (`*Sf.*` / `*SfPage.*`) must not reference DevExpress (REQ-UIMIG-11);
  * no commit may add a line that references DevExpress, bridge files excepted
    (REQ-UIMIG-12);
  * a license key literal never appears in a tracked file (REQ-UIMIG-17).
`--release` proves DevExpress is fully gone from compiled sources (REQ-UIMIG-14).

Usage: check_dx.py --staged | --release
Exit: 0 clean, 1 violations (prints `path:line: rule`), 2 usage or git error.
"""
import fnmatch
import os
import posixpath
import re
import subprocess
import sys
from typing import NamedTuple

SF_FILE = re.compile(r"^[A-Za-z0-9]+(Sf|SfPage)\.(xaml|xaml\.cs|cs)$")
BRIDGE_FILES = (
    "MyVocaList/UI/Migration/DxSheetStateConverter.cs",
    "Tests/Unit/Migration/DxSheetStateConverterTests.cs",
)
DX_EXTENSIONS = (".cs", ".xaml", ".csproj", ".props")
CSPROJ = "MyVocaList/MyVocaList.csproj"

BLOCK_MESSAGE = ("Blocked: new DevExpress line. Log an exception in "
                 ".claude/exception-registry.md and ask Helder; never bypass the hook.")

_PREFIX_DECL = re.compile(
    r'xmlns:(\w+)\s*=\s*"(?:http://schemas\.devexpress\.com/maui|clr-namespace:DevExpress[^"]*)"')
_LICENSE_KEY = re.compile(r'RegisterLicense\(\s*"[^"]{20,}"\s*\)|SYNCFUSION_LICENSE_KEY\s*=\s*\S{20,}')
_REMOVE_ATTR = re.compile(r'<(?:Compile|MauiXaml)\s+Remove="([^"]+)"')


class Violation(NamedTuple):
    path: str
    line: int
    rule: str


def dx_prefixes(text: str) -> set[str]:
    """XAML prefixes bound to a DevExpress schema or CLR namespace."""
    return set(_PREFIX_DECL.findall(text))


def has_dx_reference(line: str, prefixes: set[str]) -> bool:
    if "DevExpress" in line or "schemas.devexpress.com" in line:
        return True
    for p in prefixes:
        if re.search(r'(?:^|</?|[{("\s])' + re.escape(p) + ":", line):
            return True
    return False


def _is_dx_file(path: str) -> bool:
    return path.endswith(DX_EXTENSIONS)


def check_staged(added: dict[str, list[tuple[int, str]]],
                 contents: dict[str, str]) -> list[Violation]:
    violations: list[Violation] = []
    for path, lines in added.items():
        for lineno, text in lines:
            if _LICENSE_KEY.search(text):
                violations.append(Violation(path, lineno, "license-key-in-tracked-file"))
    for path, content in contents.items():
        base = posixpath.basename(path)
        if SF_FILE.match(base):
            prefixes = dx_prefixes(content)
            for i, text in enumerate(content.splitlines(), start=1):
                if has_dx_reference(text, prefixes):
                    violations.append(Violation(path, i, "sf-file-references-devexpress"))
    for path, lines in added.items():
        if path in BRIDGE_FILES or not _is_dx_file(path) or SF_FILE.match(posixpath.basename(path)):
            continue
        prefixes = dx_prefixes(contents.get(path, ""))
        for lineno, text in lines:
            if has_dx_reference(text, prefixes):
                violations.append(Violation(path, lineno, "added-devexpress-line"))
    return violations


def _glob_to_regex(pattern: str) -> re.Pattern:
    out, i = "", 0
    while i < len(pattern):
        if pattern.startswith("**/", i):
            out += "(?:.*/)?"
            i += 3
        elif pattern.startswith("**", i):
            out += ".*"
            i += 2
        elif pattern[i] == "*":
            out += "[^/]*"
            i += 1
        elif pattern[i] == "?":
            out += "[^/]"
            i += 1
        else:
            out += re.escape(pattern[i])
            i += 1
    return re.compile("^" + out + "$")


def check_release(files: dict[str, str], excluded: list[str]) -> list[Violation]:
    matchers = [_glob_to_regex(g) for g in excluded]
    violations: list[Violation] = []
    for path, content in files.items():
        if any(m.match(path) for m in matchers):
            continue
        prefixes = dx_prefixes(content)
        for i, text in enumerate(content.splitlines(), start=1):
            if has_dx_reference(text, prefixes):
                violations.append(Violation(path, i, "release-references-devexpress"))
            if _LICENSE_KEY.search(text):
                violations.append(Violation(path, i, "license-key-in-tracked-file"))
    return violations


def report(violations: list[Violation]) -> None:
    for v in violations:
        print(f"{v.path}:{v.line}: {v.rule}")
    if any(v.rule == "added-devexpress-line" for v in violations):
        print(BLOCK_MESSAGE)


def _git(*args: str) -> str:
    result = subprocess.run(["git", "-c", "core.quotepath=off", *args], capture_output=True,
                            text=True, encoding="utf-8", errors="replace")
    if result.returncode != 0:
        raise RuntimeError(f"git {' '.join(args)} failed: {result.stderr.strip()}")
    return result.stdout


def _parse_added(diff: str) -> dict[str, list[tuple[int, str]]]:
    added: dict[str, list[tuple[int, str]]] = {}
    path, lineno = None, 0
    for raw in diff.splitlines():
        if raw.startswith("+++ "):
            path = raw[4:] if raw[4:] != "/dev/null" else None
            if path and path.startswith("b/"):
                path = path[2:]
        elif raw.startswith("@@"):
            m = re.match(r"@@ -\S+ \+(\d+)(?:,(\d+))? @@", raw)
            lineno = int(m.group(1)) if m else 0
        elif raw.startswith("+") and path:
            added.setdefault(path, []).append((lineno, raw[1:]))
            lineno += 1
    return added


def collect_staged() -> tuple[dict[str, list[tuple[int, str]]], dict[str, str]]:
    added = _parse_added(_git("diff", "--cached", "-U0", "--no-color", "--diff-filter=ACMR"))
    names = _git("diff", "--cached", "--name-only", "--diff-filter=ACMR").splitlines()
    contents = {n: _git("show", ":" + n) for n in names if _is_dx_file(n)}
    return added, contents


def _release_exclusions(csproj_text: str) -> list[str]:
    base = posixpath.dirname(CSPROJ)
    return [posixpath.join(base, g.replace("\\", "/")) for g in _REMOVE_ATTR.findall(csproj_text)]


def collect_release() -> tuple[dict[str, str], list[str]]:
    names = _git("ls-files").splitlines()
    wanted = [n for n in names if _is_dx_file(n) or posixpath.basename(n) == "Directory.Packages.props"]
    files = {}
    for n in wanted:
        if os.path.isfile(n):
            with open(n, encoding="utf-8", errors="replace") as f:
                files[n] = f.read()
    excluded = _release_exclusions(files.get(CSPROJ, ""))
    return files, excluded


def main(argv: list[str]) -> int:
    if len(argv) != 1 or argv[0] not in ("--staged", "--release"):
        print("usage: check_dx.py --staged | --release", file=sys.stderr)
        return 2
    try:
        os.chdir(_git("rev-parse", "--show-toplevel").strip())
        if argv[0] == "--staged":
            added, contents = collect_staged()
            violations = check_staged(added, contents)
        else:
            files, excluded = collect_release()
            violations = check_release(files, excluded)
    except (RuntimeError, OSError) as e:
        print(f"check_dx: {e}", file=sys.stderr)
        return 2
    report(violations)
    return 1 if violations else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
