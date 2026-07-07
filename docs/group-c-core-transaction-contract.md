# Group C 核心交易契约

本文档约束 Group C 的核心交易实现。所有后续后端、前端、测试和演示脚本都必须与本契约保持一致；如果实现发生变化，必须同步更新本文档和根目录 `README.md`。

## 固定样板游戏

Group C 只围绕两款最终样板游戏做主链路：

| 游戏 | 编码 | Group C 规则 |
|---|---|---|
| Counter-Strike 2 | `GAME_CS2` | 免费入库或 0 元订单，不扣钱包余额 |
| Don't Starve Together / 饥荒联机版 | `GAME_DST` | 买断制购买、钱包扣款、订单明细、支付流水、游戏库、退款、CDKey 主演示 |

不得把 `Team Fortress 2`、旧的 `Neon Drift`、`Archive Runner` 或临时虚构游戏重新作为 Group C 主链路。

## 马祥珲负责范围

马祥珲负责 Group C 的核心交易总设计、购买事务、统一接口和最终集成，具体包括：

- 统一钱包、订单、支付、游戏库、退款、CDKey 的业务状态机。
- 定义购买事务、免费入库事务、退款审核事务、CDKey 兑换事务的原子边界。
- 定义幂等键、金额校验、重复购买校验、重复兑换校验。
- 协调 Group B 的游戏详情购买入口和 Group D 的市场钱包协作。
- 最终检查 Group C 相关 PR 是否符合 README、数据库约束和演示脚本。

## 事务边界

### DST 买断制购买

接口：

```text
POST /api/orders
```

最小请求字段：

```json
{
  "gameId": "GAME_DST",
  "idempotencyKey": "client-generated-key"
}
```

必须在一个数据库事务中完成：

```text
读取 GAME_DST 并校验 ONLINE
  -> 校验玩家未拥有 GAME_DST 的 NORMAL 游戏库资产
  -> 计算 payable = base_price * discount_rate
  -> 锁定 WALLET_ACCOUNT
  -> 校验 available_balance >= payable
  -> 扣减 available_balance
  -> 新建 GAME_ORDER
  -> 新建 ORDER_DETAIL
  -> 新建 PAYMENT_TRANSACTION
  -> 新建 ORDER_STATUS_LOG: CREATED
  -> 新建 ORDER_STATUS_LOG: COMPLETED
  -> 新建 PLAYER_LIBRARY，acquire_way = BUY
  -> 新建 WALLET_TRANSACTION，biz_type = BUY_GAME，funds_direction = DEBIT
  -> 提交事务
```

失败时必须整体回滚，不能出现“扣了钱但未入库”或“入库但未扣钱”。

幂等要求：

- 同一 `idempotencyKey` 重复提交，应返回第一次成功订单。
- 同一玩家重复购买 `GAME_DST`，应返回业务错误 `GAME_ALREADY_OWNED`，不能再扣款。
- 钱包余额不足，返回 `INSUFFICIENT_BALANCE`，不能创建已完成订单。

### CS2 免费入库

接口：

```text
POST /api/games/{gameId}/free-claim
```

只允许 `GAME_CS2` 使用该主线。

必须满足：

- `GAME_CS2` 的 `base_price = 0`。
- 不能写 `WALLET_TRANSACTION`。
- 不能扣减 `WALLET_ACCOUNT.available_balance`。
- 可以生成 0 元 `GAME_ORDER` + `ORDER_DETAIL` + `PAYMENT_TRANSACTION`，也可以直接写 `PLAYER_LIBRARY`；如果选择直接入库，必须在接口文档中说明。
- 推荐实现采用 0 元订单，便于演示订单历史。
- `PLAYER_LIBRARY.acquire_way = FREE`。

重复领取时返回 `GAME_ALREADY_OWNED`，不能重复写游戏库。

### DST 退款审核

申请接口：

```text
POST /api/refunds
```

审核接口：

```text
POST /api/admin/refunds/{refundId}/approve
POST /api/admin/refunds/{refundId}/reject
```

退款申请要求：

- 只允许对已支付、已完成、未全额退款的订单申请。
- 首期主线优先支持 `GAME_DST`。
- 退款金额不得超过 `ORDER_DETAIL.payable_amount - ORDER_DETAIL.refund_amount`。
- 申请成功写 `REFUND_TICKET` 和 `REFUND_DETAIL`。

退款通过必须在一个事务中完成：

```text
锁定 REFUND_TICKET
  -> 校验状态 PENDING
  -> 锁定订单和钱包
  -> 增加 WALLET_ACCOUNT.available_balance
  -> 写 WALLET_TRANSACTION，biz_type = REFUND，funds_direction = CREDIT
  -> 更新 ORDER_DETAIL.refund_amount
  -> 更新 GAME_ORDER.payment_status
  -> 必要时更新 GAME_ORDER.order_status
  -> 更新 PLAYER_LIBRARY.status = REVOKED
  -> 写 REFUND_AUDIT_LOG
  -> 提交事务
```

退款拒绝必须写 `REFUND_AUDIT_LOG`，但不能改钱包余额。

### DST CDKey 兑换

接口：

```text
POST /api/cdkeys/redeem
```

必须满足：

- 首期主线只为 `GAME_DST` 准备 CDKey 演示。
- 提交的明文 CDKey 只在服务层短暂存在，入库和日志使用哈希。
- 成功和失败都写 `CDKEY_REDEEM_LOG`。
- 成功兑换时必须把 `CDKEY.status` 改为 `REDEEMED`。
- 成功兑换时写 `PLAYER_LIBRARY.acquire_way = REDEEM`。
- 同一玩家已拥有 `GAME_DST` 时，不能再次兑换成功。
- 已兑换、过期、撤销、无效 CDKey 都必须返回可解释的业务错误。

## 服务分层建议

后端五层结构中，Group C 建议按以下服务拆分：

```text
SteamPlatform.Api
  WalletController
  OrdersController
  LibraryController
  RefundsController
  CdkeysController

SteamPlatform.Application
  WalletAppService
  OrderAppService
  LibraryAppService
  RefundAppService
  CdkeyAppService

SteamPlatform.Domain
  WalletAccount
  GameOrder
  PlayerLibraryAsset
  RefundTicket
  CdkeyBatch
  CdkeyRedeemLog

SteamPlatform.Infrastructure
  WalletRepository
  OrderRepository
  LibraryRepository
  RefundRepository
  CdkeyRepository
  UnitOfWork / OracleTransaction
```

Controller 只做参数接收、鉴权和响应包装；业务规则放在 Application / Domain；SQL 和 Oracle 事务细节放在 Infrastructure。

## 统一错误码

Group C 至少预留以下业务错误码：

```text
GAME_NOT_FOUND
GAME_OFFLINE
GAME_NOT_FREE
GAME_ALREADY_OWNED
WALLET_NOT_FOUND
INSUFFICIENT_BALANCE
ORDER_NOT_FOUND
ORDER_NOT_REFUNDABLE
REFUND_NOT_FOUND
REFUND_STATUS_INVALID
CDKEY_INVALID
CDKEY_EXPIRED
CDKEY_REDEEMED
CDKEY_BATCH_REVOKED
IDEMPOTENCY_CONFLICT
```

错误响应格式必须服从项目统一响应格式，不能每个 Controller 自己发明格式。

## 前端页面接入点

Group C 的前端页面和组件需要暴露以下稳定能力：

- `/account/wallet`：余额、冻结余额、充值模拟、流水列表。
- `/orders`：订单列表。
- `/orders/:id`：订单详情、订单明细、支付状态、退款入口。
- `/library`：玩家游戏库，展示 `GAME_CS2` 和 `GAME_DST` 的拥有状态。
- `/refunds/new`：对 `GAME_DST` 订单发起退款。
- `/admin/refunds`：管理员审核退款。
- `/developer/cdkey-batches`：为 `GAME_DST` 创建 CDKey 批次。
- `/redeem`：玩家兑换 `GAME_DST` CDKey。
- `/games/:id` 购买区域：Group B 页面调用 Group C 接口。

## 与其他组边界

- Group B 负责游戏详情展示和购买按钮，不负责扣款、订单和入库。
- Group D 负责 `CS2` 饰品库存与市场交易，不负责玩家是否拥有 `CS2` 游戏本体。
- Group D 市场交易如果需要钱包冻结或清算，必须复用 `WALLET_ACCOUNT` 和 `WALLET_TRANSACTION`，不能新增余额字段。
- Group A 提供认证用户身份，Group C 不自行发明登录态。

## 当前数据库基线

当前 `database/data.sql` 已经固定以下 Group C 演示数据：

- `GAME_CS2`：免费游戏。
- `GAME_DST`：买断制折扣游戏。
- `O_DST_001`：P001 购买 DST 的已完成付费订单。
- `O_CS2_FREE_001`：P001 免费入库 CS2 的 0 元订单。
- `WT_DST_BUY_001`：DST 购买扣款流水。
- `LIB_DST_P001`：P001 通过购买拥有 DST。
- `LIB_CS2_P001`：P001 通过免费领取拥有 CS2。
- `B_DST_001`：DST CDKey 批次。
- `CRL_DST_001`：P002 成功兑换 DST 的日志。

这些编码是演示脚本和测试的稳定锚点，后续 PR 不要随意改名。
