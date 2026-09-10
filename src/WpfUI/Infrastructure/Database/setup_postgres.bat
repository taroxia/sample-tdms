@echo off
chcp 65001 > NUL
setlocal enabledelayedexpansion

:: ----------------------------------------------------------
:: PostgreSQL Environment Variables
:: ----------------------------------------------------------
set PGHOST=localhost
set PGPORT=5432
set PGDATABASE=postgres
set PGUSER=postgres
set PGPASSWORD=postgres

set TARGET_USER=natc
set TARGET_PASS=Nats10000
set TARGET_SCHEMA=nats

echo [1/3] Checking and creating user '%TARGET_USER%'...
psql -h %PGHOST% -p %PGPORT% -U %PGUSER% -d %PGDATABASE% -v ON_ERROR_STOP=1 -c "DO $$ BEGIN IF NOT EXISTS (SELECT FROM pg_catalog.pg_roles WHERE rolname = '%TARGET_USER%') THEN CREATE ROLE %TARGET_USER% WITH LOGIN PASSWORD '%TARGET_PASS%'; END IF; END $$;"
if %ERRORLEVEL% neq 0 (
    echo [ERROR] Failed to create or verify user.
    exit /b %ERRORLEVEL%
)

echo [2/3] Checking and creating schema '%TARGET_SCHEMA%'...
psql -h %PGHOST% -p %PGPORT% -U %PGUSER% -d %PGDATABASE% -v ON_ERROR_STOP=1 -c "CREATE SCHEMA IF NOT EXISTS %TARGET_SCHEMA% AUTHORIZATION %TARGET_USER%;"
psql -h %PGHOST% -p %PGPORT% -U %PGUSER% -d %PGDATABASE% -v ON_ERROR_STOP=1 -c "GRANT ALL ON SCHEMA %TARGET_SCHEMA% TO %TARGET_USER%;"
if %ERRORLEVEL% neq 0 (
    echo [ERROR] Failed to create schema.
    exit /b %ERRORLEVEL%
)

echo [3/3] Creating tables under schema '%TARGET_SCHEMA%'...
psql -h %PGHOST% -p %PGPORT% -U %PGUSER% -d %PGDATABASE% -v ON_ERROR_STOP=1 -c "
CREATE TABLE IF NOT EXISTS nats.measurements (
    measurement_id      UUID PRIMARY KEY,
    measurement_code    VARCHAR(64) NOT NULL UNIQUE,
    title               VARCHAR(256) NOT NULL,
    device_id           VARCHAR(64) NOT NULL,
    operator_name       VARCHAR(64),
    measured_at         TIMESTAMP WITH TIME ZONE NOT NULL,
    created_at          TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP NOT NULL,
    attributes_json     TEXT
);

CREATE TABLE IF NOT EXISTS nats.channel_metrics (
    channel_id          UUID PRIMARY KEY,
    measurement_id      UUID NOT NULL REFERENCES nats.measurements(measurement_id) ON DELETE CASCADE,
    group_name          VARCHAR(128) NOT NULL,
    channel_name        VARCHAR(128) NOT NULL,
    unit                VARCHAR(32),
    data_type           VARCHAR(32) NOT NULL,
    sample_rate_hz      DOUBLE PRECISION NOT NULL,
    sample_count        BIGINT NOT NULL,
    stat_min            DOUBLE PRECISION,
    stat_max            DOUBLE PRECISION,
    stat_mean           DOUBLE PRECISION,
    stat_rms            DOUBLE PRECISION,
    stat_stddev         DOUBLE PRECISION,
    parquet_file_path   VARCHAR(1024) NOT NULL,
    parquet_row_group   INT DEFAULT 0 NOT NULL,
    updated_at          TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP NOT NULL,
    CONSTRAINT uk_measurement_group_channel UNIQUE (measurement_id, group_name, channel_name)
);

CREATE INDEX IF NOT EXISTS idx_measurements_measured_at ON nats.measurements(measured_at);
CREATE INDEX IF NOT EXISTS idx_channel_metrics_lookup ON nats.channel_metrics(measurement_id, group_name);
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA nats TO natc;
"

if %ERRORLEVEL% equ 0 (
    echo [SUCCESS] PostgreSQL database setup completed successfully.
) else (
    echo [ERROR] Failed to execute table initialization.
)

endlocal
pause
