SET SERVEROUTPUT ON
SET FEEDBACK ON
SET VERIFY OFF
SET DEFINE OFF
WHENEVER SQLERROR EXIT FAILURE ROLLBACK

PROMPT Session A: locking Alice's wallet row for eight seconds.

DECLARE
  v_version wallet_account.version%TYPE;
BEGIN
  SELECT version
  INTO v_version
  FROM wallet_account
  WHERE user_id = 'P001'
  FOR UPDATE;

  DBMS_OUTPUT.PUT_LINE('SESSION A LOCKED | user=P001 | version=' || v_version);
  DBMS_SESSION.SLEEP(8);
  ROLLBACK;
  DBMS_OUTPUT.PUT_LINE('SESSION A RELEASED | rollback completed');
END;
/
