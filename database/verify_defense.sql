SET SERVEROUTPUT ON
SET FEEDBACK ON
SET VERIFY OFF
SET DEFINE OFF
WHENEVER SQLERROR EXIT FAILURE ROLLBACK

PROMPT ============================================================
PROMPT Steam platform database defense verification
PROMPT All checks are read-only and leave the demo baseline unchanged.
PROMPT ============================================================

DECLARE
  v_count NUMBER;

  PROCEDURE pass(p_name VARCHAR2, p_detail VARCHAR2) IS
  BEGIN
    DBMS_OUTPUT.PUT_LINE('PASS | ' || RPAD(p_name, 38) || ' | ' || p_detail);
  END;

  PROCEDURE assert_eq(p_name VARCHAR2, p_actual NUMBER, p_expected NUMBER) IS
  BEGIN
    IF p_actual <> p_expected THEN
      RAISE_APPLICATION_ERROR(
        -20100,
        p_name || ' expected ' || p_expected || ' but got ' || p_actual
      );
    END IF;
    pass(p_name, TO_CHAR(p_actual));
  END;

  PROCEDURE assert_zero(p_name VARCHAR2, p_actual NUMBER) IS
  BEGIN
    assert_eq(p_name, p_actual, 0);
  END;

  PROCEDURE assert_at_least(p_name VARCHAR2, p_actual NUMBER, p_minimum NUMBER) IS
  BEGIN
    IF p_actual < p_minimum THEN
      RAISE_APPLICATION_ERROR(
        -20101,
        p_name || ' expected at least ' || p_minimum || ' but got ' || p_actual
      );
    END IF;
    pass(p_name, TO_CHAR(p_actual) || ' >= ' || TO_CHAR(p_minimum));
  END;
BEGIN
  SELECT COUNT(*) INTO v_count
  FROM user_tables
  WHERE table_name IN (
    'PLAYER', 'DEVELOPER', 'ADMIN_USER', 'WALLET_ACCOUNT', 'SYS_NOTICE',
    'GAME', 'GAME_ORDER', 'ORDER_DETAIL', 'ORDER_STATUS_LOG', 'PAYMENT_TRANSACTION',
    'REFUND_TICKET', 'REFUND_DETAIL', 'REFUND_AUDIT_LOG', 'PLAYER_LIBRARY',
    'CDKEY_BATCH', 'CDKEY', 'CDKEY_REDEEM_LOG', 'GAME_REVIEW', 'REVIEW_VERSION',
    'ACHIEVEMENT', 'PLAYER_ACHIEVEMENT', 'ITEM_TEMPLATE', 'INVENTORY_ITEM',
    'MARKET_ORDER', 'MARKET_TRADE', 'ITEM_TRANSFER_LEDGER', 'WALLET_TRANSACTION',
    'FRIEND_RELATION', 'DIRECT_MESSAGE', 'REVIEW_REACTION', 'WORKSHOP_ITEM',
    'WORKSHOP_SUBSCRIPTION', 'USER_NOTIFICATION', 'PLAYER_PROFILE', 'BADGE_CATALOG',
    'PLAYER_BADGE', 'TRADE_OFFER', 'TRADE_OFFER_ITEM', 'COMMUNITY_POST',
    'COMMUNITY_POST_REACTION', 'DISCUSSION_TOPIC', 'DISCUSSION_REPLY',
    'DEMO_RESET_RUN', 'DEMO_RESET_TABLE', 'DEMO_RESET_EVENT'
  );
  assert_eq('expected application tables', v_count, 45);

  SELECT COUNT(*) INTO v_count
  FROM user_constraints
  WHERE table_name IN (
    'PLAYER', 'DEVELOPER', 'ADMIN_USER', 'WALLET_ACCOUNT', 'SYS_NOTICE',
    'GAME', 'GAME_ORDER', 'ORDER_DETAIL', 'ORDER_STATUS_LOG', 'PAYMENT_TRANSACTION',
    'REFUND_TICKET', 'REFUND_DETAIL', 'REFUND_AUDIT_LOG', 'PLAYER_LIBRARY',
    'CDKEY_BATCH', 'CDKEY', 'CDKEY_REDEEM_LOG', 'GAME_REVIEW', 'REVIEW_VERSION',
    'ACHIEVEMENT', 'PLAYER_ACHIEVEMENT', 'ITEM_TEMPLATE', 'INVENTORY_ITEM',
    'MARKET_ORDER', 'MARKET_TRADE', 'ITEM_TRANSFER_LEDGER', 'WALLET_TRANSACTION',
    'FRIEND_RELATION', 'DIRECT_MESSAGE', 'REVIEW_REACTION', 'WORKSHOP_ITEM',
    'WORKSHOP_SUBSCRIPTION', 'USER_NOTIFICATION', 'PLAYER_PROFILE', 'BADGE_CATALOG',
    'PLAYER_BADGE', 'TRADE_OFFER', 'TRADE_OFFER_ITEM', 'COMMUNITY_POST',
    'COMMUNITY_POST_REACTION', 'DISCUSSION_TOPIC', 'DISCUSSION_REPLY',
    'DEMO_RESET_RUN', 'DEMO_RESET_TABLE', 'DEMO_RESET_EVENT'
  )
    AND status <> 'ENABLED';
  assert_zero('disabled relational constraints', v_count);

  SELECT COUNT(*) INTO v_count
  FROM user_constraints
  WHERE table_name IN (
    'PLAYER', 'DEVELOPER', 'ADMIN_USER', 'WALLET_ACCOUNT', 'SYS_NOTICE',
    'GAME', 'GAME_ORDER', 'ORDER_DETAIL', 'ORDER_STATUS_LOG', 'PAYMENT_TRANSACTION',
    'REFUND_TICKET', 'REFUND_DETAIL', 'REFUND_AUDIT_LOG', 'PLAYER_LIBRARY',
    'CDKEY_BATCH', 'CDKEY', 'CDKEY_REDEEM_LOG', 'GAME_REVIEW', 'REVIEW_VERSION',
    'ACHIEVEMENT', 'PLAYER_ACHIEVEMENT', 'ITEM_TEMPLATE', 'INVENTORY_ITEM',
    'MARKET_ORDER', 'MARKET_TRADE', 'ITEM_TRANSFER_LEDGER', 'WALLET_TRANSACTION',
    'FRIEND_RELATION', 'DIRECT_MESSAGE', 'REVIEW_REACTION', 'WORKSHOP_ITEM',
    'WORKSHOP_SUBSCRIPTION', 'USER_NOTIFICATION', 'PLAYER_PROFILE', 'BADGE_CATALOG',
    'PLAYER_BADGE', 'TRADE_OFFER', 'TRADE_OFFER_ITEM', 'COMMUNITY_POST',
    'COMMUNITY_POST_REACTION', 'DISCUSSION_TOPIC', 'DISCUSSION_REPLY',
    'DEMO_RESET_RUN', 'DEMO_RESET_TABLE', 'DEMO_RESET_EVENT'
  )
    AND constraint_type = 'P';
  assert_eq('primary key coverage', v_count, 45);

  SELECT COUNT(*) INTO v_count
  FROM user_indexes
  WHERE index_name LIKE 'IDX\_%' ESCAPE '\'
     OR index_name = 'UK_MARKET_ACTIVE_SELL_ITEM';
  assert_at_least('named business indexes', v_count, 49);

  SELECT COUNT(*) INTO v_count
  FROM user_indexes
  WHERE status <> 'VALID';
  assert_zero('invalid indexes', v_count);

  SELECT COUNT(*) INTO v_count
  FROM user_objects
  WHERE status = 'INVALID'
    AND object_name NOT LIKE 'BIN$%';
  assert_zero('invalid schema objects', v_count);

  SELECT COUNT(*) INTO v_count
  FROM user_tab_columns
  WHERE table_name = 'PLAYER'
    AND column_name = 'WALLET_BALANCE';
  assert_zero('PLAYER.wallet_balance columns', v_count);

  SELECT COUNT(*) INTO v_count
  FROM wallet_account
  WHERE available_balance < 0 OR frozen_balance < 0;
  assert_zero('negative wallet balances', v_count);

  SELECT COUNT(*) INTO v_count
  FROM (
    SELECT o.order_id
    FROM game_order o
    LEFT JOIN order_detail d ON d.order_id = o.order_id
    WHERE o.order_type = 'BUY_GAME'
    GROUP BY o.order_id, o.total_amount
    HAVING COUNT(d.detail_id) = 0
       OR ABS(o.total_amount - NVL(SUM(d.payable_amount), 0)) > 0.005
  );
  assert_zero('order/detail amount mismatches', v_count);

  SELECT COUNT(*) INTO v_count
  FROM (
    SELECT p.order_id
    FROM payment_transaction p
    JOIN game_order o ON o.order_id = p.order_id
    WHERE p.status IN ('SUCCESS', 'REFUNDED')
    GROUP BY p.order_id, o.total_amount
    HAVING ABS(o.total_amount - SUM(p.amount)) > 0.005
  );
  assert_zero('order/payment amount mismatches', v_count);

  SELECT COUNT(*) INTO v_count
  FROM (
    SELECT r.refund_id
    FROM refund_ticket r
    LEFT JOIN refund_detail d ON d.refund_id = r.refund_id
    GROUP BY r.refund_id, r.refund_amount
    HAVING COUNT(d.refund_detail_id) = 0
       OR ABS(r.refund_amount - NVL(SUM(d.refund_amount), 0)) > 0.005
  );
  assert_zero('refund/detail amount mismatches', v_count);

  SELECT COUNT(*) INTO v_count
  FROM game_order o
  JOIN order_detail d ON d.order_id = o.order_id
  WHERE o.order_type = 'BUY_GAME'
    AND o.order_status = 'COMPLETED'
    AND o.payment_status = 'PAID'
    AND NOT EXISTS (
      SELECT 1
      FROM player_library l
      WHERE l.user_id = o.user_id
        AND l.game_id = d.game_id
        AND l.status = 'NORMAL'
    );
  assert_zero('paid games missing library rights', v_count);

  SELECT COUNT(*) INTO v_count
  FROM market_order o
  LEFT JOIN inventory_item i ON i.item_id = o.item_id
  WHERE o.order_type = 'SELL'
    AND o.status = 'MATCHING'
    AND (
      i.item_id IS NULL
      OR i.user_id <> o.user_id
      OR i.template_id <> o.template_id
      OR i.status <> 'IN_MARKET'
    );
  assert_zero('active sell asset mismatches', v_count);

  SELECT COUNT(*) INTO v_count
  FROM (
    SELECT w.user_id
    FROM wallet_account w
    LEFT JOIN market_order o
      ON o.user_id = w.user_id
     AND o.order_type = 'BUY'
     AND o.status = 'MATCHING'
    GROUP BY w.user_id, w.frozen_balance
    HAVING ABS(w.frozen_balance - NVL(SUM(o.frozen_amount), 0)) > 0.005
  );
  assert_zero('wallet/order frozen mismatches', v_count);

  SELECT COUNT(*) INTO v_count
  FROM market_trade t
  JOIN market_order b ON b.market_order_id = t.buy_order_id
  JOIN market_order s ON s.market_order_id = t.sell_order_id
  WHERE b.order_type <> 'BUY'
     OR s.order_type <> 'SELL'
     OR b.status <> 'TRADED'
     OR s.status <> 'TRADED'
     OR t.trade_price > b.target_price
     OR t.trade_price < s.target_price
     OR t.platform_fee > t.trade_price;
  assert_zero('market trade/order mismatches', v_count);

  SELECT COUNT(*) INTO v_count
  FROM market_trade t
  WHERE NOT EXISTS (
    SELECT 1
    FROM item_transfer_ledger l
    WHERE l.item_id = t.item_id
      AND l.from_user_id = t.seller_id
      AND l.to_user_id = t.buyer_id
      AND l.transfer_type = 'TRADE'
  );
  assert_zero('trades missing transfer ledger', v_count);

  SELECT COUNT(*) INTO v_count
  FROM wallet_transaction
  WHERE biz_type IN ('RECHARGE', 'REFUND', 'MARKET_SELL', 'MARKET_UNFREEZE')
    AND ABS(avail_bal_after - (avail_bal_before + amount)) > 0.005;
  assert_zero('credit ledger arithmetic errors', v_count);

  SELECT COUNT(*) INTO v_count
  FROM wallet_transaction
  WHERE biz_type IN ('BUY_GAME', 'MARKET_FREEZE')
    AND ABS(avail_bal_after - (avail_bal_before - amount)) > 0.005;
  assert_zero('debit ledger arithmetic errors', v_count);

  SELECT COUNT(*) INTO v_count
  FROM friend_relation
  WHERE user_low_id >= user_high_id;
  assert_zero('non-canonical friend pairs', v_count);

  SELECT COUNT(*) INTO v_count
  FROM demo_reset_run
  WHERE status IN ('RESET_FAILED', 'RESTORE_FAILED');
  assert_zero('failed demo reset runs', v_count);

  SELECT COUNT(*) INTO v_count
  FROM game
  WHERE game_id IN ('GAME_CS2', 'GAME_DST');
  assert_eq('fixed sample games', v_count, 2);
END;
/

PROMPT ============================================================.
PROMPT Readable defense summary
PROMPT ============================================================.

COLUMN module FORMAT A24
COLUMN row_count FORMAT 999999

SELECT 'players' module, COUNT(*) row_count FROM player
UNION ALL SELECT 'games', COUNT(*) FROM game
UNION ALL SELECT 'orders', COUNT(*) FROM game_order
UNION ALL SELECT 'wallet ledger', COUNT(*) FROM wallet_transaction
UNION ALL SELECT 'inventory assets', COUNT(*) FROM inventory_item
UNION ALL SELECT 'market trades', COUNT(*) FROM market_trade
UNION ALL SELECT 'community posts', COUNT(*) FROM community_post
UNION ALL SELECT 'discussion topics', COUNT(*) FROM discussion_topic
UNION ALL SELECT 'direct messages', COUNT(*) FROM direct_message;

COLUMN account FORMAT A16
COLUMN available_balance FORMAT 9999990.00
COLUMN frozen_balance FORMAT 9999990.00
COLUMN total_balance FORMAT 9999990.00

SELECT p.account,
       w.available_balance,
       w.frozen_balance,
       w.available_balance + w.frozen_balance total_balance
FROM player p
JOIN wallet_account w ON w.user_id = p.user_id
ORDER BY p.account;

ROLLBACK;
PROMPT Database defense verification passed.
