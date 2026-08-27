SET LINESIZE 220
SET PAGESIZE 200
SET FEEDBACK ON
SET VERIFY OFF
SET DEFINE OFF
WHENEVER SQLERROR EXIT FAILURE ROLLBACK

PROMPT ============================================================
PROMPT Oracle execution plans for three defense queries
PROMPT PLAN_TABLE writes are rolled back at the end.
PROMPT ============================================================

DELETE FROM plan_table WHERE statement_id LIKE 'STEAM_DEF_%';

EXPLAIN PLAN SET STATEMENT_ID = 'STEAM_DEF_ORDER' FOR
SELECT o.order_id,
       o.order_status,
       o.payment_status,
       o.total_amount,
       o.create_time
FROM game_order o
WHERE o.user_id = 'P001'
ORDER BY o.create_time DESC;

PROMPT [Player order history: IDX_ORDER_USER_TIME candidate]
SELECT plan_table_output
FROM TABLE(DBMS_XPLAN.DISPLAY(NULL, 'STEAM_DEF_ORDER', 'BASIC +PREDICATE +ALIAS'));

EXPLAIN PLAN SET STATEMENT_ID = 'STEAM_DEF_MARKET' FOR
SELECT o.market_order_id,
       o.user_id,
       o.target_price,
       o.create_time
FROM market_order o
WHERE o.template_id = 'ITPL_CS2_AK_REDLINE'
  AND o.status = 'MATCHING'
  AND o.order_type = 'SELL'
ORDER BY o.target_price ASC, o.create_time ASC;

PROMPT [Market matching: IDX_MARKET_TEMPLATE_STATUS candidate]
SELECT plan_table_output
FROM TABLE(DBMS_XPLAN.DISPLAY(NULL, 'STEAM_DEF_MARKET', 'BASIC +PREDICATE +ALIAS'));

EXPLAIN PLAN SET STATEMENT_ID = 'STEAM_DEF_DISCUSSION' FOR
SELECT t.topic_id,
       t.title,
       t.author_id,
       t.updated_at
FROM discussion_topic t
WHERE t.game_id = 'GAME_DST'
  AND t.status = 'OPEN'
ORDER BY t.updated_at DESC;

PROMPT [Community discussion: IDX_DISCUSSION_GAME_TIME candidate]
SELECT plan_table_output
FROM TABLE(DBMS_XPLAN.DISPLAY(NULL, 'STEAM_DEF_DISCUSSION', 'BASIC +PREDICATE +ALIAS'));

ROLLBACK;
PROMPT Execution-plan inspection completed.
