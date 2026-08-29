# 数据库课程设计 20 分钟多角色答辩演示手册

本文件是正式答辩、集体彩排和备用录屏的唯一演示口径。流程使用两名现场注册玩家、两家固定开发商和一名管理员，覆盖内容提交、审核上架、两种游戏授权、钱包订单、社区互动、饰品交易、退款审计和 Oracle 证据。

正式演示前后必须执行演示数据恢复。不得临场更换账号、价格、步骤或样板游戏；流程变化后必须同步更新本文件和 README。

## 1. 演示目标

20 分钟内证明以下内容：

1. 系统采用 B/S 架构，Vue 前端、Nginx、ASP.NET Core .NET 10 五层后端和腾讯云 Oracle 真实联动。
2. `PLAYER`、`DEVELOPER`、`ADMIN` 三类主体具有明确且不可越权的职责。
3. Klei 与 Valve 两家开发商的数据彼此隔离；新游戏必须经过管理员上架后才能进入公开商店。
4. 两名玩家从现场注册开始，分别通过钱包购买和 CDKey 兑换获得 DST，通过免费领取获得 CS2。
5. 钱包、订单、退款和市场交易均由 Oracle 事务维护，并写入不可覆盖的资金或资产账本。
6. 好友、聊天、评测、成就和工坊订阅由 Oracle 持久化，SignalR 只负责实时推送。
7. 数据库结构、约束、索引、执行计划、行锁和跨表一致性均有可重复验证的证据。

## 2. 固定业务口径

- 固定样板游戏仍然只有 `GAME_CS2` 和 `GAME_DST`。
- 临时创建的 `Survival Lab` 与 `Tactical Arena Lab` 只用于演示开发商提交和管理员审核，答辩结束后由恢复工具清除，不构成第三、第四款样板游戏。
- CS2 负责免费入库、饰品掉落、库存实例、市场订单、成交和物品流转。
- DST 负责付费购买、CDKey、评测、成就、工坊和退款。
- 钱包唯一真相是 `WALLET_ACCOUNT.available_balance` 与 `frozen_balance`，不得引用或恢复 `PLAYER.wallet_balance`。
- 开发商和管理员不能公开注册。只有玩家可以现场注册；开发商和管理员账号由平台预先审核和维护。

## 3. 七人现场操作与十人知识责任

### 3.1 七人现场操作与电脑准备

正式演示使用 7 人 A-G，每人一台电脑。其余组员不安排重复操作，只在老师追问各自负责模块时补充回答。

| 人员 | 电脑与账号 | 主要职责 | 切屏时段 |
|---|---|---|---|
| A | 主讲电脑 | 开场、架构、时间控制、切屏口令、安全与总结 | 0:00、17:20、18:40 |
| B | Klei 开发商电脑 | `klei@example.com`，创建临时游戏和 DST CDKey | 0:50 |
| C | Valve 开发商电脑 | `valve@example.com`，证明开发商隔离并提交第二款临时游戏 | 2:05 |
| D | 管理员电脑 | `rootadmin`，选择性上架和退款审核 | 2:55、13:20 |
| E | 玩家甲电脑 | 注册 `defense_p1`，购买 DST、评测、成就、出售 CS2 饰品、退款 | 3:45 起 |
| F | 玩家乙电脑 | 注册 `defense_p2`，兑换 CDKey、好友聊天、工坊订阅、购买饰品 | 3:45 起 |
| G | 数据库与运维电脑 | 恢复基线、健康检查、Oracle 证据、备用录屏和故障处理 | 答辩前、14:40 |

A 负责唯一口头主线，B-G 只在被切到自己电脑时用一至两句话说明操作结果，避免多人重复解释。A 使用固定口令切屏，例如“下面请看 Klei 开发商电脑”“回到玩家甲”“切到数据库证据”。

推荐使用会议软件的屏幕共享交接或现场 HDMI 切换器。正式彩排必须采用与答辩当天相同的切屏方式。A 的电脑同时保留所有角色的备用浏览器配置和备用录屏，任一成员电脑异常时由 A 接管。

### 3.2 十人知识责任总表

现场操作仍由 A-G 七人完成，不增加角色切换。H-J 不接管玩家、开发商或管理员账号，负责专项技术问答、代码定位和故障补位。十个人都必须掌握自己模块从“前端页面 → API 端点 → Application 契约/服务 → Infrastructure/Oracle → 数据表 → 测试”的完整链路。

| 人员 | 现场身份 | 第一知识责任 | 老师提问时负责回答 |
|---|---|---|---|
| A | 总主讲，不固定业务账号 | 总体架构、五层结构、认证授权、公共约定 | B/S 选择、.NET 10、五层依赖、JWT、异常处理、全链路集成 |
| B | Klei 开发商 | 游戏管理、开发商隔离、CDKey 生成 | 开发商为什么不能越权、游戏为什么默认下架、CDKey 为什么只显示一次 |
| C | Valve 开发商 | 商店前端、游戏详情、媒体与 Steam 风格交互 | Vue 页面结构、商店数据来源、响应式布局、图片视频兜底和界面一致性 |
| D | 管理员 | 游戏审核、公告、退款审核 | 管理员权限、审核状态变化、退款审计、为什么不能由开发商自行上架 |
| E | 玩家甲 | 钱包、充值、购买、订单、退款申请 | 钱包唯一真相、购买事务、幂等、资金流水、退款资格与金额一致性 |
| F | 玩家乙 | CDKey 兑换、游戏库、好友聊天、实时通知 | 三种入库方式、重复兑换、好友关系、Oracle 持久化与 SignalR 的边界 |
| G | 数据库与运维电脑 | Oracle 总体设计、约束、索引、执行计划、锁 | 表关系、范式、主外键、索引理由、事务隔离、并发与恢复验证 |
| H | 专项问答 | 评价、版本、成就、个人资料、社区与讨论区 | 社区数据模型、评价留痕、成就口径、内容互动和数据持久化 |
| I | 专项问答 | 饰品库存、市场订单、撮合、交易报价与资产账本 | 模板和实例区别、挂单撮合、冻结资金、手续费、所有权转移和防重复出售 |
| J | 专项问答与故障补位 | 自动化测试、CI、演示恢复、HTTPS 和腾讯云部署 | 如何证明可运行、如何恢复基线、CI 检查、云端拓扑、端口和故障预案 |

“第一知识责任”表示该成员必须能独立回答，不表示其他成员不需要理解。涉及跨模块问题时，第一责任人先回答业务规则，G 补充数据库约束和 SQL 证据，J 补充测试与云端验证，A 最后统一结论。

### 3.3 A-J 具体文件与掌握要求

#### A：总体架构、五层结构与认证授权

第一责任文件：

- `README.md`
- `PRODUCT.md`
- `2026《数据库课程设计》课程提纲.doc`
- `backend/README.md`
- `frontend/README.md`
- `backend/SteamPlatform.sln`
- `backend/src/SteamPlatform.Api/Program.cs`
- `backend/src/SteamPlatform.Api/SteamPlatform.Api.csproj`
- `backend/src/SteamPlatform.Application/SteamPlatform.Application.csproj`
- `backend/src/SteamPlatform.Domain/SteamPlatform.Domain.csproj`
- `backend/src/SteamPlatform.Infrastructure/SteamPlatform.Infrastructure.csproj`
- `backend/src/SteamPlatform.Shared/SteamPlatform.Shared.csproj`
- `backend/src/SteamPlatform.Infrastructure/DependencyInjection.cs`
- `backend/src/SteamPlatform.Api/Features/Auth/AuthEndpointExtensions.cs`
- `backend/src/SteamPlatform.Api/Features/Auth/EndpointGuards.cs`
- `backend/src/SteamPlatform.Application/Auth/`
- `backend/src/SteamPlatform.Infrastructure/Auth/`
- `backend/src/SteamPlatform.Shared/`
- `frontend/src/main.ts`
- `frontend/src/router.ts`
- `frontend/src/stores/auth.ts`
- `frontend/src/api/http.ts`
- `frontend/src/api/types.ts`
- `frontend/src/env.d.ts`
- `frontend/src/utils/format.ts`
- `frontend/src/views/LoginView.vue`
- `frontend/src/views/RegisterView.vue`

A 必须完全讲明白：

1. 浏览器、Vue、Nginx、ASP.NET Core、Oracle 之间一次请求如何流动。
2. Api、Application、Domain、Infrastructure、Shared 五层分别负责什么，项目引用为什么不能反向。
3. 玩家允许注册而开发商、管理员只能使用预置身份的原因。
4. JWT 的签发、角色声明、有效期验证、路由守卫和后端最终鉴权之间的关系。
5. 统一响应、业务异常、禁止访问和资源不存在如何映射为 HTTP 结果。
6. 为什么选择 B/S、.NET 10、Vue 和 Oracle，而不是 C/S、纯前端或把 SQL 直接写在页面中。

#### B：开发商游戏管理与 CDKey 生成

第一责任文件：

- `backend/src/SteamPlatform.Api/Features/Games/GameEndpointExtensions.cs`
- `backend/src/SteamPlatform.Application/Games/GameContracts.cs`
- `backend/src/SteamPlatform.Application/Games/GameService.cs`
- `backend/src/SteamPlatform.Infrastructure/Games/GameRepository.cs`
- `frontend/src/views/DeveloperGamesView.vue`
- `frontend/src/views/CdkeyBatchView.vue`
- `frontend/src/api/games.ts`
- `backend/tests/SteamPlatform.Api.Tests/GameServiceTests.cs`
- `backend/tests/SteamPlatform.Api.Tests/GameRepositoryGuardTests.cs`

共享文件中的责任范围：

- `backend/src/SteamPlatform.Api/Features/CoreTransactions/CoreTransactionEndpointExtensions.cs` 中开发商创建 CDKey 批次的端点。
- `backend/src/SteamPlatform.Infrastructure/CoreTransactions/CoreTransactionService.cs` 中 `CreateCdkeyBatchAsync`。
- `backend/src/SteamPlatform.Application/CoreTransactions/CoreTransactionContracts.cs` 中 CDKey 批次请求与响应契约。
- `database/schema.sql` 中 `DEVELOPER`、`GAME`、`CDKEY_BATCH`、`CDKEY`。

B 必须完全讲明白：开发商身份如何映射 `dev_id`、查询和修改为什么都带所有权条件、创建游戏为何固定为 `OFFLINE`、管理员如何使其上线、CDKey 明文为什么只返回一次、Oracle 为什么只保存可校验值而不长期暴露明文。

#### C：公开商店、游戏详情与 Steam 风格前端

第一责任文件：

- `frontend/src/App.vue`
- `frontend/src/styles.css`
- `frontend/src/views/StoreView.vue`
- `frontend/src/views/HomeView.vue`
- `frontend/src/views/StoreCollectionView.vue`
- `frontend/src/views/GameDetailView.vue`
- `frontend/src/views/GameStoreView.vue`
- `frontend/src/views/NotFoundView.vue`
- `frontend/src/components/GameCard.vue`
- `frontend/src/components/GameFilterBar.vue`
- `frontend/src/components/GameHeroPanel.vue`
- `frontend/src/components/GamePriceBlock.vue`
- `frontend/src/components/GameSummarySection.vue`
- `frontend/src/components/SteamGameDetailTemplate.vue`
- `frontend/src/components/SteamMediaGallery.vue`
- `frontend/src/components/Cs2DetailSections.vue`
- `frontend/src/components/GenericGameDetailSections.vue`
- `frontend/src/data/gameCatalog.ts`
- `frontend/public/assets/games/`
- `frontend/public/assets/media/`
- `frontend/e2e/public-store.spec.ts`

C 必须完全讲明白：公开商店如何只展示在线游戏、列表与详情如何调用真实 API、CS2/DST 固定展示口径、Vue 组件为何拆分、视频海报与截图画廊如何降级、桌面和移动端如何避免溢出，以及界面仿 Steam 但不把官方 Logo 当作项目自身标识的处理方式。

#### D：管理员审核、公告与退款审批

第一责任文件：

- `frontend/src/views/AdminGamesView.vue`
- `frontend/src/views/AdminNoticesView.vue`
- `frontend/src/views/AdminRefundsView.vue`
- `backend/src/SteamPlatform.Api/Features/Notices/NoticeEndpointExtensions.cs`
- `backend/src/SteamPlatform.Application/Notices/NoticeContracts.cs`
- `backend/src/SteamPlatform.Infrastructure/Notices/NoticeRepository.cs`
- `backend/src/SteamPlatform.Domain/Notices/SysNotice.cs`
- `backend/tests/SteamPlatform.Api.Tests/NoticeEndpointTests.cs`
- `backend/tests/SteamPlatform.Api.Tests/NoticeRepositoryGuardTests.cs`

共享文件中的责任范围：

- `backend/src/SteamPlatform.Api/Features/Games/GameEndpointExtensions.cs`、`backend/src/SteamPlatform.Application/Games/GameService.cs` 和 `backend/src/SteamPlatform.Infrastructure/Games/GameRepository.cs` 中管理员上线/下线游戏的端点与 `SetStatusAsync`。
- `backend/src/SteamPlatform.Api/Features/CoreTransactions/CoreTransactionEndpointExtensions.cs` 中管理员退款列表、批准和拒绝端点。
- `backend/src/SteamPlatform.Infrastructure/CoreTransactions/CoreTransactionService.cs` 中 `ListAllRefundsAsync`、`ApproveRefundAsync`、`RejectRefundAsync`。
- `database/schema.sql` 中 `ADMIN_USER`、`SYS_NOTICE`、`REFUND_TICKET`、`REFUND_DETAIL`、`REFUND_AUDIT_LOG`。

D 必须完全讲明白：角色守卫为何必须在后端再次检查、审核状态如何影响公开商店、公告的发布时间与失效时间、退款批准如何回补钱包和撤销授权、审核人和审核意见如何留痕，以及重复审批为什么不会重复退款。

#### E：钱包、充值、购买、订单与退款申请

第一责任文件：

- `docs/group-c-core-transaction-contract.md`
- `docs/c2-wallet-module-readme.md`
- `frontend/src/views/WalletView.vue`
- `frontend/src/views/WalletRechargeCheckoutView.vue`
- `frontend/src/views/WalletHistoryView.vue`
- `frontend/src/views/WalletHistoryDetailView.vue`
- `frontend/src/views/GameCheckoutView.vue`
- `frontend/src/views/OrderDetailView.vue`
- `frontend/src/views/OrdersView.vue`
- `frontend/src/views/RefundsView.vue`
- `frontend/src/views/WalletRefundPlaceholderView.vue`
- `frontend/src/api/coreApi.ts` 中钱包、充值、购买、订单和退款申请函数。
- `backend/tests/SteamPlatform.Api.Tests/CoreTransactionEndpointTests.cs`
- `backend/tests/SteamPlatform.Api.Tests/CoreTransactionServiceGuardTests.cs`

共享文件中的责任范围：

- `backend/src/SteamPlatform.Api/Features/CoreTransactions/CoreTransactionEndpointExtensions.cs` 中钱包、充值、订单和玩家退款端点。
- `backend/src/SteamPlatform.Application/CoreTransactions/CoreTransactionContracts.cs` 中钱包、订单和退款契约。
- `backend/src/SteamPlatform.Infrastructure/CoreTransactions/CoreTransactionService.cs` 中 `GetWalletAsync`、`RechargeWalletAsync`、`ListWalletTransactionsAsync`、`ListWalletHistoryAsync`、`GetWalletHistoryEntryAsync`、`BuyGameAsync`、`ListOrdersAsync`、`GetOrderAsync`、`CreateRefundAsync`。
- `database/schema.sql` 中 `WALLET_ACCOUNT`、`GAME_ORDER`、`ORDER_DETAIL`、`ORDER_STATUS_LOG`、`PAYMENT_TRANSACTION`、`WALLET_TRANSACTION` 和退款相关表。

E 必须完全讲明白：钱包唯一真相为什么只有 available/frozen 两个余额、金额为什么使用定点小数、购买过程中如何锁定账户并在同一事务写订单/明细/支付/流水/游戏库、幂等键解决什么问题、任一步失败为什么必须整体回滚、退款金额和游玩资格如何校验。

#### F：CDKey 兑换、游戏库、好友聊天与实时通知

第一责任文件：

- `frontend/src/views/RedeemView.vue`
- `frontend/src/views/LibraryView.vue`
- `frontend/src/views/GameLibraryView.vue`
- `frontend/src/views/AccountView.vue`
- `frontend/src/components/LibraryRail.vue`
- `frontend/src/api/socialApi.ts`
- `frontend/src/api/socialRealtime.ts`
- `backend/src/SteamPlatform.Api/Features/Social/SocialEndpointExtensions.cs`
- `backend/src/SteamPlatform.Api/Realtime/SocialHub.cs`
- `backend/src/SteamPlatform.Api/Realtime/SignalRSocialNotifier.cs`
- `backend/src/SteamPlatform.Application/Social/SocialContracts.cs`
- `backend/src/SteamPlatform.Application/Social/SocialService.cs`
- `backend/src/SteamPlatform.Domain/Social/`
- `backend/src/SteamPlatform.Infrastructure/Social/SocialRepository.cs`
- `backend/tests/SteamPlatform.Api.Tests/SocialEndpointTests.cs`
- `backend/tests/SteamPlatform.Api.Tests/SocialServiceTests.cs`
- `backend/tests/SteamPlatform.Api.Tests/SocialRepositoryGuardTests.cs`

共享文件中的责任范围：

- `backend/src/SteamPlatform.Api/Features/CoreTransactions/CoreTransactionEndpointExtensions.cs` 中免费入库、游戏库、游玩时长和 CDKey 兑换端点。
- `backend/src/SteamPlatform.Infrastructure/CoreTransactions/CoreTransactionService.cs` 中 `ClaimFreeGameAsync`、`ListLibraryAsync`、`AddPlaytimeAsync`、`RedeemCdkeyAsync`。
- `database/schema.sql` 中 `PLAYER_LIBRARY`、`CDKEY_REDEEM_LOG`、`FRIEND_RELATION`、`DIRECT_MESSAGE`、`WORKSHOP_ITEM`、`WORKSHOP_SUBSCRIPTION`、`USER_NOTIFICATION`。

F 必须完全讲明白：购买、免费领取和 CDKey 兑换三种 `acquire_way` 的差异、同一 CDKey 为什么只能成功一次、失败尝试如何留痕、好友关系为什么规范化保存一对用户、陌生人为什么不能发私信、消息为什么先写 Oracle 再由 SignalR 推送、断线重连后为什么仍能查询历史消息，以及工坊订阅和用户通知如何持久化。

#### G：Oracle 总体设计、约束、索引、执行计划与并发

第一责任文件：

- `database/schema.sql`
- `database/data.sql`
- `E-R图（改）.drawio`
- `“Steam-”数字游戏平台系统（改）.pdma`
- `项目文档/“Steam-”数字游戏平台系统数据库设计文档.docx`
- `database/migrations/`
- `database/verify_phase1.sql`
- `database/verify_defense.sql`
- `database/defense/explain_plans.sql`
- `database/defense/lock_session_a.sql`
- `database/defense/lock_session_b.sql`
- `docs/database-defense-runbook.md`
- `tests/SteamPlatform.Database.Tests/SchemaContractTests.cs`
- `tests/SteamPlatform.Database.Tests/SeedDataTests.cs`
- `tests/SteamPlatform.Database.Tests/VerifyScriptTests.cs`
- `tests/SteamPlatform.Database.Tests/OracleSmokeTests.cs`
- `tests/SteamPlatform.Database.Tests/DefenseScriptContractTests.cs`

G 必须完全讲明白：核心实体及联系、主键/外键/唯一/检查约束、为什么不存在 `PLAYER.wallet_balance`、45 张表如何按业务域组织、哪些查询需要组合索引、执行计划怎么看、行锁如何防止余额超扣和物品重复出售、账本为什么只追加、迁移如何保持幂等，以及验证脚本如何证明跨表一致性。

每位业务成员仍必须掌握自己模块涉及的表；G 负责全局 DDL、规范化、索引和跨模块一致性，不代替模块成员回答业务规则。

#### H：评价、成就、资料与社区内容

第一责任文件：

- `docs/group-d-community-achievement-contract.md`
- `frontend/src/views/GameCommunityView.vue`
- `frontend/src/views/CommunityHubView.vue`
- `frontend/src/views/ProfileView.vue`
- `frontend/src/api/communityApi.ts`
- `frontend/src/api/engagementApi.ts` 中资料、动态、讨论区相关函数。
- `frontend/src/data/achievementCatalog.ts`
- `backend/src/SteamPlatform.Api/Features/Community/CommunityEndpointExtensions.cs`
- `backend/src/SteamPlatform.Api/Features/Engagement/EngagementEndpointExtensions.cs` 中资料、动态和讨论区端点。
- `backend/src/SteamPlatform.Application/Community/`
- `backend/src/SteamPlatform.Domain/Community/`
- `backend/src/SteamPlatform.Domain/Engagement/EngagementModels.cs`
- `backend/src/SteamPlatform.Application/Engagement/EngagementContracts.cs` 中资料、动态和讨论区契约。
- `backend/src/SteamPlatform.Application/Engagement/EngagementService.cs`
- `backend/src/SteamPlatform.Infrastructure/Community/`
- `backend/src/SteamPlatform.Infrastructure/Engagement/EngagementRepository.cs` 中资料、动态和讨论区方法。
- `backend/tests/SteamPlatform.Api.Tests/CommunityEndpointTests.cs`
- `backend/tests/SteamPlatform.Api.Tests/CommunityRepositoryGuardTests.cs`
- `backend/tests/SteamPlatform.Api.Tests/EngagementServiceTests.cs`

H 必须完全讲明白：评价为什么要求拥有游戏、修改评价为什么保留 `REVIEW_VERSION`、管理员隐藏和物理删除的区别、DST 成就为什么明确是课程演示口径、解锁如何防重复，以及个人资料、社区动态、回应和讨论回复之间的关系。

#### I：饰品库存、市场撮合、交易报价与资产账本

第一责任文件：

- `frontend/src/views/InventoryView.vue`
- `frontend/src/views/MarketView.vue`
- `frontend/src/views/TradeOffersView.vue`
- `frontend/src/api/inventoryApi.ts`
- `frontend/src/api/marketApi.ts`
- `frontend/src/api/engagementApi.ts` 中交易报价相关函数。
- `backend/src/SteamPlatform.Api/Features/Inventory/InventoryEndpointExtensions.cs`
- `backend/src/SteamPlatform.Api/Features/Market/MarketEndpointExtensions.cs`
- `backend/src/SteamPlatform.Api/Features/Engagement/EngagementEndpointExtensions.cs` 中交易报价端点。
- `backend/src/SteamPlatform.Application/Inventory/InventoryContracts.cs`
- `backend/src/SteamPlatform.Application/Market/MarketContracts.cs`
- `backend/src/SteamPlatform.Infrastructure/Inventory/InventoryRepository.cs`
- `backend/src/SteamPlatform.Infrastructure/Market/MarketRepository.cs`
- `backend/src/SteamPlatform.Infrastructure/Engagement/EngagementRepository.cs` 中交易报价方法。
- `backend/tests/SteamPlatform.Api.Tests/InventoryEndpointTests.cs`
- `backend/tests/SteamPlatform.Api.Tests/InventoryRepositoryGuardTests.cs`
- `backend/tests/SteamPlatform.Api.Tests/MarketEndpointTests.cs`
- `backend/tests/SteamPlatform.Api.Tests/MarketRepositoryGuardTests.cs`
- `tests/market-api.http`

I 必须完全讲明白：`ITEM_TEMPLATE` 与 `INVENTORY_ITEM` 的区别、掉落为什么产生唯一实例、上架时如何校验归属和状态、为什么同一物品只能有一个有效卖单、买单为什么冻结资金、价格优先/时间优先如何匹配、5% 手续费如何记账、成交后如何同时转移物品与资金，以及 `ITEM_TRANSFER_LEDGER` 为什么能追溯历任所有者。

#### J：测试、CI、演示恢复、HTTPS 与云端部署

第一责任文件：

- `.github/workflows/ci.yml`
- `frontend/package.json`
- `frontend/playwright.config.ts`
- `frontend/e2e/baseline.spec.ts`
- `frontend/e2e/defense-flow.spec.ts`
- `frontend/e2e/social-community-flow.spec.ts`
- `frontend/e2e/helpers.ts`
- `frontend/scripts/run-cloud-e2e.mjs`
- `frontend/scripts/realtime-smoke.mjs`
- `backend/tests/`
- `backend/tools/SteamPlatform.DemoData/`
- `backend/tools/SteamPlatform.HttpsDeploy/`
- `database/demo/manifest.json`
- `docs/playwright-regression-runbook.md`
- `docs/https-deployment-runbook.md`
- `backend/tools/SteamPlatform.DemoData/README.md`
- `backend/tools/SteamPlatform.HttpsDeploy/README.md`

J 必须完全讲明白：单元测试、契约测试、Oracle 冒烟测试和 Playwright 端到端测试分别验证什么；CI 为什么同时执行格式、构建、测试、依赖审计和前端构建；写库 E2E 为什么要先备份并在结束后恢复；演示恢复工具如何按 manifest 清理、重建并记录审计；Nginx、ASP.NET Core 和 Oracle 在腾讯云上的部署关系；为什么公网只开放 80/443 和受限 22、不开放 1521；IP HTTPS 的用途、证书更新和失败时如何切换备用录屏。

### 3.4 共享大文件的方法级责任

以下文件承载多个业务域，不能用“整个文件都归某一个人”代替方法级学习：

| 共享文件 | 方法或区域 | 第一责任人 |
|---|---|---|
| `backend/src/SteamPlatform.Infrastructure/CoreTransactions/CoreTransactionService.cs` | 钱包、充值、购买、订单、玩家退款申请 | E |
| `backend/src/SteamPlatform.Infrastructure/CoreTransactions/CoreTransactionService.cs` | 免费入库、游戏库、游玩时长、CDKey 兑换 | F |
| `backend/src/SteamPlatform.Infrastructure/CoreTransactions/CoreTransactionService.cs` | CDKey 批次生成 | B |
| `backend/src/SteamPlatform.Infrastructure/CoreTransactions/CoreTransactionService.cs` | 管理员退款批准/拒绝 | D |
| `backend/src/SteamPlatform.Api/Features/CoreTransactions/CoreTransactionEndpointExtensions.cs`、`backend/src/SteamPlatform.Application/CoreTransactions/CoreTransactionContracts.cs` | 与上面相同的端点和 DTO 分区 | B、D、E、F 各自对应 |
| `backend/src/SteamPlatform.Infrastructure/Games/GameRepository.cs` | 开发商 CRUD、所有权隔离、管理员状态修改 | B；D 负责状态修改 |
| `backend/src/SteamPlatform.Infrastructure/Games/GameRepository.cs` | 商店列表、详情、评价/成就/饰品概览查询 | C |
| `backend/src/SteamPlatform.Infrastructure/Engagement/EngagementRepository.cs` | 资料、动态、讨论区 | H |
| `backend/src/SteamPlatform.Infrastructure/Engagement/EngagementRepository.cs` | 交易报价 | I |
| `backend/tests/`、`frontend/e2e/` | 测试框架、运行配置和总体验收 | J；具体业务断言由对应模块责任人共同掌握 |
| `database/schema.sql` | 全局结构、约束、索引 | G |
| `database/schema.sql` | 各业务表含义与字段规则 | 对应业务责任人 |

### 3.5 每个人的学习验收标准

每个人在正式答辩前必须通过以下验收，不能只记住按钮顺序：

1. 不看稿，在 90 秒内说明本模块的业务目标、核心表和关键约束。
2. 在 60 秒内从前端页面定位到 API、Application、Infrastructure 和 Oracle 表。
3. 解释一个正常流程、一个越权/重复操作失败流程和一个事务回滚场景。
4. 指出至少一个本模块的自动化测试，并说明它防止什么回归。
5. 能在云端演示本模块，且知道接口或网络失败时如何判断是前端、API 还是数据库问题。
6. 能回答“为什么这样设计”，而不只回答“代码就是这样写的”。

彩排时由其他成员随机从下列角度追问：权限、事务、并发、约束、索引、异常、测试、安全和可扩展性。任何一项答不上来，责任人必须回到上述文件补学，并在下一次彩排重新接受提问。

### 3.6 现场提问转交规则

1. A 听完老师问题后，只在问题归属不清时简短复述并点名责任人。
2. 被点名者先用一句话给结论，再解释代码路径和数据库依据，控制在 30 至 60 秒。
3. 跨业务与数据库的问题由业务责任人先答，G 再补充表、约束、索引或事务证据。
4. 涉及“如何证明测试过、如何部署、如何恢复”的问题由 J 补充。
5. 其他成员不得抢答或给出不同口径；发现表述遗漏时先由 A 邀请补充。
6. A 负责最终收束，确保回答与 README、课程要求和既定技术路线一致。

## 4. 固定账号与现场数据

| 角色 | 账号 | 演示密码 | 说明 |
|---|---|---|---|
| 玩家甲 | `defense_p1` | `Demo123456` | 现场注册，昵称“答辩玩家甲” |
| 玩家乙 | `defense_p2` | `Demo123456` | 现场注册，昵称“答辩玩家乙” |
| Klei 开发商 | `klei@example.com` | `klei` | 固定演示账号，仅管理 Klei 游戏 |
| Valve 开发商 | `valve@example.com` | `valve` | 固定演示账号，仅管理 Valve 游戏 |
| 管理员 | `rootadmin` | `admin` | 固定演示账号，负责审核 |

这些密码只用于课程演示账号，不得复用于真实系统。两名临时玩家会在恢复时删除。

固定输入数据：

| 操作 | 固定值 |
|---|---|
| Klei 临时游戏 | `Survival Lab`，原价 `68.00`，折扣系数 `0.80` |
| Valve 临时游戏 | `Tactical Arena Lab`，原价 `88.00`，折扣系数 `0.90` |
| Klei CDKey 批次 | 数量 1，当前时间生效，一个月后过期 |
| 玩家甲充值与购买 | 充值 `60.00`，购买 DST `24.00` |
| 玩家乙市场充值 | `100.00` |
| 玩家甲饰品售价 | `49.00` |
| 市场平台费率 | 5%，手续费 `2.45`，卖方实收 `46.55` |

## 5. 答辩前 30 分钟检查

G 执行以下工作：

1. 运行演示恢复工具 `reset`，保存运行编号。
2. 确认 `/api/health` 和 `/health/database` 均返回 `OK`。
3. 确认 `steam-platform-api`、Nginx 和证书续期 timer 为 active，failed unit 数量为 0。
4. 确认 Oracle 只监听服务器回环地址，公网未开放 1521。
5. 打开 Oracle 只读查询、总验收结果、执行计划和行锁证据。
6. 准备最新 1080p 备用录屏，但不在主演示中播放。

B-G 执行以下工作：

1. 所有浏览器缩放保持 100%，关闭无关扩展、通知和悬浮窗口。
2. B、C、D 停留在各自登录页，不提前提交业务。
3. E、F 停留在注册页。
4. 每台电脑确认 HTTPS 页面可访问，中文、图片和视频正常。
5. B 的电脑准备一个仅本机可见的临时文本框，用于保存本轮生成的 CDKey；不得把 CDKey 写入 Git 或公开文档。

A 完成一次屏幕共享顺序检查：A → B → C → D → E/F → D → G → A。

## 6. 20 分钟精确时间轴

| 时间 | 操作电脑 | 内容 | 目标结束时间 |
|---|---|---|---|
| 0:00-0:50 | A | 项目定位、技术路线、五层结构 | 0:50 |
| 0:50-2:05 | B | Klei 创建 `Survival Lab` 和 1 个 DST CDKey | 2:05 |
| 2:05-2:55 | C | Valve 证明隔离并创建 `Tactical Arena Lab` | 2:55 |
| 2:55-3:45 | D | 管理员只上架 `Survival Lab` | 3:45 |
| 3:45-5:20 | E、F | 两名玩家注册、发送并接受好友请求 | 5:20 |
| 5:20-6:40 | E、F | 商店审核结果、CS2 免费入库、各掉落一件饰品 | 6:40 |
| 6:40-8:45 | E、F | 玩家甲购买 DST；玩家乙兑换并重复兑换 CDKey | 8:45 |
| 8:45-10:15 | E、F | SignalR 聊天、评测、成就、工坊订阅 | 10:15 |
| 10:15-13:20 | E、F | 玩家甲上架饰品，玩家乙立即购买，核对账本 | 13:20 |
| 13:20-14:40 | E、D | 玩家甲申请退款，管理员审核通过 | 14:40 |
| 14:40-17:20 | G | Oracle 数据、索引、执行计划与行锁证据 | 17:20 |
| 17:20-18:40 | A | 安全、测试、部署和团队协作 | 18:40 |
| 18:40-19:25 | A | 总结 | 19:25 |
| 19:25-20:00 | 全员 | 网络延迟、切屏或老师打断缓冲 | 20:00 |

## 7. 逐步演示与讲解词

### 7.1 开场与架构，0:00-0:50，A

A 展示一页架构图并讲解：

> 本项目实现了一个仿 Steam 的数据库应用。浏览器访问 Vue 单页应用，Nginx 负责 HTTPS 和反向代理，ASP.NET Core .NET 10 按 Api、Application、Domain、Infrastructure、Shared 五层处理业务，最终访问腾讯云 Oracle。接下来我们用两名新玩家、两家开发商和一名管理员现场完成整个平台生命周期。

只讲数据流和五层职责，不在开场逐表朗读数据库。

### 7.2 Klei 提交内容与 CDKey，0:50-2:05，B

1. 选择 `DEVELOPER`，登录 `klei@example.com`。
2. 打开“开发商游戏管理”，指出列表中只有 Klei 的 DST。
3. 创建 `Survival Lab`，原价 68，折扣系数 0.80，发行日期为当天，口碑留空。
4. 指出创建结果固定为 `OFFLINE`，不能直接进入商店。
5. 打开“CDKey 批次”，为 DST 生成 1 个当前生效、一个月后过期的 CDKey。
6. 将明文 CDKey 保存在 B 电脑的临时文本框，稍后交给 F；不在投影上长时间停留。

B 讲解：

> 开发商只能提交内容，不能自行公开上架。CDKey 明文只在创建响应中展示一次，Oracle 只保存哈希。

预期数据库变化：`GAME` 新增一条 OFFLINE 记录；`CDKEY_BATCH` 和 `CDKEY` 各新增记录。

### 7.3 Valve 证明开发商隔离，2:05-2:55，C

1. 选择 `DEVELOPER`，登录 `valve@example.com`。
2. 打开“开发商游戏管理”，指出只能管理 CS2，看不到 Klei 的 `Survival Lab`。
3. 创建 `Tactical Arena Lab`，原价 88，折扣系数 0.90，状态自动为 `OFFLINE`。

C 讲解：

> 开发商主体 ID 来自 JWT。即使修改前端请求，也不能以 Valve 身份更新 Klei 的游戏。

### 7.4 管理员选择性上架，2:55-3:45，D

1. 选择 `ADMIN`，登录 `rootadmin`。
2. 打开“管理 / 游戏上下架”，切换到“已下架”。
3. 找到两家开发商刚提交的游戏。
4. 只将 `Survival Lab` 上架；`Tactical Arena Lab` 保持下架。

D 讲解：

> 内容维护和公开审核相互分离。只有 ADMIN 可以调用上下架接口，开发商和玩家访问会被拒绝。

### 7.5 两名玩家注册并成为好友，3:45-5:20，E、F

E 注册 `defense_p1 / 答辩玩家甲 / Demo123456`，F 同时注册 `defense_p2 / 答辩玩家乙 / Demo123456`。

注册完成后：

1. E 打开玩家搜索，搜索“答辩玩家乙”并发送好友请求。
2. F 打开社区人员页，接受来自玩家甲的请求。
3. E 刷新，双方显示“已经是好友”。

A 讲解：

> 玩家可以公开注册。注册会创建 `PLAYER` 和一对一的 `WALLET_ACCOUNT`，初始可用和冻结余额均为 0。好友关系由 Oracle 保存，不是浏览器本地状态。

### 7.6 商店审核结果、CS2 免费入库与掉落，5:20-6:40，E、F

1. E 打开公开商店，搜索到已经上架的 `Survival Lab`。
2. 搜索 `Tactical Arena Lab`，确认下架游戏不可见。
3. E、F 分别打开 CS2，点击“免费入库”。
4. 两人分别进入 CS2 库存，点击一次“模拟掉落”并确认。
5. E 记录自己掉落物品的名称、`item_id` 和 `template_id`。

A 讲解：

> CS2 免费入库仍会生成零元订单、明细、支付记录和 `PLAYER_LIBRARY` 授权，但钱包不变。库存物品是带实例编号、磨损、所有者和状态的实体，不是模板数量。

### 7.7 DST 的购买授权与 CDKey 授权，6:40-8:45，E、F

E 执行钱包购买：

1. 充值 60 元，默认使用微信模拟支付。
2. 购买折后 24 元的 DST。
3. 打开钱包流水，确认 `+60.00` 和 `-24.00`，余额为 `36.00`。
4. 打开游戏库，确认 CS2 和 DST 均存在。

F 执行 CDKey 兑换：

1. 从 B 获取刚生成的明文 CDKey。
2. 兑换成功后打开游戏库，确认 DST 的授权来源为兑换，钱包仍为 0。
3. 再次输入同一个 CDKey，展示“已经兑换”的可解释结果。

A 讲解：

> 玩家甲通过 BUY 获得 DST，玩家乙通过 REDEEM 获得同一游戏。重复兑换不会产生第二份权益，并写入 `CDKEY_REDEEM_LOG`。

### 7.8 好友聊天、评测、成就与工坊，8:45-10:15，E、F

1. E、F 同时打开“好友与聊天”。
2. E 发送“答辩实时消息：Oracle 与 SignalR 已贯通”。
3. F 不刷新页面，展示实时通知和聊天内容。
4. F 刷新聊天，确认历史消息仍然存在。
5. E 打开 DST 社区，发表推荐评测并解锁 `First Night Together`。
6. F 打开 DST 工坊，订阅“自动整理箱”，刷新后仍显示已订阅。

A 讲解：

> SignalR 只负责实时通知，`DIRECT_MESSAGE` 才是消息真相。评测、成就和工坊订阅同样由 Oracle 持久化，并在后端检查游戏所有权。

### 7.9 两名新玩家完成 CS2 饰品交易，10:15-13:20，E、F

本流程采用“卖方先挂单、买方立即购买”，不使用预置 Alice/Bob 订单，也不使用可能匹配到其他模板的全局撮合按钮。

E 执行卖方操作：

1. 打开刚掉落的 CS2 饰品实例。
2. 点击“出售”，固定填写 `49.00`。
3. 确认实例状态由 `NORMAL` 变为 `IN_MARKET`。
4. 将物品名称或模板编号告诉 F。

F 执行买方操作：

1. 先充值 `100.00`。
2. 在市场搜索 E 的物品模板。
3. 打开该模板，确认当前最低售价为 `49.00`。
4. 点击“立即购买”。该操作创建绑定本次购买的买单并立即撮合最低卖单。
5. 查看成交记录、钱包和库存。

预期结果：

```text
成交价                       49.00
平台费 49.00 x 5%            2.45
玩家甲实收                  46.55
玩家甲成交后余额 36 + 46.55 82.55
玩家乙成交后余额 100 - 49    51.00
玩家乙冻结余额                0.00
```

E 打开物品流转账本，按本轮 `item_id` 查询，确认物品由玩家甲转移到玩家乙。F 在库存中确认收到同一个实例。

A 讲解：

> 撮合事务同时写市场订单、成交、钱包、钱包流水、库存所有权和物品流转账本。买卖双方 ID 和物品 ID 均来自已锁定的数据库记录，前端不能指定结算结果。

### 7.10 玩家退款与管理员审核，13:20-14:40，E、D

1. E 打开退款页，选择刚购买的 DST 订单并提交全额退款。
2. D 打开“管理 / 退款审核”，通过最新 PENDING 申请。
3. E 刷新钱包、订单和游戏库。

预期结果：

```text
玩家甲退款前余额              82.55
退款入账                      24.00
玩家甲最终余额               106.55
订单状态          CLOSED / REFUNDED
DST 授权                      REVOKED
玩家乙 DST 授权      NORMAL / REDEEM
```

A 讲解：

> 退款不会删除原购买记录，而是在一个事务中更新退款单、明细、订单、支付、钱包和授权，并写管理员审核日志。玩家乙的 CDKey 权益不受玩家甲退款影响。

### 7.11 Oracle 专项证据，14:40-17:20，G

G 使用只读查询按以下顺序展示：

1. Klei 与 Valve 各自拥有的固定游戏和临时提交，只有 `Survival Lab` 为 ONLINE。
2. 两名临时玩家、好友关系和聊天消息均存在。
3. 玩家甲最终可用余额 106.55；玩家乙最终可用余额 51.00、冻结余额 0。
4. 玩家甲 DST 权益为 REVOKED；玩家乙 DST 权益为 NORMAL 且 `acquire_way = REDEEM`。
5. 本轮市场成交价格、手续费、买卖双方和 `item_id` 与页面一致。
6. `ITEM_TRANSFER_LEDGER` 显示该实例从玩家甲转移到玩家乙。
7. `CDKEY_REDEEM_LOG` 同时存在成功和重复兑换结果。
8. 45 张表、45 个主键、至少 49 个业务索引，无禁用约束、无效索引或无效对象。
9. 订单、市场和讨论查询分别使用对应业务索引。
10. 双会话钱包行锁采用有限等待并安全回滚，不改变业务数据。

G 只展示结论和三条代表性记录，不滚动大量 SQL 输出。详细脚本见 `database/verify_defense.sql`、`database/defense/` 和 `docs/database-defense-runbook.md`。

### 7.12 安全、工程质量与总结，17:20-19:25，A

A 说明：

- JWT 验证 issuer、audience、HMAC-SHA256 签名和生命周期，浏览器仅在 `sessionStorage` 保存 token。
- 登录和注册按真实客户端 IP 限流；Oracle 1521 和 API 内部端口不向公网开放。
- 钱包、订单、退款和市场接口从 token 读取当前主体，不信任前端传入的用户 ID。
- Nginx 已启用 CSP、HSTS、防 iframe 和其他安全响应头。
- `main` 受保护，普通组员必须通过 PR、review 和 GitHub Actions `verify`。
- 当前验收包括后端与工具测试 `208/208`、云端 Playwright `14/14`、Oracle 总验收 `21/21`、npm 审计 0 漏洞。

固定总结词：

> 我们现场使用了两名新注册玩家、两家相互隔离的开发商和一名管理员，完成了内容提交审核、两种游戏授权、钱包订单、实时社区、饰品交易和退款审计。每个页面结果都可以在 Oracle 业务表、资金账本和资产账本中交叉验证，因此本项目不是静态界面原型，而是一套可部署、可测试、可恢复的数据库应用系统。

19:25 后停止主动增加演示内容，将剩余时间留给切屏延迟或老师临时提问。

## 8. 切屏与协作规则

1. 只有 A 发出切屏口令，其他成员不得主动抢占共享画面。
2. 下一位操作者必须在上一阶段结束前准备好目标页面，切屏后直接点击，不现场寻找菜单。
3. B 生成 CDKey 后通过私下消息发送给 F，同时保留在 B 的临时文本框作为备用。
4. E 掉落饰品后把名称、`item_id`、`template_id` 发给 F 和 G；F 用模板搜索，G 用实例编号查询账本。
5. D 的管理员页面始终保持登录，用于前半段上架和后半段退款，避免重复登录。
6. G 全程观察健康状态，但只在异常或数据库证据阶段发言。
7. 任何成员点击后等待明确成功提示，不因网络延迟重复提交。

## 9. 故障预案

| 问题 | 立即处理 | 主讲说明 |
|---|---|---|
| 页面暂时无数据 | G 检查健康接口；操作者只刷新一次 | 正在重新读取云端数据 |
| 临时账号或批次已存在 | G 执行 `reset`，从该阶段开头重来 | 上次彩排数据未清理，正在恢复固定基线 |
| 视频加载慢 | 使用海报、截图和全屏图片，不等待视频 | 媒体与核心数据库业务解耦 |
| CDKey 未及时传给 F | B 重新复制本轮结果；不得生成第二批 | 明文只在创建响应中展示一次 |
| 市场物品搜索错误 | F 使用 E 提供的 `template_id` 搜索 | 交易按模板匹配、按实例转移 |
| 交易按钮尚未可用 | F 等待一次市场刷新；不要再次上架 | 等待云端订单查询完成 |
| SignalR 暂时断线 | 刷新后展示 Oracle 聊天历史 | 实时推送失败不会丢失持久化消息 |
| 某成员电脑故障 | A 使用备用浏览器配置接管该角色 | 切换备用演示环境 |
| 公网不可用 | A 播放最新 1080p 录屏，G 展示本地测试和 Oracle 证据 | 使用已经过同一数据恢复保护的备用录屏 |

不得手工修改 Oracle 表来修复演示状态。需要回滚时使用恢复工具的运行编号，或执行一次新的 `reset`。

## 10. 老师常见追问与回答负责人

| 问题方向 | 第一回答人 | 回答重点 |
|---|---|---|
| 总体架构、B/S、五层 | A | 各层职责与调用方向 |
| 游戏、开发商、上下架 | B 或 C | 开发商隔离、管理员审核、状态约束 |
| 钱包、订单、退款 | E | 唯一余额真相、显式事务、幂等和审计 |
| 库存、市场、资产转移 | F | 实例化资产、锁定状态、手续费和流转账本 |
| Oracle 表设计、索引、锁 | G | 约束、执行计划、行锁与一致性检查 |
| 权限与安全 | A | JWT、角色、限流、端口和私钥规则 |

被追问的成员只回答自己负责的部分；A 在回答结束后补一句与整体架构的关系。

## 11. 答辩结束

1. G 再执行一次 `reset`，保存最终运行编号。
2. 确认两名临时玩家、两款临时提交、CDKey、订单、退款、聊天、订阅和市场成交已清理。
3. 确认 `/api/health` 与 `/health/database` 仍为 `OK`。
4. 确认云端部署标记与 `main` 最新验收提交一致。
5. B 删除临时保存的明文 CDKey。

## 12. 正式答辩前必须完成的彩排

1. 七人按本文件完整彩排至少 3 次。
2. 第一次允许暂停并修正文档；第二次必须控制在 20 分钟内；第三次完全模拟老师打断和一次网络延迟。
3. 每次彩排前后都执行恢复，并记录运行编号。
4. 第三次彩排生成最终备用录屏。
5. 只有完整流程、金额、Oracle 查询和恢复结果全部一致后，才把该录屏标记为答辩备用版本。
