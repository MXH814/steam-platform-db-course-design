SET SERVEROUTPUT ON
SET FEEDBACK ON
SET VERIFY OFF
SET DEFINE OFF
WHENEVER SQLERROR EXIT FAILURE ROLLBACK

PROMPT Session B: expecting a bounded lock timeout while Session A owns the row.

DECLARE
  v_version wallet_account.version%TYPE;
BEGIN
  BEGIN
    SELECT version
    INTO v_version
    FROM wallet_account
    WHERE user_id = 'P001'
    FOR UPDATE WAIT 2;

    RAISE_APPLICATION_ERROR(
      -20120,
      'Session B acquired the row unexpectedly; start Session A first.'
    );
  EXCEPTION
    WHEN OTHERS THEN
      IF SQLCODE IN (-54, -30006) THEN
        DBMS_OUTPUT.PUT_LINE(
          'PASS | SESSION B BLOCKED | SQLCODE=' || SQLCODE ||
          ' proves row-level serialization'
        );
      ELSE
        RAISE;
      END IF;
  END;

  ROLLBACK;
END;
/
