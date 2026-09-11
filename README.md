# “Steam-”数字游戏发行与玩家社区互动平台数据库系统

同济大学软件学院 2026 年数据库课程设计，第 4 组。

本项目实现一个面向玩家、开发商和管理员的数字游戏平台。系统采用 B/S 架构，浏览器端使用 Vue 3，应用服务器使用 C#、.NET 10 和 ASP.NET Core，业务数据统一保存在 Oracle 数据库中。项目以《Counter-Strike 2》（CS2）和《饥荒联机版》（Don't Starve Together，DST）组织演示流程。

## 1. 项目完成情况

- 3 类登录角色：玩家、开发商、管理员。
- 32 个已实现功能点，覆盖发行、购买、退款、CDKey、游戏库、评价、成就、库存、市场、好友、私信、工坊、动态、讨论和平台治理。
- 45 张 Oracle 数据表，全部具有主码，共 71 个外码约束、17 个唯一约束、92 个检查约束和 49 个显式业务索引。
- 购买、退款、市场撮合、饰品过户等关键流程使用 Oracle 事务和行级锁。
- 381 项 .NET 解决方案测试通过，前端生产构建通过；浏览器回归共 28 项。
- 系统已部署到腾讯云轻量应用服务器，通过 HTTPS 对外提供访问。
- 提供固定演示数据、受控恢复工具、数据库验收脚本和答辩操作手册。

## 2. 主要业务

### 2.1 玩家端

- 注册、登录和个人资料维护。
- 浏览、搜索和筛选已上架游戏。
- 免费游戏入库，使用钱包购买付费游戏。
- 查看订单、支付记录、钱包流水和游戏库。
- 申请退款并查看审核结果。
- 使用 CDKey 兑换游戏。
- 发布与修改评价，查看评价版本和成就进度。
- 查看饰品库存，发布买单或卖单，完成市场交易。
- 添加好友、发送私信、接收站内通知。
- 参与动态、讨论、评价投票、工坊订阅和玩家间交易报价。

### 2.2 开发商端

- 登录开发商工作台。
- 创建和修改归属自己的游戏。
- 查看游戏状态和基础发行数据。
- 为自有游戏创建 CDKey 批次。
- 访问控制按开发商主体隔离，不能读取或修改其他开发商的数据。

### 2.3 管理员端

- 审核游戏并在 `ONLINE`、`OFFLINE` 状态间切换。
- 审核玩家退款申请。
- 发布、修改和撤销平台公告。
- 查看关键操作结果和数据库健康状态。

## 3. 演示游戏

| 游戏 | 商业模式 | 主要演示内容 |
| --- | --- | --- |
| Counter-Strike 2 | 免费入库 | 游戏库、饰品库存、买卖挂单、价格优先与时间优先撮合、资金冻结、成交记录、饰品转移账本 |
| 饥荒联机版 | 买断制 | 钱包购买、订单支付、退款审核、CDKey、评价版本、成就、内容包、公告和社区互动 |

项目中的 DST 成就是课程演示数据，不表示与 Steam 官方成就同步。

## 4. 系统架构

```text
浏览器
  │ HTTPS
  ▼
Nginx
  ├── Vue 3 生产静态文件
  └── /api、/health、/hubs 反向代理
          │
          ▼
ASP.NET Core 10 / Kestrel
  ├── Api
  ├── Application
  ├── Domain
  ├── Infrastructure
  └── Shared
          │ Dapper + ODP.NET
          ▼
Oracle Database Free
```

后端五层职责如下：

| 层 | 职责 |
| --- | --- |
| Api | 接收 HTTP 请求，执行认证、授权、限流和异常映射 |
| Application | 定义接口契约，组织业务流程和输入校验 |
| Domain | 定义实体、值和业务规则 |
| Infrastructure | 使用 Dapper 与 ODP.NET 执行参数化 SQL、事务和仓储操作 |
| Shared | 提供统一响应、业务异常、标识生成等公共类型 |

`Oracle.EntityFrameworkCore` 作为依赖保留，但当前业务路径未使用 `DbContext` 或实体映射。Oracle 数据访问由 Dapper 和 `Oracle.ManagedDataAccess.Core` 完成。

## 5. 数据库设计

### 5.1 数据主题

| 数据主题 | 主要关系 |
| --- | --- |
| 主体与发行 | `PLAYER`、`DEVELOPER`、`ADMIN_USER`、`GAME`、`SYS_NOTICE` |
| 资金与订单 | `WALLET_ACCOUNT`、`WALLET_TRANSACTION`、`GAME_ORDER`、`ORDER_DETAIL`、`PAYMENT_TRANSACTION` |
| 退款与权益 | `REFUND_TICKET`、`REFUND_DETAIL`、`REFUND_AUDIT_LOG`、`PLAYER_LIBRARY`、`CDKEY*` |
| 评价与成就 | `GAME_REVIEW`、`REVIEW_VERSION`、`ACHIEVEMENT`、`PLAYER_ACHIEVEMENT` |
| 饰品与市场 | `ITEM_TEMPLATE`、`INVENTORY_ITEM`、`MARKET_ORDER`、`MARKET_TRADE`、`ITEM_TRANSFER_LEDGER` |
| 社交与社区 | 好友、私信、通知、资料、徽章、报价、工坊、动态和讨论相关关系 |
| 演示恢复 | `DEMO_RESET_RUN`、`DEMO_RESET_TABLE`、`DEMO_RESET_EVENT` |

### 5.2 核心数据规则

- 钱包余额的唯一真相是 `WALLET_ACCOUNT.available_balance` 和 `WALLET_ACCOUNT.frozen_balance`。
- 每次资金变化都追加 `WALLET_TRANSACTION`，不以覆盖历史记录代替流水。
- 玩家与游戏的有效权益由 `PLAYER_LIBRARY` 表示，退款后保留记录并改为 `REVOKED`。
- 每件饰品由 `INVENTORY_ITEM` 唯一标识，挂售状态为 `IN_MARKET`。
- 市场有效订单状态为 `MATCHING`，成交后资金结算、物品过户和账本记录在同一事务内完成。
- 评价正文修改时追加 `REVIEW_VERSION`，保留历史版本。
- 好友、消息、通知、工坊订阅和社区互动均持久化到 Oracle，不以浏览器本地存储代替业务数据。

## 6. 仓库结构

```text
backend/          C#/.NET 解决方案、五层后端、测试和运维工具
database/         Oracle 建库、初始化、迁移、验收和并发演示脚本
frontend/         Vue 3 前端、静态资源和 Playwright 回归测试
tests/            数据库契约测试与云端 API 测试
docs/             答辩、数据库验收、HTTPS 和回归操作手册
项目文档/        三份课程文档和答辩 PPT
_archive/         团队早期设计与已停止使用的旧工程
```

`_archive/` 仅用于保存早期成果，不作为当前实现依据。当前数据库事实来源是 `database/schema.sql`，当前程序事实来源是 `backend/` 与 `frontend/`。

## 7. 本地运行

### 7.1 环境要求

- .NET SDK 10
- Node.js 22 或兼容版本
- Oracle Database 18c 或更高版本
- SQL*Plus、SQLcl、DBeaver 等任一 Oracle 客户端

### 7.2 初始化 Oracle

使用具有建表权限的项目模式账号连接 Oracle，依次执行：

```sql
@database/schema.sql
@database/data.sql
@database/verify_phase1.sql
@database/verify_defense.sql
```

新建空库只执行 `schema.sql` 和 `data.sql`。`database/migrations/` 用于旧版本数据库按日期顺序升级，不应在新建数据库后重复执行。`database/admin/` 中的脚本仅供明确的管理任务使用。

### 7.3 配置并启动后端

真实连接串和 JWT 签名密钥必须保存在 User Secrets 或服务器私有环境变量中，不得写入仓库。

```powershell
dotnet user-secrets set --project backend\src\SteamPlatform.Api "ConnectionStrings:Oracle" "User Id=steam_app;Password=***;Data Source=localhost:1521/FREEPDB1"
dotnet user-secrets set --project backend\src\SteamPlatform.Api "Auth:SigningKey" "至少32字节的随机签名密钥"
dotnet run --project backend\src\SteamPlatform.Api
```

开发环境默认地址以终端输出为准，仓库 `launchSettings.json` 的 HTTP 地址为 `http://localhost:5253`。

### 7.4 启动前端

```powershell
cd frontend
npm install
npm run dev
```

浏览器访问 `http://localhost:5173`。开发代理默认连接项目云端 API；如需连接本机后端，在 `frontend/.env.development.local` 中设置：

```env
VITE_API_PROXY_TARGET=http://localhost:5253
```

## 8. 演示账号

以下账号仅用于课程演示数据：

| 角色 | 账号 | 密码 |
| --- | --- | --- |
| 玩家甲 | `alice` | `alice` |
| 玩家乙 | `bob` | `bob` |
| 管理员 | `rootadmin` | `admin` |
| 开发商一 | `valve@example.com` | `valve` |
| 开发商二 | `klei@example.com` | `klei` |

生产环境不得沿用演示口令。

## 9. 构建与测试

### 9.1 后端与数据库契约测试

```powershell
dotnet restore backend\SteamPlatform.sln
dotnet build backend\SteamPlatform.sln -c Release
dotnet test backend\SteamPlatform.sln -c Release
```

当前测试组成：

| 测试项目 | 数量 |
| --- | ---: |
| `SteamPlatform.Api.Tests` | 293 |
| `SteamPlatform.Database.Tests` | 63 |
| `SteamPlatform.Api.CloudTests` | 9 |
| `SteamPlatform.DemoData.Tests` | 4 |
| `SteamPlatform.HttpsDeploy.Tests` | 12 |
| 合计 | 381 |

未设置真实云端或 Oracle 环境变量时，外部连接型用例只检查本地契约，不会修改远端数据库。

### 9.2 前端构建与浏览器回归

```powershell
cd frontend
npm run build
npm run test:e2e -- --list
```

浏览器回归共 28 项：桌面与移动只读场景 26 项，固定答辩业务链和社交社区闭环各 1 项。会写入云端数据的完整回归必须通过 `frontend/scripts/run-cloud-e2e.mjs` 执行，使测试前后都恢复固定演示基线。

## 10. 云端部署

- 云平台：腾讯云轻量应用服务器
- 操作系统：Ubuntu Server 22.04 LTS 64 位
- Web 入口：`https://124.222.213.245`
- Web 服务：Nginx
- 应用服务：ASP.NET Core 10，Kestrel 仅监听服务器回环地址
- 数据库：Oracle Database Free，1521 端口不向公网开放
- 公网入站端口：22、80、443

服务器上的 Nginx、应用服务和 Oracle 独立于开发电脑运行。组员和答辩电脑通过 Web 与 API 使用系统，不直接连接公网 Oracle。

## 11. 数据安全与提交规范

- 所有文本文件使用 UTF-8 编码。
- 不提交数据库密码、JWT 签名密钥、SSH 私钥、连接字符串或本地环境文件。
- `*.pem`、`*.key`、`.env.*.local`、本地发布配置和测试输出由 `.gitignore` 排除。
- SQL 使用参数化查询，不拼接用户输入。
- 前端只负责输入提示和界面状态，权限、金额、所有权和状态校验由后端与 Oracle 共同保证。
- 普通业务操作不得执行 `database/admin/` 下的管理脚本。

## 12. 最终提交资料

按照课程最终提交要求，交付目录只包含以下三部分：

```text
document/   系统需求分析文档、数据库设计文档、系统设计与实现文档、答辩 PPT
database/   建库脚本、初始化数据、迁移与验证脚本、Oracle 导入说明
program/    前后端源码、解决方案、测试和本地运行说明
```

需求分析文档使用 UML 用例与流程描述需求；设计与实现文档使用 UML 和当前代码说明系统设计，并包含数据库设计、界面截图和核心实现；数据库设计文档以最终 45 表 `schema.sql` 为唯一结构依据。

课程提交目录中，进入 `program/` 后可按本说明运行前后端；数据库脚本位于同级的 `../database/`。首次部署先按 `../database/Oracle数据库导入与初始化说明.md` 完成建库和初始化，再配置后端连接字符串。

## 13. 项目成员

| 学号 | 姓名 |
| --- | --- |
| 2451753 | 马祥珲 |
| 2451754 | 胡知鱼 |
| 2452874 | 张茗博 |
| 2354127 | 靳岱泽 |
| 2453195 | 李胤龙 |
| 2452654 | 郭炫君 |
| 2452676 | 徐京 |
| 2451398 | 王子轩 |
| 2451769 | 元梓浩 |
| 2453736 | 周力扬 |
