SET DEFINE OFF

PROMPT Cleaning extra alice drop demo inventory items...
PROMPT This script keeps seed items and items referenced by market orders or trades.

DELETE FROM ITEM_TRANSFER_LEDGER
 WHERE item_id IN (
   SELECT i.item_id
     FROM INVENTORY_ITEM i
     JOIN PLAYER p ON p.user_id = i.user_id
    WHERE p.account = 'alice'
      AND i.item_id NOT IN ('ITEM_CS2_001', 'ITEM_CS2_002', 'ITEM_DST_001')
      AND EXISTS (
        SELECT 1
          FROM ITEM_TRANSFER_LEDGER l
         WHERE l.item_id = i.item_id
           AND l.from_user_id IS NULL
           AND l.to_user_id = i.user_id
           AND l.transfer_type = 'DROP'
      )
      AND NOT EXISTS (
        SELECT 1
          FROM MARKET_ORDER o
         WHERE o.item_id = i.item_id
      )
      AND NOT EXISTS (
        SELECT 1
          FROM MARKET_TRADE tr
         WHERE tr.item_id = i.item_id
      )
 );

DELETE FROM INVENTORY_ITEM i
 WHERE i.user_id = (SELECT p.user_id FROM PLAYER p WHERE p.account = 'alice')
   AND i.item_id NOT IN ('ITEM_CS2_001', 'ITEM_CS2_002', 'ITEM_DST_001')
   AND NOT EXISTS (
     SELECT 1
       FROM ITEM_TRANSFER_LEDGER l
      WHERE l.item_id = i.item_id
   )
   AND NOT EXISTS (
     SELECT 1
       FROM MARKET_ORDER o
      WHERE o.item_id = i.item_id
   )
   AND NOT EXISTS (
     SELECT 1
       FROM MARKET_TRADE tr
      WHERE tr.item_id = i.item_id
   );

COMMIT;
