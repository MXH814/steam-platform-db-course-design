SET DEFINE OFF

PROMPT Inserting Steam course seed data...

INSERT INTO PLAYER (user_id, account, password_hash, nickname, credit_score, status, version, create_time, update_time)
VALUES ('P001', 'alice', 'PBKDF2$SHA256$100000$c2VlZC1hbGljZV9fX19fXw==$iTPCU6/lngHZz3zx/gYotoK0h7N0WJu8m0Vnre7/1NA=', 'Alice', 100, 'NORMAL', 0, TIMESTAMP '2026-07-05 09:00:00', TIMESTAMP '2026-07-05 09:00:00');

INSERT INTO PLAYER (user_id, account, password_hash, nickname, credit_score, status, version, create_time, update_time)
VALUES ('P002', 'bob', 'PBKDF2$SHA256$100000$c2VlZC1ib2JfX19fX19fXw==$2CvTcEyGV8IfmgB6hEZN+em2lyvIsaRLrQJ/5YgkipM=', 'Bob', 96, 'NORMAL', 0, TIMESTAMP '2026-07-05 09:05:00', TIMESTAMP '2026-07-05 09:05:00');

INSERT INTO DEVELOPER (dev_id, company_name, tax_id, contact_email, status, join_time)
VALUES ('DEV001', 'Night City Studio', 'TAX-DEMO-001', 'dev@example.com', 'APPROVED', TIMESTAMP '2026-07-05 09:10:00');

INSERT INTO ADMIN_USER (admin_id, account, password_hash, role, create_time)
VALUES ('ADM001', 'rootadmin', 'PBKDF2$SHA256$100000$c2VlZC1yb290YWRtaW5fXw==$yHE6M2jmsTpAplUmz5Vjp4o3zmV30sSQwdnx0jMVHpo=', 'SUPER_ADMIN', TIMESTAMP '2026-07-05 09:15:00');

INSERT INTO WALLET_ACCOUNT (wallet_id, user_id, available_balance, frozen_balance, version)
VALUES ('W001', 'P001', 355.00, 0.00, 1);

INSERT INTO WALLET_ACCOUNT (wallet_id, user_id, available_balance, frozen_balance, version)
VALUES ('W002', 'P002', 345.00, 50.00, 1);

INSERT INTO SYS_NOTICE (notice_id, publisher_type, publisher_id, title, content, priority, status, publish_time, expire_time)
VALUES ('N001', 'SYSTEM', NULL, 'Summer Sale Live', 'The platform summer sale is live with selected limited-time discounts.', 1, 'PUBLISHED', TIMESTAMP '2026-07-05 10:00:00', TIMESTAMP '2026-08-05 10:00:00');

INSERT INTO GAME (game_id, dev_id, game_name, base_price, discount_rate, release_date, reputation, status)
VALUES ('G001', 'DEV001', 'Neon Drift', 98.00, 0.70, DATE '2026-06-01', 'VERY_POSITIVE', 'ONLINE');

INSERT INTO GAME (game_id, dev_id, game_name, base_price, discount_rate, release_date, reputation, status)
VALUES ('G002', 'DEV001', 'Archive Runner', 128.00, 1.00, DATE '2026-07-01', 'MOSTLY_POSITIVE', 'ONLINE');

INSERT INTO GAME_ORDER (order_id, user_id, total_amount, order_type, order_status, payment_status, idempotency_key, expire_time, create_time)
VALUES ('O001', 'P001', 68.60, 'BUY_GAME', 'COMPLETED', 'PAID', 'idem-order-001', TIMESTAMP '2026-07-05 10:30:00', TIMESTAMP '2026-07-05 10:00:00');

INSERT INTO ORDER_DETAIL (detail_id, order_id, game_id, original_price, discount_amount, payable_amount, refund_amount)
VALUES ('OD001', 'O001', 'G001', 98.00, 29.40, 68.60, 0.00);

INSERT INTO ORDER_STATUS_LOG (log_id, order_id, from_status, to_status, create_time)
VALUES ('OSL001', 'O001', NULL, 'CREATED', TIMESTAMP '2026-07-05 10:00:00');

INSERT INTO ORDER_STATUS_LOG (log_id, order_id, from_status, to_status, create_time)
VALUES ('OSL002', 'O001', 'CREATED', 'COMPLETED', TIMESTAMP '2026-07-05 10:01:00');

INSERT INTO PAYMENT_TRANSACTION (payment_id, order_id, provider_trade_no, amount, status, create_time)
VALUES ('PAY001', 'O001', 'GW-DEMO-001', 68.60, 'SUCCESS', TIMESTAMP '2026-07-05 10:01:00');

INSERT INTO REFUND_TICKET (refund_id, order_id, refund_amount, refund_type, reason, play_time_hours, status, apply_time)
VALUES ('R001', 'O001', 20.00, 'PARTIAL', 'Demo partial refund request', 1.25, 'REJECTED', TIMESTAMP '2026-07-05 11:00:00');

INSERT INTO REFUND_DETAIL (refund_detail_id, refund_id, order_detail_id, refund_amount)
VALUES ('RD001', 'R001', 'OD001', 20.00);

INSERT INTO REFUND_AUDIT_LOG (audit_id, refund_id, operator_id, from_status, to_status, reason, create_time)
VALUES ('RAL001', 'R001', 'ADM001', 'PENDING', 'REJECTED', 'Demo data: play time and reason require further review', TIMESTAMP '2026-07-05 11:05:00');

INSERT INTO PLAYER_LIBRARY (lib_id, user_id, game_id, acquire_way, status, play_minutes, last_play_time)
VALUES ('LIB001', 'P001', 'G001', 'BUY', 'NORMAL', 80, TIMESTAMP '2026-07-05 12:00:00');

INSERT INTO PLAYER_LIBRARY (lib_id, user_id, game_id, acquire_way, status, play_minutes, last_play_time)
VALUES ('LIB002', 'P002', 'G001', 'REDEEM', 'NORMAL', 10, TIMESTAMP '2026-07-05 12:10:00');

INSERT INTO CDKEY_BATCH (batch_id, game_id, batch_no, valid_from, expire_time)
VALUES ('B001', 'G001', 'BATCH-NEON-0001', TIMESTAMP '2026-07-01 00:00:00', TIMESTAMP '2026-12-31 23:59:59');

INSERT INTO CDKEY (cdkey_hash, batch_id, status, generate_time)
VALUES ('0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF', 'B001', 'REDEEMED', TIMESTAMP '2026-07-05 09:30:00');

INSERT INTO CDKEY_REDEEM_LOG (log_id, user_id, submitted_hash, cdkey_hash, result, fail_reason, ip_hash, create_time)
VALUES ('CRL001', 'P002', '0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF', '0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF', 'SUCCESS', NULL, 'iphash-demo-001', TIMESTAMP '2026-07-05 10:20:00');

INSERT INTO GAME_REVIEW (review_id, user_id, game_id, thumbs_up, status, create_time)
VALUES ('REV001', 'P001', 'G001', 12, 'VISIBLE', TIMESTAMP '2026-07-05 13:00:00');

INSERT INTO REVIEW_VERSION (version_id, review_id, version_no, is_recommend, content, create_time)
VALUES ('RV001', 'REV001', 1, 1, 'Great neon art direction and satisfying drift handling. Worth buying during sale.', TIMESTAMP '2026-07-05 13:00:00');

INSERT INTO ACHIEVEMENT (ach_id, game_id, ach_name, description, global_rate)
VALUES ('ACH001', 'G001', 'First Drift', 'Complete the first drift.', 42.50);

INSERT INTO PLAYER_ACHIEVEMENT (unlock_id, user_id, ach_id, unlock_time)
VALUES ('PA001', 'P001', 'ACH001', TIMESTAMP '2026-07-05 12:30:00');

INSERT INTO ITEM_TEMPLATE (template_id, game_id, item_name, rarity, image_url)
VALUES ('ITPL001', 'G001', 'Neon Card', 'RARE', '/assets/items/neon-card.png');

INSERT INTO ITEM_TEMPLATE (template_id, game_id, item_name, rarity, image_url)
VALUES ('ITPL002', 'G001', 'Chrome Badge', 'EPIC', '/assets/items/chrome-badge.png');

INSERT INTO INVENTORY_ITEM (item_id, template_id, user_id, wear_rating, status, acquire_time, version)
VALUES ('ITEM001', 'ITPL001', 'P001', 0.1250, 'IN_MARKET', TIMESTAMP '2026-07-05 12:40:00', 1);

INSERT INTO INVENTORY_ITEM (item_id, template_id, user_id, wear_rating, status, acquire_time, version)
VALUES ('ITEM002', 'ITPL001', 'P001', 0.3320, 'NORMAL', TIMESTAMP '2026-07-05 12:45:00', 1);

INSERT INTO MARKET_ORDER (market_order_id, user_id, template_id, item_id, order_type, target_price, frozen_amount, status, create_time)
VALUES ('MO_BUY_1', 'P001', 'ITPL001', NULL, 'BUY', 45.00, 0.00, 'TRADED', TIMESTAMP '2026-07-05 14:00:00');

INSERT INTO MARKET_ORDER (market_order_id, user_id, template_id, item_id, order_type, target_price, frozen_amount, status, create_time)
VALUES ('MO_SELL_1', 'P002', 'ITPL001', 'ITEM002', 'SELL', 45.00, 0.00, 'TRADED', TIMESTAMP '2026-07-05 14:01:00');

INSERT INTO MARKET_ORDER (market_order_id, user_id, template_id, item_id, order_type, target_price, frozen_amount, status, create_time)
VALUES ('MO_BUY_2', 'P002', 'ITPL001', NULL, 'BUY', 50.00, 50.00, 'MATCHING', TIMESTAMP '2026-07-05 14:10:00');

INSERT INTO MARKET_ORDER (market_order_id, user_id, template_id, item_id, order_type, target_price, frozen_amount, status, create_time)
VALUES ('MO_SELL_2', 'P001', 'ITPL001', 'ITEM001', 'SELL', 50.00, 0.00, 'MATCHING', TIMESTAMP '2026-07-05 14:11:00');

INSERT INTO MARKET_TRADE (trade_id, buy_order_id, sell_order_id, template_id, item_id, buyer_id, seller_id, trade_price, platform_fee, currency, trade_time)
VALUES ('TRD001', 'MO_BUY_1', 'MO_SELL_1', 'ITPL001', 'ITEM002', 'P001', 'P002', 45.00, 2.25, 'CNY', TIMESTAMP '2026-07-05 14:02:00');

INSERT INTO ITEM_TRANSFER_LEDGER (transfer_id, item_id, from_user_id, to_user_id, transfer_type, transfer_time)
VALUES ('ITL001', 'ITEM001', NULL, 'P001', 'DROP', TIMESTAMP '2026-07-05 12:40:00');

INSERT INTO ITEM_TRANSFER_LEDGER (transfer_id, item_id, from_user_id, to_user_id, transfer_type, transfer_time)
VALUES ('ITL002', 'ITEM002', NULL, 'P002', 'DROP', TIMESTAMP '2026-07-05 12:45:00');

INSERT INTO ITEM_TRANSFER_LEDGER (transfer_id, item_id, from_user_id, to_user_id, transfer_type, transfer_time)
VALUES ('ITL003', 'ITEM002', 'P002', 'P001', 'TRADE', TIMESTAMP '2026-07-05 14:02:00');

INSERT INTO WALLET_TRANSACTION (txn_id, wallet_id, biz_type, biz_ref_id, funds_direction, amount, avail_bal_before, avail_bal_after, idempotency_key, create_time)
VALUES ('WT001', 'W001', 'BUY_GAME', 'O001', 'DEBIT', 68.60, 423.60, 355.00, 'idem-wallet-001', TIMESTAMP '2026-07-05 10:01:00');

INSERT INTO WALLET_TRANSACTION (txn_id, wallet_id, biz_type, biz_ref_id, funds_direction, amount, avail_bal_before, avail_bal_after, idempotency_key, create_time)
VALUES ('WT002', 'W002', 'MARKET_FREEZE', 'MO_BUY_2', 'FREEZE', 50.00, 395.00, 345.00, 'idem-wallet-002', TIMESTAMP '2026-07-05 14:10:00');

INSERT INTO WALLET_TRANSACTION (txn_id, wallet_id, biz_type, biz_ref_id, funds_direction, amount, avail_bal_before, avail_bal_after, idempotency_key, create_time)
VALUES ('WT003', 'W002', 'MARKET_SELL', 'TRD001', 'CREDIT', 42.75, 302.25, 345.00, 'idem-wallet-003', TIMESTAMP '2026-07-05 14:02:00');

COMMIT;

PROMPT Steam course seed data inserted.
