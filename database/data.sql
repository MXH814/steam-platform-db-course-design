SET DEFINE OFF

PROMPT Inserting Steam course seed data...

INSERT INTO PLAYER (user_id, account, password_hash, nickname, credit_score, status, version, create_time, update_time)
VALUES ('P001', 'alice', '$2a$10$demoHashAlice', 'Alice', 100, 'NORMAL', 0, TIMESTAMP '2026-07-05 09:00:00', TIMESTAMP '2026-07-05 09:00:00');

INSERT INTO PLAYER (user_id, account, password_hash, nickname, credit_score, status, version, create_time, update_time)
VALUES ('P002', 'bob', '$2a$10$demoHashBob', 'Bob', 96, 'NORMAL', 0, TIMESTAMP '2026-07-05 09:05:00', TIMESTAMP '2026-07-05 09:05:00');

INSERT INTO DEVELOPER (dev_id, company_name, tax_id, contact_email, status, join_time)
VALUES ('DEV_VALVE', 'Valve', 'TAX-DEMO-VALVE', 'valve@example.com', 'APPROVED', TIMESTAMP '2026-07-05 09:10:00');

INSERT INTO DEVELOPER (dev_id, company_name, tax_id, contact_email, status, join_time)
VALUES ('DEV_KLEI', 'Klei Entertainment', 'TAX-DEMO-KLEI', 'klei@example.com', 'APPROVED', TIMESTAMP '2026-07-05 09:12:00');

INSERT INTO ADMIN_USER (admin_id, account, password_hash, role, create_time)
VALUES ('ADM001', 'rootadmin', '$2a$10$demoHashAdmin', 'SUPER_ADMIN', TIMESTAMP '2026-07-05 09:15:00');

INSERT INTO WALLET_ACCOUNT (wallet_id, user_id, available_balance, frozen_balance, version)
VALUES ('W001', 'P001', 176.00, 0.00, 2);

INSERT INTO WALLET_ACCOUNT (wallet_id, user_id, available_balance, frozen_balance, version)
VALUES ('W002', 'P002', 242.75, 50.00, 3);

INSERT INTO SYS_NOTICE (notice_id, publisher_type, publisher_id, title, content, priority, status, publish_time, expire_time)
VALUES ('N001', 'SYSTEM', NULL, 'CS2 and DST demo catalog ready', 'Counter-Strike 2 and Don''t Starve Together are the fixed sample games for the course demo.', 1, 'PUBLISHED', TIMESTAMP '2026-07-05 10:00:00', TIMESTAMP '2026-08-05 10:00:00');

INSERT INTO GAME (game_id, dev_id, game_name, base_price, discount_rate, release_date, reputation, status)
VALUES ('GAME_CS2', 'DEV_VALVE', 'Counter-Strike 2', 0.00, 1.00, DATE '2023-09-27', 'VERY_POSITIVE', 'ONLINE');

INSERT INTO GAME (game_id, dev_id, game_name, base_price, discount_rate, release_date, reputation, status)
VALUES ('GAME_DST', 'DEV_KLEI', 'Don''t Starve Together / 饥荒联机版', 48.00, 0.50, DATE '2016-04-21', 'OVERWHELMINGLY_POSITIVE', 'ONLINE');

INSERT INTO GAME_ORDER (order_id, user_id, total_amount, order_type, order_status, payment_status, idempotency_key, expire_time, create_time)
VALUES ('O_DST_001', 'P001', 24.00, 'BUY_GAME', 'COMPLETED', 'PAID', 'idem-order-dst-001', TIMESTAMP '2026-07-05 10:30:00', TIMESTAMP '2026-07-05 10:00:00');

INSERT INTO ORDER_DETAIL (detail_id, order_id, game_id, original_price, discount_amount, payable_amount, refund_amount)
VALUES ('OD_DST_001', 'O_DST_001', 'GAME_DST', 48.00, 24.00, 24.00, 0.00);

INSERT INTO ORDER_STATUS_LOG (log_id, order_id, from_status, to_status, create_time)
VALUES ('OSL_DST_001', 'O_DST_001', NULL, 'CREATED', TIMESTAMP '2026-07-05 10:00:00');

INSERT INTO ORDER_STATUS_LOG (log_id, order_id, from_status, to_status, create_time)
VALUES ('OSL_DST_002', 'O_DST_001', 'CREATED', 'COMPLETED', TIMESTAMP '2026-07-05 10:01:00');

INSERT INTO PAYMENT_TRANSACTION (payment_id, order_id, provider_trade_no, amount, status, create_time)
VALUES ('PAY_DST_001', 'O_DST_001', 'GW-DST-001', 24.00, 'SUCCESS', TIMESTAMP '2026-07-05 10:01:00');

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

INSERT INTO PLAYER_ACHIEVEMENT (unlock_id, user_id, ach_id, unlock_time)
VALUES ('PA_DST_001', 'P001', 'ACH_DST_SURVIVE_001', TIMESTAMP '2026-07-05 12:30:00');

INSERT INTO ITEM_TEMPLATE (template_id, game_id, item_name, rarity, image_url)
VALUES ('ITPL_CS2_AK_REDLINE', 'GAME_CS2', 'AK-47 | Redline', 'EPIC', '/assets/items/cs2-ak-redline.png');

INSERT INTO ITEM_TEMPLATE (template_id, game_id, item_name, rarity, image_url)
VALUES ('ITPL_CS2_AWP_ASIIMOV', 'GAME_CS2', 'AWP | Asiimov', 'LEGENDARY', '/assets/items/cs2-awp-asiimov.png');

INSERT INTO ITEM_TEMPLATE (template_id, game_id, item_name, rarity, image_url)
VALUES ('ITPL_CS2_M4A1_PRINTSTREAM', 'GAME_CS2', 'M4A1-S | Printstream', 'LEGENDARY', '/assets/items/cs2-m4a1-printstream.png');

INSERT INTO ITEM_TEMPLATE (template_id, game_id, item_name, rarity, image_url)
VALUES ('ITPL_CS2_GLOCK_WATER', 'GAME_CS2', 'Glock-18 | Water Elemental', 'RARE', '/assets/items/cs2-glock-water.png');

INSERT INTO ITEM_TEMPLATE (template_id, game_id, item_name, rarity, image_url)
VALUES ('ITPL_CS2_USP_TRAITOR', 'GAME_CS2', 'USP-S | The Traitor', 'EPIC', '/assets/items/cs2-usp-traitor.png');

INSERT INTO ITEM_TEMPLATE (template_id, game_id, item_name, rarity, image_url)
VALUES ('ITPL_CS2_DEAGLE_BLAZE', 'GAME_CS2', 'Desert Eagle | Blaze', 'LEGENDARY', '/assets/items/cs2-deagle-blaze.png');

INSERT INTO ITEM_TEMPLATE (template_id, game_id, item_name, rarity, image_url)
VALUES ('ITPL_CS2_CASE_DREAMS', 'GAME_CS2', 'Dreams & Nightmares Case', 'UNCOMMON', '/assets/items/cs2-dreams-case.png');

INSERT INTO ITEM_TEMPLATE (template_id, game_id, item_name, rarity, image_url)
VALUES ('ITPL_CS2_STICKER_CROWN', 'GAME_CS2', 'Sticker | Crown', 'RARE', '/assets/items/cs2-sticker-crown.png');

INSERT INTO ITEM_TEMPLATE (template_id, game_id, item_name, rarity, image_url)
VALUES ('ITPL_DST_HOLLY_WREATH', 'GAME_DST', 'Holly Wreath', 'COMMON', '/assets/items/dst-holly-wreath.png');

INSERT INTO ITEM_TEMPLATE (template_id, game_id, item_name, rarity, image_url)
VALUES ('ITPL_DST_NIGHTGOWN', 'GAME_DST', 'Distinguished Nightgown', 'RARE', '/assets/items/dst-nightgown.png');

INSERT INTO ITEM_TEMPLATE (template_id, game_id, item_name, rarity, image_url)
VALUES ('ITPL_DST_GUEST_HONOR', 'GAME_DST', 'Elegant Guest of Honor', 'EPIC', '/assets/items/dst-guest-honor.png');

INSERT INTO ITEM_TEMPLATE (template_id, game_id, item_name, rarity, image_url)
VALUES ('ITPL_DST_STARTER_PACK', 'GAME_DST', 'Starter Pack 2025 Chest', 'UNCOMMON', '/assets/items/dst-starter-pack-2025.png');

INSERT INTO INVENTORY_ITEM (item_id, template_id, user_id, wear_rating, status, acquire_time, version)
VALUES ('ITEM_CS2_001', 'ITPL_CS2_AK_REDLINE', 'P001', 0.1250, 'IN_MARKET', TIMESTAMP '2026-07-05 12:40:00', 1);

INSERT INTO INVENTORY_ITEM (item_id, template_id, user_id, wear_rating, status, acquire_time, version)
VALUES ('ITEM_CS2_002', 'ITPL_CS2_AK_REDLINE', 'P001', 0.3320, 'NORMAL', TIMESTAMP '2026-07-05 12:45:00', 2);

INSERT INTO INVENTORY_ITEM (item_id, template_id, user_id, wear_rating, status, acquire_time, version)
VALUES ('ITEM_DST_001', 'ITPL_DST_HOLLY_WREATH', 'P001', NULL, 'NORMAL', TIMESTAMP '2026-07-05 12:50:00', 1);

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

INSERT INTO WALLET_TRANSACTION (txn_id, wallet_id, biz_type, biz_ref_id, funds_direction, amount, avail_bal_before, avail_bal_after, idempotency_key, create_time)
VALUES ('WT_DST_BUY_001', 'W001', 'BUY_GAME', 'O_DST_001', 'DEBIT', 24.00, 200.00, 176.00, 'idem-wallet-dst-buy-001', TIMESTAMP '2026-07-05 10:01:00');

INSERT INTO WALLET_TRANSACTION (txn_id, wallet_id, biz_type, biz_ref_id, funds_direction, amount, avail_bal_before, avail_bal_after, idempotency_key, create_time)
VALUES ('WT_MARKET_SELL_001', 'W002', 'MARKET_SELL', 'TRD_CS2_001', 'CREDIT', 42.75, 200.00, 242.75, 'idem-wallet-market-sell-001', TIMESTAMP '2026-07-05 14:02:00');

INSERT INTO WALLET_TRANSACTION (txn_id, wallet_id, biz_type, biz_ref_id, funds_direction, amount, avail_bal_before, avail_bal_after, idempotency_key, create_time)
VALUES ('WT_MARKET_FREEZE_001', 'W002', 'MARKET_FREEZE', 'MO_CS2_BUY_2', 'FREEZE', 50.00, 292.75, 242.75, 'idem-wallet-market-freeze-001', TIMESTAMP '2026-07-05 14:10:00');

COMMIT;

PROMPT Steam course seed data inserted.
