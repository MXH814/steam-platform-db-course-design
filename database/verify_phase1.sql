SET SERVEROUTPUT ON
SET DEFINE OFF
WHENEVER SQLERROR EXIT FAILURE ROLLBACK

PROMPT Verifying phase 1 database deliverables...

DECLARE
  v_count NUMBER;
  v_owner VARCHAR2(128) := SYS_CONTEXT('USERENV', 'CURRENT_SCHEMA');

  PROCEDURE assert_eq(p_name VARCHAR2, p_actual NUMBER, p_expected NUMBER) IS
  BEGIN
    IF p_actual <> p_expected THEN
      RAISE_APPLICATION_ERROR(-20000, p_name || ' expected ' || p_expected || ' but got ' || p_actual);
    END IF;
    DBMS_OUTPUT.PUT_LINE('PASS: ' || p_name || ' = ' || p_expected);
  END;

  PROCEDURE assert_zero(p_name VARCHAR2, p_actual NUMBER) IS
  BEGIN
    assert_eq(p_name, p_actual, 0);
  END;

  PROCEDURE assert_positive(p_name VARCHAR2, p_actual NUMBER) IS
  BEGIN
    IF p_actual <= 0 THEN
      RAISE_APPLICATION_ERROR(-20001, p_name || ' expected positive count but got ' || p_actual);
    END IF;
    DBMS_OUTPUT.PUT_LINE('PASS: ' || p_name || ' count = ' || p_actual);
  END;

  PROCEDURE expect_error(p_name VARCHAR2, p_sql VARCHAR2, p_sqlcode NUMBER) IS
  BEGIN
    BEGIN
      EXECUTE IMMEDIATE p_sql;
      RAISE_APPLICATION_ERROR(-20002, p_name || ' unexpectedly succeeded');
    EXCEPTION
      WHEN OTHERS THEN
        IF SQLCODE = p_sqlcode THEN
          DBMS_OUTPUT.PUT_LINE('PASS: ' || p_name || ' raised expected SQLCODE ' || p_sqlcode);
        ELSE
          RAISE;
        END IF;
    END;
  END;
BEGIN
  SELECT COUNT(*) INTO v_count
  FROM all_tables
  WHERE owner = v_owner
    AND table_name IN (
    'PLAYER', 'WALLET_ACCOUNT', 'WALLET_TRANSACTION', 'DEVELOPER', 'ADMIN_USER',
    'SYS_NOTICE', 'GAME', 'GAME_ORDER', 'ORDER_DETAIL', 'ORDER_STATUS_LOG',
    'PAYMENT_TRANSACTION', 'REFUND_TICKET', 'REFUND_DETAIL', 'REFUND_AUDIT_LOG',
    'PLAYER_LIBRARY', 'CDKEY_BATCH', 'CDKEY', 'CDKEY_REDEEM_LOG',
    'GAME_REVIEW', 'REVIEW_VERSION', 'ACHIEVEMENT', 'PLAYER_ACHIEVEMENT',
    'ITEM_TEMPLATE', 'INVENTORY_ITEM', 'MARKET_ORDER', 'MARKET_TRADE',
    'ITEM_TRANSFER_LEDGER'
  );
  assert_eq('core table count', v_count, 27);

  SELECT COUNT(*) INTO v_count
  FROM all_tab_columns
  WHERE owner = v_owner
    AND table_name = 'PLAYER'
    AND column_name = 'WALLET_BALANCE';
  assert_zero('PLAYER.wallet_balance column count', v_count);

  SELECT COUNT(*) INTO v_count FROM all_constraints WHERE owner = v_owner AND constraint_type = 'P';
  assert_eq('primary key constraint count', v_count, 27);

  SELECT COUNT(*) INTO v_count FROM all_constraints WHERE owner = v_owner AND constraint_type = 'R';
  assert_eq('foreign key constraint count', v_count, 40);

  SELECT COUNT(*) INTO v_count FROM all_constraints WHERE owner = v_owner AND constraint_type = 'U';
  assert_eq('unique constraint count', v_count, 13);

  SELECT COUNT(*) INTO v_count FROM all_constraints WHERE owner = v_owner AND constraint_type = 'C';
  assert_positive('check constraint', v_count);

  SELECT COUNT(*) INTO v_count
  FROM all_indexes
  WHERE owner = v_owner
    AND index_name = 'UK_MARKET_ACTIVE_SELL_ITEM';
  assert_eq('active sell item unique index count', v_count, 1);

  FOR t IN (
    SELECT column_value AS table_name
    FROM TABLE(sys.odcivarchar2list(
      'PLAYER', 'WALLET_ACCOUNT', 'WALLET_TRANSACTION', 'DEVELOPER', 'ADMIN_USER',
      'SYS_NOTICE', 'GAME', 'GAME_ORDER', 'ORDER_DETAIL', 'ORDER_STATUS_LOG',
      'PAYMENT_TRANSACTION', 'REFUND_TICKET', 'REFUND_DETAIL', 'REFUND_AUDIT_LOG',
      'PLAYER_LIBRARY', 'CDKEY_BATCH', 'CDKEY', 'CDKEY_REDEEM_LOG',
      'GAME_REVIEW', 'REVIEW_VERSION', 'ACHIEVEMENT', 'PLAYER_ACHIEVEMENT',
      'ITEM_TEMPLATE', 'INVENTORY_ITEM', 'MARKET_ORDER', 'MARKET_TRADE',
      'ITEM_TRANSFER_LEDGER'
    ))
  ) LOOP
    EXECUTE IMMEDIATE 'SELECT COUNT(*) FROM ' || t.table_name INTO v_count;
    assert_positive('seed rows in ' || t.table_name, v_count);
  END LOOP;

  SELECT COUNT(*) INTO v_count
  FROM (
    SELECT p.user_id,
           w.available_balance,
           w.frozen_balance,
           w.available_balance + w.frozen_balance AS total_balance
    FROM player p
    JOIN wallet_account w ON w.user_id = p.user_id
    WHERE p.user_id = 'P002'
      AND w.available_balance = 242.75
      AND w.frozen_balance = 50.00
      AND w.available_balance + w.frozen_balance = 292.75
  );
  assert_eq('computed total balance query', v_count, 1);

  expect_error(
    'duplicate player account',
    q'[INSERT INTO player (user_id, account, password_hash, nickname, credit_score, status, version, create_time, update_time)
       VALUES ('P_DUP', 'alice', 'x', 'Dup', 100, 'NORMAL', 0, SYSTIMESTAMP, SYSTIMESTAMP)]',
    -1
  );

  expect_error(
    'negative wallet balance check',
    q'[INSERT INTO wallet_account (wallet_id, user_id, available_balance, frozen_balance, version)
       VALUES ('W_BAD', 'P001', -1, 0, 0)]',
    -2290
  );

  expect_error(
    'invalid order player foreign key',
    q'[INSERT INTO game_order (order_id, user_id, total_amount, order_type, order_status, payment_status, idempotency_key, expire_time, create_time)
       VALUES ('O_BAD', 'P404', 1, 'BUY_GAME', 'CREATED', 'UNPAID', 'idem-bad-order', SYSTIMESTAMP, SYSTIMESTAMP)]',
    -2291
  );

  expect_error(
    'duplicate library ownership',
    q'[INSERT INTO player_library (lib_id, user_id, game_id, acquire_way, status, play_minutes)
       VALUES ('LIB_DUP', 'P001', 'GAME_DST', 'BUY', 'NORMAL', 0)]',
    -1
  );

  expect_error(
    'duplicate achievement unlock',
    q'[INSERT INTO player_achievement (unlock_id, user_id, ach_id, unlock_time)
       VALUES ('PA_DUP', 'P001', 'ACH_DST_SURVIVE_001', SYSTIMESTAMP)]',
    -1
  );

  expect_error(
    'duplicate active sell order for same item',
    q'[INSERT INTO market_order (market_order_id, user_id, template_id, item_id, order_type, target_price, frozen_amount, status, create_time)
       VALUES ('MO_SELL_DUP', 'P001', 'ITPL_CS2_AK_REDLINE', 'ITEM_CS2_001', 'SELL', 49, 0, 'MATCHING', SYSTIMESTAMP)]',
    -1
  );
END;
/

ROLLBACK;

PROMPT Phase 1 database verification passed.
