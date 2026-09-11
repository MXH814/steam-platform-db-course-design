# 数据库课程设计 20 分钟多角色答辩演示手册

本文件是正式答辩和集体彩排的唯一演示口径。流程使用两名现场注册玩家、两家固定开发商和一名管理员，覆盖内容提交、审核上架、三种游戏授权、钱包订单、社区互动、饰品交易、退款审计和 Oracle 证据。

正式演示前后必须执行演示数据恢复。不得临场更换账号、价格、步骤或样板游戏；流程变化后必须同步更新本文件和 README。

## 1. 演示目标

20 分钟内证明以下内容：

1. 系统采用 B/S 架构，Vue 前端、Nginx、ASP.NET Core .NET 10 五层后端和腾讯云 Oracle 真实联动。
2. `PLAYER`、`DEVELOPER`、`ADMIN` 三类主体具有明确且不可越权的职责。
3. Klei 与 Valve 两家开发商的数据彼此隔离；新游戏必须经过管理员上架后才能进入公开商店。
4. 两名玩家从现场注册开始，分别通过钱包购买和 CDKey 兑换获得 DST，通过免费领取获得 CS2。
5. 钱包、订单、退款和市场交易均由 Oracle 事务维护，并保留可审计的资金流水或资产流转记录。
6. 好友、聊天、评测、成就和工坊订阅由 Oracle 持久化，SignalR 只负责实时推送。
7. 数据库结构、约束、索引、执行计划、行锁和跨表一致性均有可重复验证的证据。

## 2. 固定业务口径

- 固定样板游戏限定为 `GAME_CS2` 和 `GAME_DST`。
- 临时创建的 `Survival Lab` 与 `Tactical Arena Lab` 只用于演示开发商提交和管理员审核，答辩结束后由恢复工具清除，不构成第三、第四款样板游戏。
- CS2 负责免费入库、饰品掉落、库存实例、市场订单、成交和物品流转。
- DST 负责付费购买、CDKey、评测、成就、工坊和退款。
- 钱包唯一真相是 `WALLET_ACCOUNT.available_balance` 与 `frozen_balance`，不得引用或恢复 `PLAYER.wallet_balance`。
- 开发商和管理员不能公开注册。只有玩家可以现场注册；开发商和管理员账号由平台预先审核和维护。

## 3. 七人现场操作与十人知识责任

### 3.1 七人现场操作与电脑准备

正式演示设置 7 个操作岗位，每人使用一台电脑。其余 3 名组员承担专项技术问答、代码定位和故障补位职责，不重复设置业务操作岗位。

| 人员 | 电脑与账号 | 主要职责 | 单路共享时段 |
|---|---|---|---|
| 马祥珲 | 主讲电脑 | 开场、架构、时间控制、共享交接口令、安全、工程质量与总结 | 0:00-0:50、17:20-19:25 |
| 徐京 | Klei 开发商电脑 | `klei@example.com`，创建临时游戏和 DST CDKey | 0:50-2:05 |
| 王子轩 | Valve 开发商电脑 | `valve@example.com`，证明开发商隔离、提交第二款临时游戏并说明商店展示 | 2:05-2:55 |
| 李胤龙 | 管理员电脑 | `rootadmin`，选择性上架、权限说明和退款审核 | 2:55-3:45、13:20-13:55 |
| 胡知鱼 | 玩家甲电脑 | 注册 `defense_p1`，充值、购买 DST、评测、出售 CS2 饰品和申请退款 | 3:45-4:10、4:35-4:55、5:55-7:45、9:05-9:40、10:15-11:05、12:25-13:20、13:55-14:40 |
| 靳岱泽 | 玩家乙电脑 | 注册 `defense_p2`，兑换 CDKey、好友聊天、解锁成就、工坊订阅和购买饰品 | 4:10-4:35、4:55-5:55、7:45-9:05、9:40-10:15、11:05-12:25 |
| 张茗博 | 数据库与运维电脑 | 恢复基线、健康检查、Oracle 证据、市场账本和故障处理 | 答辩前、14:40-17:20 |

马祥珲负责开场、统一共享交接口令、必要的环节衔接和最后总结；每个业务环节由当前共享屏幕的实际操作者边操作边讲解，其他成员不得代替操作者说明。所有环节严格串行，不存在两名成员同时演示或同时共享屏幕。马祥珲使用固定口令交接，例如“徐京停止共享，下面请王子轩开始共享”“靳岱泽停止共享，回到胡知鱼的玩家甲”“张茗博停止共享，回到主讲电脑”。

腾讯会议始终只允许一名成员共享屏幕。交接时先由当前操作者说“本阶段完成”并停止共享，马祥珲确认画面消失后点名下一位，下一位再开始共享；禁止在上一位尚未停止时发起共享。正式彩排必须采用与答辩当天相同的交接方式。马祥珲的电脑预先保留所有角色的备用浏览器配置，任一成员电脑异常时由马祥珲接管。

### 3.2 十人知识责任总表

现场操作仍由上表 7 人完成，不增加角色切换。元梓浩、周力扬、郭炫君不接管玩家、开发商或管理员账号，负责专项技术问答、代码定位和故障补位。十个人都必须掌握自己模块从“前端页面 → API 端点 → Application 契约/服务 → Infrastructure/Oracle → 数据表 → 测试”的完整链路。

| 人员 | 现场身份 | 第一知识责任 | 老师提问时负责回答 |
|---|---|---|---|
| 马祥珲 | 总主讲，不固定业务账号 | 项目统筹、总体架构、退款审批与审计、核心事务工作流回归及最终验收 | B/S、.NET 10、五层结构、退款事务、关键跨模块边界与全局验收结论 |
| 李胤龙 | 管理员 | 认证后端、JWT、角色守卫、HTTPS 与后端部署 | 登录鉴权、越权防护、管理员边界、Nginx 和云端健康检查 |
| 元梓浩 | 专项问答 | 前端公共入口、公告、CI 与 Playwright | 会话恢复、角色路由、统一请求、自动构建与前端回归 |
| 周力扬 | 专项问答 | 开发商游戏全链路、管理员状态修改、商店列表与筛选 | 默认下架、`dev_id` 所有权、公开可见性、搜索筛选与集合页数据流 |
| 王子轩 | Valve 开发商 | 游戏详情、媒体画廊与 Steam 风格交互 | 详情组件拆分、响应式布局、图片视频兜底与视觉一致性 |
| 胡知鱼 | 玩家甲 | 钱包、充值、购买、订单与资金流水 | 钱包唯一真相、定点金额、购买事务、幂等和流水核对 |
| 徐京 | Klei 开发商 | CDKey、免费入库与游戏库授权 | 三种授权来源、CDKey 哈希、重复兑换和游戏库状态 |
| 靳岱泽 | 玩家乙 | 好友、私信、通知、个人资料、社区动态与 SignalR | 好友状态、消息持久化、资料徽章、动态讨论、实时推送和断线恢复 |
| 郭炫君 | 专项问答 | 饰品库存、评价、成就与工坊 | 模板/实例、所有权、评价版本、成就防重与工坊订阅 |
| 张茗博 | 数据库与运维电脑 | 市场撮合、Oracle 总体证据与演示数据恢复 | 撮合与资产转移、全局约束、索引、锁、迁移规范和恢复审计 |

第一知识责任人须能够独立回答对应模块问题。跨模块问题由第一责任人说明业务规则，张茗博补充数据库约束和 SQL 证据，相关测试或部署责任人补充验证证据，马祥珲负责归纳其与总体架构的关系并统一结论。

### 3.3 十人具体文件与掌握要求

#### 李胤龙、元梓浩、马祥珲：总体架构、认证授权与前端公共入口

重点掌握文件（文件级第一责任分配见 3.4 节）：

- `README.md`
- `项目文档/Steam-数字游戏平台系统需求分析文档.docx`
- `项目文档/Steam-数字游戏平台系统设计与实现文档.docx`
- `项目文档/“Steam-”数字游戏平台系统答辩PPT.pptx`
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

责任边界：李胤龙负责认证后端、JWT、角色守卫和公共异常；元梓浩负责登录/注册页面、路由守卫、会话恢复、统一 HTTP 调用与格式化；马祥珲负责 B/S、五层依赖、项目集成和技术选型。三人共同掌握以下内容：

1. 浏览器、Vue、Nginx、ASP.NET Core 与 Oracle 之间的请求链路。
2. Api、Application、Domain、Infrastructure、Shared 五层职责及项目引用的单向依赖原则。
3. 玩家开放注册、开发商与管理员采用预置身份的权限依据。
4. JWT 签发、角色声明、有效期验证、路由守卫与后端鉴权的协作关系。
5. 统一响应、业务异常、禁止访问和资源不存在的 HTTP 结果映射。
6. B/S、.NET 10、Vue 和 Oracle 技术选型及其相对于 C/S、纯前端和页面直连 SQL 方案的适用性。

#### 周力扬、徐京：开发商游戏管理与 CDKey 生成

重点掌握文件（文件级第一责任分配见 3.4 节）：

- `backend/src/SteamPlatform.Api/Features/Games/GameEndpointExtensions.cs`
- `backend/src/SteamPlatform.Application/Games/GameContracts.cs`
- `backend/src/SteamPlatform.Application/Games/GameService.cs`
- `backend/src/SteamPlatform.Infrastructure/Games/GameRepository.cs`
- `frontend/src/views/DeveloperGamesView.vue`
- `frontend/src/views/AdminGamesView.vue`
- `frontend/src/views/StoreView.vue`
- `frontend/src/views/StoreCollectionView.vue`
- `frontend/src/components/GameCard.vue`
- `frontend/src/components/GameFilterBar.vue`
- `frontend/src/data/gameCatalog.ts`
- `frontend/src/views/CdkeyBatchView.vue`
- `frontend/src/api/games.ts`
- `backend/tests/SteamPlatform.Api.Tests/GameServiceTests.cs`
- `backend/tests/SteamPlatform.Api.Tests/GameRepositoryGuardTests.cs`
- `frontend/e2e/public-store.spec.ts`

共享文件中的责任范围：

- `backend/src/SteamPlatform.Api/Features/CoreTransactions/CoreTransactionEndpointExtensions.cs` 中开发商创建 CDKey 批次的端点。
- `backend/src/SteamPlatform.Infrastructure/CoreTransactions/CoreTransactionService.cs` 中 `CreateCdkeyBatchAsync`。
- `backend/src/SteamPlatform.Application/CoreTransactions/CoreTransactionContracts.cs` 中 CDKey 批次请求与响应契约。
- `database/schema.sql` 中 `DEVELOPER`、`GAME`、`CDKEY_BATCH`、`CDKEY`。

周力扬负责开发商身份、`dev_id` 所有权条件、游戏创建与 `OFFLINE/ONLINE` 状态，以及公开商店列表、搜索、筛选、集合页和后端可见性查询的完整数据流；徐京负责 CDKey 批次、明文只返回一次、哈希保存、兑换和重复兑换留痕。两人必须能把共享端点和事务服务按方法准确分开。

#### 王子轩：游戏详情、媒体画廊与 Steam 风格前端

重点掌握文件（文件级第一责任分配见 3.4 节）：

- `frontend/src/App.vue`
- `frontend/src/styles.css`
- `frontend/src/views/GameDetailView.vue`
- `frontend/src/views/NotFoundView.vue`
- `frontend/src/components/GameHeroPanel.vue`
- `frontend/src/components/GamePriceBlock.vue`
- `frontend/src/components/GameSummarySection.vue`
- `frontend/src/components/SteamGameDetailTemplate.vue`
- `frontend/src/components/SteamMediaGallery.vue`
- `frontend/src/components/Cs2DetailSections.vue`
- `frontend/src/components/GenericGameDetailSections.vue`
- `frontend/public/assets/games/`
- `frontend/public/assets/media/`
- `frontend/e2e/public-store.spec.ts`

掌握范围：游戏详情 API 到 Vue 详情组件的数据映射、CS2/DST 固定展示口径、详情组件拆分、视频海报与截图画廊降级、桌面与移动端溢出控制，以及仿 Steam 界面中的项目标识规范。根路径、商店列表、搜索筛选和集合页由周力扬负责，二人共同掌握从列表进入详情的路由衔接。

#### 李胤龙、元梓浩、马祥珲：管理员审核、公告与退款审批

重点掌握文件（文件级第一责任分配见 3.4 节）：

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

责任边界：李胤龙负责管理员角色守卫与审核权限；元梓浩负责公告发布时间、失效时间和管理页面；马祥珲负责退款批准/拒绝、钱包回补、授权撤销和审核日志。三人共同掌握退款审批的幂等控制机制。

#### 胡知鱼、马祥珲：钱包、充值、购买、订单与退款

重点掌握文件（文件级第一责任分配见 3.4 节）：

- `frontend/src/views/WalletView.vue`
- `frontend/src/views/WalletRechargeCheckoutView.vue`
- `frontend/src/views/WalletHistoryView.vue`
- `frontend/src/views/WalletHistoryDetailView.vue`
- `frontend/src/views/GameCheckoutView.vue`
- `frontend/src/views/OrderDetailView.vue`
- `frontend/src/views/RefundsView.vue`
- `frontend/src/views/WalletRefundRequestView.vue`
- `frontend/src/api/coreApi.ts` 中钱包、充值、购买、订单和退款申请函数。
- `backend/tests/SteamPlatform.Api.Tests/CoreTransactionEndpointTests.cs`
- `backend/tests/SteamPlatform.Api.Tests/CoreTransactionServiceGuardTests.cs`

共享文件中的责任范围：

- `backend/src/SteamPlatform.Api/Features/CoreTransactions/CoreTransactionEndpointExtensions.cs` 中钱包、充值、订单和玩家退款端点。
- `backend/src/SteamPlatform.Application/CoreTransactions/CoreTransactionContracts.cs` 中钱包、订单和退款契约。
- `backend/src/SteamPlatform.Infrastructure/CoreTransactions/CoreTransactionService.cs` 中 `GetWalletAsync`、`RechargeWalletAsync`、`ListWalletTransactionsAsync`、`ListWalletHistoryAsync`、`GetWalletHistoryEntryAsync`、`BuyGameAsync`、`ListOrdersAsync`、`GetOrderAsync`、`CreateRefundAsync`。
- `database/schema.sql` 中 `WALLET_ACCOUNT`、`GAME_ORDER`、`ORDER_DETAIL`、`ORDER_STATUS_LOG`、`PAYMENT_TRANSACTION`、`WALLET_TRANSACTION` 和退款相关表。

胡知鱼掌握钱包唯一余额来源、`available/frozen` 双余额模型、定点金额、充值、购买、订单、资金流水核对、账户锁定、订单/明细/支付/流水/游戏库原子写入和购买幂等。马祥珲负责退款申请与审批、退款幂等、钱包回补、支付与订单终态、授权撤销、审核日志，以及核心交易与授权之间的跨模块事务回归验收。

#### 徐京：CDKey、免费入库与游戏库授权

重点掌握文件（文件级第一责任分配见 3.4 节）：

- `frontend/src/views/RedeemView.vue`
- `frontend/src/views/LibraryView.vue`
- `frontend/src/views/GameLibraryView.vue`
- `frontend/src/components/LibraryRail.vue`
- `frontend/src/api/coreApi.ts` 中免费入库、游戏库、游玩时长和 CDKey 兑换函数。

共享文件中的责任范围：

- `backend/src/SteamPlatform.Api/Features/CoreTransactions/CoreTransactionEndpointExtensions.cs` 中免费入库、游戏库、游玩时长和 CDKey 兑换端点。
- `backend/src/SteamPlatform.Infrastructure/CoreTransactions/CoreTransactionService.cs` 中 `ClaimFreeGameAsync`、`ListLibraryAsync`、`AddPlaytimeAsync`、`RedeemCdkeyAsync`。
- `database/schema.sql` 中 `PLAYER_LIBRARY`、`CDKEY_BATCH`、`CDKEY` 和 `CDKEY_REDEEM_LOG`。

掌握范围：购买、免费领取和 CDKey 兑换三种 `acquire_way`，CDKey 明文单次展示与哈希保存、单次成功约束、失败尝试留痕、游戏库状态和游玩时长。

#### 靳岱泽：好友、聊天、个人资料、社区内容与实时通信

重点掌握文件（文件级第一责任分配见 3.4 节）：

- `frontend/src/api/socialApi.ts` 中玩家搜索、好友、私信和通知函数。
- `frontend/src/api/socialRealtime.ts`
- `frontend/src/views/CommunityHubView.vue` 中玩家搜索与好友区域。
- `frontend/src/views/ProfileView.vue`
- `frontend/src/api/engagementApi.ts` 中资料、徽章、动态和讨论区函数。
- `frontend/src/views/GameCommunityView.vue` 中好友概览与聊天区域。
- `backend/src/SteamPlatform.Api/Features/Social/SocialEndpointExtensions.cs`
- `backend/src/SteamPlatform.Api/Realtime/SocialHub.cs`
- `backend/src/SteamPlatform.Api/Realtime/SignalRSocialNotifier.cs`
- `backend/src/SteamPlatform.Application/Social/SocialContracts.cs`
- `backend/src/SteamPlatform.Application/Social/SocialService.cs`
- `backend/src/SteamPlatform.Domain/Social/`
- `backend/src/SteamPlatform.Infrastructure/Social/SocialRepository.cs` 中玩家搜索、好友、私信和通知方法。
- `backend/src/SteamPlatform.Api/Features/Engagement/EngagementEndpointExtensions.cs` 中资料、徽章、动态和讨论区端点。
- `backend/src/SteamPlatform.Application/Engagement/`
- `backend/src/SteamPlatform.Domain/Engagement/`
- `backend/src/SteamPlatform.Infrastructure/Engagement/EngagementRepository.cs` 中资料、徽章、动态和讨论区方法。
- `backend/tests/SteamPlatform.Api.Tests/SocialEndpointTests.cs`
- `backend/tests/SteamPlatform.Api.Tests/SocialServiceTests.cs`
- `backend/tests/SteamPlatform.Api.Tests/SocialRepositoryGuardTests.cs`
- `backend/tests/SteamPlatform.Api.Tests/EngagementServiceTests.cs`
- `backend/tests/SteamPlatform.Api.Tests/EngagementRepositoryGuardTests.cs`
- `database/schema.sql` 中 `FRIEND_RELATION`、`DIRECT_MESSAGE` 和 `USER_NOTIFICATION`。
- `database/migrations/20260825_social_realtime_foundation.sql`
- `database/migrations/20260825_community_engagement_expansion.sql`

掌握范围：好友关系规范化、请求方向与状态转换、陌生人私信限制、Oracle 消息持久化、SignalR 实时推送、断线后的历史查询，以及个人资料、徽章、社区动态、回应、讨论主题与回复的数据关系。交易报价仍由张茗博负责。

#### 张茗博：Oracle 总体设计、约束、索引、执行计划与并发；马祥珲：验收口径

重点掌握文件（文件级第一责任分配见 3.4 节）：

- `database/schema.sql`
- `database/data.sql`
- `database/Oracle数据库导入与初始化说明.md`
- `项目文档/“Steam-”数字游戏平台系统数据库设计文档.docx`
- `database/migrations/20260825_demo_reset_audit.sql`
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
- `tests/SteamPlatform.Database.Tests/MigrationScriptConventionTests.cs`

张茗博掌握核心实体及联系、主键/外键/唯一/检查约束、钱包字段归一化、45 张表的业务域划分、组合索引、执行计划、行锁、市场账本、迁移规范、演示恢复和跨表一致性。各业务迁移脚本的业务语义由对应模块责任人掌握，张茗博负责检查其全局兼容性。马祥珲掌握数据库设计在总体架构中的位置以及最终验收口径。

每位业务成员仍必须掌握自己模块涉及的表；张茗博负责全局 DDL、规范化、索引和跨模块一致性，不代替模块成员回答业务规则。

#### 郭炫君、靳岱泽：评价、成就、工坊、资料与社区内容

重点掌握文件（文件级第一责任分配见 3.4 节）：

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

责任边界：郭炫君负责评价所有权校验、`REVIEW_VERSION` 版本留痕、管理员隐藏、DST 课程演示成就、防重复解锁和工坊订阅；靳岱泽负责个人资料、徽章、社区动态、回应、讨论主题与回复的数据关系和事务实现。

#### 郭炫君、张茗博：饰品库存、市场撮合、交易报价与资产账本

重点掌握文件（文件级第一责任分配见 3.4 节）：

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

郭炫君掌握 `ITEM_TEMPLATE` 与 `INVENTORY_ITEM` 的区别、掉落实例化、磨损/归属/状态保存和上架校验。张茗博掌握单物品有效卖单约束、买单资金冻结、价格优先/时间优先规则、5% 手续费、成交事务和 `ITEM_TRANSFER_LEDGER` 追溯。

#### 马祥珲：项目统筹、总体架构与总体验收

重点掌握文件（文件级第一责任分配见 3.4 节）：

- `README.md`
- `backend/SteamPlatform.sln`
- `docs/defense-demo-runbook.md`
- `项目文档/Steam-数字游戏平台系统需求分析文档.docx`
- `项目文档/Steam-数字游戏平台系统设计与实现文档.docx`
- `项目文档/“Steam-”数字游戏平台系统答辩PPT.pptx`
- `backend/tests/SteamPlatform.Api.Tests/CoreTransactionWorkflowRegressionTests.cs`
- `frontend/src/views/RefundsView.vue`
- `frontend/src/views/AdminRefundsView.vue`
- `frontend/src/views/WalletRefundRequestView.vue`

掌握范围：课程要求、技术路线、B/S 与五层架构、模块边界、文档统一口径、退款审批与审计闭环、核心交易跨模块回归、整体验收结果、答辩流程和成员协作。各专项工具的实现与操作由下列责任人承担，马祥珲掌握验收结论及其在整体架构中的作用。

#### 李胤龙、元梓浩、张茗博：部署、自动化测试与演示恢复

重点掌握文件（文件级第一责任分配见 3.4 节）：

- 李胤龙：`backend/tools/SteamPlatform.HttpsDeploy/`、`backend/tests/SteamPlatform.HttpsDeploy.Tests/`、`docs/https-deployment-runbook.md`、`tests/SteamPlatform.Api.CloudTests/`。
- 元梓浩：`.github/workflows/ci.yml`、`frontend/playwright.config.ts`、`frontend/e2e/`、`frontend/scripts/`、`docs/playwright-regression-runbook.md`。
- 张茗博：`backend/tools/SteamPlatform.DemoData/`、`backend/tests/SteamPlatform.DemoData.Tests/`、`database/demo/manifest.json`。

责任边界：李胤龙负责 Nginx、ASP.NET Core、IP HTTPS、端口策略和云端健康检查；元梓浩负责 CI、前端构建、Playwright 与依赖审计；张茗博负责写库测试前后的备份恢复、基于 manifest 的演示数据清理重建和恢复审计。

### 3.4 全仓库文件责任覆盖

本节以 GitHub 主仓库最终版的 374 个跟踪文件为基线。课程提交包不含 `_archive` 等历史资料，因此文件总数少于主仓库。下表中的 `/**` 表示对应目录下的全部当前文件；每个文件设置一名第一责任人，共享业务文件按 3.5 节进一步划分方法级责任。仓库文件发生新增、删除或移动时，本节须同步更新。

| 第一责任人 | 完整文件范围 | 掌握重点 |
|---|---|---|
| 马祥珲 | `README.md`、`.gitignore`、`_archive/**`；`项目文档/Steam-数字游戏平台系统需求分析文档.docx`、`项目文档/Steam-数字游戏平台系统设计与实现文档.docx`、`项目文档/“Steam-”数字游戏平台系统答辩PPT.pptx`；`docs/README.md`、`docs/defense-demo-runbook.md`；`backend/README.md`、`backend/SteamPlatform.sln`、`backend/tests/SteamPlatform.Api.Tests/SteamPlatform.Api.Tests.csproj`、`backend/tests/SteamPlatform.Api.Tests/TestDoubles.cs`、`backend/tests/SteamPlatform.Api.Tests/CoreTransactionWorkflowRegressionTests.cs`；`frontend/src/views/RefundsView.vue`、`frontend/src/views/AdminRefundsView.vue`、`frontend/src/views/WalletRefundRequestView.vue` | 项目统筹、课程要求、文档统一、总体架构、退款审批与审计、核心事务回归和最终验收；掌握 `_archive/**` 的历史来源及其不参与当前构建的原因 |
| 李胤龙 | `backend/src/SteamPlatform.Api/Program.cs`、`backend/src/SteamPlatform.Api/Properties/**`、`backend/src/SteamPlatform.Api/appsettings*.json`、`backend/src/SteamPlatform.Api/SteamPlatform.Api.csproj`、`backend/src/SteamPlatform.Api/Infrastructure/**`；`backend/src/SteamPlatform.Application/Auth/**`、`backend/src/SteamPlatform.Application/Common/**`、`backend/src/SteamPlatform.Application/Diagnostics/**`、`backend/src/SteamPlatform.Application/SteamPlatform.Application.csproj`；`backend/src/SteamPlatform.Infrastructure/Auth/**`、`backend/src/SteamPlatform.Infrastructure/Data/**`、`backend/src/SteamPlatform.Infrastructure/DependencyInjection.cs`、`backend/src/SteamPlatform.Infrastructure/SteamPlatform.Infrastructure.csproj`；`backend/src/SteamPlatform.Shared/**`；`backend/src/SteamPlatform.Domain/SteamPlatform.Domain.csproj`；`backend/src/SteamPlatform.Api/Features/Auth/**`；`backend/tests/SteamPlatform.Api.Tests/Auth*Tests.cs`、`backend/tests/SteamPlatform.Api.Tests/EndpointGuardTests.cs`、`backend/tests/SteamPlatform.Api.Tests/ExceptionHandlingTests.cs`、`backend/tests/SteamPlatform.Api.Tests/PasswordHasherRegressionTests.cs`、`backend/tests/SteamPlatform.Api.Tests/ProtectedEndpointRegressionTests.cs`、`backend/tests/SteamPlatform.Api.Tests/HealthEndpointTests.cs`、`backend/tests/SteamPlatform.Api.Tests/UtcDateTimeJsonConverterTests.cs`；`backend/tools/SteamPlatform.HttpsDeploy/**`、`backend/tests/SteamPlatform.HttpsDeploy.Tests/**`、`tests/SteamPlatform.Api.CloudTests/**`、`docs/https-deployment-runbook.md` | 五层装配、认证、JWT、角色守卫、统一异常、Oracle 连接、HTTPS、后端部署和云端健康检查 |
| 元梓浩 | `.github/**`；`backend/src/SteamPlatform.Api/Features/Notices/**`、`backend/src/SteamPlatform.Application/Notices/**`、`backend/src/SteamPlatform.Domain/Notices/**`、`backend/src/SteamPlatform.Infrastructure/Notices/**`、`backend/tests/SteamPlatform.Api.Tests/Notice*Tests.cs`；`frontend/.editorconfig`、`frontend/.env.example`、`frontend/README.md`、`frontend/index.html`、`frontend/package.json`、`frontend/package-lock.json`、`frontend/tsconfig*.json`、`frontend/vite.config.ts`、`frontend/playwright.config.ts`、`frontend/e2e/**`、`frontend/scripts/**`；`frontend/src/App.vue`、`frontend/src/main.ts`、`frontend/src/router.ts`、`frontend/src/env.d.ts`、`frontend/src/stores/**`、`frontend/src/utils/**`、`frontend/src/api/http.ts`、`frontend/src/api/types.ts`、`frontend/src/components/PageState.vue`、`frontend/src/components/StatusBadge.vue`、`frontend/src/components/SteamInfoPanel.vue`、`frontend/src/views/LoginView.vue`、`frontend/src/views/RegisterView.vue`、`frontend/src/views/AccountView.vue`、`frontend/src/views/AdminNoticesView.vue`、`frontend/src/views/NotFoundView.vue`；`docs/playwright-regression-runbook.md` | 前端启动与构建、登录注册、会话恢复、路由权限、公告、公共请求、CI、Playwright 和依赖审计 |
| 周力扬 | `backend/src/SteamPlatform.Api/Features/Games/**`、`backend/src/SteamPlatform.Application/Games/**`、`backend/src/SteamPlatform.Infrastructure/Games/**`、`backend/tests/SteamPlatform.Api.Tests/Game*Tests.cs`；`frontend/src/api/games.ts`、`frontend/src/views/DeveloperGamesView.vue`、`frontend/src/views/AdminGamesView.vue`、`frontend/src/views/StoreView.vue`、`frontend/src/views/StoreCollectionView.vue`、`frontend/src/components/GameCard.vue`、`frontend/src/components/GameFilterBar.vue`、`frontend/src/data/gameCatalog.ts`；`database/migrations/20260708_developer_login_backend_completion.sql` | 开发商游戏 CRUD、所有权隔离、管理员上下架、公开可见性、商店列表、搜索筛选、集合页及对应迁移 |
| 王子轩 | `frontend/src/styles.css`、`frontend/src/views/GameDetailView.vue`；`frontend/src/components/GameHeroPanel.vue`、`frontend/src/components/GamePriceBlock.vue`、`frontend/src/components/GameSummarySection.vue`、`frontend/src/components/SteamGameDetailTemplate.vue`、`frontend/src/components/SteamMediaGallery.vue`、`frontend/src/components/Cs2DetailSections.vue`、`frontend/src/components/GenericGameDetailSections.vue`；`frontend/public/assets/games/**`、`frontend/public/assets/media/**` | 游戏详情、Steam 风格、响应式布局、图片/视频与加载兜底 |
| 胡知鱼 | `backend/src/SteamPlatform.Application/CoreTransactions/**`、`backend/src/SteamPlatform.Infrastructure/CoreTransactions/**`、`backend/tests/SteamPlatform.Api.Tests/CoreTransactionServiceGuardTests.cs`；`frontend/src/api/coreApi.ts`、`frontend/src/views/WalletView.vue`、`frontend/src/views/WalletRechargeCheckoutView.vue`、`frontend/src/views/WalletHistoryView.vue`、`frontend/src/views/WalletHistoryDetailView.vue`、`frontend/src/views/GameCheckoutView.vue`、`frontend/src/views/OrderDetailView.vue`；`database/migrations/20260712_wallet_payment_method_history.sql`、`tests/SteamPlatform.Database.Tests/GroupCSeedContractTests.cs` | 钱包、充值、购买、订单、资金流水、业务契约、事务实现及对应迁移；退款以及 CDKey 与授权方法见 3.5 节 |
| 徐京 | `backend/src/SteamPlatform.Api/Features/CoreTransactions/**`、`backend/tests/SteamPlatform.Api.Tests/CoreTransactionEndpointTests.cs`、`backend/tests/SteamPlatform.Api.Tests/CoreTransactionRepositoryGuardTests.cs`、`backend/tests/SteamPlatform.Api.Tests/InMemoryCoreTransactionService.cs`；`frontend/src/components/LibraryRail.vue`、`frontend/src/views/CdkeyBatchView.vue`、`frontend/src/views/RedeemView.vue`、`frontend/src/views/LibraryView.vue`、`frontend/src/views/GameLibraryView.vue` | 核心交易端点、CDKey 生成与兑换、免费入库、授权来源、游戏库状态和游玩时长；共享事务方法见 3.5 节 |
| 靳岱泽 | `backend/src/SteamPlatform.Api/Features/Social/**`、`backend/src/SteamPlatform.Api/Realtime/**`、`backend/src/SteamPlatform.Application/Social/**`、`backend/src/SteamPlatform.Domain/Social/**`、`backend/src/SteamPlatform.Infrastructure/Social/**`、`backend/tests/SteamPlatform.Api.Tests/Social*Tests.cs`；`backend/src/SteamPlatform.Api/Features/Engagement/**`、`backend/src/SteamPlatform.Application/Engagement/**`、`backend/src/SteamPlatform.Domain/Engagement/**`、`backend/src/SteamPlatform.Infrastructure/Engagement/**`、`backend/tests/SteamPlatform.Api.Tests/Engagement*Tests.cs`；`frontend/src/api/socialApi.ts`、`frontend/src/api/socialRealtime.ts`、`frontend/src/api/engagementApi.ts`、`frontend/src/views/CommunityHubView.vue`、`frontend/src/views/ProfileView.vue`；`database/migrations/20260825_social_realtime_foundation.sql`、`database/migrations/20260825_community_engagement_expansion.sql` | 玩家搜索、好友、私信、通知、资料、徽章、社区动态、讨论区、Oracle 持久化和 SignalR；交易报价方法见 3.5 节 |
| 郭炫君 | `backend/src/SteamPlatform.Api/Features/Inventory/**`、`backend/src/SteamPlatform.Application/Inventory/**`、`backend/src/SteamPlatform.Infrastructure/Inventory/**`、`backend/tests/SteamPlatform.Api.Tests/Inventory*Tests.cs`；`backend/src/SteamPlatform.Api/Features/Community/**`、`backend/src/SteamPlatform.Application/Community/**`、`backend/src/SteamPlatform.Domain/Community/**`、`backend/src/SteamPlatform.Infrastructure/Community/**`、`backend/tests/SteamPlatform.Api.Tests/Community*Tests.cs`；`frontend/src/api/inventoryApi.ts`、`frontend/src/api/communityApi.ts`、`frontend/src/data/achievementCatalog.ts`、`frontend/src/views/InventoryView.vue`、`frontend/src/views/GameCommunityView.vue`、`frontend/public/assets/items/**`、`frontend/public/assets/achievements/**`；`database/migrations/20260709_cs2_item_template_image_assets.sql`、`database/migrations/20260710_item_template_image_assets_by_game.sql`、`database/migrations/20260713_group_d_achievement_seed.sql`、`tests/SteamPlatform.Database.Tests/AchievementMigrationTests.cs` | 饰品模板与实例、掉落、库存状态、评价版本、成就、工坊订阅及对应迁移 |
| 张茗博 | `database/README.md`、`database/Oracle数据库导入与初始化说明.md`、`database/admin/**`、`database/data.sql`、`database/defense/**`、`database/demo/**`、`database/schema.sql`、`database/verify_defense.sql`、`database/verify_phase1.sql`、`database/migrations/20260825_demo_reset_audit.sql`；`tests/SteamPlatform.Database.Tests/DefenseScriptContractTests.cs`、`tests/SteamPlatform.Database.Tests/MigrationScriptConventionTests.cs`、`tests/SteamPlatform.Database.Tests/OracleSmokeTests.cs`、`tests/SteamPlatform.Database.Tests/README.md`、`tests/SteamPlatform.Database.Tests/SchemaContractTests.cs`、`tests/SteamPlatform.Database.Tests/SeedDataTests.cs`、`tests/SteamPlatform.Database.Tests/SqlFile.cs`、`tests/SteamPlatform.Database.Tests/SteamPlatform.Database.Tests.csproj`、`tests/SteamPlatform.Database.Tests/VerifyScriptTests.cs`；`tests/market-api.http`、`项目文档/“Steam-”数字游戏平台系统数据库设计文档.docx`、`docs/database-defense-runbook.md`；`backend/tools/SteamPlatform.DemoData/**`、`backend/tests/SteamPlatform.DemoData.Tests/**`；`backend/src/SteamPlatform.Api/Features/Market/**`、`backend/src/SteamPlatform.Application/Market/**`、`backend/src/SteamPlatform.Infrastructure/Market/**`、`backend/tests/SteamPlatform.Api.Tests/Market*Tests.cs`；`frontend/src/api/marketApi.ts`、`frontend/src/views/MarketView.vue`、`frontend/src/views/TradeOffersView.vue` | 45 表总体设计、全局 SQL/迁移规范/验证、演示数据恢复、市场撮合、交易报价、资产账本、执行计划和行锁 |

各业务测试的具体断言仍由对应模块成员共同掌握。以上规则覆盖根目录、`_archive`、后端、数据库、文档、前端、测试和正式项目文档，不存在无人负责或多人同时作为第一责任人的跟踪文件。

### 3.5 共享大文件的方法级责任

以下文件承载多个业务域，不能用“整个文件都归某一个人”代替方法级学习：

| 共享文件 | 方法或区域 | 第一责任人 |
|---|---|---|
| `backend/src/SteamPlatform.Infrastructure/CoreTransactions/CoreTransactionService.cs` | 钱包、充值、购买、订单及资金流水 | 胡知鱼 |
| `backend/src/SteamPlatform.Infrastructure/CoreTransactions/CoreTransactionService.cs` | `CreateRefundAsync`、`ListRefundsAsync`、`ListAllRefundsAsync`、`ApproveRefundAsync`、`RejectRefundAsync` 及退款审计事务 | 马祥珲 |
| `backend/src/SteamPlatform.Infrastructure/CoreTransactions/CoreTransactionService.cs` | 免费入库、游戏库、游玩时长、CDKey 生成与兑换 | 徐京 |
| `backend/src/SteamPlatform.Infrastructure/CoreTransactions/CoreTransactionService.cs` | 管理员退款批准/拒绝中的权限边界 | 李胤龙；退款交易与审计逻辑由马祥珲负责 |
| `backend/src/SteamPlatform.Api/Features/CoreTransactions/CoreTransactionEndpointExtensions.cs`、`backend/src/SteamPlatform.Application/CoreTransactions/CoreTransactionContracts.cs` | 与上面相同的端点和 DTO 分区 | 胡知鱼、徐京、马祥珲、李胤龙各自对应 |
| `backend/tests/SteamPlatform.Api.Tests/CoreTransactionWorkflowRegressionTests.cs` | 核心交易、授权、钱包与退款的跨模块回归 | 马祥珲 |
| `backend/src/SteamPlatform.Infrastructure/Games/GameRepository.cs` | 开发商 CRUD、所有权隔离、管理员状态修改 | 周力扬；李胤龙负责管理员权限解释 |
| `backend/src/SteamPlatform.Infrastructure/Games/GameRepository.cs` | 商店列表、详情、评价/成就/饰品概览查询 | 周力扬负责公开查询实现与列表需求，王子轩负责详情展示需求 |
| `backend/src/SteamPlatform.Infrastructure/Social/SocialRepository.cs`、`frontend/src/api/socialApi.ts` | 玩家搜索、好友、私信和通知 | 靳岱泽 |
| `backend/src/SteamPlatform.Infrastructure/Social/SocialRepository.cs`、`frontend/src/api/socialApi.ts` | 工坊浏览与订阅 | 郭炫君 |
| `frontend/src/views/CommunityHubView.vue` | 玩家搜索、好友、动态与讨论区域 | 靳岱泽 |
| `backend/src/SteamPlatform.Infrastructure/Engagement/EngagementRepository.cs`、`frontend/src/api/engagementApi.ts` | 资料、徽章、动态和讨论区 | 靳岱泽 |
| `backend/src/SteamPlatform.Infrastructure/Engagement/EngagementRepository.cs`、`frontend/src/api/engagementApi.ts` | 交易报价 | 张茗博 |
| `backend/tests/**`、`frontend/e2e/**` | 验收口径与结果汇总 | 马祥珲；测试配置与执行由对应文件责任人负责，业务断言由模块责任人掌握 |
| `database/schema.sql` | 全局结构、约束、索引 | 张茗博 |
| `database/schema.sql` | 各业务表含义与字段规则 | 对应业务责任人 |

### 3.6 每个人的学习验收标准

所有成员在正式答辩前均须通过以下知识验收：

1. 不看稿，在 90 秒内说明本模块的业务目标、核心表和关键约束。
2. 在 60 秒内从前端页面定位到 API、Application、Infrastructure 和 Oracle 表。
3. 解释一个正常流程、一个越权/重复操作失败流程和一个事务回滚场景。
4. 指出至少一个本模块的自动化测试，并说明它防止什么回归。
5. 能在云端演示本模块，且知道接口或网络失败时如何判断是前端、API 还是数据库问题。
6. 能够说明关键设计决策的依据、约束条件与取舍。

彩排时由其他成员随机从下列角度追问：权限、事务、并发、约束、索引、异常、测试、安全和可扩展性。任何一项答不上来，责任人必须回到上述文件补学，并在下一次彩排重新接受提问。

### 3.7 现场提问转交规则

1. 马祥珲听完老师问题后，只在问题归属不清时简短复述并点名责任人。
2. 被点名者先用一句话给结论，再解释代码路径和数据库依据，控制在 30 至 60 秒。
3. 跨业务与数据库的问题由业务责任人先答，张茗博再补充表、约束、索引或事务证据。
4. 总体验收与跨模块回归由马祥珲回答；HTTPS 与后端部署由李胤龙回答，CI 与 Playwright 由元梓浩回答，演示数据恢复由张茗博回答。
5. 其他成员不得抢答或给出不同口径；发现表述遗漏时先由马祥珲邀请补充。
6. 马祥珲负责最终收束，确保回答与 README、课程要求和既定技术路线一致。

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
| Klei CDKey 批次 | 批次号 `DST-DEFENSE-LIVE`，数量 1，当前时间生效，30 天后过期 |
| 玩家甲充值与购买 | 充值 `60.00`，购买 DST `24.00` |
| 玩家乙市场充值 | `150.00` |
| 玩家甲饰品售价 | `49.00` |
| 市场平台费率 | 5%，手续费 `2.45`，卖方实收 `46.55` |

## 5. 答辩前 30 分钟检查

张茗博执行以下工作：

1. 运行演示恢复工具 `reset`，保存运行编号。
2. 确认 `/api/health` 和 `/health/database` 均返回 `OK`。
3. 确认 `steam-platform-api`、Nginx 和证书续期 timer 为 active，failed unit 数量为 0。
4. 确认 Oracle 只监听服务器回环地址，公网未开放 1521。
5. 打开 Oracle 只读查询、总验收结果、执行计划和行锁证据。
6. 准备健康接口、21 项验收汇总、三组代表性查询和关键执行计划的静态截图，仅在公网异常时使用。

徐京、王子轩、李胤龙、胡知鱼、靳岱泽、张茗博执行以下工作：

1. 所有浏览器缩放保持 100%，关闭无关扩展、通知和悬浮窗口。
2. 徐京、王子轩、李胤龙停留在各自登录页，不提前提交业务。
3. 胡知鱼、靳岱泽停留在注册页。
4. 每台电脑确认 HTTPS 页面可访问，中文、图片和视频正常。
5. 徐京的电脑准备一个仅本机可见的临时文本框，用于保存本轮生成的 CDKey；不得把 CDKey 写入 Git 或公开文档。

马祥珲最后新建一个无痕或 InPrivate 浏览器窗口，并从根路径 `/` 打开正式站点。该窗口不得提前关闭启动公告；新会话必须不存在 `game-deck-startup-announcement-dismissed:2026-08-25` 这一 `sessionStorage` 标记，以确保 0:00 时展示商店首页及其公告浮窗。

马祥珲完成一次腾讯会议单路共享顺序检查：马祥珲 → 徐京 → 王子轩 → 李胤龙 → 胡知鱼 → 靳岱泽 → 胡知鱼 → 靳岱泽 → 胡知鱼 → 靳岱泽 → 胡知鱼 → 靳岱泽 → 胡知鱼 → 靳岱泽 → 胡知鱼 → 李胤龙 → 胡知鱼 → 张茗博 → 马祥珲。每次箭头都表示前一人已经停止共享、后一人才开始共享。

## 6. 20 分钟精确时间轴

| 时间 | 操作电脑 | 内容 | 目标结束时间 |
|---|---|---|---|
| 0:00-0:50 | 马祥珲 | 商店首页与启动公告、项目定位、技术路线、五层结构 | 0:50 |
| 0:50-2:05 | 徐京 | Klei 创建 `Survival Lab` 和 1 个 DST CDKey | 2:05 |
| 2:05-2:55 | 王子轩 | Valve 证明隔离并创建 `Tactical Arena Lab` | 2:55 |
| 2:55-3:45 | 李胤龙 | 管理员只上架 `Survival Lab` | 3:45 |
| 3:45-4:10 | 胡知鱼 | 玩家甲注册并确认 PLAYER 身份 | 4:10 |
| 4:10-4:35 | 靳岱泽 | 玩家乙注册并预先打开好友页面 | 4:35 |
| 4:35-4:55 | 胡知鱼 | 搜索玩家乙并发送好友请求 | 4:55 |
| 4:55-5:20 | 靳岱泽 | 接受请求并确认好友关系 | 5:20 |
| 5:20-5:55 | 靳岱泽 | 玩家乙免费领取 CS2 并掉落饰品 | 5:55 |
| 5:55-6:40 | 胡知鱼 | 验证商店审核结果，免费领取 CS2、掉落并记录待售饰品 | 6:40 |
| 6:40-7:45 | 胡知鱼 | 充值 60 元并以 24 元购买 DST | 7:45 |
| 7:45-8:45 | 靳岱泽 | 兑换并重复兑换 DST CDKey | 8:45 |
| 8:45-9:05 | 靳岱泽 | 打开与玩家甲的聊天窗口并停止共享，保持页面在线 | 9:05 |
| 9:05-9:40 | 胡知鱼 | 发送消息并发表 DST 评测 | 9:40 |
| 9:40-10:15 | 靳岱泽 | 展示未刷新收到消息、重载持久化消息、解锁成就并订阅工坊 | 10:15 |
| 10:15-11:05 | 胡知鱼 | 上架本轮 CS2 饰品 | 11:05 |
| 11:05-12:25 | 靳岱泽 | 充值、购买饰品并核对买方钱包与库存 | 12:25 |
| 12:25-13:20 | 胡知鱼 | 核对卖方流水与物品流转并提交 DST 退款 | 13:20 |
| 13:20-13:55 | 李胤龙 | 管理员审核通过退款 | 13:55 |
| 13:55-14:40 | 胡知鱼 | 核对退款、订单终态、余额和授权撤销 | 14:40 |
| 14:40-17:20 | 张茗博 | Oracle 数据、索引、执行计划与行锁证据 | 17:20 |
| 17:20-18:40 | 马祥珲 | 安全、工程质量、团队协作与总体验收 | 18:40 |
| 18:40-19:25 | 马祥珲 | 总结 | 19:25 |
| 19:25-20:00 | 全员 | 网络延迟、切屏或老师打断缓冲 | 20:00 |

硬性时间控制：10:15 必须结束社区环节，14:40 必须结束业务页面操作，17:20 必须离开 Oracle 证据。每次共享交接控制在 5 秒内；累计延迟达到 30 秒时，张茗博只展示总验收结论、玩家资金/授权查询和市场成交查询，执行计划与双会话锁改用预先保存的本轮截图；不得压缩最后的总结，也不得为了补一个次要页面让总时长超过 20 分钟。

## 7. 逐步演示与讲解词

本节是正式演示脚本。所有成员按“操作 → 等待结果 → 指向证据 → 讲解”的顺序执行。引号中的内容可直接照念；方括号中的内容是动作提示，不需要读出。除马祥珲负责开场、串场和总结外，业务讲解均由当前屏幕的实际操作者完成。出现加载状态时先等待成功提示，不连续点击按钮，也不临时补充本节之外的功能。

### 7.1 开场与架构，0:00-0:50，马祥珲

1. [保持投影在马祥珲电脑；从正式 HTTPS 根路径 `/` 打开网站。等待商店首页和启动公告浮窗同时出现。]

   马祥珲说：

   > 老师好，本项目是部署在腾讯云上的“Steam-数字游戏平台系统”。根路径进入商店，浮窗来自 Oracle 公告数据。

2. [用鼠标依次指向公告图片、标题、轮播圆点和右上角关闭按钮，不点击“点击查看详细信息”；随后点击右上角关闭按钮。]

   马祥珲说：

   > 公告支持轮播和有效期。现在关闭浮窗，继续演示真实业务。

3. [切到准备好的系统架构图，鼠标沿“浏览器 → Nginx → ASP.NET Core → Oracle”方向移动。]

   马祥珲说：

   > 系统采用 B/S 架构：Vue 经 HTTPS 和 Nginx 访问 .NET 10 后端，数据进入 Oracle；后端按 Api、Application、Domain、Infrastructure、Shared 五层组织。下面演示完整业务生命周期。

4. [说完下列交接语后，马祥珲停止共享。]

   马祥珲只说：

   > 下面由徐京演示 Klei 开发商。

### 7.2 Klei 提交内容与 CDKey，0:50-2:05，徐京

1. [徐京开始共享；登录页角色选择“开发商”，账号输入 `klei@example.com`，密码输入 `klei`，点击“登录”。]

   徐京说：

   > 我使用 Klei 开发商账号登录，开发商身份由 JWT 确定。

2. [展开顶部“管理”，点击“游戏管理”。等待“开发商游戏管理”页面加载；指向“当前开发商”和“我的游戏”中的 DST。]

   徐京说：

   > 页面只返回当前开发商的游戏。这里能看到 Klei 的 DST，看不到 Valve 的 CS2。

3. [在“新建游戏”表单依次填写：游戏名称 `Survival Lab`，原价 `68.00`，折扣系数 `0.80`，发行日期选择当天，口碑保持“暂无口碑”；指一下价格预览，然后点击“创建游戏”。]

   徐京边填边说：

   > 我提交临时游戏，原价 68 元、折扣系数 0.80，开发商不能指定公开状态。

4. [等待“游戏已创建”成功提示；在“我的游戏”中找到 `Survival Lab`，指向“已下架”状态。]

   徐京说：

   > 创建后状态固定为 OFFLINE。Oracle 已新增游戏，但必须经管理员审核才能公开。

5. [再次展开“管理”，点击“CDKey 批次”。确认“游戏”为饥荒联机版；把“批次号”改为 `DST-DEFENSE-LIVE`，数量改为 `1`，生效时间和过期时间保留页面默认值；点击“生成 CDKey”。]

   徐京说：

   > 现在为 DST 生成一个兑换码，当前生效，三十天后过期。

6. [等待“本次生成结果”和“明文只展示一次”出现；复制唯一的 CDKey 到本机临时文本框，并通过私下消息发送给靳岱泽。]

   徐京说：

   > 明文只展示一次，Oracle 长期保存哈希。本次新增批次和 CDKey，稍后由玩家乙兑换。

7. [把页面停在生成结果，不再生成第二批；说完下列交接语后停止共享。]

   徐京说：

   > Klei 操作完成，下面切换到 Valve。

### 7.3 Valve 证明开发商隔离，2:05-2:55，王子轩

1. [王子轩开始共享；登录页角色选择“开发商”，账号输入 `valve@example.com`，密码输入 `valve`，点击“登录”；展开“管理”，点击“游戏管理”。]

   王子轩说：

   > 我使用 Valve 账号登录。列表中只有 Valve 的 CS2，看不到 Klei 的 `Survival Lab`。

2. [快速滚动“我的游戏”列表确认不存在 `Survival Lab`，然后回到“新建游戏”表单。依次填写：游戏名称 `Tactical Arena Lab`，原价 `88.00`，折扣系数 `0.90`，发行日期为当天，口碑保持“暂无口碑”；点击“创建游戏”。]

   王子轩边操作边说：

   > 我提交第二款临时游戏。后端查询和修改都校验 `dev_id`，Valve 无法管理 Klei 数据。

3. [等待成功提示，在列表中指向 `Tactical Arena Lab` 的“已下架”状态。]

   王子轩说：

   > 第二款游戏同样默认下架，公开权统一交给管理员。

4. [说完下列交接语后，王子轩停止共享。]

   王子轩说：

   > 下面由管理员选择性审核。

### 7.4 管理员选择性上架，2:55-3:45，李胤龙

1. [李胤龙开始共享；登录页角色选择“管理员”，账号输入 `rootadmin`，密码输入 `admin`，点击“登录”。]

   李胤龙说：

   > 我使用管理员账号登录，负责平台级审核。

2. [展开顶部“管理”，点击“游戏上下架”；在状态筛选中点击“已下架”。]

   李胤龙说：

   > “已下架”列表显示两家开发商刚提交、尚未公开的游戏。

3. [在 `Survival Lab` 行点击“上架”，等待“Survival Lab 已上架”提示；不要操作 `Tactical Arena Lab`。]

   李胤龙说：

   > 我只上架 `Survival Lab`；`Tactical Arena Lab` 保持下架。上下架接口只允许 ADMIN 调用。

4. [保持管理员登录，不退出；说完下列交接语后停止共享。]

   李胤龙说：

   > 内容维护与审核相互分离。管理员保持登录，稍后继续审核退款。

### 7.5 两名玩家串行注册并成为好友，3:45-5:20，胡知鱼、靳岱泽

1. [胡知鱼开始共享，在注册页填写账号 `defense_p1`、昵称“答辩玩家甲”、密码 `Demo123456`，点击“注册”；等待跳转到“账户”页面，指向账号和 PLAYER 角色。]

   胡知鱼说：

   > 我现场注册玩家甲。公开注册只能创建 PLAYER 角色，注册事务还会创建一对一的钱包账户，初始余额为零。

2. [胡知鱼说“玩家甲注册完成”，停止共享。马祥珲确认画面消失后，请靳岱泽开始共享。靳岱泽填写账号 `defense_p2`、昵称“答辩玩家乙”、密码 `Demo123456`，点击“注册”；等待跳转并确认 PLAYER 角色，然后进入“社区 → 寻找玩家”。]

   靳岱泽说：

   > 我再注册玩家乙。两个账号均为本轮新数据，没有使用预置 Alice 和 Bob；我先把好友页面准备好。

3. [靳岱泽停止共享。胡知鱼开始共享，进入“社区 → 寻找玩家”；在“搜索玩家”中输入“答辩玩家乙”，等待搜索结果，点击“添加好友”。]

   胡知鱼说：

   > 玩家乙已经完成注册，所以我现在能够搜索到他。发送请求后，好友关系先进入 PENDING。

4. [胡知鱼停止共享。靳岱泽开始共享；在右侧“待处理请求”找到“答辩玩家甲”，点击接受图标或在搜索结果中点击“接受”；刷新一次或重新搜索玩家甲，指向“已经是好友”。]

   靳岱泽说：

   > 玩家乙接受请求后，关系变为 ACCEPTED。刷新后仍显示“已经是好友”，证明关系保存在 Oracle，而不是只存在浏览器里。

5. [靳岱泽保持共享，直接进入下一阶段。]

### 7.6 商店审核结果、CS2 免费入库与掉落，5:20-6:40，靳岱泽、胡知鱼

1. [靳岱泽点击顶部“商店”，搜索 `Counter-Strike 2`，进入详情页，点击绿色“免费入库”；等待页面显示已加入游戏库或按钮变为“开始游戏”。]

   靳岱泽说：

   > 玩家乙免费领取 CS2。系统记录零元订单、支付和游戏库授权，但钱包余额不变。

2. [靳岱泽点击顶部“库存”，确认游戏分类为 CS2；只点击一次“模拟掉落”，在浏览器确认框中选择确定，并等待“已获得……”成功提示。]

   靳岱泽说：

   > 玩家乙获得一件独立饰品。掉落从模板生成唯一实例，并保存所有者、磨损率和状态。

3. [靳岱泽说“玩家乙操作完成”，停止共享。胡知鱼开始共享，点击顶部“商店”；在搜索框输入 `Survival Lab` 展示结果，随后改为 `Tactical Arena Lab`，确认没有结果。]

   胡知鱼说：

   > `Survival Lab` 已进入公开商店，未审核的 `Tactical Arena Lab` 不可见，公开查询只返回 ONLINE 游戏。

4. [胡知鱼搜索 `Counter-Strike 2`，进入详情页并点击绿色“免费入库”；等待成功后进入“库存”，只点击一次“模拟掉落”，确认获得新饰品。]

   胡知鱼说：

   > 玩家甲也完成免费入库并获得一件独立饰品。两名玩家的授权、钱包和物品所有权互不混用。

5. [胡知鱼选中带有“这是刚刚模拟掉落获得的新饰品”提示的物品；把页面中的“饰品实例”和“图案模板”复制到本机临时记录，并私下发送给靳岱泽、张茗博。]

   胡知鱼说：

   > 我记录 `item_id` 和 `template_id`。市场按模板聚合，成交按唯一实例转移。

6. [胡知鱼保持共享，直接进入下一阶段。]

### 7.7 DST 的购买授权与 CDKey 授权，6:40-8:45，胡知鱼、靳岱泽

1. [胡知鱼点击顶部“钱包”，在充值金额列表找到 60 元一行并点击“充值”；在“支付方式”页明确选择“微信支付”，核对充值金额 60 元后点击“确认充值”。]

   胡知鱼说：

   > 玩家甲通过模拟微信支付充值 60 元，系统同时更新余额并写入充值流水。

2. [等待“充值成功，当前钱包余额 ¥60.00”提示；点击“商店”，打开饥荒联机版详情；指向原价、折扣和折后价 24 元，点击“购买游戏”。]

   胡知鱼说：

   > DST 原价 48 元，折扣系数 0.50，应付 24 元。

3. [在“复核并购买”页确认购入账户是 `defense_p1`、合计 24 元；支付方式选择“Steam 钱包”，点击“支付”，等待成功后跳转。]

   胡知鱼说：

   > 使用 Steam 钱包支付。订单、明细、支付、扣款流水和游戏库授权在同一事务中完成。

4. [打开顶部账号菜单中的“购买历史”或进入“钱包 → 查看我的账户明细”；指向 `+60.00` 充值和 `-24.00` 购买两条记录，再展示当前余额 `36.00`；点击顶部“库”，确认 CS2 与 DST 都在库中。]

   胡知鱼说：

   > 流水显示加 60、减 24，余额 36 元；游戏库已有 CS2 和 BUY 来源的 DST。

5. [胡知鱼说“玩家甲购买完成”，停止共享。马祥珲确认后请靳岱泽开始共享。靳岱泽点击底部“添加游戏”或顶部“激活产品”，把徐京私下发送的 CDKey 粘贴到“在此处输入您的产品代码”，点击“确认”。]

   靳岱泽说：

   > 玩家乙不购买，直接兑换 CDKey。校验哈希、有效期和状态后写入 REDEEM 授权。

6. [等待成功提示；打开“库”，确认 DST 已出现，再进入钱包确认余额仍为 0。]

   靳岱泽说：

   > DST 已入库，来源为 REDEEM，钱包仍为零。

7. [再次进入“激活产品”，粘贴同一个 CDKey，点击“确认”；指向“您已经拥有该游戏”或结果 `REDEEMED`。]

   靳岱泽说：

   > 重复提交不会生成第二份权益，并在兑换日志中保留 REDEEMED 结果。

### 7.8 好友聊天、评测、成就与工坊，8:45-10:15，靳岱泽、胡知鱼、靳岱泽

1. [靳岱泽保持共享，点击页面右下角“好友与聊天”，在好友列表中选择“答辩玩家甲”，确认聊天窗口已打开且 SignalR 连接正常；不要刷新，保持窗口打开。]

   靳岱泽说：

   > 玩家乙先打开与玩家甲的聊天窗口并保持在线。现在我停止共享，但浏览器和实时连接继续运行。

2. [靳岱泽停止共享。胡知鱼开始共享，打开“好友与聊天”，选择“答辩玩家乙”，输入“答辩实时消息：Oracle 与 SignalR 已贯通”，点击发送图标；等待自己的消息出现在聊天记录中。]

   胡知鱼说：

   > 消息先写入 `DIRECT_MESSAGE`，事务提交后再由 SignalR 推送给在线好友。发送完成后再切换画面，不需要两个人同时共享。

3. [胡知鱼关闭聊天浮层，进入“库 → 饥荒联机版 → 社区中心”，选择“评测”；保持“推荐”，输入“联机生存体验完整，推荐和好友一起游玩。”，点击“发表评测”，等待“评价已发表”。]

   胡知鱼说：

   > 玩家甲拥有 BUY 来源的 DST，后端校验有效授权后保存评测及版本记录。

4. [胡知鱼停止共享。靳岱泽开始共享；不要刷新页面，直接指向已经出现在原聊天窗口中的消息和通知。]

   靳岱泽说：

   > 玩家乙的页面在未刷新时已经收到消息，证明 SignalR 实时推送有效；腾讯会议只是在两台电脑之间串行切换画面。

5. [靳岱泽关闭并重新打开“好友与聊天”，或刷新页面后重新选择玩家甲；确认消息仍存在。]

   靳岱泽说：

   > 重新载入后消息仍在，证明 Oracle 负责持久化；即使实时连接中断，历史消息也不会丢失。

6. [进入 DST“社区中心”，点击“成就”，找到 `First Night Together` 并点击“解锁”；然后点击“创意工坊”，搜索“自动整理箱”，点击“订阅”；等待成功提示后把排序改为“我的订阅”或刷新页面，确认仍显示该作品。]

   靳岱泽说：

   > 玩家乙有 REDEEM 授权，可以解锁项目自定义成就；唯一约束防止重复解锁。工坊订阅刷新后仍存在，说明订阅状态也由 Oracle 持久化。

### 7.9 两名新玩家串行完成 CS2 饰品交易，10:15-12:25，胡知鱼、靳岱泽

本流程采用“卖方先挂单、买方立即购买”，不使用预置 Alice/Bob 订单，也不使用可能匹配到其他模板的全局撮合按钮。

1. [靳岱泽停止共享。胡知鱼开始共享，返回“库存”，选择刚才记录的 CS2 饰品实例，确认状态为 `NORMAL`；点击“出售”。]

   胡知鱼说：

   > 我选择本轮掉落的唯一实例。只有所有者能出售 NORMAL 状态物品。

2. [在“出售库存物品”窗口再次核对“饰品实例”和“图案模板”；“出售价格”输入 `49.00`，点击“确认出售”。]

   胡知鱼说：

   > 我以 49 元创建卖单，系统校验所有权并把实例改为 IN_MARKET。

3. [等待“已为……创建出售挂单”提示；重新选中该物品，指向状态 `IN_MARKET`；把物品名称和 `template_id` 告诉靳岱泽。]

   胡知鱼说：

   > IN_MARKET 状态防止重复出售；市场按模板展示，成交精确到实例。

4. [胡知鱼说“卖单已经创建”，停止共享。靳岱泽开始共享，进入“钱包”，选择 150 元一行的“充值”，支付方式选择“微信支付”，点击“确认充值”；等待余额变为 150 元。]

   靳岱泽说：

   > 玩家乙充值 150 元，稍后直接核对市场扣款。

5. [点击顶部“社区市场”；保持“游戏内物品”，在“筛选结果...”输入胡知鱼提供的物品名称或 `template_id`；等待只出现对应模板。]

   靳岱泽说：

   > 市场按模板聚合同类物品，我用模板编号精确搜索。

6. [点击搜索结果卡片或价格按钮；在“社区市场交易”窗口指向“当前最低售价”49 元和“将以当前最低售价立即购买一件在售物品”。]

   靳岱泽说：

   > 当前最低卖价是 49 元，系统将按价格和时间优先立即撮合。

7. [点击“立即购买”一次，等待成功提示；不要重复点击。]

   靳岱泽说：

   > 撮合事务同时处理订单、钱包、手续费、成交、流水和所有权，失败则整体回滚。

8. [依次点击市场顶部“市场交易记录”，找到刚完成的成交；打开钱包确认余额 101 元；打开库存确认收到同一个饰品实例。]

   靳岱泽说：

   > 成交价 49 元，余额变为 101 元；库存收到的是同一个实例。

预期结果：

```text
成交价                       49.00
平台费 49.00 x 5%            2.45
玩家甲实收                  46.55
玩家甲成交后余额 36 + 46.55 82.55
玩家乙成交后余额 150 - 49   101.00
玩家乙冻结余额                0.00
```

9. [靳岱泽说“买方核对完成”，停止共享。]

### 7.10 玩家退款与管理员审核，12:25-14:40，胡知鱼、李胤龙、胡知鱼

1. [胡知鱼开始共享，打开原饰品的“饰品流转记录”，点击“更新”；确认记录显示由玩家甲转移到玩家乙，再打开钱包确认余额为 82.55 元。]

   胡知鱼说：

   > 平台费 2.45 元，玩家甲实收 46.55 元，余额变为 82.55 元；流转记录证明该实例已转给玩家乙。

2. [胡知鱼点击顶部“客服”或窗口菜单“帮助”，进入“退款申请”；在“选择订单”中选择本轮 DST 订单，确认可退金额 24 元；保留默认退款原因“体验不符合预期，申请课程演示退款。”，点击“提交退款”。]

   胡知鱼说：

   > 玩家只能对自己的可退款订单申请。提交后状态为 PENDING，钱包和授权暂不改变。

3. [等待“退款申请已提交”提示；在“我的退款单”中指向最新记录的订单号、金额 24 元和状态 PENDING。]

   胡知鱼说：

   > 申请已写入 Oracle，金额来自订单明细。下面由管理员审核。

4. [胡知鱼说“退款申请已经提交”，停止共享。李胤龙开始共享；展开“管理”，点击“退款审核”，点击“刷新”，找到最新一张金额 24 元、状态 PENDING 的退款单；核对订单号后点击“通过”。]

   李胤龙说：

   > 管理员核对退款号、订单、金额和状态，玩家不能调用此接口。

5. [等待“退款单……已更新为 APPROVED”提示；指向该行状态 APPROVED，确认“通过”和“拒绝”按钮已不可再次操作。]

   李胤龙说：

   > 审批事务回补 24 元，更新支付和订单，撤销 BUY 授权并写审计日志；重复审批不会重复入账。

6. [李胤龙说“管理员审批完成”，停止共享。胡知鱼开始共享；刷新钱包，确认余额 106.55 元；进入“购买历史”打开本轮 DST 交易，确认 CLOSED / REFUNDED；打开“库”，确认玩家甲的 DST 已不再作为正常授权显示。]

   胡知鱼说：

   > 玩家甲余额变为 106.55 元，订单与支付保留终态，BUY 权益被撤销；玩家乙的 REDEEM 权益不受影响。

预期结果：

```text
玩家甲退款前余额              82.55
退款入账                      24.00
玩家甲最终余额               106.55
订单业务状态                  CLOSED
订单支付状态               REFUNDED
退款单状态                 APPROVED
玩家甲 DST 授权       REVOKED / BUY
玩家乙 DST 授权      NORMAL / REDEEM
```

退款模块的知识责任人是马祥珲；正式演示中的页面操作和上述讲解仍按胡知鱼 → 李胤龙 → 胡知鱼的顺序完成。老师在演示后追问事务实现、幂等或跨模块回归时，再由马祥珲回答代码与测试细节。

7. [胡知鱼说“业务页面演示完成”，停止共享。]

### 7.11 Oracle 专项证据，14:40-17:20，张茗博

张茗博使用只读查询按以下顺序展示：

1. Klei 与 Valve 各自拥有对应固定游戏和临时提交；固定样板 CS2、DST 保持 ONLINE，两款临时提交中只有 `Survival Lab` 为 ONLINE。
2. 两名临时玩家、好友关系和聊天消息均存在。
3. 玩家甲最终可用余额 106.55；玩家乙最终可用余额 101.00、冻结余额 0。
4. 玩家甲 DST 权益为 REVOKED；玩家乙 DST 权益为 NORMAL 且 `acquire_way = REDEEM`。
5. 本轮市场成交价格、手续费、买卖双方和 `item_id` 与页面一致。
6. `ITEM_TRANSFER_LEDGER` 显示该实例从玩家甲转移到玩家乙。
7. `CDKEY_REDEEM_LOG` 同时存在成功和重复兑换结果。
8. 45 张表、45 个主键、至少 49 个业务索引，无禁用约束、无效索引或无效对象。
9. 订单、市场和讨论查询分别使用对应业务索引。
10. 双会话钱包行锁采用有限等待并安全回滚，不改变业务数据。

张茗博只展示结论和三组代表性结果，不滚动大量 SQL 输出。正式答辩前把下列查询保存在同一只读 SQL 工作表中，现场只执行选中的语句；不得临时手敲表名或账号。

现场按以下顺序操作和讲解：

1. [张茗博开始共享。先展示已经打开的 `/api/health` 与 `/health/database` 两个页面，确认服务和 Oracle 均为 `OK`；随后切到预先连接好的 Oracle 只读 SQL 工作表，连接口令不得出现在投影中。]

   张茗博说：

   > 页面操作已经形成真实数据。两个健康接口均为 OK，下面用只读 SQL 从 Oracle 侧交叉验证。

2. [展示本轮已经执行完成的 `database/verify_defense.sql` 结果页，只停留在 PASS 汇总，不从头滚动脚本。]

   张茗博说：

   > 21 项总验收全部通过：45 张表、45 个主键、至少 49 个业务索引；禁用约束、无效对象和跨表金额错误均为零。

3. [执行下方第 1 组查询；用鼠标依次指向公司名、游戏名和状态列。]

   张茗博说：

   > 第一组验证开发商归属和审核：`Survival Lab` 在线，`Tactical Arena Lab` 下架，与页面一致。

4. [执行第 2 组查询；先指玩家甲一行，再指玩家乙一行，最后指好友状态、消息数量和 CDKey 结果。]

   张茗博说：

   > 第二组验证玩家闭环：玩家甲余额 106.55 元、BUY 权益已撤销、退款已批准；玩家乙余额 101 元、REDEEM 权益正常。好友、消息和 CDKey 日志也都存在。

5. [执行第 3 组查询；指向卖方、买方、`item_id`、成交价、手续费和账本方向。]

   张茗博说：

   > 第三组验证同一饰品：玩家甲卖给玩家乙，成交 49 元、平台费 2.45 元，成交和流转账本的 `item_id` 相同。

6. [打开预先保存的三张执行计划截图或结果页，依次指向订单、市场、讨论查询使用的索引；再打开双会话钱包行锁证据，只展示有限等待和回滚结果。]

   张茗博说：

   > 订单、市场和讨论查询均命中相应索引。双会话行锁采用有限等待并在验证后回滚，可防止钱包并发超扣且不污染数据。

7. [停在三组代表性结果的汇总页；说完下列交接语后停止共享。]

   张茗博说：

   > 数据库证据与页面一致。下面进行最终总结。

```sql
-- 1. 两家开发商、固定样板与两款临时提交
select d.company_name, g.game_name, g.status, g.base_price, g.discount_rate
  from developer d
  join game g on g.dev_id = d.dev_id
 where d.contact_email in ('klei@example.com', 'valve@example.com')
   and (g.game_id in ('GAME_CS2', 'GAME_DST')
        or g.game_name in ('Survival Lab', 'Tactical Arena Lab'))
 order by d.company_name, g.game_name;

-- 2. 两名玩家的余额、DST 权益、订单/支付/退款终态与 CDKey 结果
select p.account,
       w.available_balance,
       w.frozen_balance,
       pl.status as dst_library_status,
       pl.acquire_way as dst_acquire_way,
       max(case when od.detail_id is not null then go.order_status end) as dst_order_status,
       max(case when od.detail_id is not null then go.payment_status end) as dst_payment_status,
       max(rt.status) as dst_refund_status,
       (select listagg(crl.result, ',') within group (order by crl.create_time, crl.log_id)
          from cdkey_redeem_log crl
         where crl.user_id = p.user_id) as cdkey_results,
       (select max(fr.status)
          from friend_relation fr
          join player low_player on low_player.user_id = fr.user_low_id
          join player high_player on high_player.user_id = fr.user_high_id
         where low_player.account in ('defense_p1', 'defense_p2')
           and high_player.account in ('defense_p1', 'defense_p2')) as friend_status,
       (select count(*)
          from direct_message dm
          join friend_relation fr on fr.relation_id = dm.relation_id
          join player low_player on low_player.user_id = fr.user_low_id
          join player high_player on high_player.user_id = fr.user_high_id
         where low_player.account in ('defense_p1', 'defense_p2')
           and high_player.account in ('defense_p1', 'defense_p2')) as message_count
  from player p
  join wallet_account w on w.user_id = p.user_id
  left join player_library pl
    on pl.user_id = p.user_id and pl.game_id = 'GAME_DST'
  left join game_order go on go.user_id = p.user_id
  left join order_detail od
    on od.order_id = go.order_id and od.game_id = 'GAME_DST'
  left join refund_ticket rt on rt.order_id = go.order_id
 where p.account in ('defense_p1', 'defense_p2')
 group by p.user_id, p.account, w.available_balance, w.frozen_balance,
          pl.status, pl.acquire_way
 order by p.account;

-- 3. 本轮 CS2 成交与同一物品的资产转移
select tr.trade_id,
       seller.account as seller_account,
       buyer.account as buyer_account,
       tr.item_id,
       tr.trade_price,
       tr.platform_fee,
       from_player.account as ledger_from,
       to_player.account as ledger_to
  from market_trade tr
  join player seller on seller.user_id = tr.seller_id
  join player buyer on buyer.user_id = tr.buyer_id
  join item_transfer_ledger itl
    on itl.item_id = tr.item_id and itl.transfer_type = 'TRADE'
  left join player from_player on from_player.user_id = itl.from_user_id
  join player to_player on to_player.user_id = itl.to_user_id
 where seller.account = 'defense_p1'
   and buyer.account = 'defense_p2'
 order by tr.trade_time desc
 fetch first 1 row only;
```

第一组结果应证明开发商隔离和审核状态；第二组应显示玩家甲 `106.55 / 0 / REVOKED / BUY / CLOSED / REFUNDED / APPROVED`，玩家乙 `101.00 / 0 / NORMAL / REDEEM` 且 CDKey 结果包含 `SUCCESS,REDEEMED`，两行的好友状态均为 `ACCEPTED`、消息数至少为 1；第三组应显示成交价 `49.00`、手续费 `2.45`，账本方向为玩家甲到玩家乙。任何一项不符时不得继续照念预设结论，应先按第 9 节故障预案处理。表/约束/索引、执行计划和行锁证据使用 `database/verify_defense.sql`、`database/defense/` 与 `docs/database-defense-runbook.md`。

### 7.12 安全、工程质量与总结，17:20-19:25，马祥珲

1. [马祥珲开始共享，打开事先准备好的安全与部署汇总页；页面只显示架构、开放端口和关键安全项，不展示任何密码、私钥或连接串。]

   马祥珲说：

   > JWT 验证签发者、接收方、签名和有效期。资金与资产接口从令牌读取主体，不信任前端传入的用户 ID。

2. [指向网络拓扑中的 22、80、443 和仅回环监听的 1521；再指向 Nginx 安全响应头清单。]

   马祥珲说：

   > 公网只开放 22、80、443；Oracle 1521 和后端端口不公开。Nginx 提供 HTTPS、HSTS、CSP 和防 iframe 策略。

3. [切到 GitHub 仓库的分支保护和最近一次通过的 `verify` 工作流；不要打开任何包含密钥的本地目录。]

   马祥珲说：

   > 普通组员通过功能分支、PR、审查和 CI 合并代码。私钥与本地配置不入库，`_archive` 只保存团队历史文件。

4. [切到准备好的验收汇总页，依次指向四组数字。]

   马祥珲说：

   > 本轮 .NET 测试 381 项全部通过，腾讯云 Playwright 26 项全部通过，Oracle 总验收 21 项全部通过，npm 审计为零漏洞；写库测试前后均成功恢复演示基线。

5. [回到项目总览页或商店首页，停止切换页面，面向老师完成总结。]

   马祥珲照念：

   > 我们用两名新玩家、两家开发商和一名管理员完成了提交审核、三种授权、社区互动、饰品交易和退款审计。页面结果均可由 Oracle 业务表、资金流水和资产账本交叉验证。系统具备权限、事务、并发、索引、测试、HTTPS 部署和数据恢复能力，是一套围绕数据库完整实现、可部署、可重复验收的 B/S 应用。演示完毕，谢谢老师。

6. [说完后停止主动操作。若老师没有立即提问，保持商店首页，不再临时添加功能。]

19:25 后停止主动增加演示内容，将剩余时间留给切屏延迟或老师临时提问。

## 8. 腾讯会议单路共享与协作规则

1. 腾讯会议任一时刻只能有一名成员共享屏幕，禁止并行共享、抢占共享或在共享交接过程中操作业务按钮。
2. 固定交接顺序为：当前操作者完成最后一句讲解并停止共享 → 马祥珲确认共享画面消失并点名 → 下一位开始共享 → 下一位确认目标页面可见后再操作。不得省略“停止共享”和“确认”两个动作。
3. 下一位操作者必须在上一阶段结束前于本机准备好目标页面，但不得提前发起共享；开始共享后直接点击，不现场寻找菜单。
4. 徐京生成 CDKey 后通过私下消息发送给靳岱泽，并保留在徐京的临时文本框作为备用。
5. 胡知鱼掉落饰品后把名称、`item_id`、`template_id` 发给靳岱泽和张茗博；靳岱泽用模板搜索，张茗博用实例编号查询账本。
6. 李胤龙的管理员页面始终保持登录，用于前半段上架和后半段退款，避免重复登录。
7. 张茗博全程观察健康状态，但只在异常或数据库证据阶段发言。
8. 任何成员点击后等待明确成功提示，不因网络延迟重复提交。
9. 胡知鱼和靳岱泽始终各自操作自己的玩家账号；玩家环节按步骤中出现的姓名串行切换两块独立屏幕，不互登、不代点。
10. SignalR 环节允许玩家乙的浏览器在后台保持在线，但此时靳岱泽已经停止共享；玩家甲完成发送并停止共享后，靳岱泽才重新共享。后台在线不等于同时演示。

## 9. 故障预案

| 问题 | 立即处理 | 主讲说明 |
|---|---|---|
| 页面暂时无数据 | 张茗博检查健康接口；操作者只刷新一次 | 正在重新读取云端数据 |
| 临时账号或批次已存在 | 张茗博执行 `reset`，从该阶段开头重来 | 上次彩排数据未清理，正在恢复固定基线 |
| 视频加载慢 | 使用海报、截图和全屏图片，不等待视频 | 媒体与核心数据库业务解耦 |
| CDKey 未及时传给靳岱泽 | 徐京重新复制本轮结果；不得生成第二批 | 明文只在创建响应中展示一次 |
| 市场物品搜索错误 | 靳岱泽使用胡知鱼提供的 `template_id` 搜索 | 交易按模板匹配、按实例转移 |
| 交易按钮尚未可用 | 靳岱泽等待一次市场刷新；不得再次上架 | 等待云端订单查询完成 |
| SignalR 暂时断线 | 刷新后展示 Oracle 聊天历史 | 实时推送失败不会丢失持久化消息 |
| 某成员电脑故障 | 马祥珲使用备用浏览器配置接管该角色 | 切换备用演示环境 |
| 公网不可用 | 马祥珲展示预先保存的关键页面截图与架构图，张茗博展示本地测试、验收汇总和 Oracle 证据 | 网络异常不改变已经完成的数据库与测试证据 |

不得手工修改 Oracle 表来修复演示状态。需要回滚时使用恢复工具的运行编号，或执行一次新的 `reset`。

## 10. 老师常见追问与回答负责人

| 问题方向 | 第一回答人 | 回答重点 |
|---|---|---|
| 总体架构、B/S、五层与整体验收 | 马祥珲 | 各层职责、技术选型、跨模块事务边界和验收结论 |
| 认证、JWT、管理员权限、HTTPS 与后端部署 | 李胤龙 | 主体声明、后端守卫、越权防护、Nginx 和健康检查 |
| 登录注册、路由、公告、CI 与 Playwright | 元梓浩 | 会话恢复、前端公共状态、自动构建和端到端回归 |
| 游戏、开发商、上下架、商店列表与筛选 | 周力扬 | 开发商隔离、管理员审核、状态约束、公开查询、搜索筛选和集合页 |
| 游戏详情、媒体和 Steam 风格 | 王子轩 | 详情数据映射、组件拆分、媒体降级与响应式界面 |
| 钱包、充值、购买、订单与资金流水 | 胡知鱼 | 唯一余额真相、定点金额、购买事务、幂等和流水核对 |
| 退款申请、审批、授权撤销与审计 | 马祥珲 | 退款事务、钱包回补、支付与订单终态、审批幂等和审核日志 |
| CDKey、免费入库与游戏库 | 徐京 | 授权来源、哈希保存、重复兑换、库状态和游玩时长 |
| 好友、私信、通知、个人资料、动态讨论与 SignalR | 靳岱泽 | 关系状态、消息持久化、资料徽章、社区内容、实时推送和断线恢复 |
| 饰品库存、评价、成就与工坊 | 郭炫君 | 唯一实例、所有权校验、版本留痕、防重复和订阅状态 |
| 市场、Oracle 总体设计与数据恢复 | 张茗博 | 撮合、资产账本、全局约束、索引、锁、迁移规范和恢复审计 |

被追问的成员先回答自己负责的部分；马祥珲在回答结束后补一句与整体架构的关系。

## 11. 答辩结束

1. 张茗博再执行一次 `reset`，保存最终运行编号。
2. 确认两名临时玩家、两款临时提交、CDKey、订单、退款、聊天、订阅和市场成交已清理。
3. 确认 `/api/health` 与 `/health/database` 仍为 `OK`。
4. 读取云端 `/opt/steam-platform/DEPLOYED_COMMIT`，确认其提交哈希与最新可部署代码提交一致；纯文档提交不要求重新发布运行产物。
5. 徐京删除临时保存的明文 CDKey。

## 12. 正式答辩前必须完成的彩排

1. 七人按本文件完整彩排至少 3 次。
2. 第一次允许暂停并修正文档；第二次必须控制在 20 分钟内；第三次完全模拟老师打断和一次网络延迟。
3. 每次彩排前后都执行恢复，并记录运行编号。
4. 第三次彩排必须完整采用腾讯会议单路共享交接，不允许任何两名成员同时共享。
5. 完整流程、共享顺序、固定金额、Oracle 查询和恢复结果全部一致后，方可确定为正式答辩版本。
