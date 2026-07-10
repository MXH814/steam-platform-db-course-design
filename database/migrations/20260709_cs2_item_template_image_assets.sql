SET DEFINE OFF

PROMPT Updating CS2 item templates to match committed image assets...

MERGE INTO ITEM_TEMPLATE t
USING (
  SELECT 'ITPL_CS2_AK_NEON_RIDER' AS template_id, 'GAME_CS2' AS game_id, 'AK-47 | Neon Rider' AS item_name, 'LEGENDARY' AS rarity, '/assets/items/cs2-ak-neon-rider.png' AS image_url FROM dual
  UNION ALL SELECT 'ITPL_CS2_AK_REDLINE', 'GAME_CS2', 'AK-47 | Redline', 'EPIC', '/assets/items/cs2-ak-redline.png' FROM dual
  UNION ALL SELECT 'ITPL_CS2_AWP_ASIIMOV', 'GAME_CS2', 'AWP | Asiimov', 'LEGENDARY', '/assets/items/cs2-awp-asiimov.png' FROM dual
  UNION ALL SELECT 'ITPL_CS2_AWP_DRAGON_LORE', 'GAME_CS2', 'AWP | Dragon Lore', 'LEGENDARY', '/assets/items/cs2-awp-dragon-lore.png' FROM dual
  UNION ALL SELECT 'ITPL_CS2_DEAGLE_BLAZE', 'GAME_CS2', 'Desert Eagle | Blaze', 'LEGENDARY', '/assets/items/cs2-deagle-blaze.png' FROM dual
  UNION ALL SELECT 'ITPL_CS2_DEAGLE_PRINTSTREAM', 'GAME_CS2', 'Desert Eagle | Printstream', 'EPIC', '/assets/items/cs2-deagle-printstream.png' FROM dual
  UNION ALL SELECT 'ITPL_CS2_CASE_DREAMS', 'GAME_CS2', 'Dreams & Nightmares Case', 'RARE', '/assets/items/cs2-dreams-case.png' FROM dual
  UNION ALL SELECT 'ITPL_CS2_FAMAS_COMMEMORATION', 'GAME_CS2', 'FAMAS | Commemoration', 'EPIC', '/assets/items/cs2-famas-commemoration.png' FROM dual
  UNION ALL SELECT 'ITPL_CS2_FAMAS_MECHA', 'GAME_CS2', 'FAMAS | Mecha Industries', 'RARE', '/assets/items/cs2-famas-mecha.png' FROM dual
  UNION ALL SELECT 'ITPL_CS2_GALIL_CHROMATIC', 'GAME_CS2', 'Galil AR | Chromatic Aberration', 'EPIC', '/assets/items/cs2-galil-chromatic.png' FROM dual
  UNION ALL SELECT 'ITPL_CS2_GALIL_PHOENIX', 'GAME_CS2', 'Galil AR | Phoenix Blacklight', 'RARE', '/assets/items/cs2-galil-phoenix.png' FROM dual
  UNION ALL SELECT 'ITPL_CS2_GLOCK_FADE', 'GAME_CS2', 'Glock-18 | Fade', 'LEGENDARY', '/assets/items/cs2-glock-fade.png' FROM dual
  UNION ALL SELECT 'ITPL_CS2_GLOCK_WATER', 'GAME_CS2', 'Glock-18 | Water Elemental', 'RARE', '/assets/items/cs2-glock-water.png' FROM dual
  UNION ALL SELECT 'ITPL_CS2_M4A1_PRINTSTREAM', 'GAME_CS2', 'M4A1-S | Printstream', 'LEGENDARY', '/assets/items/cs2-m4a1-printstream.png' FROM dual
  UNION ALL SELECT 'ITPL_CS2_M4A4_HOWL', 'GAME_CS2', 'M4A4 | Howl', 'LEGENDARY', '/assets/items/cs2-m4a4-howl.png' FROM dual
  UNION ALL SELECT 'ITPL_CS2_M4A4_TOOTH_FAIRY', 'GAME_CS2', 'M4A4 | Tooth Fairy', 'EPIC', '/assets/items/cs2-m4a4-tooth-fairy.png' FROM dual
  UNION ALL SELECT 'ITPL_CS2_MP9_DARK_TIDE', 'GAME_CS2', 'MP9 | Dark Tide', 'UNCOMMON', '/assets/items/cs2-mp9-dark-tide.png' FROM dual
  UNION ALL SELECT 'ITPL_CS2_MP9_STARLIGHT', 'GAME_CS2', 'MP9 | Starlight Protector', 'EPIC', '/assets/items/cs2-mp9-starlight.png' FROM dual
  UNION ALL SELECT 'ITPL_CS2_P250_CYBER_SHELL', 'GAME_CS2', 'P250 | Cyber Shell', 'UNCOMMON', '/assets/items/cs2-p250-cyber-shell.png' FROM dual
  UNION ALL SELECT 'ITPL_CS2_P250_SEE_YA_LATER', 'GAME_CS2', 'P250 | See Ya Later', 'EPIC', '/assets/items/cs2-p250-see-ya-later.png' FROM dual
  UNION ALL SELECT 'ITPL_CS2_P90_ASIIMOV', 'GAME_CS2', 'P90 | Asiimov', 'EPIC', '/assets/items/cs2-p90-asiimov.png' FROM dual
  UNION ALL SELECT 'ITPL_CS2_P90_ELITE_BUILD', 'GAME_CS2', 'P90 | Elite Build', 'COMMON', '/assets/items/cs2-p90-elite-build.png' FROM dual
  UNION ALL SELECT 'ITPL_CS2_STICKER_CROWN', 'GAME_CS2', 'Sticker | Crown', 'RARE', '/assets/items/cs2-sticker-crown.png' FROM dual
  UNION ALL SELECT 'ITPL_CS2_TEC9_DECIMATOR', 'GAME_CS2', 'Tec-9 | Decimator', 'RARE', '/assets/items/cs2-tec9-decimator.png' FROM dual
  UNION ALL SELECT 'ITPL_CS2_TEC9_NUCLEAR', 'GAME_CS2', 'Tec-9 | Nuclear Threat', 'EPIC', '/assets/items/cs2-tec9-nuclear-threat.png' FROM dual
  UNION ALL SELECT 'ITPL_CS2_USP_KILL_CONFIRMED', 'GAME_CS2', 'USP-S | Kill Confirmed', 'LEGENDARY', '/assets/items/cs2-usp-kill-confirmed.png' FROM dual
  UNION ALL SELECT 'ITPL_CS2_USP_TRAITOR', 'GAME_CS2', 'USP-S | The Traitor', 'EPIC', '/assets/items/cs2-usp-traitor.png' FROM dual
) s
ON (t.template_id = s.template_id)
WHEN MATCHED THEN UPDATE SET
  t.game_id = s.game_id,
  t.item_name = s.item_name,
  t.rarity = s.rarity,
  t.image_url = s.image_url
WHEN NOT MATCHED THEN INSERT (template_id, game_id, item_name, rarity, image_url)
VALUES (s.template_id, s.game_id, s.item_name, s.rarity, s.image_url);

PROMPT Clearing alice inventory demo data...

DELETE FROM ITEM_TRANSFER_LEDGER
 WHERE item_id IN (
   SELECT item_id
     FROM INVENTORY_ITEM
    WHERE user_id = (SELECT user_id FROM PLAYER WHERE account = 'alice')
 );

DELETE FROM MARKET_TRADE
 WHERE item_id IN (
   SELECT item_id
     FROM INVENTORY_ITEM
    WHERE user_id = (SELECT user_id FROM PLAYER WHERE account = 'alice')
 );

DELETE FROM MARKET_ORDER
 WHERE item_id IN (
   SELECT item_id
     FROM INVENTORY_ITEM
    WHERE user_id = (SELECT user_id FROM PLAYER WHERE account = 'alice')
 );

DELETE FROM INVENTORY_ITEM
 WHERE user_id = (SELECT user_id FROM PLAYER WHERE account = 'alice');

UPDATE ITEM_TEMPLATE
   SET image_url = NULL
 WHERE game_id = 'GAME_DST';

COMMIT;
