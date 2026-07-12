DECLARE
  v_count NUMBER;
BEGIN
  SELECT COUNT(*) INTO v_count
    FROM user_tab_cols
   WHERE table_name = 'PAYMENT_TRANSACTION'
     AND column_name = 'PAYMENT_METHOD';

  IF v_count = 0 THEN
    EXECUTE IMMEDIATE 'ALTER TABLE PAYMENT_TRANSACTION ADD (payment_method VARCHAR2(20))';
  END IF;

  EXECUTE IMMEDIATE 'UPDATE PAYMENT_TRANSACTION SET payment_method = ''STEAM_WALLET'' WHERE payment_method IS NULL';
  EXECUTE IMMEDIATE 'ALTER TABLE PAYMENT_TRANSACTION MODIFY (payment_method DEFAULT ''STEAM_WALLET'' NOT NULL)';

  SELECT COUNT(*) INTO v_count
    FROM user_constraints
   WHERE table_name = 'PAYMENT_TRANSACTION'
     AND constraint_name = 'CK_PAYMENT_METHOD';

  IF v_count = 0 THEN
    EXECUTE IMMEDIATE q'[ALTER TABLE PAYMENT_TRANSACTION ADD CONSTRAINT CK_PAYMENT_METHOD CHECK (payment_method IN ('STEAM_WALLET', 'WECHAT_PAY', 'ALIPAY', 'VISA', 'MASTERCARD'))]';
  END IF;

  SELECT COUNT(*) INTO v_count
    FROM user_tab_cols
   WHERE table_name = 'WALLET_TRANSACTION'
     AND column_name = 'PAYMENT_METHOD';

  IF v_count = 0 THEN
    EXECUTE IMMEDIATE 'ALTER TABLE WALLET_TRANSACTION ADD (payment_method VARCHAR2(20))';
  END IF;

  -- Backfill legacy wallet rows consistently as Steam Wallet history.
  EXECUTE IMMEDIATE 'UPDATE WALLET_TRANSACTION SET payment_method = ''STEAM_WALLET'' WHERE payment_method IS NULL';
  EXECUTE IMMEDIATE 'ALTER TABLE WALLET_TRANSACTION MODIFY (payment_method DEFAULT ''STEAM_WALLET'' NOT NULL)';

  SELECT COUNT(*) INTO v_count
    FROM user_constraints
   WHERE table_name = 'WALLET_TRANSACTION'
     AND constraint_name = 'CK_WALLET_TXN_PAYMENT_METHOD';

  IF v_count = 0 THEN
    EXECUTE IMMEDIATE q'[ALTER TABLE WALLET_TRANSACTION ADD CONSTRAINT CK_WALLET_TXN_PAYMENT_METHOD CHECK (payment_method IN ('STEAM_WALLET', 'WECHAT_PAY', 'ALIPAY', 'VISA', 'MASTERCARD'))]';
  END IF;
END;
/

DECLARE
  v_invalid_count NUMBER;
BEGIN
  SELECT COUNT(*) INTO v_invalid_count
    FROM payment_transaction
   WHERE payment_method IS NULL
      OR payment_method NOT IN ('STEAM_WALLET', 'WECHAT_PAY', 'ALIPAY', 'VISA', 'MASTERCARD');

  IF v_invalid_count > 0 THEN
    raise_application_error(-20012, 'Invalid PAYMENT_TRANSACTION.payment_method rows remain.');
  END IF;

  SELECT COUNT(*) INTO v_invalid_count
    FROM wallet_transaction
   WHERE payment_method IS NULL
      OR payment_method NOT IN ('STEAM_WALLET', 'WECHAT_PAY', 'ALIPAY', 'VISA', 'MASTERCARD');

  IF v_invalid_count > 0 THEN
    raise_application_error(-20013, 'Invalid WALLET_TRANSACTION.payment_method rows remain.');
  END IF;
END;
/

COMMIT;
