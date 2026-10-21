"""Small registry isolation check; never contacts or modifies a Unity Editor."""
import unittest

from run_lastbell_server import INSTANCE, LastBellRegistry, PROJECT_HASH, PROJECT_ROOT


class ProjectGuardTests(unittest.IsolatedAsyncioTestCase):
    async def test_accepts_exact_lastbell_identity(self):
        registry = LastBellRegistry()
        session, _ = await registry.register("test", "Client", PROJECT_HASH, "6000.3.15f1", PROJECT_ROOT)
        self.assertEqual(session.project_path, PROJECT_ROOT)
        self.assertEqual(f"{session.project_name}@{session.project_hash}", INSTANCE)

    async def test_rejects_other_path_missing_path_and_wrong_hash(self):
        registry = LastBellRegistry()
        for path, hash_value in [("C:/Github/SimpleGame", PROJECT_HASH),
                                 (None, PROJECT_HASH), (PROJECT_ROOT, "wrong-hash")]:
            with self.subTest(path=path, hash=hash_value):
                with self.assertRaises(ValueError):
                    await registry.register("test", "Client", hash_value, "6000.3.15f1", path)
        self.assertEqual(await registry.list_sessions(), {})


if __name__ == "__main__":
    unittest.main()
