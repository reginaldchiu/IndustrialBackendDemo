CREATE TABLE devices
(
    id UUID NOT NULL,
    name VARCHAR(100) NOT NULL,
    serial_number VARCHAR(100) NOT NULL,
    status SMALLINT NOT NULL,
    created_at_utc TIMESTAMPTZ NOT NULL,
    updated_at_utc TIMESTAMPTZ NOT NULL,

    CONSTRAINT pk_devices PRIMARY KEY (id),
    CONSTRAINT ux_devices_serial_number UNIQUE (serial_number),
    CONSTRAINT ck_devices_name CHECK (name !~ '^[[:space:]]*$'),
    CONSTRAINT ck_devices_serial_number CHECK (serial_number !~ '^[[:space:]]*$'),
    CONSTRAINT ck_devices_status CHECK (status IN (0, 1, 2, 3))
);

CREATE TABLE analysis_jobs
(
    id UUID NOT NULL,
    device_id UUID NOT NULL,
    status SMALLINT NOT NULL,
    created_at_utc TIMESTAMPTZ NOT NULL,
    started_at_utc TIMESTAMPTZ NULL,
    completed_at_utc TIMESTAMPTZ NULL,
    failure_reason TEXT NULL,

    CONSTRAINT pk_analysis_jobs PRIMARY KEY (id),
    CONSTRAINT fk_analysis_jobs_devices
        FOREIGN KEY (device_id)
        REFERENCES devices (id)
        ON DELETE RESTRICT,
    CONSTRAINT ck_analysis_jobs_status CHECK (status IN (0, 1, 2, 3)),
    CONSTRAINT ck_analysis_jobs_lifecycle
        CHECK
        (
            (
                status = 0
                AND started_at_utc IS NULL
                AND completed_at_utc IS NULL
                AND failure_reason IS NULL
            )
            OR
            (
                status = 1
                AND started_at_utc IS NOT NULL
                AND completed_at_utc IS NULL
                AND failure_reason IS NULL
            )
            OR
            (
                status = 2
                AND started_at_utc IS NOT NULL
                AND completed_at_utc IS NOT NULL
                AND failure_reason IS NULL
            )
            OR
            (
                status = 3
                AND started_at_utc IS NOT NULL
                AND completed_at_utc IS NOT NULL
                AND failure_reason IS NOT NULL
                AND failure_reason !~ '^[[:space:]]*$'
            )
        )
);

CREATE INDEX ix_analysis_jobs_device_id_created_at_utc
    ON analysis_jobs (device_id, created_at_utc);

CREATE INDEX ix_analysis_jobs_status_created_at_utc
    ON analysis_jobs (status, created_at_utc);
