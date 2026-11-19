"""Place `Docs/` solution items in the solution folder that mirrors their physical folder.

In `MyVocaList.sln` a file is listed under a Solution Folder. The logical tree must equal the
physical one: `Docs\\Management\\X\\feat\\changes\\slug\\plan.md` belongs to the solution folder
chain Docs > Management > X > feat > changes > slug. Missing folders are created.

  place(text, rel_paths)   pure: returns the new .sln text with each path listed in its folder
                           (an entry listed anywhere else is moved, never duplicated)
  python sln_nest.py [--dry-run]   one-time repair: move every `Docs\\` entry that sits in a
                           solution folder not matching its physical folder

New folder GUIDs are derived from the folder path (uuid5), so a re-run is stable. The two-digit
`FA1234BC-...` counter in constraints-reference.md is for hand-made folders only.
The file is rewritten byte-preservingly: BOM and line endings are kept.
"""
import os
import re
import sys
import uuid

FOLDER_TYPE = "{2150E333-8FDC-42A3-9474-1A3956D46DE8}"
_PROJECT = re.compile(r'^Project\("(\{[^}]+\})"\) = "([^"]*)", "([^"]*)", "(\{[^}]+\})"\s*$')
_NS = uuid.UUID("6f0f3c1e-5a57-4d0b-9d0a-3d1c5a2b7e41")
ROOT = "Docs"


class SlnError(Exception):
    pass


class Sln(object):
    def __init__(self, text):
        self.nl = "\r\n" if "\r\n" in text else "\n"
        lines = text.split(self.nl)
        try:
            g = next(i for i, l in enumerate(lines) if l.strip() == "Global")
        except StopIteration:
            raise SlnError("no Global section")
        first = next((i for i, l in enumerate(lines) if l.startswith("Project(")), g)
        self.head, self.tail = lines[:first], lines[g:]
        # Tolerant block parse. A `Project(` line met while a block is still open closes that
        # block (its `EndProject` was lost), and an `EndProject` with no open block is dropped.
        # Both defects existed in MyVocaList.sln before 2026-11-15; `repairs` counts them.
        self.blocks, cur, self.repairs = [], None, 0
        for l in lines[first:g]:
            if l.startswith("Project("):
                if cur is not None:
                    cur.append("EndProject")
                    self.blocks.append(cur)
                    self.repairs += 1
                cur = [l]
            elif cur is not None:
                cur.append(l)
                if l.strip() == "EndProject":
                    self.blocks.append(cur)
                    cur = None
            elif l.strip() == "EndProject":
                self.repairs += 1
            elif l.strip():
                raise SlnError("unexpected line outside a Project block: " + l[:60])
        if cur is not None:
            raise SlnError("unterminated Project block at end of the project list")
        try:
            self.n0 = next(i for i, l in enumerate(self.tail) if "GlobalSection(NestedProjects)" in l)
            self.n1 = next(i for i in range(self.n0, len(self.tail)) if "EndGlobalSection" in self.tail[i])
        except StopIteration:
            raise SlnError("no NestedProjects section")

    # ---- model ----
    def folders(self):
        out = {}
        for b in self.blocks:
            m = _PROJECT.match(b[0])
            if m and m.group(1).upper() == FOLDER_TYPE:
                out[m.group(4).upper()] = (m.group(2), b)
        return out

    def nested(self):
        out = {}
        for l in self.tail[self.n0 + 1:self.n1]:
            if "=" in l:
                a, b = [x.strip().upper() for x in l.split("=", 1)]
                out[a] = b
        return out

    def guid_paths(self):
        """[(logical path tuple, guid)] for every solution folder, in file order."""
        folders, nested, out = self.folders(), self.nested(), []
        for g in folders:
            parts, node, guard = [], g, 0
            while node in folders and guard < 60:
                parts.append(folders[node][0])
                node = nested.get(node)
                guard += 1
            out.append((tuple(reversed(parts)), g))
        return out

    def paths(self):
        """{logical path tuple: guid}; the first folder wins when two share a path."""
        out = {}
        for parts, g in self.guid_paths():
            out.setdefault(parts, g)
        return out

    def paths_ci(self):
        """`paths()` keyed case-insensitively: Visual Studio and the .slnx parser treat
        `Docs/ChangeLog` and `Docs/Changelog` as the same folder."""
        out = {}
        for parts, g in self.paths().items():
            out.setdefault(tuple(x.lower() for x in parts), g)
        return out

    def merge_case_duplicates(self):
        """Fold solution folders whose paths differ only by letter case into the first one.
        Returns the number of folders removed."""
        removed = 0
        while True:
            seen, dup = {}, None
            for parts, g in sorted(self.guid_paths(), key=lambda kv: len(kv[0])):
                key = tuple(x.lower() for x in parts)
                if key in seen and seen[key] != g:
                    dup = (seen[key], g)
                    break
                seen.setdefault(key, g)
            if dup is None:
                return removed
            keep, gone = dup
            block = self.folders()[gone][1]
            for l in block:
                if l.startswith("\t\t") and " = " in l:
                    self.add_entry(keep, l.strip().split(" = ", 1)[0])
            self.blocks = [b for b in self.blocks if b is not block]
            for i in range(self.n1 - 1, self.n0, -1):
                a, b = [x.strip().upper() for x in self.tail[i].split("=", 1)]
                if a == gone:
                    del self.tail[i]
                    self.n1 -= 1
                elif b == gone:
                    self.tail[i] = "\t\t{0} = {1}".format(a, keep)
            removed += 1

    def entries(self):
        """[(entry path, folder guid)] for every solution item."""
        out = []
        for g, (_name, b) in self.folders().items():
            for l in b:
                if l.startswith("\t\t") and " = " in l:
                    out.append((l.strip().split(" = ", 1)[0], g))
        return out

    # ---- edits ----
    def ensure_folder(self, parts):
        parts = tuple(parts)
        known = self.paths_ci()
        key = tuple(x.lower() for x in parts)
        if key in known:
            return known[key]
        if len(parts) <= 1:
            raise SlnError("root solution folder '{0}' not found".format("/".join(parts)))
        parent = self.ensure_folder(parts[:-1])
        guid = "{" + str(uuid.uuid5(_NS, "/".join(parts))).upper() + "}"
        if guid in self.folders():
            raise SlnError("GUID collision for " + "/".join(parts))
        name = parts[-1]
        self.blocks.append([
            'Project("{0}") = "{1}", "{1}", "{2}"'.format(FOLDER_TYPE, name, guid),
            "EndProject",
        ])
        self.tail.insert(self.n1, "\t\t{0} = {1}".format(guid, parent))
        self.n1 += 1
        return guid

    def remove_entry(self, rel):
        removed = 0
        for b in self.blocks:
            for i in range(len(b) - 1, -1, -1):
                if b[i].startswith("\t\t") and b[i].strip().split(" = ", 1)[0] == rel:
                    del b[i]
                    removed += 1
        return removed

    def add_entry(self, guid, rel):
        b = self.folders()[guid][1]
        line = "\t\t{0} = {0}".format(rel)
        end = next((i for i, l in enumerate(b) if l.strip() == "EndProjectSection"), None)
        if end is None:
            b[1:1] = ["\tProjectSection(SolutionItems) = preProject", line, "\tEndProjectSection"]
        else:
            b.insert(end, line)

    def text(self):
        lines = list(self.head)
        for b in self.blocks:
            lines.extend(b)
        lines.extend(self.tail)
        return self.nl.join(lines)


def folder_parts(rel):
    return tuple(rel.replace("/", "\\").split("\\")[:-1])


def place(text, rel_paths):
    """Return the .sln text with each `Docs\\...` path listed under its mirrored folder."""
    sln = Sln(text)
    sln.merge_case_duplicates()
    for rel in rel_paths:
        rel = rel.replace("/", "\\")
        parts = folder_parts(rel)
        if not parts or parts[0] != ROOT:
            raise SlnError("not a Docs path: " + rel)
        guid = sln.ensure_folder(parts)
        sln.remove_entry(rel)
        sln.add_entry(guid, rel)
    return sln.text()


def misplaced(text):
    """Docs entries whose solution folder path differs from their physical folder."""
    sln = Sln(text)
    by_guid = dict((g, tuple(x.lower() for x in p)) for p, g in sln.paths().items())
    return sorted(rel for rel, g in sln.entries()
                  if rel.startswith(ROOT + "\\")
                  and by_guid.get(g) != tuple(x.lower() for x in folder_parts(rel)))


def case_duplicates(text):
    """Number of solution folders whose path differs from another only by letter case."""
    paths = [p for p, _g in Sln(text).guid_paths()]
    return len(paths) - len(set(tuple(x.lower() for x in p) for p in paths))


def docs_entries(text):
    return sorted(rel for rel, _g in Sln(text).entries() if rel.startswith(ROOT + "\\"))


def main(argv):
    path = os.path.join(os.environ.get("CLAUDE_PROJECT_DIR", "."), "MyVocaList.sln")
    raw = open(path, "rb").read()
    bom = raw.startswith(b"\xef\xbb\xbf")
    text = raw[3:].decode("utf-8") if bom else raw.decode("utf-8")
    todo = misplaced(text)
    before = docs_entries(text)
    dupes = len(before) - len(set(before))  # the same file listed in two solution folders
    new = place(text, sorted(set(todo)))
    after = docs_entries(new)
    if set(before) != set(after) or len(after) != len(set(after)):
        raise SlnError("Docs entry set changed: {0} distinct -> {1}".format(len(set(before)), len(after)))
    if dupes:
        print("sln_nest: {0} duplicate listings collapse to one entry each.".format(dupes))
    if misplaced(new) or case_duplicates(new):
        raise SlnError("entries are still misplaced after the move")
    created = len(Sln(new).folders()) - len(Sln(text).folders())
    repairs, case_dupes = Sln(text).repairs, case_duplicates(text)
    print("sln_nest: {0} of {1} Docs entries misplaced; {2} solution folders to create; "
          "{3} malformed EndProject lines to repair; {4} case-variant duplicate folders to merge.".format(
              len(todo), len(before), max(created + case_dupes, 0), repairs, case_dupes))
    repairs += case_dupes
    if "--dry-run" in argv or not (todo or repairs):
        print("sln_nest: nothing written.")
        return 0
    open(path, "wb").write((b"\xef\xbb\xbf" if bom else b"") + new.encode("utf-8"))
    print("sln_nest: MyVocaList.sln rewritten.")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
