import json
from pathlib import Path
import tempfile
import unittest

from validate_publish import validate


class PublishSafetyTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.manifest = self.root / "manifest.json"
        self.resources = {
            "db": {
                "type": "parameter.v0",
                "connectionString": "{db.value}",
                "value": "{db.inputs.value}",
                "inputs": {"value": {"type": "string", "secret": True}},
            },
            "boardadmin": {
                "env": {"ConnectionStrings__db": "{db.connectionString}"},
                "deployment": {"params": {"db_connectionstring": "{db.connectionString}"}},
            },
            "cache": {"deployment": {}},
            "bingoboard": {"deployment": {}},
        }
        for name in ("main", "env", "env-acr", "cache", "boardadmin", "bingoboard"):
            (self.root / f"{name}.bicep").write_text("")
        (self.root / "env.bicep").write_text(
            "appLogsConfiguration: {\ndestination: 'none'\n}\n"
            "retentionInDays: 30\ndailyQuotaGb: 1\n")
        (self.root / "boardadmin.bicep").write_text(
            "@secure()\nparam db_connectionstring string\n"
            "name: 'connectionstrings--db'\nvalue: db_connectionstring\n"
            "name: 'ConnectionStrings__db'\nsecretRef: 'connectionstrings--db'\n")

    def check(self):
        self.manifest.write_text(json.dumps({"resources": self.resources}))
        validate(self.manifest, self.root)

    def test_accepts_external_connection_and_only_routine_workloads(self):
        self.check()

    def test_rejects_emergency_regressions(self):
        for name in ("postgres", "migrations", "producer-console"):
            with self.subTest(name=name):
                self.resources[name] = {"type": "project.v1"}
                with self.assertRaises(ValueError):
                    self.check()
                del self.resources[name]
        for resource_type in ("Microsoft.DBforPostgreSQL/flexibleServers",
                              "Microsoft.KeyVault/vaults/secrets"):
            with self.subTest(resource_type=resource_type):
                (self.root / "main.bicep").write_text(resource_type)
                with self.assertRaises(ValueError):
                    self.check()
        (self.root / "main.bicep").write_text("")
        self.resources["db"]["inputs"]["value"]["default"] = {"generate": {}}
        with self.assertRaises(ValueError):
            self.check()

    def test_rejects_wrong_admin_connection_or_weakened_logs(self):
        self.resources["boardadmin"]["env"]["ConnectionStrings__db"] = "{old-secret.value}"
        with self.assertRaises(ValueError):
            self.check()
        self.resources["boardadmin"]["env"]["ConnectionStrings__db"] = "{db.connectionString}"
        (self.root / "env.bicep").write_text(
            "appLogsConfiguration: {\ndestination: 'none'\n}\n"
            "retentionInDays: 90\ndailyQuotaGb: 5\n")
        with self.assertRaises(ValueError):
            self.check()

    def test_rejects_reenabled_or_unspecified_environment_logging(self):
        for logging in ("", "destination: 'log-analytics'", "destination: 'azure-monitor'",
                        "destination: 'none'\nlogAnalyticsConfiguration: {}"):
            with self.subTest(logging=logging):
                (self.root / "env.bicep").write_text(
                    f"appLogsConfiguration: {{\n{logging}\n}}\n"
                    "retentionInDays: 30\ndailyQuotaGb: 1\n")
                with self.assertRaisesRegex(ValueError, "ingestion must remain disabled"):
                    self.check()


if __name__ == "__main__":
    unittest.main()
