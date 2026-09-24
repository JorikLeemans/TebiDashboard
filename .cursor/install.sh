#!/usr/bin/env bash
# Idempotent bootstrap for the TEBI Dashboard Cloud Agent environment.
# Installs the .NET 10 SDK and PostgreSQL if missing, then restores and
# builds the non-MAUI solution filter (API + Web + shared libs + tests).
set -euo pipefail

DOTNET_VERSION="10.0.301"
DOTNET_DIR="$HOME/.dotnet"

# Install the pinned .NET SDK into the user home when it is not already present.
if ! "$DOTNET_DIR/dotnet" --list-sdks 2>/dev/null | grep -q "$DOTNET_VERSION"; then
  echo "Installing .NET SDK $DOTNET_VERSION ..."
  curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
  chmod +x /tmp/dotnet-install.sh
  /tmp/dotnet-install.sh --version "$DOTNET_VERSION" --install-dir "$DOTNET_DIR"
fi

export PATH="$DOTNET_DIR:$PATH"
export DOTNET_ROOT="$DOTNET_DIR"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1

# Install PostgreSQL (the app's default provider) when it is not already available.
if ! command -v pg_ctlcluster >/dev/null 2>&1; then
  echo "Installing PostgreSQL ..."
  sudo apt-get update -qq
  sudo apt-get install -y -qq postgresql postgresql-contrib
fi

# Restore and build the CI/local solution filter (excludes the MAUI project,
# which requires Windows/Android workloads unavailable in a Linux container).
cd "$(dirname "$0")/.."
dotnet restore src/TebiDashboard.slnf
dotnet build src/TebiDashboard.slnf -c Release --no-restore

echo "install.sh completed."
