SET DEFINE OFF
WHENEVER SQLERROR EXIT FAILURE ROLLBACK

PROMPT Running full phase 1 verification in FREEPDB1 as STEAM_COURSE schema...

ALTER SESSION SET CONTAINER = FREEPDB1;
ALTER SESSION SET CURRENT_SCHEMA = STEAM_COURSE;

@database/schema.sql
@database/data.sql
@database/verify_phase1.sql

PROMPT Full phase 1 verification completed.

