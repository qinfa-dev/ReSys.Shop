-- ============================================================================
-- Quartz.NET PostgreSQL Database Schema
-- Table prefix: qrtz_
-- ============================================================================

-- Drop tables if they exist (for clean install)
DROP TABLE IF EXISTS qrtz_fired_triggers CASCADE;
DROP TABLE IF EXISTS qrtz_paused_trigger_grps CASCADE;
DROP TABLE IF EXISTS qrtz_scheduler_state CASCADE;
DROP TABLE IF EXISTS qrtz_locks CASCADE;
DROP TABLE IF EXISTS qrtz_simprop_triggers CASCADE;
DROP TABLE IF EXISTS qrtz_simple_triggers CASCADE;
DROP TABLE IF EXISTS qrtz_cron_triggers CASCADE;
DROP TABLE IF EXISTS qrtz_blob_triggers CASCADE;
DROP TABLE IF EXISTS qrtz_triggers CASCADE;
DROP TABLE IF EXISTS qrtz_job_details CASCADE;
DROP TABLE IF EXISTS qrtz_calendars CASCADE;

-- ============================================================================
-- Job Details Table
-- ============================================================================
CREATE TABLE qrtz_job_details
(
    sched_name        VARCHAR(120) NOT NULL,
    job_name          VARCHAR(200) NOT NULL,
    job_group         VARCHAR(200) NOT NULL,
    description       VARCHAR(250),
    job_class_name    VARCHAR(250) NOT NULL,
    is_durable        BOOLEAN NOT NULL,
    is_nonconcurrent  BOOLEAN NOT NULL,
    is_update_data    BOOLEAN NOT NULL,
    requests_recovery BOOLEAN NOT NULL,
    job_data          BYTEA,
    PRIMARY KEY (sched_name, job_name, job_group)
);

-- ============================================================================
-- Triggers Table
-- ============================================================================
CREATE TABLE qrtz_triggers
(
    sched_name     VARCHAR(120) NOT NULL,
    trigger_name   VARCHAR(200) NOT NULL,
    trigger_group  VARCHAR(200) NOT NULL,
    job_name       VARCHAR(200) NOT NULL,
    job_group      VARCHAR(200) NOT NULL,
    description    VARCHAR(250),
    next_fire_time BIGINT,
    prev_fire_time BIGINT,
    priority       INTEGER,
    trigger_state  VARCHAR(16) NOT NULL,
    trigger_type   VARCHAR(8) NOT NULL,
    start_time     BIGINT NOT NULL,
    end_time       BIGINT,
    calendar_name  VARCHAR(200),
    misfire_instr  SMALLINT,
    job_data       BYTEA,
    PRIMARY KEY (sched_name, trigger_name, trigger_group),
    FOREIGN KEY (sched_name, job_name, job_group)
        REFERENCES qrtz_job_details (sched_name, job_name, job_group)
);

-- ============================================================================
-- Simple Triggers Table
-- ============================================================================
CREATE TABLE qrtz_simple_triggers
(
    sched_name      VARCHAR(120) NOT NULL,
    trigger_name    VARCHAR(200) NOT NULL,
    trigger_group   VARCHAR(200) NOT NULL,
    repeat_count    BIGINT NOT NULL,
    repeat_interval BIGINT NOT NULL,
    times_triggered BIGINT NOT NULL,
    PRIMARY KEY (sched_name, trigger_name, trigger_group),
    FOREIGN KEY (sched_name, trigger_name, trigger_group)
        REFERENCES qrtz_triggers (sched_name, trigger_name, trigger_group) ON DELETE CASCADE
);

-- ============================================================================
-- Cron Triggers Table
-- ============================================================================
CREATE TABLE qrtz_cron_triggers
(
    sched_name      VARCHAR(120) NOT NULL,
    trigger_name    VARCHAR(200) NOT NULL,
    trigger_group   VARCHAR(200) NOT NULL,
    cron_expression VARCHAR(250) NOT NULL,
    time_zone_id    VARCHAR(80),
    PRIMARY KEY (sched_name, trigger_name, trigger_group),
    FOREIGN KEY (sched_name, trigger_name, trigger_group)
        REFERENCES qrtz_triggers (sched_name, trigger_name, trigger_group) ON DELETE CASCADE
);

-- ============================================================================
-- Simple Property Triggers Table
-- ============================================================================
CREATE TABLE qrtz_simprop_triggers
(
    sched_name    VARCHAR(120) NOT NULL,
    trigger_name  VARCHAR(200) NOT NULL,
    trigger_group VARCHAR(200) NOT NULL,
    str_prop_1    VARCHAR(512),
    str_prop_2    VARCHAR(512),
    str_prop_3    VARCHAR(512),
    int_prop_1    INT,
    int_prop_2    INT,
    long_prop_1   BIGINT,
    long_prop_2   BIGINT,
    dec_prop_1    NUMERIC(13,4),
    dec_prop_2    NUMERIC(13,4),
    bool_prop_1   BOOLEAN,
    bool_prop_2   BOOLEAN,
    time_zone_id  VARCHAR(80),
    PRIMARY KEY (sched_name, trigger_name, trigger_group),
    FOREIGN KEY (sched_name, trigger_name, trigger_group)
        REFERENCES qrtz_triggers (sched_name, trigger_name, trigger_group) ON DELETE CASCADE
);

-- ============================================================================
-- Blob Triggers Table
-- ============================================================================
CREATE TABLE qrtz_blob_triggers
(
    sched_name    VARCHAR(120) NOT NULL,
    trigger_name  VARCHAR(200) NOT NULL,
    trigger_group VARCHAR(200) NOT NULL,
    blob_data     BYTEA,
    PRIMARY KEY (sched_name, trigger_name, trigger_group),
    FOREIGN KEY (sched_name, trigger_name, trigger_group)
        REFERENCES qrtz_triggers (sched_name, trigger_name, trigger_group) ON DELETE CASCADE
);

-- ============================================================================
-- Calendars Table
-- ============================================================================
CREATE TABLE qrtz_calendars
(
    sched_name    VARCHAR(120) NOT NULL,
    calendar_name VARCHAR(200) NOT NULL,
    calendar      BYTEA NOT NULL,
    PRIMARY KEY (sched_name, calendar_name)
);

-- ============================================================================
-- Paused Trigger Groups Table
-- ============================================================================
CREATE TABLE qrtz_paused_trigger_grps
(
    sched_name    VARCHAR(120) NOT NULL,
    trigger_group VARCHAR(200) NOT NULL,
    PRIMARY KEY (sched_name, trigger_group)
);

-- ============================================================================
-- Fired Triggers Table
-- ============================================================================
CREATE TABLE qrtz_fired_triggers
(
    sched_name        VARCHAR(120) NOT NULL,
    entry_id          VARCHAR(140) NOT NULL,
    trigger_name      VARCHAR(200) NOT NULL,
    trigger_group     VARCHAR(200) NOT NULL,
    instance_name     VARCHAR(200) NOT NULL,
    fired_time        BIGINT NOT NULL,
    sched_time        BIGINT NOT NULL,
    priority          INTEGER NOT NULL,
    state             VARCHAR(16) NOT NULL,
    job_name          VARCHAR(200),
    job_group         VARCHAR(200),
    is_nonconcurrent  BOOLEAN,
    requests_recovery BOOLEAN,
    PRIMARY KEY (sched_name, entry_id)
);

-- ============================================================================
-- Scheduler State Table
-- ============================================================================
CREATE TABLE qrtz_scheduler_state
(
    sched_name        VARCHAR(120) NOT NULL,
    instance_name     VARCHAR(200) NOT NULL,
    last_checkin_time BIGINT NOT NULL,
    checkin_interval  BIGINT NOT NULL,
    PRIMARY KEY (sched_name, instance_name)
);

-- ============================================================================
-- Locks Table
-- ============================================================================
CREATE TABLE qrtz_locks
(
    sched_name VARCHAR(120) NOT NULL,
    lock_name  VARCHAR(40)  NOT NULL,
    PRIMARY KEY (sched_name, lock_name)
);

-- ============================================================================
-- Indexes
-- ============================================================================
CREATE INDEX idx_qrtz_j_req_recovery ON qrtz_job_details (sched_name, requests_recovery);
CREATE INDEX idx_qrtz_j_grp ON qrtz_job_details (sched_name, job_group);

CREATE INDEX idx_qrtz_t_j ON qrtz_triggers (sched_name, job_name, job_group);
CREATE INDEX idx_qrtz_t_jg ON qrtz_triggers (sched_name, job_group);
CREATE INDEX idx_qrtz_t_c ON qrtz_triggers (sched_name, calendar_name);
CREATE INDEX idx_qrtz_t_g ON qrtz_triggers (sched_name, trigger_group);
CREATE INDEX idx_qrtz_t_state ON qrtz_triggers (sched_name, trigger_state);
CREATE INDEX idx_qrtz_t_n_state ON qrtz_triggers (sched_name, trigger_name, trigger_group, trigger_state);
CREATE INDEX idx_qrtz_t_n_g_state ON qrtz_triggers (sched_name, trigger_group, trigger_state);
CREATE INDEX idx_qrtz_t_next_fire_time ON qrtz_triggers (sched_name, next_fire_time);
CREATE INDEX idx_qrtz_t_nft_st ON qrtz_triggers (sched_name, trigger_state, next_fire_time);
CREATE INDEX idx_qrtz_t_nft_misfire ON qrtz_triggers (sched_name, misfire_instr, next_fire_time);
CREATE INDEX idx_qrtz_t_nft_st_misfire ON qrtz_triggers (sched_name, misfire_instr, next_fire_time, trigger_state);
CREATE INDEX idx_qrtz_t_nft_st_misfire_grp ON qrtz_triggers (sched_name, misfire_instr, next_fire_time, trigger_group, trigger_state);

CREATE INDEX idx_qrtz_ft_trig_inst_name ON qrtz_fired_triggers (sched_name, instance_name);
CREATE INDEX idx_qrtz_ft_inst_job_req_rcvry ON qrtz_fired_triggers (sched_name, instance_name, requests_recovery);
CREATE INDEX idx_qrtz_ft_j_g ON qrtz_fired_triggers (sched_name, job_name, job_group);
CREATE INDEX idx_qrtz_ft_jg ON qrtz_fired_triggers (sched_name, job_group);
CREATE INDEX idx_qrtz_ft_t_g ON qrtz_fired_triggers (sched_name, trigger_name, trigger_group);
CREATE INDEX idx_qrtz_ft_tg ON qrtz_fired_triggers (sched_name, trigger_group);

-- ============================================================================
-- Initial Lock Rows
-- ============================================================================
INSERT INTO qrtz_locks VALUES ('ReSysFashionScheduler', 'TRIGGER_ACCESS');
INSERT INTO qrtz_locks VALUES ('ReSysFashionScheduler', 'JOB_ACCESS');
INSERT INTO qrtz_locks VALUES ('ReSysFashionScheduler', 'CALENDAR_ACCESS');
INSERT INTO qrtz_locks VALUES ('ReSysFashionScheduler', 'STATE_ACCESS');
INSERT INTO qrtz_locks VALUES ('ReSysFashionScheduler', 'MISFIRE_ACCESS');

-- ============================================================================
-- Grant Permissions (adjust username as needed)
-- ============================================================================
-- GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA public TO your_app_user;
-- GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA public TO your_app_user;

-- # Run PostgreSQL schema script (production only)
-- psql -U postgres -d resysfashion -f quartz_schema.sql