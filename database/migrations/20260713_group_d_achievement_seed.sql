SET DEFINE OFF

PROMPT Seeding Group D project achievement metadata...

MERGE INTO ACHIEVEMENT t
USING (
  SELECT 'ACH_DST_SURVIVE_001' AS ach_id, 'GAME_DST' AS game_id, 'First Night Together' AS ach_name, 'Survive the first night with another player.' AS description, 42.50 AS global_rate FROM dual
  UNION ALL SELECT 'ACH_DST_SCIENCE_MACHINE', 'GAME_DST', 'Science Started', 'Build a Science Machine and unlock the first survival recipes.', 37.80 FROM dual
  UNION ALL SELECT 'ACH_DST_WINTER_SURVIVOR', 'GAME_DST', 'Winter Camp', 'Keep the camp and teammates alive through winter.', 18.40 FROM dual
  UNION ALL SELECT 'ACH_DST_RUINS_DIVER', 'GAME_DST', 'Ruins Lantern', 'Explore the ruins and bring back ancient technology.', 12.10 FROM dual
  UNION ALL SELECT 'ACH_DST_SHADOW_DUEL', 'GAME_DST', 'Shadow Duel', 'Defeat a shadow creature while keeping sanity stable.', 21.60 FROM dual
  UNION ALL SELECT 'ACH_DST_CELESTIAL_CARTOGRAPHER', 'GAME_DST', 'Celestial Cartographer', 'Discover Lunar Island and collect moon tech materials.', 9.70 FROM dual
  UNION ALL SELECT 'ACH_CS2_FIRST_ROUND', 'GAME_CS2', 'Pistol Round Opener', 'Win the opening pistol round.', 55.20 FROM dual
  UNION ALL SELECT 'ACH_CS2_ACE', 'GAME_CS2', 'Ace Round', 'Eliminate all five enemies in one round.', 8.60 FROM dual
  UNION ALL SELECT 'ACH_CS2_BOMB_PLANT', 'GAME_CS2', 'Bomb Site Secured', 'Plant the C4 as the attacking side.', 44.30 FROM dual
  UNION ALL SELECT 'ACH_CS2_DEFUSE', 'GAME_CS2', 'Defuse Expert', 'Defuse the bomb in a key round.', 31.40 FROM dual
  UNION ALL SELECT 'ACH_CS2_MARKET_MAKER', 'GAME_CS2', 'Market Rookie', 'Complete one item market trade record.', 16.90 FROM dual
) s
ON (t.ach_id = s.ach_id)
WHEN MATCHED THEN UPDATE SET
  t.game_id = s.game_id,
  t.ach_name = s.ach_name,
  t.description = s.description,
  t.global_rate = s.global_rate
WHEN NOT MATCHED THEN INSERT (ach_id, game_id, ach_name, description, global_rate)
VALUES (s.ach_id, s.game_id, s.ach_name, s.description, s.global_rate);

COMMIT;