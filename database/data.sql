SET DEFINE OFF

PROMPT Inserting Steam course seed data...

INSERT INTO PLAYER (user_id, account, password_hash, nickname, credit_score, status, version, create_time, update_time)
VALUES ('P001', 'alice', 'PBKDF2$SHA256$100000$c2VlZC1hbGljZV9fX19fXw==$iTPCU6/lngHZz3zx/gYotoK0h7N0WJu8m0Vnre7/1NA=', 'Alice', 100, 'NORMAL', 0, TIMESTAMP '2026-07-05 09:00:00', TIMESTAMP '2026-07-05 09:00:00');

INSERT INTO PLAYER (user_id, account, password_hash, nickname, credit_score, status, version, create_time, update_time)
VALUES ('P002', 'bob', 'PBKDF2$SHA256$100000$c2VlZC1ib2JfX19fX19fXw==$2CvTcEyGV8IfmgB6hEZN+em2lyvIsaRLrQJ/5YgkipM=', 'Bob', 96, 'NORMAL', 0, TIMESTAMP '2026-07-05 09:05:00', TIMESTAMP '2026-07-05 09:05:00');

INSERT INTO DEVELOPER (dev_id, company_name, tax_id, contact_email, password_hash, status, join_time)
VALUES ('DEV_VALVE', 'Valve', 'TAX-DEMO-VALVE', 'valve@example.com', 'PBKDF2$SHA256$100000$c2VlZC12YWx2ZS1kZXZfX18=$apqFEKjAoaMZvUroAQ9eaiAH4qutVdFRtt0Yorzqf44=', 'APPROVED', TIMESTAMP '2026-07-05 09:10:00');

INSERT INTO DEVELOPER (dev_id, company_name, tax_id, contact_email, password_hash, status, join_time)
VALUES ('DEV_KLEI', 'Klei Entertainment', 'TAX-DEMO-KLEI', 'klei@example.com', 'PBKDF2$SHA256$100000$c2VlZC1rbGVpLWRldl9fX18=$Syi9RKVX+XpYxt6A39k3dDAC0DAfWVxDolxY0mRn4O8=', 'APPROVED', TIMESTAMP '2026-07-05 09:12:00');

INSERT INTO ADMIN_USER (admin_id, account, password_hash, role, create_time)
VALUES ('ADM001', 'rootadmin', 'PBKDF2$SHA256$100000$c2VlZC1yb290YWRtaW5fXw==$yHE6M2jmsTpAplUmz5Vjp4o3zmV30sSQwdnx0jMVHpo=', 'SUPER_ADMIN', TIMESTAMP '2026-07-05 09:15:00');

INSERT INTO WALLET_ACCOUNT (wallet_id, user_id, available_balance, frozen_balance, version)
VALUES ('W001', 'P001', 176.00, 0.00, 2);

INSERT INTO WALLET_ACCOUNT (wallet_id, user_id, available_balance, frozen_balance, version)
VALUES ('W002', 'P002', 242.75, 50.00, 3);

INSERT INTO SYS_NOTICE (notice_id, publisher_type, publisher_id, title, content, priority, status, publish_time, expire_time)
VALUES ('N001', 'SYSTEM', NULL, 'CS2 and DST demo catalog ready', 'Counter-Strike 2 and Don''t Starve Together are the fixed sample games for the course demo.', 1, 'PUBLISHED', TIMESTAMP '2026-07-05 10:00:00', TIMESTAMP '2026-08-05 10:00:00');

INSERT INTO GAME (game_id, dev_id, game_name, base_price, discount_rate, release_date, reputation, status)
VALUES ('GAME_CS2', 'DEV_VALVE', 'Counter-Strike 2', 0.00, 1.00, DATE '2023-09-27', 'VERY_POSITIVE', 'ONLINE');

INSERT INTO GAME (game_id, dev_id, game_name, base_price, discount_rate, release_date, reputation, status)
VALUES ('GAME_DST', 'DEV_KLEI', 'Don''t Starve Together / ' || UNISTR('\9965\8352\8054\673A\7248'), 48.00, 0.50, DATE '2016-04-21', 'OVERWHELMINGLY_POSITIVE', 'ONLINE');

INSERT INTO GAME_ORDER (order_id, user_id, total_amount, order_type, order_status, payment_status, idempotency_key, expire_time, create_time)
VALUES ('O_DST_001', 'P001', 24.00, 'BUY_GAME', 'COMPLETED', 'PAID', 'idem-order-dst-001', TIMESTAMP '2026-07-05 10:30:00', TIMESTAMP '2026-07-05 10:00:00');

INSERT INTO ORDER_DETAIL (detail_id, order_id, game_id, original_price, discount_amount, payable_amount, refund_amount)
VALUES ('OD_DST_001', 'O_DST_001', 'GAME_DST', 48.00, 24.00, 24.00, 0.00);

INSERT INTO ORDER_STATUS_LOG (log_id, order_id, from_status, to_status, create_time)
VALUES ('OSL_DST_001', 'O_DST_001', NULL, 'CREATED', TIMESTAMP '2026-07-05 10:00:00');

INSERT INTO ORDER_STATUS_LOG (log_id, order_id, from_status, to_status, create_time)
VALUES ('OSL_DST_002', 'O_DST_001', 'CREATED', 'COMPLETED', TIMESTAMP '2026-07-05 10:01:00');

INSERT INTO PAYMENT_TRANSACTION (payment_id, order_id, provider_trade_no, amount, status, payment_method, create_time)
VALUES ('PAY_DST_001', 'O_DST_001', 'GW-DST-001', 24.00, 'SUCCESS', 'STEAM_WALLET', TIMESTAMP '2026-07-05 10:01:00');

INSERT INTO GAME_ORDER (order_id, user_id, total_amount, order_type, order_status, payment_status, idempotency_key, expire_time, create_time)
VALUES ('O_CS2_FREE_001', 'P001', 0.00, 'BUY_GAME', 'COMPLETED', 'PAID', 'idem-order-cs2-free-001', TIMESTAMP '2026-07-05 10:35:00', TIMESTAMP '2026-07-05 10:05:00');

INSERT INTO ORDER_DETAIL (detail_id, order_id, game_id, original_price, discount_amount, payable_amount, refund_amount)
VALUES ('OD_CS2_FREE_001', 'O_CS2_FREE_001', 'GAME_CS2', 0.00, 0.00, 0.00, 0.00);

INSERT INTO ORDER_STATUS_LOG (log_id, order_id, from_status, to_status, create_time)
VALUES ('OSL_CS2_001', 'O_CS2_FREE_001', NULL, 'CREATED', TIMESTAMP '2026-07-05 10:05:00');

INSERT INTO ORDER_STATUS_LOG (log_id, order_id, from_status, to_status, create_time)
VALUES ('OSL_CS2_002', 'O_CS2_FREE_001', 'CREATED', 'COMPLETED', TIMESTAMP '2026-07-05 10:06:00');

INSERT INTO PAYMENT_TRANSACTION (payment_id, order_id, provider_trade_no, amount, status, create_time)
VALUES ('PAY_CS2_FREE_001', 'O_CS2_FREE_001', 'GW-CS2-FREE-001', 0.00, 'SUCCESS', TIMESTAMP '2026-07-05 10:06:00');

INSERT INTO REFUND_TICKET (refund_id, order_id, refund_amount, refund_type, reason, play_time_hours, status, apply_time)
VALUES ('R_DST_001', 'O_DST_001', 24.00, 'FULL', 'Demo full refund request for DST.', 0.50, 'REJECTED', TIMESTAMP '2026-07-05 11:00:00');

INSERT INTO REFUND_DETAIL (refund_detail_id, refund_id, order_detail_id, refund_amount)
VALUES ('RD_DST_001', 'R_DST_001', 'OD_DST_001', 24.00);

INSERT INTO REFUND_AUDIT_LOG (audit_id, refund_id, operator_id, from_status, to_status, reason, create_time)
VALUES ('RAL_DST_001', 'R_DST_001', 'ADM001', 'PENDING', 'REJECTED', 'Demo data: refund rejected for audit-flow display.', TIMESTAMP '2026-07-05 11:05:00');

INSERT INTO PLAYER_LIBRARY (lib_id, user_id, game_id, acquire_way, status, play_minutes, last_play_time)
VALUES ('LIB_DST_P001', 'P001', 'GAME_DST', 'BUY', 'NORMAL', 80, TIMESTAMP '2026-07-05 12:00:00');

INSERT INTO PLAYER_LIBRARY (lib_id, user_id, game_id, acquire_way, status, play_minutes, last_play_time)
VALUES ('LIB_CS2_P001', 'P001', 'GAME_CS2', 'FREE', 'NORMAL', 15, TIMESTAMP '2026-07-05 12:05:00');

INSERT INTO PLAYER_LIBRARY (lib_id, user_id, game_id, acquire_way, status, play_minutes, last_play_time)
VALUES ('LIB_DST_P002', 'P002', 'GAME_DST', 'REDEEM', 'NORMAL', 10, TIMESTAMP '2026-07-05 12:10:00');

INSERT INTO CDKEY_BATCH (batch_id, game_id, batch_no, valid_from, expire_time)
VALUES ('B_DST_001', 'GAME_DST', 'BATCH-DST-0001', TIMESTAMP '2026-07-01 00:00:00', TIMESTAMP '2026-12-31 23:59:59');

INSERT INTO CDKEY (cdkey_hash, batch_id, status, generate_time)
VALUES ('AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA', 'B_DST_001', 'REDEEMED', TIMESTAMP '2026-07-05 09:30:00');

INSERT INTO CDKEY (cdkey_hash, batch_id, status, generate_time)
VALUES ('BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB', 'B_DST_001', 'AVAILABLE', TIMESTAMP '2026-07-05 09:31:00');

INSERT INTO CDKEY_REDEEM_LOG (log_id, user_id, submitted_hash, cdkey_hash, result, fail_reason, ip_hash, create_time)
VALUES ('CRL_DST_001', 'P002', 'AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA', 'AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA', 'SUCCESS', NULL, 'iphash-demo-001', TIMESTAMP '2026-07-05 10:20:00');

INSERT INTO GAME_REVIEW (review_id, user_id, game_id, thumbs_up, status, create_time)
VALUES ('REV_DST_001', 'P001', 'GAME_DST', 12, 'VISIBLE', TIMESTAMP '2026-07-05 13:00:00');

INSERT INTO REVIEW_VERSION (version_id, review_id, version_no, is_recommend, content, create_time)
VALUES ('RV_DST_001', 'REV_DST_001', 1, 1, 'Co-op survival, seasonal skins, and workshop content make DST a good Steam-like demo game.', TIMESTAMP '2026-07-05 13:00:00');

INSERT INTO ACHIEVEMENT (ach_id, game_id, ach_name, description, global_rate)
VALUES ('ACH_DST_SURVIVE_001', 'GAME_DST', 'First Night Together', 'Survive the first night with another player.', 42.50);

INSERT INTO ACHIEVEMENT (ach_id, game_id, ach_name, description, global_rate)
VALUES ('ACH_DST_SCIENCE_MACHINE', 'GAME_DST', 'Science Started', 'Build a Science Machine and unlock the first survival recipes.', 37.80);

INSERT INTO ACHIEVEMENT (ach_id, game_id, ach_name, description, global_rate)
VALUES ('ACH_DST_WINTER_SURVIVOR', 'GAME_DST', 'Winter Camp', 'Keep the camp and teammates alive through winter.', 18.40);

INSERT INTO ACHIEVEMENT (ach_id, game_id, ach_name, description, global_rate)
VALUES ('ACH_DST_RUINS_DIVER', 'GAME_DST', 'Ruins Lantern', 'Explore the ruins and bring back ancient technology.', 12.10);

INSERT INTO ACHIEVEMENT (ach_id, game_id, ach_name, description, global_rate)
VALUES ('ACH_DST_SHADOW_DUEL', 'GAME_DST', 'Shadow Duel', 'Defeat a shadow creature while keeping sanity stable.', 21.60);

INSERT INTO ACHIEVEMENT (ach_id, game_id, ach_name, description, global_rate)
VALUES ('ACH_DST_CELESTIAL_CARTOGRAPHER', 'GAME_DST', 'Celestial Cartographer', 'Discover Lunar Island and collect moon tech materials.', 9.70);

INSERT INTO ACHIEVEMENT (ach_id, game_id, ach_name, description, global_rate)
VALUES ('ACH_CS2_FIRST_ROUND', 'GAME_CS2', 'Pistol Round Opener', 'Win the opening pistol round.', 55.20);

INSERT INTO ACHIEVEMENT (ach_id, game_id, ach_name, description, global_rate)
VALUES ('ACH_CS2_ACE', 'GAME_CS2', 'Ace Round', 'Eliminate all five enemies in one round.', 8.60);

INSERT INTO ACHIEVEMENT (ach_id, game_id, ach_name, description, global_rate)
VALUES ('ACH_CS2_BOMB_PLANT', 'GAME_CS2', 'Bomb Site Secured', 'Plant the C4 as the attacking side.', 44.30);

INSERT INTO ACHIEVEMENT (ach_id, game_id, ach_name, description, global_rate)
VALUES ('ACH_CS2_DEFUSE', 'GAME_CS2', 'Defuse Expert', 'Defuse the bomb in a key round.', 31.40);

INSERT INTO ACHIEVEMENT (ach_id, game_id, ach_name, description, global_rate)
VALUES ('ACH_CS2_MARKET_MAKER', 'GAME_CS2', 'Market Rookie', 'Complete one item market trade record.', 16.90);

INSERT INTO PLAYER_ACHIEVEMENT (unlock_id, user_id, ach_id, unlock_time)
VALUES ('PA_DST_001', 'P001', 'ACH_DST_SURVIVE_001', TIMESTAMP '2026-07-05 12:30:00');

INSERT INTO ITEM_TEMPLATE (template_id, game_id, item_name, rarity, image_url)
VALUES ('ITPL_CS2_AK_REDLINE', 'GAME_CS2', 'AK-47 | Redline', 'EPIC', '/assets/items/cs2/cs2-ak-redline.png');

INSERT INTO ITEM_TEMPLATE (template_id, game_id, item_name, rarity, image_url)
VALUES ('ITPL_CS2_AK_NEON_RIDER', 'GAME_CS2', 'AK-47 | Neon Rider', 'LEGENDARY', '/assets/items/cs2/cs2-ak-neon-rider.png');

INSERT INTO ITEM_TEMPLATE (template_id, game_id, item_name, rarity, image_url)
VALUES ('ITPL_CS2_AWP_ASIIMOV', 'GAME_CS2', 'AWP | Asiimov', 'LEGENDARY', '/assets/items/cs2/cs2-awp-asiimov.png');

INSERT INTO ITEM_TEMPLATE (template_id, game_id, item_name, rarity, image_url)
VALUES ('ITPL_CS2_AWP_DRAGON_LORE', 'GAME_CS2', 'AWP | Dragon Lore', 'LEGENDARY', '/assets/items/cs2/cs2-awp-dragon-lore.png');

INSERT INTO ITEM_TEMPLATE (template_id, game_id, item_name, rarity, image_url)
VALUES ('ITPL_CS2_M4A1_PRINTSTREAM', 'GAME_CS2', 'M4A1-S | Printstream', 'LEGENDARY', '/assets/items/cs2/cs2-m4a1-printstream.png');

INSERT INTO ITEM_TEMPLATE (template_id, game_id, item_name, rarity, image_url)
VALUES ('ITPL_CS2_GLOCK_WATER', 'GAME_CS2', 'Glock-18 | Water Elemental', 'RARE', '/assets/items/cs2/cs2-glock-water.png');

INSERT INTO ITEM_TEMPLATE (template_id, game_id, item_name, rarity, image_url)
VALUES ('ITPL_CS2_GLOCK_FADE', 'GAME_CS2', 'Glock-18 | Fade', 'LEGENDARY', '/assets/items/cs2/cs2-glock-fade.png');

INSERT INTO ITEM_TEMPLATE (template_id, game_id, item_name, rarity, image_url)
VALUES ('ITPL_CS2_USP_TRAITOR', 'GAME_CS2', 'USP-S | The Traitor', 'EPIC', '/assets/items/cs2/cs2-usp-traitor.png');

INSERT INTO ITEM_TEMPLATE (template_id, game_id, item_name, rarity, image_url)
VALUES ('ITPL_CS2_USP_KILL_CONFIRMED', 'GAME_CS2', 'USP-S | Kill Confirmed', 'LEGENDARY', '/assets/items/cs2/cs2-usp-kill-confirmed.png');

INSERT INTO ITEM_TEMPLATE (template_id, game_id, item_name, rarity, image_url)
VALUES ('ITPL_CS2_DEAGLE_BLAZE', 'GAME_CS2', 'Desert Eagle | Blaze', 'LEGENDARY', '/assets/items/cs2/cs2-deagle-blaze.png');

INSERT INTO ITEM_TEMPLATE (template_id, game_id, item_name, rarity, image_url)
VALUES ('ITPL_CS2_DEAGLE_PRINTSTREAM', 'GAME_CS2', 'Desert Eagle | Printstream', 'EPIC', '/assets/items/cs2/cs2-deagle-printstream.png');

INSERT INTO ITEM_TEMPLATE (template_id, game_id, item_name, rarity, image_url)
VALUES ('ITPL_CS2_CASE_DREAMS', 'GAME_CS2', 'Dreams & Nightmares Case', 'RARE', '/assets/items/cs2/cs2-dreams-case.png');

INSERT INTO ITEM_TEMPLATE (template_id, game_id, item_name, rarity, image_url)
VALUES ('ITPL_CS2_FAMAS_MECHA', 'GAME_CS2', 'FAMAS | Mecha Industries', 'RARE', '/assets/items/cs2/cs2-famas-mecha.png');

INSERT INTO ITEM_TEMPLATE (template_id, game_id, item_name, rarity, image_url)
VALUES ('ITPL_CS2_FAMAS_COMMEMORATION', 'GAME_CS2', 'FAMAS | Commemoration', 'EPIC', '/assets/items/cs2/cs2-famas-commemoration.png');

INSERT INTO ITEM_TEMPLATE (template_id, game_id, item_name, rarity, image_url)
VALUES ('ITPL_CS2_GALIL_PHOENIX', 'GAME_CS2', 'Galil AR | Phoenix Blacklight', 'RARE', '/assets/items/cs2/cs2-galil-phoenix.png');

INSERT INTO ITEM_TEMPLATE (template_id, game_id, item_name, rarity, image_url)
VALUES ('ITPL_CS2_GALIL_CHROMATIC', 'GAME_CS2', 'Galil AR | Chromatic Aberration', 'EPIC', '/assets/items/cs2/cs2-galil-chromatic.png');

INSERT INTO ITEM_TEMPLATE (template_id, game_id, item_name, rarity, image_url)
VALUES ('ITPL_CS2_M4A4_TOOTH_FAIRY', 'GAME_CS2', 'M4A4 | Tooth Fairy', 'EPIC', '/assets/items/cs2/cs2-m4a4-tooth-fairy.png');

INSERT INTO ITEM_TEMPLATE (template_id, game_id, item_name, rarity, image_url)
VALUES ('ITPL_CS2_M4A4_HOWL', 'GAME_CS2', 'M4A4 | Howl', 'LEGENDARY', '/assets/items/cs2/cs2-m4a4-howl.png');

INSERT INTO ITEM_TEMPLATE (template_id, game_id, item_name, rarity, image_url)
VALUES ('ITPL_CS2_MP9_DARK_TIDE', 'GAME_CS2', 'MP9 | Dark Tide', 'UNCOMMON', '/assets/items/cs2/cs2-mp9-dark-tide.png');

INSERT INTO ITEM_TEMPLATE (template_id, game_id, item_name, rarity, image_url)
VALUES ('ITPL_CS2_MP9_STARLIGHT', 'GAME_CS2', 'MP9 | Starlight Protector', 'EPIC', '/assets/items/cs2/cs2-mp9-starlight.png');

INSERT INTO ITEM_TEMPLATE (template_id, game_id, item_name, rarity, image_url)
VALUES ('ITPL_CS2_P250_CYBER_SHELL', 'GAME_CS2', 'P250 | Cyber Shell', 'UNCOMMON', '/assets/items/cs2/cs2-p250-cyber-shell.png');

INSERT INTO ITEM_TEMPLATE (template_id, game_id, item_name, rarity, image_url)
VALUES ('ITPL_CS2_P250_SEE_YA_LATER', 'GAME_CS2', 'P250 | See Ya Later', 'EPIC', '/assets/items/cs2/cs2-p250-see-ya-later.png');

INSERT INTO ITEM_TEMPLATE (template_id, game_id, item_name, rarity, image_url)
VALUES ('ITPL_CS2_P90_ELITE_BUILD', 'GAME_CS2', 'P90 | Elite Build', 'COMMON', '/assets/items/cs2/cs2-p90-elite-build.png');

INSERT INTO ITEM_TEMPLATE (template_id, game_id, item_name, rarity, image_url)
VALUES ('ITPL_CS2_P90_ASIIMOV', 'GAME_CS2', 'P90 | Asiimov', 'EPIC', '/assets/items/cs2/cs2-p90-asiimov.png');

INSERT INTO ITEM_TEMPLATE (template_id, game_id, item_name, rarity, image_url)
VALUES ('ITPL_CS2_STICKER_CROWN', 'GAME_CS2', 'Sticker | Crown', 'RARE', '/assets/items/cs2/cs2-sticker-crown.png');

INSERT INTO ITEM_TEMPLATE (template_id, game_id, item_name, rarity, image_url)
VALUES ('ITPL_CS2_TEC9_NUCLEAR', 'GAME_CS2', 'Tec-9 | Nuclear Threat', 'EPIC', '/assets/items/cs2/cs2-tec9-nuclear-threat.png');

INSERT INTO ITEM_TEMPLATE (template_id, game_id, item_name, rarity, image_url)
VALUES ('ITPL_CS2_TEC9_DECIMATOR', 'GAME_CS2', 'Tec-9 | Decimator', 'RARE', '/assets/items/cs2/cs2-tec9-decimator.png');

INSERT INTO ITEM_TEMPLATE (template_id, game_id, item_name, rarity, image_url)
VALUES ('ITPL_DST_HOLLY_WREATH', 'GAME_DST', 'Holly Wreath', 'COMMON', '/assets/items/dst/dst-holly-wreath.png');

INSERT INTO ITEM_TEMPLATE (template_id, game_id, item_name, rarity, image_url)
VALUES ('ITPL_DST_NIGHTGOWN', 'GAME_DST', 'Distinguished Nightgown', 'RARE', '/assets/items/dst/dst-distinguished-nightgown.png');

INSERT INTO ITEM_TEMPLATE (template_id, game_id, item_name, rarity, image_url)
VALUES ('ITPL_DST_GUEST_HONOR', 'GAME_DST', 'Elegant Guest of Honor', 'EPIC', '/assets/items/dst/dst-guest-of-honor.png');

INSERT INTO ITEM_TEMPLATE (template_id, game_id, item_name, rarity, image_url)
VALUES ('ITPL_DST_STARTER_PACK', 'GAME_DST', 'Starter Pack 2025 Chest', 'UNCOMMON', '/assets/items/dst/dst-starter-pack-chest.png');

INSERT INTO ITEM_TEMPLATE (template_id, game_id, item_name, rarity, image_url)
VALUES ('ITPL_DST_LUNAR_WINGS', 'GAME_DST', 'Lunar Moth Wings', 'RARE', '/assets/items/dst/dst-lunar-moth-wings.png');

INSERT INTO ITEM_TEMPLATE (template_id, game_id, item_name, rarity, image_url)
VALUES ('ITPL_DST_CELESTIAL_GOBLET', 'GAME_DST', 'Celestial Goblet', 'EPIC', '/assets/items/dst/dst-celestial-goblet.png');

INSERT INTO ITEM_TEMPLATE (template_id, game_id, item_name, rarity, image_url)
VALUES ('ITPL_DST_CRYSTAL_AXE', 'GAME_DST', 'Crystal Axe', 'RARE', '/assets/items/dst/dst-crystal-axe.png');

INSERT INTO ITEM_TEMPLATE (template_id, game_id, item_name, rarity, image_url)
VALUES ('ITPL_DST_ICE_CROWN', 'GAME_DST', 'Ice Crown', 'EPIC', '/assets/items/dst/dst-ice-crown.png');

INSERT INTO ITEM_TEMPLATE (template_id, game_id, item_name, rarity, image_url)
VALUES ('ITPL_DST_SHROOM_HAT', 'GAME_DST', 'Shroom Hat', 'UNCOMMON', '/assets/items/dst/dst-shroom-hat.png');

INSERT INTO ITEM_TEMPLATE (template_id, game_id, item_name, rarity, image_url)
VALUES ('ITPL_DST_PUMPKIN_LANTERN', 'GAME_DST', 'Pumpkin Lantern', 'UNCOMMON', '/assets/items/dst/dst-pumpkin-lantern.png');

INSERT INTO ITEM_TEMPLATE (template_id, game_id, item_name, rarity, image_url)
VALUES ('ITPL_DST_MOON_STAFF', 'GAME_DST', 'Moon Staff', 'LEGENDARY', '/assets/items/dst/dst-moon-staff.png');

INSERT INTO ITEM_TEMPLATE (template_id, game_id, item_name, rarity, image_url)
VALUES ('ITPL_DST_GHOST_COSTUME', 'GAME_DST', 'Ghost Costume', 'COMMON', '/assets/items/dst/dst-ghost-costume.png');

INSERT INTO INVENTORY_ITEM (item_id, template_id, user_id, wear_rating, status, acquire_time, version)
VALUES ('ITEM_CS2_001', 'ITPL_CS2_AK_REDLINE', 'P001', 0.1250, 'IN_MARKET', TIMESTAMP '2026-07-05 12:40:00', 1);

INSERT INTO INVENTORY_ITEM (item_id, template_id, user_id, wear_rating, status, acquire_time, version)
VALUES ('ITEM_CS2_002', 'ITPL_CS2_AK_REDLINE', 'P001', 0.3320, 'NORMAL', TIMESTAMP '2026-07-05 12:45:00', 2);

INSERT INTO INVENTORY_ITEM (item_id, template_id, user_id, wear_rating, status, acquire_time, version)
VALUES ('ITEM_DST_001', 'ITPL_DST_HOLLY_WREATH', 'P001', NULL, 'NORMAL', TIMESTAMP '2026-07-05 12:50:00', 1);

INSERT INTO INVENTORY_ITEM (item_id, template_id, user_id, wear_rating, status, acquire_time, version)
VALUES ('ITEM_OFFER_P001', 'ITPL_CS2_P90_ELITE_BUILD', 'P001', 0.1840, 'LOCKED', TIMESTAMP '2026-07-05 12:52:00', 1);

INSERT INTO INVENTORY_ITEM (item_id, template_id, user_id, wear_rating, status, acquire_time, version)
VALUES ('ITEM_OFFER_P002', 'ITPL_CS2_P250_CYBER_SHELL', 'P002', 0.0910, 'LOCKED', TIMESTAMP '2026-07-05 12:53:00', 1);

INSERT INTO INVENTORY_ITEM (item_id, template_id, user_id, wear_rating, status, acquire_time, version)
VALUES ('ITEM_TRADEABLE_P002', 'ITPL_CS2_P250_SEE_YA_LATER', 'P002', 0.2410, 'NORMAL', TIMESTAMP '2026-07-05 12:54:00', 0);

INSERT INTO MARKET_ORDER (market_order_id, user_id, template_id, item_id, order_type, target_price, frozen_amount, status, create_time)
VALUES ('MO_CS2_BUY_1', 'P001', 'ITPL_CS2_AK_REDLINE', NULL, 'BUY', 45.00, 0.00, 'TRADED', TIMESTAMP '2026-07-05 14:00:00');

INSERT INTO MARKET_ORDER (market_order_id, user_id, template_id, item_id, order_type, target_price, frozen_amount, status, create_time)
VALUES ('MO_CS2_SELL_1', 'P002', 'ITPL_CS2_AK_REDLINE', 'ITEM_CS2_002', 'SELL', 45.00, 0.00, 'TRADED', TIMESTAMP '2026-07-05 14:01:00');

INSERT INTO MARKET_ORDER (market_order_id, user_id, template_id, item_id, order_type, target_price, frozen_amount, status, create_time)
VALUES ('MO_CS2_BUY_2', 'P002', 'ITPL_CS2_AK_REDLINE', NULL, 'BUY', 50.00, 50.00, 'MATCHING', TIMESTAMP '2026-07-05 14:10:00');

INSERT INTO MARKET_ORDER (market_order_id, user_id, template_id, item_id, order_type, target_price, frozen_amount, status, create_time)
VALUES ('MO_CS2_SELL_2', 'P001', 'ITPL_CS2_AK_REDLINE', 'ITEM_CS2_001', 'SELL', 50.00, 0.00, 'MATCHING', TIMESTAMP '2026-07-05 14:11:00');

INSERT INTO MARKET_TRADE (trade_id, buy_order_id, sell_order_id, template_id, item_id, buyer_id, seller_id, trade_price, platform_fee, currency, trade_time)
VALUES ('TRD_CS2_001', 'MO_CS2_BUY_1', 'MO_CS2_SELL_1', 'ITPL_CS2_AK_REDLINE', 'ITEM_CS2_002', 'P001', 'P002', 45.00, 2.25, 'CNY', TIMESTAMP '2026-07-05 14:02:00');

INSERT INTO ITEM_TRANSFER_LEDGER (transfer_id, item_id, from_user_id, to_user_id, transfer_type, transfer_time)
VALUES ('ITL_CS2_DROP_001', 'ITEM_CS2_001', NULL, 'P001', 'DROP', TIMESTAMP '2026-07-05 12:40:00');

INSERT INTO ITEM_TRANSFER_LEDGER (transfer_id, item_id, from_user_id, to_user_id, transfer_type, transfer_time)
VALUES ('ITL_CS2_DROP_002', 'ITEM_CS2_002', NULL, 'P002', 'DROP', TIMESTAMP '2026-07-05 12:45:00');

INSERT INTO ITEM_TRANSFER_LEDGER (transfer_id, item_id, from_user_id, to_user_id, transfer_type, transfer_time)
VALUES ('ITL_CS2_TRADE_001', 'ITEM_CS2_002', 'P002', 'P001', 'TRADE', TIMESTAMP '2026-07-05 14:02:00');

INSERT INTO ITEM_TRANSFER_LEDGER (transfer_id, item_id, from_user_id, to_user_id, transfer_type, transfer_time)
VALUES ('ITL_OFFER_DROP_001', 'ITEM_OFFER_P001', NULL, 'P001', 'DROP', TIMESTAMP '2026-07-05 12:52:00');

INSERT INTO ITEM_TRANSFER_LEDGER (transfer_id, item_id, from_user_id, to_user_id, transfer_type, transfer_time)
VALUES ('ITL_OFFER_DROP_002', 'ITEM_OFFER_P002', NULL, 'P002', 'DROP', TIMESTAMP '2026-07-05 12:53:00');

INSERT INTO ITEM_TRANSFER_LEDGER (transfer_id, item_id, from_user_id, to_user_id, transfer_type, transfer_time)
VALUES ('ITL_TRADEABLE_DROP_002', 'ITEM_TRADEABLE_P002', NULL, 'P002', 'DROP', TIMESTAMP '2026-07-05 12:54:00');

INSERT INTO WALLET_TRANSACTION (txn_id, wallet_id, biz_type, biz_ref_id, funds_direction, amount, avail_bal_before, avail_bal_after, idempotency_key, payment_method, create_time)
VALUES ('WT_DST_BUY_001', 'W001', 'BUY_GAME', 'O_DST_001', 'DEBIT', 24.00, 200.00, 176.00, 'idem-wallet-dst-buy-001', 'STEAM_WALLET', TIMESTAMP '2026-07-05 10:01:00');

INSERT INTO WALLET_TRANSACTION (txn_id, wallet_id, biz_type, biz_ref_id, funds_direction, amount, avail_bal_before, avail_bal_after, idempotency_key, payment_method, create_time)
VALUES ('WT_MARKET_SELL_001', 'W002', 'MARKET_SELL', 'TRD_CS2_001', 'CREDIT', 42.75, 200.00, 242.75, 'idem-wallet-market-sell-001', 'STEAM_WALLET', TIMESTAMP '2026-07-05 14:02:00');

INSERT INTO WALLET_TRANSACTION (txn_id, wallet_id, biz_type, biz_ref_id, funds_direction, amount, avail_bal_before, avail_bal_after, idempotency_key, payment_method, create_time)
VALUES ('WT_MARKET_FREEZE_001', 'W002', 'MARKET_FREEZE', 'MO_CS2_BUY_2', 'FREEZE', 50.00, 292.75, 242.75, 'idem-wallet-market-freeze-001', 'STEAM_WALLET', TIMESTAMP '2026-07-05 14:10:00');

INSERT INTO FRIEND_RELATION (relation_id, user_low_id, user_high_id, requested_by, status, created_at, responded_at)
VALUES ('FR_P001_P002', 'P001', 'P002', 'P001', 'ACCEPTED', TIMESTAMP '2026-07-05 15:00:00', TIMESTAMP '2026-07-05 15:01:00');

INSERT INTO DIRECT_MESSAGE (message_id, relation_id, sender_id, content, status, sent_at, read_at)
VALUES ('MSG_DEMO_001', 'FR_P001_P002', 'P002', 'CS2 市场演示准备好了吗？', 'SENT', TIMESTAMP '2026-07-05 15:10:00', TIMESTAMP '2026-07-05 15:11:00');

INSERT INTO DIRECT_MESSAGE (message_id, relation_id, sender_id, content, status, sent_at, read_at)
VALUES ('MSG_DEMO_002', 'FR_P001_P002', 'P001', '准备好了，稍后用 AK-47 | Redline 完成撮合。', 'SENT', TIMESTAMP '2026-07-05 15:12:00', NULL);

INSERT INTO REVIEW_REACTION (review_id, user_id, vote_type, is_starred, is_funny, is_awarded, updated_at)
VALUES ('REV_DST_001', 'P002', 'UP', 1, 0, 1, TIMESTAMP '2026-07-05 15:20:00');

INSERT INTO WORKSHOP_ITEM (workshop_item_id, game_id, creator_user_id, title, category, summary, details, image_url, status, created_at, updated_at)
VALUES ('WS_CS2_AIM', 'GAME_CS2', 'P002', 'Aim Training Arena', '训练地图', '社区制作的瞄准与压枪训练场。', '支持多种武器、距离与移动目标组合，用于 CS2 训练演示。', '/assets/games/cs2-library-hero.jpg', 'PUBLISHED', TIMESTAMP '2026-07-05 16:00:00', TIMESTAMP '2026-07-08 10:00:00');

INSERT INTO WORKSHOP_ITEM (workshop_item_id, game_id, creator_user_id, title, category, summary, details, image_url, status, created_at, updated_at)
VALUES ('WS_CS2_MIRAGE', 'GAME_CS2', 'P001', 'Mirage Night Practice', '竞技地图', '夜间 Mirage 战术练习版本。', '保留主要点位并加入夜间灯光，适合课程展示地图详情和订阅。', '/assets/games/cs2-header.jpg', 'PUBLISHED', TIMESTAMP '2026-07-05 16:05:00', TIMESTAMP '2026-07-07 11:00:00');

INSERT INTO WORKSHOP_ITEM (workshop_item_id, game_id, creator_user_id, title, category, summary, details, image_url, status, created_at, updated_at)
VALUES ('WS_CS2_RETAKE', 'GAME_CS2', 'P002', 'Retake Utility Lab', '战术工具', '残局与道具投掷训练工具。', '提供常用烟雾、闪光和残局站位提示。', '/assets/games/cs2-library-cover.jpg', 'PUBLISHED', TIMESTAMP '2026-07-05 16:10:00', TIMESTAMP '2026-07-06 12:00:00');

INSERT INTO WORKSHOP_ITEM (workshop_item_id, game_id, creator_user_id, title, category, summary, details, image_url, status, created_at, updated_at)
VALUES ('WS_CS2_WINGMAN', 'GAME_CS2', 'P001', 'Warehouse Wingman', '搭档地图', '紧凑仓库主题搭档地图。', '为 2v2 快节奏对局设计的课程演示地图。', '/assets/games/cs2-library-hero.jpg', 'PUBLISHED', TIMESTAMP '2026-07-05 16:15:00', TIMESTAMP '2026-07-05 16:15:00');

INSERT INTO WORKSHOP_ITEM (workshop_item_id, game_id, creator_user_id, title, category, summary, details, image_url, status, created_at, updated_at)
VALUES ('WS_DST_SEASONS', 'GAME_DST', 'P002', '四季生存扩展', '世界模组', '强化四季天气与团队生存节奏。', '加入季节事件和团队任务，适合联机生存演示。', '/assets/games/dst-library-hero.jpg', 'PUBLISHED', TIMESTAMP '2026-07-05 16:20:00', TIMESTAMP '2026-07-08 09:00:00');

INSERT INTO WORKSHOP_ITEM (workshop_item_id, game_id, creator_user_id, title, category, summary, details, image_url, status, created_at, updated_at)
VALUES ('WS_DST_ISLAND', 'GAME_DST', 'P001', '海岛营地合集', '建筑合集', '海岛主题营地建筑与装饰合集。', '提供适合多人基地规划的装饰、照明和储物建筑。', '/assets/games/dst-header.jpg', 'PUBLISHED', TIMESTAMP '2026-07-05 16:25:00', TIMESTAMP '2026-07-07 09:00:00');

INSERT INTO WORKSHOP_ITEM (workshop_item_id, game_id, creator_user_id, title, category, summary, details, image_url, status, created_at, updated_at)
VALUES ('WS_DST_STORAGE', 'GAME_DST', 'P002', '自动整理箱', '实用工具', '按物品类型自动整理团队仓库。', '降低多人联机的物资整理成本，并展示订阅状态持久化。', '/assets/games/dst-library-cover.jpg', 'PUBLISHED', TIMESTAMP '2026-07-05 16:30:00', TIMESTAMP '2026-07-06 09:00:00');

INSERT INTO WORKSHOP_ITEM (workshop_item_id, game_id, creator_user_id, title, category, summary, details, image_url, status, created_at, updated_at)
VALUES ('WS_DST_RUINS', 'GAME_DST', 'P001', '远古遗迹挑战', '冒险模组', '面向多人队伍的遗迹挑战路线。', '包含阶段目标、战利品和自定义成就演示。', '/assets/games/dst-library-hero.jpg', 'PUBLISHED', TIMESTAMP '2026-07-05 16:35:00', TIMESTAMP '2026-07-05 16:35:00');

INSERT INTO WORKSHOP_SUBSCRIPTION (workshop_item_id, user_id, subscribed_at)
VALUES ('WS_DST_SEASONS', 'P001', TIMESTAMP '2026-07-05 17:00:00');

INSERT INTO WORKSHOP_SUBSCRIPTION (workshop_item_id, user_id, subscribed_at)
VALUES ('WS_CS2_AIM', 'P001', TIMESTAMP '2026-07-05 17:05:00');

INSERT INTO USER_NOTIFICATION (notification_id, user_id, notification_type, title, message, target_url, is_read, created_at, read_at)
VALUES ('NTF_DEMO_001', 'P001', 'DIRECT_MESSAGE', 'Bob 发来新消息', 'CS2 市场演示准备好了吗？', '/community', 1, TIMESTAMP '2026-07-05 15:10:00', TIMESTAMP '2026-07-05 15:11:00');

INSERT INTO USER_NOTIFICATION (notification_id, user_id, notification_type, title, message, target_url, is_read, created_at, read_at)
VALUES ('NTF_DEMO_002', 'P001', 'WORKSHOP_UPDATE', '工坊作品已更新', '四季生存扩展发布了新的团队任务。', '/games/GAME_DST/community?section=workshop', 0, TIMESTAMP '2026-07-08 09:05:00', NULL);

INSERT INTO PLAYER_PROFILE (user_id, headline, bio, avatar_key, background_key, theme_key, showcase_game_id, profile_visibility, updated_at)
VALUES ('P001', '生存游戏与饰品收藏爱好者', '正在完善 CS2 饰品交易和饥荒联机版生存挑战展示。', 'AVATAR_BLUE', 'BACKGROUND_CS2', 'STEAM_BLUE', 'GAME_CS2', 'PUBLIC', TIMESTAMP '2026-07-08 10:00:00');

INSERT INTO PLAYER_PROFILE (user_id, headline, bio, avatar_key, background_key, theme_key, showcase_game_id, profile_visibility, updated_at)
VALUES ('P002', '合作生存与社区工坊创作者', '喜欢制作联机模组，也会关注 CS2 社区市场。', 'AVATAR_ORANGE', 'BACKGROUND_DST', 'SURVIVAL_GREEN', 'GAME_DST', 'PUBLIC', TIMESTAMP '2026-07-08 10:05:00');

INSERT INTO BADGE_CATALOG (badge_id, badge_name, description, icon_key, xp_value, rarity)
VALUES ('BDG_EARLY_MEMBER', '平台先行者', '完成账号注册并加入课程演示平台。', 'SPARKLES', 100, 'COMMON');

INSERT INTO BADGE_CATALOG (badge_id, badge_name, description, icon_key, xp_value, rarity)
VALUES ('BDG_DST_SURVIVOR', '荒野生存者', '完成饥荒联机版首个生存挑战。', 'CAMPFIRE', 250, 'RARE');

INSERT INTO BADGE_CATALOG (badge_id, badge_name, description, icon_key, xp_value, rarity)
VALUES ('BDG_CS2_TRADER', '饰品交易员', '完成一次 CS2 饰品市场交易。', 'GEM', 300, 'EPIC');

INSERT INTO BADGE_CATALOG (badge_id, badge_name, description, icon_key, xp_value, rarity)
VALUES ('BDG_COMMUNITY_HELPER', '社区热心人', '积极参与评测、工坊与讨论互动。', 'MESSAGES', 180, 'RARE');

INSERT INTO PLAYER_BADGE (user_id, badge_id, earned_at, is_featured)
VALUES ('P001', 'BDG_EARLY_MEMBER', TIMESTAMP '2026-07-05 09:00:00', 0);

INSERT INTO PLAYER_BADGE (user_id, badge_id, earned_at, is_featured)
VALUES ('P001', 'BDG_CS2_TRADER', TIMESTAMP '2026-07-05 14:02:00', 1);

INSERT INTO PLAYER_BADGE (user_id, badge_id, earned_at, is_featured)
VALUES ('P001', 'BDG_DST_SURVIVOR', TIMESTAMP '2026-07-05 18:00:00', 0);

INSERT INTO PLAYER_BADGE (user_id, badge_id, earned_at, is_featured)
VALUES ('P002', 'BDG_EARLY_MEMBER', TIMESTAMP '2026-07-05 09:05:00', 0);

INSERT INTO PLAYER_BADGE (user_id, badge_id, earned_at, is_featured)
VALUES ('P002', 'BDG_COMMUNITY_HELPER', TIMESTAMP '2026-07-08 09:30:00', 1);

INSERT INTO TRADE_OFFER (offer_id, sender_id, recipient_id, message, status, created_at, responded_at, version)
VALUES ('TO_DEMO_001', 'P001', 'P002', '用 P90 | Elite Build 交换你的 P250 | Cyber Shell，可以吗？', 'PENDING', TIMESTAMP '2026-07-08 11:00:00', NULL, 0);

INSERT INTO TRADE_OFFER_ITEM (offer_id, item_id, item_role, owner_id_at_create)
VALUES ('TO_DEMO_001', 'ITEM_OFFER_P001', 'OFFERED', 'P001');

INSERT INTO TRADE_OFFER_ITEM (offer_id, item_id, item_role, owner_id_at_create)
VALUES ('TO_DEMO_001', 'ITEM_OFFER_P002', 'REQUESTED', 'P002');

INSERT INTO COMMUNITY_POST (post_id, author_id, game_id, post_type, content, media_url, visibility, status, created_at, updated_at)
VALUES ('POST_DEMO_001', 'P001', 'GAME_DST', 'STATUS', '和 Bob 完成了第一晚生存，营地终于稳定下来了。', NULL, 'PUBLIC', 'VISIBLE', TIMESTAMP '2026-07-08 12:00:00', TIMESTAMP '2026-07-08 12:00:00');

INSERT INTO COMMUNITY_POST (post_id, author_id, game_id, post_type, content, media_url, visibility, status, created_at, updated_at)
VALUES ('POST_DEMO_002', 'P002', 'GAME_DST', 'SCREENSHOT', '远古遗迹挑战路线测试完成，准备发布到创意工坊。', '/assets/media/dst-screenshot-3.jpg', 'PUBLIC', 'VISIBLE', TIMESTAMP '2026-07-08 12:10:00', TIMESTAMP '2026-07-08 12:10:00');

INSERT INTO COMMUNITY_POST (post_id, author_id, game_id, post_type, content, media_url, visibility, status, created_at, updated_at)
VALUES ('POST_DEMO_003', 'P001', 'GAME_CS2', 'ACHIEVEMENT', '解锁了 Defuse Expert，下一目标是 Ace Round。', '/assets/media/cs2-screenshot-2.jpg', 'PUBLIC', 'VISIBLE', TIMESTAMP '2026-07-08 12:20:00', TIMESTAMP '2026-07-08 12:20:00');

INSERT INTO COMMUNITY_POST (post_id, author_id, game_id, post_type, content, media_url, visibility, status, created_at, updated_at)
VALUES ('POST_DEMO_004', 'P002', 'GAME_CS2', 'TRADE', '正在查看新的 CS2 物品报价，欢迎好友交流收藏。', NULL, 'FRIENDS', 'VISIBLE', TIMESTAMP '2026-07-08 12:30:00', TIMESTAMP '2026-07-08 12:30:00');

INSERT INTO COMMUNITY_POST_REACTION (post_id, user_id, reaction_type, created_at)
VALUES ('POST_DEMO_001', 'P002', 'LIKE', TIMESTAMP '2026-07-08 12:05:00');

INSERT INTO COMMUNITY_POST_REACTION (post_id, user_id, reaction_type, created_at)
VALUES ('POST_DEMO_002', 'P001', 'AWARD', TIMESTAMP '2026-07-08 12:15:00');

INSERT INTO COMMUNITY_POST_REACTION (post_id, user_id, reaction_type, created_at)
VALUES ('POST_DEMO_003', 'P002', 'LIKE', TIMESTAMP '2026-07-08 12:25:00');

INSERT INTO DISCUSSION_TOPIC (topic_id, game_id, author_id, title, body, status, created_at, updated_at)
VALUES ('TOPIC_DST_001', 'GAME_DST', 'P001', '新手第一年应该优先准备什么？', '我们准备双人开局，希望讨论秋季到冬季的资源和营地规划。', 'OPEN', TIMESTAMP '2026-07-08 13:00:00', TIMESTAMP '2026-07-08 13:10:00');

INSERT INTO DISCUSSION_TOPIC (topic_id, game_id, author_id, title, body, status, created_at, updated_at)
VALUES ('TOPIC_CS2_001', 'GAME_CS2', 'P002', '如何练习 Mirage 常用烟雾？', '想配合创意工坊训练图整理一套固定练习流程。', 'OPEN', TIMESTAMP '2026-07-08 13:20:00', TIMESTAMP '2026-07-08 13:25:00');

INSERT INTO DISCUSSION_REPLY (reply_id, topic_id, author_id, body, status, created_at, updated_at)
VALUES ('REPLY_DST_001', 'TOPIC_DST_001', 'P002', '先稳定食物、木材和保暖物资，冬季前再扩展探索范围。', 'VISIBLE', TIMESTAMP '2026-07-08 13:10:00', TIMESTAMP '2026-07-08 13:10:00');

INSERT INTO DISCUSSION_REPLY (reply_id, topic_id, author_id, body, status, created_at, updated_at)
VALUES ('REPLY_CS2_001', 'TOPIC_CS2_001', 'P001', '可以先订阅 Aim Training Arena，再按 A 点、B 点分别保存投掷站位。', 'VISIBLE', TIMESTAMP '2026-07-08 13:25:00', TIMESTAMP '2026-07-08 13:25:00');

INSERT INTO USER_NOTIFICATION (notification_id, user_id, notification_type, title, message, target_url, is_read, created_at, read_at)
VALUES ('NTF_DEMO_003', 'P002', 'TRADE_OFFER', '收到新的交易报价', 'Alice 希望与你交换两件 CS2 饰品。', '/trade-offers', 0, TIMESTAMP '2026-07-08 11:00:00', NULL);

COMMIT;

PROMPT Steam course seed data inserted.
