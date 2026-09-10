@echo off
chcp 65001 > NUL
setlocal enabledelayedexpansion

:: ----------------------------------------------------------
:: Configuration
:: ----------------------------------------------------------
set "DUCKDB_PATH=app_data.duckdb"
set "DUCKDB_CLI=duckdb.exe"
set "TEMP_SQL=%TEMP%\duckdb_setup_%RANDOM%.sql"

echo =========================================================================
echo   DuckDB Database Setup ^& Migration Tool
echo =========================================================================
echo.

:: ----------------------------------------------------------
:: [1/4] Check Environment
:: ----------------------------------------------------------
echo [1/4] Checking DuckDB CLI availability...
where %DUCKDB_CLI% >nul 2>nul
if %ERRORLEVEL% neq 0 (
    echo [ERROR] '%DUCKDB_CLI%' was not found in PATH or the current directory.
    pause
    exit /b 1
)
echo       - Status: OK

:: ----------------------------------------------------------
:: [2/4] Generate SQL Script Block
:: ----------------------------------------------------------
echo [2/4] Generating temporary DDL script...

(
    echo -- Create Schema
    echo CREATE SCHEMA IF NOT EXISTS nats;
    echo.
    echo -- Create Measurements Table
    echo CREATE TABLE IF NOT EXISTS nats.measurements ^(
    echo     measurement_id      UUID PRIMARY KEY,
    echo     measurement_code    VARCHAR NOT NULL UNIQUE,
    echo     title               VARCHAR NOT NULL,
    echo     device_id           VARCHAR NOT NULL,
    echo     operator_name       VARCHAR,
    echo     measured_at         TIMESTAMPTZ NOT NULL,
    echo     created_at          TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    echo     attributes_json     VARCHAR
    echo ^);
    echo.
    echo -- Create Channel Metrics Table
    echo CREATE TABLE IF NOT EXISTS nats.channel_metrics ^(
    echo     channel_id          UUID PRIMARY KEY,
    echo     measurement_id      UUID NOT NULL REFERENCES nats.measurements^(measurement_id^),
    echo     group_name          VARCHAR NOT NULL,
    echo     channel_name        VARCHAR NOT NULL,
    echo     unit                VARCHAR,
    echo     data_type           VARCHAR NOT NULL,
    echo     sample_rate_hz      DOUBLE NOT NULL,
    echo     sample_count        BIGINT NOT NULL,
    echo     stat_min            DOUBLE,
    echo     stat_max            DOUBLE,
    echo     stat_mean           DOUBLE,
    echo     stat_rms            DOUBLE,
    echo     stat_stddev         DOUBLE,
    parquet_file_path   VARCHAR NOT NULL,
    echo     parquet_row_group   INTEGER DEFAULT 0,
    echo     updated_at          TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    echo     CONSTRAINT uk_measurement_group_channel UNIQUE ^(measurement_id, group_name, channel_name^)
    echo ^);
) > "%TEMP_SQL%"

echo       - Script generated: "%TEMP_SQL%"

:: ----------------------------------------------------------
:: [3/4] Execute DDL Script
:: ----------------------------------------------------------
echo [3/4] Executing SQL script on '%DUCKDB_PATH%'...
type "%TEMP_SQL%" | "%DUCKDB_CLI%" "%DUCKDB_PATH%"
set BUILD_STATUS=%ERRORLEVEL%

:: Always clean up temp file
if exist "%TEMP_SQL%" del "%TEMP_SQL%"

if %BUILD_STATUS% neq 0 (
    echo [ERROR] DDL execution failed.
    pause
    exit /b %BUILD_STATUS%
)
echo       - Status: Success

:: ----------------------------------------------------------
:: [4/4] Verification and Data Display (Max 5 rows)
:: ----------------------------------------------------------
echo.
echo =========================================================================
echo   [4/4] Verification ^& Data Preview
echo =========================================================================
echo.
echo ■ Registered Tables in Schema 'nats':
"%DUCKDB_CLI%" "%DUCKDB_PATH%" -c "SELECT table_schema AS schema_name, table_name FROM information_schema.tables WHERE table_schema = 'nats';"

echo.
echo ■ Table Content: nats.measurements (Max 5 rows)
"%DUCKDB_CLI%" "%DUCKDB_PATH%" -c "SELECT * FROM nats.measurements LIMIT 5;"

echo.
echo ■ Table Content: nats.channel_metrics (Max 5 rows)
"%DUCKDB_CLI%" "%DUCKDB_PATH%" -c "SELECT * FROM nats.channel_metrics LIMIT 5;"

echo =========================================================================
echo   Setup process completed successfully!
echo =========================================================================

endlocal
pause
