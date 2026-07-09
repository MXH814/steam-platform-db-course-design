SET DEFINE OFF

PROMPT Applying developer-login and backend-completion migration...

DECLARE
  v_count NUMBER;
BEGIN
  SELECT COUNT(*)
    INTO v_count
    FROM user_tab_cols
   WHERE table_name = 'DEVELOPER'
     AND column_name = 'PASSWORD_HASH';

  IF v_count = 0 THEN
    EXECUTE IMMEDIATE 'ALTER TABLE developer ADD (password_hash VARCHAR2(256))';
  END IF;
END;
/

UPDATE developer
   SET password_hash = 'PBKDF2$SHA256$100000$c2VlZC12YWx2ZS1kZXZfX18=$apqFEKjAoaMZvUroAQ9eaiAH4qutVdFRtt0Yorzqf44='
 WHERE dev_id = 'DEV_VALVE';

UPDATE developer
   SET password_hash = 'PBKDF2$SHA256$100000$c2VlZC1rbGVpLWRldl9fX18=$Syi9RKVX+XpYxt6A39k3dDAC0DAfWVxDolxY0mRn4O8='
 WHERE dev_id = 'DEV_KLEI';

DECLARE
  v_nullable VARCHAR2(1);
BEGIN
  SELECT nullable
    INTO v_nullable
    FROM user_tab_cols
   WHERE table_name = 'DEVELOPER'
     AND column_name = 'PASSWORD_HASH';

  IF v_nullable = 'Y' THEN
    EXECUTE IMMEDIATE 'ALTER TABLE developer MODIFY (password_hash NOT NULL)';
  END IF;
END;
/

COMMIT;

PROMPT Migration completed.
