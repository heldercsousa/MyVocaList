import os
import unittest


def _load():
    import importlib.util
    path = os.path.join(os.path.dirname(__file__), "..", "protect-generated.py")
    spec = importlib.util.spec_from_file_location("protect_generated", path)
    mod = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(mod)
    return mod


class ProtectGeneratedTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.m = _load()

    def test_blocks_generated_and_secret_files(self):
        for p in [
            r"C:\repo\Infra\Data\Migrations\20260101_Init.Designer.cs",
            "Infra/Data/Migrations/AppDbContextModelSnapshot.cs",
            r"C:\repo\.env.local",
            ".env",
            r"C:\Users\x\AppData\Roaming\DevExpress\DevExpress_License.txt",
        ]:
            self.assertIsNotNone(self.m.blocked_reason(p), p)

    def test_allows_normal_files(self):
        for p in [
            r"C:\repo\Infra\Data\Migrations\20260101_Init.cs",
            ".env.local.example",
            "MyVocaList.Services/ArtistService.cs",
            "Docs/environment.md",
            "",
        ]:
            self.assertIsNone(self.m.blocked_reason(p), p)


if __name__ == "__main__":
    unittest.main()
