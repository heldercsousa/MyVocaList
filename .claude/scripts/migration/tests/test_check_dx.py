# .claude/scripts/migration/tests/test_check_dx.py
"""Tests for check_dx.py (UI component library migration, task T0.2)."""
import contextlib
import io
import os
import sys
import unittest

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
import check_dx  # noqa: E402

DX_NS = 'xmlns:dx="http://schemas.devexpress.com/maui"'


def rules(violations):
    return [v.rule for v in violations]


class CheckDxTests(unittest.TestCase):
    def test_sf_file_with_dx_namespace_fails(self):
        """[AC] REQ-UIMIG-11: an Sf copy must not reference DevExpress."""
        path = "MyVocaList/UI/X/FooSf.xaml"
        result = check_dx.check_staged({}, {path: DX_NS + "\n<dx:DXButton/>"})
        self.assertIn(check_dx.Violation(path, 1, "sf-file-references-devexpress"), result)

    def test_sf_file_name_pattern_is_case_sensitive(self):
        """[AC] REQ-UIMIG-11: only exact Sf / SfPage suffixes mark an Sf copy."""
        self.assertIsNone(check_dx.SF_FILE.match("Foosf.xaml"))
        self.assertIsNotNone(check_dx.SF_FILE.match("VenuesSfPage.xaml.cs"))

    def test_closing_tag_and_markup_extension_forms_are_detected(self):
        """[AC] REQ-UIMIG-11: closing tags and markup extensions count as references."""
        prefixes = {"dx"}
        self.assertTrue(check_dx.has_dx_reference("</dx:DXButton>", prefixes))
        self.assertTrue(check_dx.has_dx_reference("Foo=\"{x:Static dx:Foo.Bar}\"", prefixes))

    def test_added_dx_line_in_dx_original_fails(self):
        """[AC] REQ-UIMIG-12: a new DevExpress line in a DX original is blocked."""
        path = "MyVocaList/UI/Pages/Venues/VenuesPage.xaml.cs"
        added = {path: [(12, "using DevExpress.Maui.Core;")]}
        result = check_dx.check_staged(added, {path: "using DevExpress.Maui.Core;\n"})
        self.assertEqual(["added-devexpress-line"], rules(result))

    def test_bridge_files_are_exempt(self):
        """[AC] REQ-UIMIG-12: bridge files may carry DevExpress lines."""
        for path in check_dx.BRIDGE_FILES:
            added = {path: [(3, "using DevExpress.Maui.Controls;")]}
            self.assertEqual([], check_dx.check_staged(added, {path: "using DevExpress.Maui.Controls;"}))

    def test_removed_lines_are_ignored(self):
        """[AC] REQ-UIMIG-12: untouched or removed DX lines are not flagged."""
        path = "MyVocaList/UI/Pages/Venues/VenuesPage.xaml"
        result = check_dx.check_staged({}, {path: DX_NS + "\n<dx:DXButton/>"})
        self.assertNotIn("added-devexpress-line", rules(result))

    def test_word_containing_dx_without_declared_prefix_passes(self):
        """[AC] REQ-UIMIG-12: no declared DX prefix means no false positive."""
        path = "MyVocaList/UI/Pages/Venues/VenuesPage.xaml"
        line = '<local:dxLike Text="dx:none"/>'
        result = check_dx.check_staged({path: [(5, line)]}, {path: line})
        self.assertEqual([], result)

    def test_block_message_names_exception_registry(self):
        """Review Focus 1: the block message points to the exception registry."""
        out = io.StringIO()
        with contextlib.redirect_stdout(out), contextlib.redirect_stderr(out):
            check_dx.report([check_dx.Violation("a.cs", 1, "added-devexpress-line")])
        self.assertIn("exception-registry.md", out.getvalue())

    def test_release_flags_package_reference(self):
        """[AC] REQ-UIMIG-14: a DevExpress package reference fails the release check."""
        files = {"Directory.Packages.props": '<PackageVersion Include="DevExpress.Maui.Core" Version="1" />'}
        result = check_dx.check_release(files, [])
        self.assertEqual(["release-references-devexpress"], rules(result))

    def test_release_skips_excluded_paths(self):
        """[AC] REQ-UIMIG-14: files removed from compilation are not checked."""
        files = {"MyVocaList/UI/Old/Thing.xaml": DX_NS}
        self.assertEqual([], check_dx.check_release(files, ["MyVocaList/UI/Old/**/*.xaml"]))
        self.assertEqual([], check_dx.check_release(files, ["MyVocaList/UI/Old/**"]))

    def test_license_key_literal_is_flagged(self):
        """[AC] REQ-UIMIG-17: a literal license key never appears in a tracked file."""
        path = "MyVocaList/MauiProgram.cs"
        bad = 'RegisterLicense("' + "A" * 40 + '");'
        ok = "RegisterLicense(SyncfusionLicense.Key);"
        self.assertEqual(["license-key-in-tracked-file"],
                         rules(check_dx.check_staged({path: [(1, bad)]}, {path: bad})))
        self.assertEqual([], check_dx.check_staged({path: [(1, ok)]}, {path: ok}))

    def test_exit_codes(self):
        """Exit codes: 0 clean, 1 violations, 2 usage error."""
        self.assertEqual(2, check_dx.main([]))
        orig = check_dx.collect_staged
        try:
            check_dx.collect_staged = lambda: ({}, {})
            with contextlib.redirect_stdout(io.StringIO()):
                self.assertEqual(0, check_dx.main(["--staged"]))
            check_dx.collect_staged = lambda: ({"a.cs": [(1, "using DevExpress.X;")]}, {"a.cs": "using DevExpress.X;"})
            with contextlib.redirect_stdout(io.StringIO()):
                self.assertEqual(1, check_dx.main(["--staged"]))
        finally:
            check_dx.collect_staged = orig


if __name__ == "__main__":
    unittest.main()
