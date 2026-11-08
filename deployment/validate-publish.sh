#!/usr/bin/env bash
set -euo pipefail

output="${1:?Provide an unused artifact output directory}"
mkdir -p "$output"
dotnet run ./src/apphost.cs -- --publisher manifest --output-path "$output/manifest.json"
aspire publish --apphost ./src/apphost.cs --environment Production --non-interactive -o "$output/bicep"
az bicep build --file "$output/bicep/env/env.bicep" --outfile "$output/environment.arm.json"
python3 -B ./deployment/validate_publish.py "$output/manifest.json" "$output/bicep" "$output/environment.arm.json"
