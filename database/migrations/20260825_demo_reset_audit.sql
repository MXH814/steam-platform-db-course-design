SET DEFINE OFF

PROMPT Creating demo-data reset audit tables...

DECLARE
  v_count NUMBER;
BEGIN
  SELECT COUNT(*) INTO v_count FROM user_tables WHERE table_name = 'DEMO_RESET_RUN';
  IF v_count = 0 THEN
    EXECUTE IMMEDIATE q'[
      CREATE TABLE DEMO_RESET_RUN (
        run_id VARCHAR2(20) NOT NULL,
        status VARCHAR2(30) NOT NULL,
        initiated_by VARCHAR2(100) NOT NULL,
        baseline_sha256 VARCHAR2(64) NOT NULL,
        started_at TIMESTAMP DEFAULT SYSTIMESTAMP NOT NULL,
        completed_at TIMESTAMP,
        error_message VARCHAR2(1000),
        CONSTRAINT PK_DEMO_RESET_RUN PRIMARY KEY (run_id),
        CONSTRAINT CK_DEMO_RESET_STATUS CHECK (status IN (
          'SNAPSHOTTING', 'SNAPSHOT_READY', 'RESETTING', 'RESET_COMPLETED',
          'RESET_FAILED', 'RESTORING', 'RESTORED', 'RESTORE_FAILED'
        ))
      )]';
  END IF;

  SELECT COUNT(*) INTO v_count FROM user_tables WHERE table_name = 'DEMO_RESET_TABLE';
  IF v_count = 0 THEN
    EXECUTE IMMEDIATE q'[
      CREATE TABLE DEMO_RESET_TABLE (
        run_id VARCHAR2(20) NOT NULL,
        table_order NUMBER(3) NOT NULL,
        source_table VARCHAR2(128) NOT NULL,
        backup_table VARCHAR2(128) NOT NULL,
        row_count NUMBER(20) NOT NULL,
        CONSTRAINT PK_DEMO_RESET_TABLE PRIMARY KEY (run_id, table_order),
        CONSTRAINT FK_DEMO_RESET_TABLE_RUN FOREIGN KEY (run_id) REFERENCES DEMO_RESET_RUN(run_id),
        CONSTRAINT UK_DEMO_RESET_SOURCE UNIQUE (run_id, source_table),
        CONSTRAINT UK_DEMO_RESET_BACKUP UNIQUE (backup_table),
        CONSTRAINT CK_DEMO_RESET_TABLE_ORDER CHECK (table_order > 0),
        CONSTRAINT CK_DEMO_RESET_ROW_COUNT CHECK (row_count >= 0)
      )]';
  END IF;

  SELECT COUNT(*) INTO v_count FROM user_tables WHERE table_name = 'DEMO_RESET_EVENT';
  IF v_count = 0 THEN
    EXECUTE IMMEDIATE q'[
      CREATE TABLE DEMO_RESET_EVENT (
        event_id VARCHAR2(32) NOT NULL,
        run_id VARCHAR2(20) NOT NULL,
        event_type VARCHAR2(40) NOT NULL,
        message VARCHAR2(1000) NOT NULL,
        event_time TIMESTAMP DEFAULT SYSTIMESTAMP NOT NULL,
        CONSTRAINT PK_DEMO_RESET_EVENT PRIMARY KEY (event_id),
        CONSTRAINT FK_DEMO_RESET_EVENT_RUN FOREIGN KEY (run_id) REFERENCES DEMO_RESET_RUN(run_id)
      )]';
  END IF;
END;
/

DECLARE
  v_count NUMBER;
BEGIN
  SELECT COUNT(*) INTO v_count FROM user_indexes WHERE index_name = 'IDX_DEMO_RESET_RUN_TIME';
  IF v_count = 0 THEN
    EXECUTE IMMEDIATE 'CREATE INDEX IDX_DEMO_RESET_RUN_TIME ON DEMO_RESET_RUN(started_at)';
  END IF;

  SELECT COUNT(*) INTO v_count FROM user_indexes WHERE index_name = 'IDX_DEMO_RESET_EVENT_RUN_TIME';
  IF v_count = 0 THEN
    EXECUTE IMMEDIATE 'CREATE INDEX IDX_DEMO_RESET_EVENT_RUN_TIME ON DEMO_RESET_EVENT(run_id, event_time)';
  END IF;
END;
/

COMMIT;

PROMPT Demo-data reset audit tables are ready.
