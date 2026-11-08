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
        self.arm = self.root / "environment.arm.json"
        self.environment_properties = {"appLogsConfiguration": {"destination": None}}
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
            "appLogsConfiguration: {\ndestination: null\n}\n"
            "retentionInDays: 30\ndailyQuotaGb: 1\n")
        (self.root / "boardadmin.bicep").write_text(
            "@secure()\nparam db_connectionstring string\n"
            "name: 'connectionstrings--db'\nvalue: db_connectionstring\n"
            "name: 'ConnectionStrings__db'\nsecretRef: 'connectionstrings--db'\n")

    def check(self):
        self.manifest.write_text(json.dumps({"resources": self.resources}))
        self.arm.write_text(json.dumps({"resources": [{
            "type": "Microsoft.App/managedEnvironments",
            "properties": self.environment_properties,
        }]}))
        validate(self.manifest, self.root, self.arm)

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
            "appLogsConfiguration: {\ndestination: null\n}\n"
            "retentionInDays: 90\ndailyQuotaGb: 5\n")
        with self.assertRaises(ValueError):
            self.check()

    def test_accepts_supported_disabled_arm_shapes(self):
        for properties in (
                {},
                {"appLogsConfiguration": None},
                {"appLogsConfiguration": {}},
                {"appLogsConfiguration": {"destination": None, "logAnalyticsConfiguration": None}}):
            with self.subTest(properties=properties):
                self.environment_properties = properties
                self.check()

    def test_rejects_invalid_literal_none_or_attached_logging_in_arm(self):
        for logging in (
                {"destination": "none"},
                {"destination": "log-analytics"},
                {"destination": "azure-monitor"},
                {"destination": None, "logAnalyticsConfiguration": {}},
                {"logAnalyticsConfiguration": {"customerId": "synthetic-workspace"}}):
            with self.subTest(logging=logging):
                self.environment_properties = {"appLogsConfiguration": logging}
                with self.assertRaisesRegex(ValueError, "ingestion must remain disabled"):
                    self.check()


if __name__ == "__main__":
    unittest.main()
