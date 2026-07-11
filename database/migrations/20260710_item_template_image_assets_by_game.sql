SET DEFINE OFF

PROMPT Updating CS2 item template image paths for per-game asset directories...

MERGE INTO ITEM_TEMPLATE t
USING (
  SELECT 'ITPL_CS2_AK_NEON_RIDER' AS template_id, 'GAME_CS2' AS game_id, 'AK-47 | Neon Rider' AS item_name, 'LEGENDARY' AS rarity, '/assets/items/cs2/cs2-ak-neon-rider.png' AS image_url FROM dual
  UNION ALL SELECT 'ITPL_CS2_AK_REDLINE', 'GAME_CS2', 'AK-47 | Redline', 'EPIC', '/assets/items/cs2/cs2-ak-redline.png' FROM dual
  UNION ALL SELECT 'ITPL_CS2_AWP_ASIIMOV', 'GAME_CS2', 'AWP | Asiimov', 'LEGENDARY', '/assets/items/cs2/cs2-awp-asiimov.png' FROM dual
  UNION ALL SELECT 'ITPL_CS2_AWP_DRAGON_LORE', 'GAME_CS2', 'AWP | Dragon Lore', 'LEGENDARY', '/assets/items/cs2/cs2-awp-dragon-lore.png' FROM dual
  UNION ALL SELECT 'ITPL_CS2_DEAGLE_BLAZE', 'GAME_CS2', 'Desert Eagle | Blaze', 'LEGENDARY', '/assets/items/cs2/cs2-deagle-blaze.png' FROM dual
  UNION ALL SELECT 'ITPL_CS2_DEAGLE_PRINTSTREAM', 'GAME_CS2', 'Desert Eagle | Printstream', 'EPIC', '/assets/items/cs2/cs2-deagle-printstream.png' FROM dual
  UNION ALL SELECT 'ITPL_CS2_CASE_DREAMS', 'GAME_CS2', 'Dreams & Nightmares Case', 'RARE', '/assets/items/cs2/cs2-dreams-case.png' FROM dual
  UNION ALL SELECT 'ITPL_CS2_FAMAS_COMMEMORATION', 'GAME_CS2', 'FAMAS | Commemoration', 'EPIC', '/assets/items/cs2/cs2-famas-commemoration.png' FROM dual
  UNION ALL SELECT 'ITPL_CS2_FAMAS_MECHA', 'GAME_CS2', 'FAMAS | Mecha Industries', 'RARE', '/assets/items/cs2/cs2-famas-mecha.png' FROM dual
  UNION ALL SELECT 'ITPL_CS2_GALIL_CHROMATIC', 'GAME_CS2', 'Galil AR | Chromatic Aberration', 'EPIC', '/assets/items/cs2/cs2-galil-chromatic.png' FROM dual
  UNION ALL SELECT 'ITPL_CS2_GALIL_PHOENIX', 'GAME_CS2', 'Galil AR | Phoenix Blacklight', 'RARE', '/assets/items/cs2/cs2-galil-phoenix.png' FROM dual
  UNION ALL SELECT 'ITPL_CS2_GLOCK_FADE', 'GAME_CS2', 'Glock-18 | Fade', 'LEGENDARY', '/assets/items/cs2/cs2-glock-fade.png' FROM dual
  UNION ALL SELECT 'ITPL_CS2_GLOCK_WATER', 'GAME_CS2', 'Glock-18 | Water Elemental', 'RARE', '/assets/items/cs2/cs2-glock-water.png' FROM dual
  UNION ALL SELECT 'ITPL_CS2_M4A1_PRINTSTREAM', 'GAME_CS2', 'M4A1-S | Printstream', 'LEGENDARY', '/assets/items/cs2/cs2-m4a1-printstream.png' FROM dual
  UNION ALL SELECT 'ITPL_CS2_M4A4_HOWL', 'GAME_CS2', 'M4A4 | Howl', 'LEGENDARY', '/assets/items/cs2/cs2-m4a4-howl.png' FROM dual
  UNION ALL SELECT 'ITPL_CS2_M4A4_TOOTH_FAIRY', 'GAME_CS2', 'M4A4 | Tooth Fairy', 'EPIC', '/assets/items/cs2/cs2-m4a4-tooth-fairy.png' FROM dual
  UNION ALL SELECT 'ITPL_CS2_MP9_DARK_TIDE', 'GAME_CS2', 'MP9 | Dark Tide', 'UNCOMMON', '/assets/items/cs2/cs2-mp9-dark-tide.png' FROM dual
  UNION ALL SELECT 'ITPL_CS2_MP9_STARLIGHT', 'GAME_CS2', 'MP9 | Starlight Protector', 'EPIC', '/assets/items/cs2/cs2-mp9-starlight.png' FROM dual
  UNION ALL SELECT 'ITPL_CS2_P250_CYBER_SHELL', 'GAME_CS2', 'P250 | Cyber Shell', 'UNCOMMON', '/assets/items/cs2/cs2-p250-cyber-shell.png' FROM dual
  UNION ALL SELECT 'ITPL_CS2_P250_SEE_YA_LATER', 'GAME_CS2', 'P250 | See Ya Later', 'EPIC', '/assets/items/cs2/cs2-p250-see-ya-later.png' FROM dual
  UNION ALL SELECT 'ITPL_CS2_P90_ASIIMOV', 'GAME_CS2', 'P90 | Asiimov', 'EPIC', '/assets/items/cs2/cs2-p90-asiimov.png' FROM dual
  UNION ALL SELECT 'ITPL_CS2_P90_ELITE_BUILD', 'GAME_CS2', 'P90 | Elite Build', 'COMMON', '/assets/items/cs2/cs2-p90-elite-build.png' FROM dual
  UNION ALL SELECT 'ITPL_CS2_STICKER_CROWN', 'GAME_CS2', 'Sticker | Crown', 'RARE', '/assets/items/cs2/cs2-sticker-crown.png' FROM dual
  UNION ALL SELECT 'ITPL_CS2_TEC9_DECIMATOR', 'GAME_CS2', 'Tec-9 | Decimator', 'RARE', '/assets/items/cs2/cs2-tec9-decimator.png' FROM dual
  UNION ALL SELECT 'ITPL_CS2_TEC9_NUCLEAR', 'GAME_CS2', 'Tec-9 | Nuclear Threat', 'EPIC', '/assets/items/cs2/cs2-tec9-nuclear-threat.png' FROM dual
  UNION ALL SELECT 'ITPL_CS2_USP_KILL_CONFIRMED', 'GAME_CS2', 'USP-S | Kill Confirmed', 'LEGENDARY', '/assets/items/cs2/cs2-usp-kill-confirmed.png' FROM dual
  UNION ALL SELECT 'ITPL_CS2_USP_TRAITOR', 'GAME_CS2', 'USP-S | The Traitor', 'EPIC', '/assets/items/cs2/cs2-usp-traitor.png' FROM dual
) s
ON (t.template_id = s.template_id)
WHEN MATCHED THEN UPDATE SET
  t.game_id = s.game_id,
  t.item_name = s.item_name,
  t.rarity = s.rarity,
  t.image_url = s.image_url
WHEN NOT MATCHED THEN INSERT (template_id, game_id, item_name, rarity, image_url)
VALUES (s.template_id, s.game_id, s.item_name, s.rarity, s.image_url);

PROMPT Updating DST item templates to match committed image assets...

MERGE INTO ITEM_TEMPLATE t
USING (
  SELECT 'ITPL_DST_HOLLY_WREATH' AS template_id, 'GAME_DST' AS game_id, 'Holly Wreath' AS item_name, 'COMMON' AS rarity, '/assets/items/dst/dst-holly-wreath.png' AS image_url FROM dual
  UNION ALL SELECT 'ITPL_DST_NIGHTGOWN', 'GAME_DST', 'Distinguished Nightgown', 'RARE', '/assets/items/dst/dst-distinguished-nightgown.png' FROM dual
  UNION ALL SELECT 'ITPL_DST_GUEST_HONOR', 'GAME_DST', 'Elegant Guest of Honor', 'EPIC', '/assets/items/dst/dst-guest-of-honor.png' FROM dual
  UNION ALL SELECT 'ITPL_DST_STARTER_PACK', 'GAME_DST', 'Starter Pack 2025 Chest', 'UNCOMMON', '/assets/items/dst/dst-starter-pack-chest.png' FROM dual
  UNION ALL SELECT 'ITPL_DST_LUNAR_WINGS', 'GAME_DST', 'Lunar Moth Wings', 'RARE', '/assets/items/dst/dst-lunar-moth-wings.png' FROM dual
  UNION ALL SELECT 'ITPL_DST_CELESTIAL_GOBLET', 'GAME_DST', 'Celestial Goblet', 'EPIC', '/assets/items/dst/dst-celestial-goblet.png' FROM dual
  UNION ALL SELECT 'ITPL_DST_CRYSTAL_AXE', 'GAME_DST', 'Crystal Axe', 'RARE', '/assets/items/dst/dst-crystal-axe.png' FROM dual
  UNION ALL SELECT 'ITPL_DST_ICE_CROWN', 'GAME_DST', 'Ice Crown', 'EPIC', '/assets/items/dst/dst-ice-crown.png' FROM dual
  UNION ALL SELECT 'ITPL_DST_SHROOM_HAT', 'GAME_DST', 'Shroom Hat', 'UNCOMMON', '/assets/items/dst/dst-shroom-hat.png' FROM dual
  UNION ALL SELECT 'ITPL_DST_PUMPKIN_LANTERN', 'GAME_DST', 'Pumpkin Lantern', 'UNCOMMON', '/assets/items/dst/dst-pumpkin-lantern.png' FROM dual
  UNION ALL SELECT 'ITPL_DST_MOON_STAFF', 'GAME_DST', 'Moon Staff', 'LEGENDARY', '/assets/items/dst/dst-moon-staff.png' FROM dual
  UNION ALL SELECT 'ITPL_DST_GHOST_COSTUME', 'GAME_DST', 'Ghost Costume', 'COMMON', '/assets/items/dst/dst-ghost-costume.png' FROM dual
) s
ON (t.template_id = s.template_id)
WHEN MATCHED THEN UPDATE SET
  t.game_id = s.game_id,
  t.item_name = s.item_name,
  t.rarity = s.rarity,
  t.image_url = s.image_url
WHEN NOT MATCHED THEN INSERT (template_id, game_id, item_name, rarity, image_url)
VALUES (s.template_id, s.game_id, s.item_name, s.rarity, s.image_url);

COMMIT;
