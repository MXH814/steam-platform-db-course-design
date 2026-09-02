# "Steam-" 平台项目现状问题排查与答辩风险总结

> **发布说明**：本文件整理自对项目现阶段代码、Oracle 数据库、腾讯云部署、测试集及课程设计提纲硬性指标的全方位排查，旨在面向全体小组成员同步当前项目存在的**关键问题、致命短板与答辩风险**。距离 9 月 12 日答辩仅剩约 10 天，请全员高度重视并对照认领整改。

---

## 总体态势概述

目前后端 ASP.NET Core 五层架构规范、Oracle 45 张数据表及其约束、腾讯云 HTTPS 公网访问、一键演示数据恢复（DemoData）以及前端 Steam 风格交互的工程实现度极高，核心代码与云端部署已具备极佳的技术底座。

然而，**技术实现的领先掩盖了文档与答辩层面的严重短板**。按照同济大学《数据库课程设计》课程提纲（占总成绩 65% 的项目考核部分），我们在**官方交付文档、数据库设计文档一致性、申报功能对齐、答辩材料与考勤依据**上存在多项可能导致严重失分的硬伤。

---

## 一、 致命硬伤：官方硬性交付文档严重缺失（直接影响 65% 成绩）

对照《`2026《数据库课程设计》课程提纲.doc`》第（2）条与评分因素硬性要求：
> “*撰写系统需求分析文档、系统设计与实现文档、数据库设计文档、答辩 PPT。评分关注文档质量（文档必须清晰、完整）*”

目前项目文档目录（`项目文档/`）的实际现状如下：

| 考核硬性交付物 | 当前状态 | 严重程度 | 具体问题说明 |
|---|---|---|---|
| **1. 系统需求分析文档** | ❌ **完全缺失** | 🚨 **致命** | 根目录只有 `模版文档/系统需求分析文档模板.doc`，`项目文档/` 内**完全没有任何需求分析文档**。未定义系统总体用例、角色规约与功能性/非功能性需求。 |
| **2. 系统设计与实现文档** | ❌ **完全缺失** | 🚨 **致命** | 根目录只有 `模版文档/系统设计与实现文档模板.doc`，`项目文档/` 内**同样完全缺失**。未说明系统架构设计、动作序列（购买、退款、撮合时序图）、类图与核心方法实现。 |
| **3. 答辩 PPT** | ❌ **完全缺失** | 🚨 **致命** | 全仓库没有任何 `.ppt` 或 `.pptx` 文件。20 分钟多角色答辩需要依赖幻灯片进行汇报开场与架构总结，当前处于空白状态。 |
| **4. 数据库设计文档** | ⚠️ **存在重大事实错误且严重滞后** | 🚨 **致命** | 已有文档存在原则性违规与大面积表结构缺失，详见第二节专项分析。 |

---

## 二、 原则性硬伤：《数据库设计文档》与实际表结构严重脱节

文件位置：[`项目文档/“Steam-”数字游戏平台系统数据库设计文档.docx`](file:///home/kn/yuan_proj/steam-platform-db-course-design/项目文档/“Steam-”数字游戏平台系统数据库设计文档.docx)

该文档是第一阶段提交给助教的产物，若答辩时评审老师以此文档审查数据库，会立刻发现以下严重问题：

### 1. 违背架构铁律，残留已废除字段（严重原则性错误）
- **问题**：文档内多处（如第 212 行表结构定义中）仍然白纸黑字写着 **`PLAYER.wallet_balance`** 字段。
- **事实**：项目 README 与云端部署早已确立铁律——“*删除 `PLAYER.wallet_balance`，资金唯一真相放在 `WALLET_ACCOUNT`，不允许绕过决策恢复冗余字段*”。数据库中也通过触发器和只读总验收脚本验证了该列数量为 0。
- **风险**：文档与实际数据库矛盾，会被答辩老师判定为“文档胡乱编写 / 代码与设计脱节 / 资金账实不符”。

### 2. 表数量严重落后（文档 27 张表 vs 实际 45 张表）
- **问题**：当前文档仅记录了 7 月份初期的 **27 张核心表**。
- **事实**：8 月份为了完善系统业务闭环与答辩演示，系统通过迁移脚本新增了 **18 张表**，当前生产环境实际运行的是 **45 张表**：
  - **社交与实时表（6 张）**：`FRIEND_RELATION`、`DIRECT_MESSAGE`、`REVIEW_REACTION`、`WORKSHOP_ITEM`、`WORKSHOP_SUBSCRIPTION`、`USER_NOTIFICATION`。
  - **社区扩展表（9 张）**：`PLAYER_PROFILE`、`BADGE_CATALOG`、`PLAYER_BADGE`、`TRADE_OFFER`、`TRADE_OFFER_ITEM`、`COMMUNITY_POST`、`COMMUNITY_POST_REACTION`、`DISCUSSION_TOPIC`、`DISCUSSION_REPLY`。
  - **演示运维审计表（3 张）**：`DEMO_RESET_RUN`、`DEMO_RESET_TABLE`、`DEMO_RESET_EVENT`。
- **风险**：当前文档缺失了 18 张表的物理模型、主外键定义、约束规则和字段解释，导致答辩演示时展示的社区动态、工坊、好友私聊、交易报价在数据库设计文档中“查无此表”。

### 3. E-R 图与数据库关系图未同步
- 根目录下 [`E-R图（改）.drawio`](file:///home/kn/yuan_proj/steam-platform-db-course-design/E-R图（改）.drawio)、[`图（改）/E-R图/`](file:///home/kn/yuan_proj/steam-platform-db-course-design/图（改）/E-R图) 下的 5 张 PNG 导图，以及 [`图（改）/数据库关系图/”steam-“平台数据库关系图.png`](file:///home/kn/yuan_proj/steam-platform-db-course-design/图（改）/数据库关系图/”steam-“平台数据库关系图.png)，均停留在此前的 27 表模型，未画入新增的 18 张表及其实体联系。

---

## 三、 答辩问答风险：开题申报功能（21个）与当前实现的偏差

查看初期提交给助教的开题表 [`项目文档/分组名单及项目选题.xlsx`](file:///home/kn/yuan_proj/steam-platform-db-course-design/项目文档/分组名单及项目选题.xlsx)：
当时申报了 16 个核心业务逻辑功能 + 5 个基础管理功能。其中存在**部分申报功能在后续迭代中被替换或未实际落地**的情况：

| 开题申报的功能点 | 当前实际状态 | 答辩可能面临的追问与风险 |
|---|---|---|
| **功能 4：家庭库共享与并发挤占处理** | ❌ 未在当前代码与数据库中体现 | 老师如果对照申报表提问：“你们的家庭库互斥挤占在哪演示？”，将无法当场展示。 |
| **功能 5：愿望单降价检测与促销广播** | ❌ 未实现愿望单表及检测逻辑 | 缺少愿望单相关数据表与通知触发逻辑。 |
| **功能 7：Mod 依赖树递归解析与订阅** | ⚠️ 简化为工坊作品与用户订阅 | 实现了工坊发布与订阅，但没有复杂的“前置递归依赖树解析”。 |
| **功能 8：基于标签重合度的相似游戏推荐** | ⚠️ 前端展示推荐，后端无标签交集聚合算法 | 推荐目前主要依托固定基线和标签展示，未做复杂标签交集运算。 |
| **功能 10：开发者销售收益阶梯式抽成核算** | ❌ 未实现销售额阶梯费率表与核算逻辑 | 仅实现了钱包扣款与订单明细，无月度梯队抽成。 |
| **功能 13：跨区游戏赠礼价格倒挂风控** | ❌ 未实现不同定价区及赠礼汇率比对 | 仅支持玩家自身购买入库与 CDKey 兑换。 |

### ⚠️ 破局与应对要求：
我们在后续开发中，实际上实现了比开题更具数据库含金量的新链路：
- **行级排他锁并发控制**（市场买卖单撮合、双会话防并发、有限等待与 SQLCODE 54 捕获）；
- **数字资产确权与状态不可篡改**（`ITEM_TRANSFER_LEDGER` 资产流转账本、钱包流水唯一真相）；
- **多主体数据隔离与审核流**（Klei/Valve 开发商隔离、管理员双重审核上架、退款双重校验）；
- **好友关系与 SignalR 私有组实时通讯**。

**【必须统一的答辩口径】**：在撰写需求文档、设计文档和答辩 PPT 时，必须正式设立**《功能演进与架构深化说明》**章节，主动向老师阐明：*“团队在第 1 阶段需求细化时发现，与其做宽泛但缺乏数据库深度的小功能，不如将业务重心聚焦在商业级高并发与资产防刷场景，因而将精力投入到双样板游戏（CS2 饰品交易闭环 + DST 买断与社区闭环）的行锁并发防御与资金/资产双账本一致性上。”* 避免被老师质疑“货不对板”。

---

## 四、 代码与工程结构排查问题

代码总体质量良好，但有几处影响 Visual Studio 体验和代码整洁度的小问题：

### 1. Visual Studio 解决方案未收录根目录测试工程
- **问题文件**：[`backend/SteamPlatform.sln`](file:///home/kn/yuan_proj/steam-platform-db-course-design/backend/SteamPlatform.sln)
- **问题**：根目录下的两个重要测试工程未被添加到解决方案中：
  - [`tests/SteamPlatform.Database.Tests/SteamPlatform.Database.Tests.csproj`](file:///home/kn/yuan_proj/steam-platform-db-course-design/tests/SteamPlatform.Database.Tests/SteamPlatform.Database.Tests.csproj)（包含 39 个数据库契约、表结构与迁移脚本测试）
  - [`tests/SteamPlatform.Api.CloudTests/SteamPlatform.Api.CloudTests.csproj`](file:///home/kn/yuan_proj/steam-platform-db-course-design/tests/SteamPlatform.Api.CloudTests/SteamPlatform.Api.CloudTests.csproj)（云端 API 只读回归测试）
- **影响**：如果组员或评委在 Windows 上使用 Visual Studio 打开该解决方案，在“测试资源管理器”里根本看不到数据库测试，也无法一键运行全部测试。

### 2. 前端目录残留 3 个废弃死代码组件
- **问题文件**：
  - [`frontend/src/views/HomeView.vue`](file:///home/kn/yuan_proj/steam-platform-db-course-design/frontend/src/views/HomeView.vue)（早期公告首页，已被 `StoreView.vue` 替代）
  - [`frontend/src/views/OrdersView.vue`](file:///home/kn/yuan_proj/steam-platform-db-course-design/frontend/src/views/OrdersView.vue)（早期简易订单页，已被 `WalletHistoryView.vue` + `GameCheckoutView.vue` 替代）
  - [`frontend/src/views/GameStoreView.vue`](file:///home/kn/yuan_proj/steam-platform-db-course-design/frontend/src/views/GameStoreView.vue)（早期旧版商店页，已被 `GameDetailView.vue` 替代）
- **影响**：未在 [`frontend/src/router.ts`](file:///home/kn/yuan_proj/steam-platform-db-course-design/frontend/src/router.ts) 或其他组件中被任何地方引用，属于未清理的无用代码，容易造成组员理解混乱。

### 3. 误导性命名的组件
- **问题文件**：[`frontend/src/views/WalletRefundPlaceholderView.vue`](file:///home/kn/yuan_proj/steam-platform-db-course-design/frontend/src/views/WalletRefundPlaceholderView.vue)
- **问题**：组件文件名带有 `Placeholder`（占位符），但实际上它已经完整实现了订单交易核对、退款资格校验、表单原因提交与 API 联动的真实流程。该命名容易让人误以为是“未完成的占位页面”。

---

## 五、 提纲明文规定的答辩评分要素缺口

依据课程提纲关于答辩成绩评定的硬性说明：

### 1. 考勤分依据材料未准备（占总成绩 15%）
> 提纲原文：*“答辩前，组长将每位组员的考勤分交给助教，同时提交给出考勤分的依据，如交流记录、github上的代码上传、修改记录。”*
- **风险**：目前我们尚未对团队各成员在 GitHub 上的 Commit 提交次数、PR 审查参与度、微信/腾讯会议交流记录进行归纳与留痕。若助教索要考勤依据，将难以立即拿出客观凭证。

### 2. 组员分数占比表未敲定
> 提纲原文：*“答辩前，组长将每个组员的分数占比交给助教。每个组员的项目分数=每组项目的分数\*每个组员的分数占比。”*
- **要求**：组长与全组成员需提前协商并锁定该比例表，避免临近答辩产生争议或仓促提交。

### 3. 答辩演示角色 A-J 与 10 位真实组员尚未明确映射
- [`docs/defense-demo-runbook.md`](file:///home/kn/yuan_proj/steam-platform-db-course-design/docs/defense-demo-runbook.md) 详细设计了 20 分钟多角色演示链（A-G 7人实操电脑 + H-J 3人专项问答），目前文档中仅标注了代号 A~J，尚未将 10 位同学的真实姓名与代号进行固定绑定，且全组尚未进行带真实切屏的脱稿彩排。

---

## 六、 攻坚整改建议与优先级清单

```text
【P0 - 极高危：必须在 3 天内启动并攻坚】
1. 编写《系统需求分析文档.docx》（依照模版文档规范，将当前系统功能完整规约化）
2. 编写《系统设计与实现文档.docx》（补充架构图、时序图、核心类与数据流说明）
3. 制作《20分钟答辩演示PPT》（结合 defense-demo-runbook.md 提炼架构、核心创新点与答辩演练流程）
4. 彻底重修《数据库设计文档.docx》：
   - 彻底删除 PLAYER.wallet_balance 冗余字段说明；
   - 补齐新增的 18 张表结构（形成完整的 45 表数据字典）；
   - 更新 E-R 图与数据库物理关系图。

【P1 - 高危：必须在答辩前 5 天内落实】
5. 在需求文档与 PPT 中补充《申报功能演进说明》，主动化解开题 21 项功能对齐风险。
6. 将真实组员姓名与 runbook 中的 A-J 角色绑定，组织至少 2 次 20 分钟多机切屏全流程彩排。
7. 统计 GitHub Commit 与会议记录，整理完成《组员考勤依据及分数占比表》。

【P2 - 中危：顺手修复与代码规范化】
8. 将 tests 目录下的两个 csproj 纳入 backend/SteamPlatform.sln。
9. 删除前端 3 个废弃 .vue 文件，并将 WalletRefundPlaceholderView.vue 更名为正常业务命名。
```

---
*本文档已保存至 `docs/defense-issues-and-action-items.md`，请大家对照各自模块，积极配合完成答辩前最后的收口工作！*
