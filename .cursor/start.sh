#!/usr/bin/env bash
# Per-boot startup: ensure the local PostgreSQL instance is running and that the
# application role and database referenced by the default connection string exist.
# The API applies EF Core migrations and seeds demo data on startup, so no schema
# work is needed here. This script is idempotent and safe to re-run.
set -euo pipefail

DB_NAME="TebiDashboard"
DB_USER="Tebi"
DB_PASSWORD="Tebi_dev_password"

# Start the default PostgreSQL 16 cluster if it is not already accepting connections.
if ! pg_isready -q 2>/dev/null; then
  echo "Starting PostgreSQL cluster ..."
  sudo pg_ctlcluster 16 main start || true
  # Wait until the server is ready before creating roles/databases.
  for _ in $(seq 1 30); do
    pg_isready -q && break
    sleep 1
  done
fi

# Create the login role if it does not exist yet.
if ! sudo -u postgres psql -tAc "SELECT 1 FROM pg_roles WHERE rolname='${DB_USER}'" | grep -q 1; then
  echo "Creating role ${DB_USER} ..."
  sudo -u postgres psql -c "CREATE ROLE \"${DB_USER}\" LOGIN PASSWORD '${DB_PASSWORD}';"
fi

# Create the application database (owned by the role) if it does not exist yet.
if ! sudo -u postgres psql -tAc "SELECT 1 FROM pg_database WHERE datname='${DB_NAME}'" | grep -q 1; then
  echo "Creating database ${DB_NAME} ..."
  sudo -u postgres createdb -O "${DB_USER}" "${DB_NAME}"
fi
sudo -u postgres psql -c "GRANT ALL PRIVILEGES ON DATABASE \"${DB_NAME}\" TO \"${DB_USER}\";" >/dev/null

echo "start.sh completed. PostgreSQL is ready on localhost:5432."
