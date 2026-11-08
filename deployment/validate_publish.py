"""Fail closed on routine production database or local-only resource changes."""

import json
from pathlib import Path
import re
import sys


def validate(manifest_path, publish_path):
    resources = json.loads(Path(manifest_path).read_text())["resources"]
    forbidden = ("postgres", "migration", "producer", "keyvault", "key-vault")
    for name, resource in resources.items():
        description = f"{name} {resource.get('type', '')} {resource.get('path', '')}".lower()
        if any(word in description for word in forbidden):
            raise ValueError("Routine publish contains a database or local-only resource.")

    deployments = {name for name, resource in resources.items() if "deployment" in resource}
    if deployments != {"cache", "boardadmin", "bingoboard"}:
        raise ValueError("Routine publish must deploy only cache, boardadmin, and bingoboard.")

    db = resources["db"]
    if (db.get("type") != "parameter.v0"
            or db.get("connectionString") != "{db.value}"
            or db.get("value") != "{db.inputs.value}"
            or db.get("inputs", {}).get("value") != {"type": "string", "secret": True}):
        raise ValueError("Database must be an external secret input with no generated/default credential.")
    admin = resources["boardadmin"]
    if (admin["env"].get("ConnectionStrings__db") != "{db.connectionString}"
            or admin["deployment"]["params"].get("db_connectionstring") != "{db.connectionString}"):
        raise ValueError("Admin must use the external database connection directly.")

    files = list(Path(publish_path).rglob("*.bicep"))
    expected = {"main", "env", "env-acr", "cache", "boardadmin", "bingoboard"}
    if len(files) != len(expected) or {file.stem for file in files} != expected:
        raise ValueError("Unexpected or missing production Bicep modules.")
    for file in files:
        text = file.read_text().lower()
        if any(word in text for word in (
            "microsoft.dbforpostgresql", "microsoft.keyvault", "keyvaulturl",
            "producer", "migration", "postgres",
        )):
            raise ValueError("Production Bicep must not manage PostgreSQL, Key Vault, or local-only workers.")

    admin_bicep = next(file.read_text() for file in files if file.stem == "boardadmin")
    required = (
        r"@secure\(\)\s+param db_connectionstring string",
        r"name:\s*'connectionstrings--db'\s+value:\s*db_connectionstring",
        r"name:\s*'ConnectionStrings__db'\s+secretRef:\s*'connectionstrings--db'",
    )
    if not all(re.search(pattern, admin_bicep) for pattern in required):
        raise ValueError("Admin database connection must flow through a secure external Bicep parameter.")
    env = next(file.read_text() for file in files if file.stem == "env")
    destinations = re.findall(r"\bdestination:\s*'([^']*)'", env)
    if (destinations != ["none"]
            or not re.search(r"appLogsConfiguration:\s*\{\s*destination:\s*'none'\s*\}", env)
            or "logAnalyticsConfiguration:" in env):
        raise ValueError("Production environment log ingestion must remain disabled.")
    if not (re.search(r"retentionInDays:\s*30\b", env)
            and re.search(r"dailyQuotaGb:\s*1\b", env)):
        raise ValueError("Production log retention/cap safeguards changed.")


if __name__ == "__main__":
    try:
        validate(sys.argv[1], sys.argv[2])
    except ValueError as error:
        sys.exit(f"::error::{error}")
    except (KeyError, OSError, IndexError):
        # Never print artifact contents, which could contain secret inputs.
        sys.exit("::error::Routine production deployment safety validation failed.")
    print("Routine production deployment safety validation passed.")
