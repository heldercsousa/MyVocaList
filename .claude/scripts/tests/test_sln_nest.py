"""sln_nest tests — REQ-TUR-10: Docs solution items sit in the folder that mirrors the disk."""
import os
import sys
import unittest

sys.path.insert(0, os.path.normpath(os.path.join(os.path.dirname(__file__), "..")))
import sln_nest as sn  # noqa: E402

FT = sn.FOLDER_TYPE
ROOT_ITEMS = "{00000000-0000-0000-0000-0000000000A1}"
DOCS = "{00000000-0000-0000-0000-0000000000D0}"
MGMT = "{00000000-0000-0000-0000-0000000000D1}"
APP = "{11111111-1111-1111-1111-111111111111}"


def sln(extra_blocks="", nested_extra=""):
    return (
        "\nMicrosoft Visual Studio Solution File, Format Version 12.00\n"
        'Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "App", "App\\App.csproj", "' + APP + '"\n'
        "EndProject\n"
        'Project("' + FT + '") = "Itens", "Itens", "' + ROOT_ITEMS + '"\n'
        "\tProjectSection(SolutionItems) = preProject\n"
        "\t\tCLAUDE.md = CLAUDE.md\n"
        "\t\tDocs\\Management\\feat\\changes\\c1\\plan.md = Docs\\Management\\feat\\changes\\c1\\plan.md\n"
        "\t\tDocs\\Management\\top.md = Docs\\Management\\top.md\n"
        "\tEndProjectSection\n"
        "EndProject\n"
        'Project("' + FT + '") = "Docs", "Docs", "' + DOCS + '"\n'
        "EndProject\n"
        'Project("' + FT + '") = "Management", "Management", "' + MGMT + '"\n'
        "\tProjectSection(SolutionItems) = preProject\n"
        "\t\tDocs\\Management\\ok.md = Docs\\Management\\ok.md\n"
        "\tEndProjectSection\n"
        "EndProject\n" + extra_blocks +
        "Global\n"
        "\tGlobalSection(NestedProjects) = preSolution\n"
        "\t\t" + MGMT + " = " + DOCS + "\n" + nested_extra +
        "\tEndGlobalSection\n"
        "EndGlobal\n"
    )


def where(text):
    s = sn.Sln(text)
    by_guid = dict((g, "/".join(p)) for p, g in s.paths().items())
    return dict((rel, by_guid[g]) for rel, g in s.entries())


class PlaceTests(unittest.TestCase):
    def test_misplaced_lists_only_docs_entries_in_the_wrong_folder(self):
        self.assertEqual(sn.misplaced(sln()), [
            "Docs\\Management\\feat\\changes\\c1\\plan.md", "Docs\\Management\\top.md"])

    def test_entry_moves_to_an_existing_folder(self):
        out = sn.place(sln(), ["Docs\\Management\\top.md"])
        self.assertEqual(where(out)["Docs\\Management\\top.md"], "Docs/Management")
        self.assertEqual(out.count("Docs\\Management\\top.md = "), 1)

    def test_missing_folder_chain_is_created_and_nested(self):
        rel = "Docs\\Management\\feat\\changes\\c1\\plan.md"
        out = sn.place(sln(), [rel])
        self.assertEqual(where(out)[rel], "Docs/Management/feat/changes/c1")
        self.assertEqual(len(sn.Sln(out).folders()), len(sn.Sln(sln()).folders()) + 3)
        self.assertEqual(sn.misplaced(out), ["Docs\\Management\\top.md"])

    def test_placing_everything_is_idempotent_and_keeps_other_entries(self):
        once = sn.place(sln(), sn.misplaced(sln()))
        self.assertEqual(sn.misplaced(once), [])
        self.assertEqual(sn.place(once, sn.misplaced(once)), once)
        self.assertEqual(where(once)["CLAUDE.md"], "Itens")
        self.assertEqual(sn.docs_entries(once), sn.docs_entries(sln()))
        self.assertIn(APP, once)

    def test_new_path_is_added_once_even_if_listed_elsewhere(self):
        rel = "Docs\\Management\\ok.md"
        dup = sln().replace("\t\tCLAUDE.md = CLAUDE.md\n",
                            "\t\tCLAUDE.md = CLAUDE.md\n\t\t" + rel + " = " + rel + "\n")
        out = sn.place(dup, [rel])
        self.assertEqual(out.count(rel + " = "), 1)
        self.assertEqual(where(out)[rel], "Docs/Management")

    def test_forward_slash_input_is_accepted(self):
        out = sn.place(sln(), ["Docs/Management/new/README.md"])
        self.assertEqual(where(out)["Docs\\Management\\new\\README.md"], "Docs/Management/new")

    def test_non_docs_path_is_refused(self):
        with self.assertRaises(sn.SlnError):
            sn.place(sln(), ["src\\x.md"])

    def test_crlf_is_kept(self):
        out = sn.place(sln().replace("\n", "\r\n"), ["Docs\\Management\\top.md"])
        self.assertNotIn("\n", out.replace("\r\n", ""))

    def test_register_places_a_new_readme_in_its_mirrored_folder(self):
        """AC-10.4: backlog_gen.register lists the item README under its own folder."""
        sys.path.insert(0, os.path.normpath(os.path.join(os.path.dirname(__file__), "..", "backlog")))
        import backlog_gen
        out = backlog_gen.sln_add_entry(sln(), "Docs/Management/feat/bugs/b1/README.md")
        self.assertEqual(where(out)["Docs\\Management\\feat\\bugs\\b1\\README.md"],
                         "Docs/Management/feat/bugs/b1")

    def test_folder_names_match_case_insensitively(self):
        """Regression 2026-11-16: `Docs/Management` exists, so a path spelled with another
        case must reuse it, never create a second folder beside it."""
        out = sn.place(sln(), ["Docs\\management\\x.md"])
        self.assertEqual(len(sn.Sln(out).folders()), len(sn.Sln(sln()).folders()))
        self.assertEqual(sn.case_duplicates(out), 0)
        self.assertEqual(sn.misplaced(out), sn.misplaced(sln()))

    def test_case_variant_duplicate_folders_are_merged_with_their_children(self):
        dup = "{00000000-0000-0000-0000-0000000000E1}"
        child = "{00000000-0000-0000-0000-0000000000E2}"
        blocks = ('Project("' + FT + '") = "management", "management", "' + dup + '"\n'
                  "\tProjectSection(SolutionItems) = preProject\n"
                  "\t\tDocs\\Management\\dup.md = Docs\\Management\\dup.md\n"
                  "\tEndProjectSection\nEndProject\n"
                  'Project("' + FT + '") = "kid", "kid", "' + child + '"\n'
                  "\tProjectSection(SolutionItems) = preProject\n"
                  "\t\tDocs\\Management\\kid\\k.md = Docs\\Management\\kid\\k.md\n"
                  "\tEndProjectSection\nEndProject\n")
        nested = "\t\t" + dup + " = " + DOCS + "\n\t\t" + child + " = " + dup + "\n"
        text = sln(extra_blocks=blocks, nested_extra=nested)
        self.assertEqual(sn.case_duplicates(text), 1)
        out = sn.place(text, [])
        self.assertEqual(sn.case_duplicates(out), 0)
        self.assertNotIn(dup, out)
        self.assertEqual(where(out)["Docs\\Management\\dup.md"], "Docs/Management")
        self.assertEqual(where(out)["Docs\\Management\\kid\\k.md"], "Docs/Management/kid")
        self.assertEqual(sn.docs_entries(out), sn.docs_entries(text))

    def test_malformed_blocks_are_repaired(self):
        # The shape found in MyVocaList.sln: a block lost its EndProject (the next Project
        # line starts while it is open) and a stray EndProject follows a later block.
        other = ('Project("' + FT + '") = "Other", "Other", "{00000000-0000-0000-0000-0000000000D2}"\n'
                 "EndProject\nEndProject\n")
        bad = sln(extra_blocks=other).replace(
            "\t\tDocs\\Management\\ok.md = Docs\\Management\\ok.md\n\tEndProjectSection\nEndProject\n",
            "\t\tDocs\\Management\\ok.md = Docs\\Management\\ok.md\n\tEndProjectSection\n")
        s = sn.Sln(bad)
        self.assertEqual(s.repairs, 2)
        text = s.text()
        self.assertEqual(where(text)["Docs\\Management\\ok.md"], "Docs/Management")
        self.assertEqual(text.count("\nEndProject\n"), text.count("\nProject("))
        self.assertEqual(sn.Sln(text).repairs, 0)


if __name__ == "__main__":
    unittest.main()
