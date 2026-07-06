# "Steam-" 数字游戏平台系统课程设计 README

> 本 README 是项目后续设计、开发、部署、验收和答辩的统一参考文件。  
> 任何技术选型、部署方案、数据库结构、开发计划、实际实现与本文档不一致时，必须先更新 README，再继续开发。

## 协作铁律：禁止直接推送 main

`main` 分支是项目稳定主线，必须始终保持可运行、可演示、可用于答辩。

所有组员开发功能时必须遵守以下流程：

```text
从 main 拉取最新代码
  -> 创建自己的功能分支
  -> 在功能分支提交代码
  -> 推送功能分支到 GitHub
  -> 发起 Pull Request
  -> 由总负责人检查代码、接口、数据库脚本、README 和演示流程
  -> 通过后合并进 main
```

GitHub 仓库已对 `main` 分支启用保护规则：

- 禁止直接 push 到 `main`。
- 禁止 force push。
- 禁止删除 `main`。
- 合并前必须通过 Pull Request。
- Pull Request 至少需要 1 个 review。
- Pull Request 中未解决的讨论必须先处理完。

任何数据库表结构、公共接口、统一响应格式、技术路线、部署方案、团队分工和交付规范变更，都必须在 Pull Request 中同步更新 README。

## 1. 最高约束：课程提纲

本项目必须严格遵守 `2026《数据库课程设计》课程提纲.doc` 的要求。

课程提纲中的关键硬要求：

- 使用 VS.NET 较新版本。
- 使用 C# 语言。
- 使用 Oracle 18c 或更高版本作为 DBMS。
- 使用 Oracle 数据访问组件或 ORM 框架。
- 开发一个实用的信息管理系统。
- C/S 或 B/S 均可。
- 至少 12 张表，且符合第三范式。
- 至少 20 个功能点，其中至少 15 个功能点必须具有一定业务逻辑，不能只是表的增删改查。
- 编码实现课程项目，并进行测试，部署或生成可执行程序。
- 撰写系统需求分析文档、系统设计与实现文档、答辩 PPT。
- 评分关注工作量、应用性、复杂性、总体设计、功能完整性、数据库合理性、界面美观、编码规范、鲁棒性、扩展性、文档质量和答辩表现。

本项目额外确定的约束：

- 架构选择 B/S。
- 除前端界面外，后端、应用服务器、数据访问层、部署脚本等项目实现均使用 C# / .NET 技术栈。
- 数据库和应用服务器均部署到阿里云云服务器。
- 前端继续使用 Vue 技术栈实现 Steam 风格界面。

废弃旧计划：

- 不再使用 Spring Boot。
- 不再使用 Java 作为后端语言。
- 不再使用 Maven 作为项目构建主线。
- 不再使用 MyBatis / MyBatis-Plus。
- 不再使用 Tomcat / HikariCP / Spring Security。

## 2. 当前项目定位

本项目实现一个类似 Steam 的数字游戏平台系统。系统不仅要完成基础游戏商店功能，还要体现数据库课程设计重点：关系模型、约束、事务、一致性、日志、审计、幂等、防并发、资产确权和复杂业务流程。

核心业务域：

- 玩家账号与权限。
- 开发商与管理员。
- 游戏商店与公告。
- 钱包账户与资金流水。
- 游戏订单、订单明细、支付流水、订单状态日志。
- 退款申请、退款明细、退款审核日志。
- 玩家游戏库与数字资产确权。
- CDKey 批次、CDKey、兑换风控日志。
- 游戏评价主记录与评价历史版本。
- 成就字典与玩家成就解锁。
- 饰品模板、饰品实例、玩家库存。
- 饰品市场买卖挂单、撮合成交、资金清算、饰品流转账本。

主要参考文件：

- `2026《数据库课程设计》课程提纲.doc`
- `项目文档/“Steam-”数字游戏平台系统数据库设计文档.docx`
- `图（改）/E-R图/`
- `图（改）/数据库关系图/`
- `“Steam-”数字游戏平台系统（改）.pdma`

## 3. 最终架构选择

选择：B/S 架构。

```text
浏览器
  -> Vue 3 前端页面
  -> HTTPS / HTTP
  -> 阿里云 Nginx
  -> ASP.NET Core Web API 应用服务器
  -> EF Core / Dapper / ODP.NET
  -> Oracle Database
```

选择 B/S 的原因：

- 课程允许 C/S 或 B/S，B/S 符合要求。
- Steam 风格界面更适合 Web 前端实现。
- 答辩演示只需浏览器访问云服务器地址。
- 应用服务器和数据库都可以部署在阿里云，满足云部署要求。
- 前后端分离方便团队协作。
- Oracle 端口不需要暴露给客户端，安全性明显好于桌面客户端直连数据库。

不选择纯 C/S 的原因：

- 桌面客户端直连云 Oracle 需要暴露数据库端口，安全性差。
- WinForms/WPF 做 Steam 风格界面工作量更大。
- 客户端部署、版本更新和答辩演示都更麻烦。

## 4. 推荐技术栈

| 层级 | 选型 | 说明 |
|---|---|---|
| 云平台 | 阿里云 ECS | 运行 Oracle、ASP.NET Core API、Nginx、前端静态文件 |
| 操作系统 | Ubuntu 24.04 LTS 或 Ubuntu 22.04 LTS | 轻量、资料多、适合 Nginx + Kestrel 部署 |
| 数据库 | Oracle Database Free / Oracle 26ai Free，满足 Oracle 18c+ 要求 | 课程要求 Oracle 18c 或更高版本 |
| 后端语言 | C# | 课程提纲硬要求 |
| 应用服务器 | ASP.NET Core Web API on .NET 10 LTS | C# Web API，运行于 Kestrel，前置 Nginx 反向代理 |
| IDE | Visual Studio Community 2022 或更新版本 | 满足课程对 VS.NET 较新版本的要求，团队统一使用 |
| ORM | Oracle.EntityFrameworkCore | Oracle 官方 EF Core Provider |
| Oracle 数据访问 | Oracle.ManagedDataAccess.Core | Oracle 官方 ODP.NET Core 驱动 |
| 复杂 SQL | Dapper + ODP.NET | 钱包流水、市场撮合、报表查询等复杂 SQL 可控 |
| API 权限 | ASP.NET Core Authentication + JWT | 玩家、开发商、管理员分角色鉴权 |
| 前端 | Vue 3 + Vite + TypeScript | Steam 风格 Web 界面 |
| 前端状态 | Pinia | 登录状态、用户信息、钱包、购物车等 |
| 前端路由 | Vue Router | 页面路由 |
| 前端请求 | Axios | 调用 ASP.NET Core API |
| UI / CSS | Vue 单文件组件 + 项目内 CSS | 当前先控制复杂度，后续按页面规模评估组件库 |
| Web Server | Nginx | 托管前端静态文件并反向代理 `/api` |
| API 调试 | Apifox 或 Postman | 接口测试 |
| 版本管理 | Git + GitHub | 代码提交、协作、考勤依据 |

建议 NuGet 包版本：

```text
Oracle.EntityFrameworkCore     10.23.26200
Oracle.ManagedDataAccess.Core  23.26.200
Dapper                         2.1.79 或兼容 2.x
dotnet-ef                      10.0.9
```

注意：

- 不使用 EF Core Migration 作为数据库结构主来源。
- 数据库结构以 `database/schema.sql` 为准。
- EF Core 采用 Database-first / 手动映射思路。
- 复杂事务和复杂 SQL 不强行塞进 EF Core，优先使用 Dapper 或 ODP.NET 原生 SQL。

## 5. 团队开发环境基线

团队成员开发环境应尽量保持一致，避免因工具版本差异导致接口、依赖或构建结果不一致。

推荐开发工具：

| 工具 | 要求 |
|---|---|
| Visual Studio Community 2022 或更新版本 | 必须包含 ASP.NET and web development 工作负载 |
| .NET SDK | 使用 .NET 10 SDK，确保能创建和构建 ASP.NET Core Web API 项目 |
| ASP.NET Core Runtime | 与项目目标框架保持一致 |
| Entity Framework CLI | 使用 `dotnet-ef 10.x` |
| Node.js | 使用当前 LTS 或团队统一指定版本 |
| npm | 随 Node.js 安装，使用团队统一镜像源策略 |
| Git | 用于代码版本管理 |
| Oracle 客户端工具 | 推荐 SQL*Plus、SQL Developer 或 DataGrip，至少保证能连接 Oracle 并执行脚本 |
| API 调试工具 | Apifox 或 Postman |

环境自查命令：

```powershell
dotnet --version
dotnet --list-sdks
node -v
npm -v
git --version
sqlplus -V
```

团队工具链基线：

- .NET 10 Web API 模板必须能创建并编译。
- `Oracle.EntityFrameworkCore 10.23.26200` 作为 Oracle EF Core Provider 版本基线。
- `Dapper 2.1.79` 作为复杂 SQL 辅助访问版本基线。
- `Oracle.ManagedDataAccess.Core 23.26.200` 作为 ODP.NET Core 驱动版本基线。

不作为项目主线的环境：

- JDK 21。
- Maven 3.9.16。

这些环境不用于本课程项目后端实现，不应写入后端构建或部署流程。

## 6. 云服务器选择与部署目标

云平台：阿里云。

推荐实例：

```text
最低配置：2 核 4G，80GB 云盘，3Mbps 公网带宽
推荐配置：4 核 8G，100GB 云盘，3-5Mbps 公网带宽
地域：华东 2 上海，或华东 1 杭州
系统：Ubuntu 24.04 LTS 或 Ubuntu 22.04 LTS
```

本项目更推荐 4 核 8G，因为 Oracle 与 ASP.NET Core 同机部署时，4G 内存会比较紧。

云服务器部署结构：

```text
阿里云 ECS
  /opt/steam-platform/
    api/        ASP.NET Core 发布产物
    frontend/   Vue 打包后的 dist 静态文件
    scripts/    部署脚本

  Oracle Database
  Nginx
  systemd service: steam-platform-api
```

公网开放端口：

```text
22    SSH，建议限制来源 IP 或使用密钥登录
80    HTTP
443   HTTPS，后续有域名后启用
```

不对公网开放：

```text
1521  Oracle，只允许服务器内部访问
5000  ASP.NET Core Kestrel，只允许 Nginx 在服务器内部反向代理
```

Nginx 路由建议：

```text
/        -> Vue 前端静态文件
/api     -> ASP.NET Core Web API
```

## 7. ASP.NET Core 五层结构

本项目后端采用 MVC 思想下的五层结构。

```text
View 层
  Vue 前端页面

Controller 层
  ASP.NET Core Controllers

Application / BLL 业务逻辑层
  业务服务、事务编排、权限判断、业务规则

Infrastructure / DAL 数据访问层
  EF Core、Dapper、ODP.NET、Repository、SQL 查询

Domain / Model 模型层
  Entity、DTO、Request、Response、Enum、领域模型
```

推荐后端目录：

```text
backend/
  SteamPlatform.sln
  src/
    SteamPlatform.Api/
      Controllers/
      Middleware/
      Filters/
      Program.cs

    SteamPlatform.Application/
      Services/
      Contracts/
      Transactions/

    SteamPlatform.Domain/
      Entities/
      Enums/
      ValueObjects/

    SteamPlatform.Infrastructure/
      Data/
      Repositories/
      Sql/
      Oracle/

    SteamPlatform.Shared/
      Responses/
      Exceptions/
      Constants/
      Utilities/

  tests/
    SteamPlatform.Tests/
```

各层职责：

- `SteamPlatform.Api`：接收 HTTP 请求，做参数校验、鉴权入口、调用 Application 层，返回 JSON。
- `SteamPlatform.Application`：实现业务用例，例如购买游戏、退款审核、CDKey 兑换、市场撮合。
- `SteamPlatform.Domain`：定义实体、枚举和领域概念，不依赖数据库访问实现。
- `SteamPlatform.Infrastructure`：访问 Oracle，封装 EF Core、Dapper、ODP.NET 和 SQL。
- `SteamPlatform.Shared`：统一响应、错误码、业务异常、通用工具。

禁止：

- Controller 直接写复杂业务。
- Controller 直接拼 SQL。
- 前端直接访问 Oracle。
- Oracle 端口公网开放。
- EF Core Migration 反向改写课程设计数据库结构。

## 8. 数据库设计原则

数据库脚本目录：

```text
database/
  schema.sql
  data.sql
  verify_phase1.sql
  admin/
```

当前数据库设计文档共 27 张核心表：

1. `PLAYER`
2. `WALLET_ACCOUNT`
3. `WALLET_TRANSACTION`
4. `DEVELOPER`
5. `ADMIN_USER`
6. `SYS_NOTICE`
7. `GAME`
8. `GAME_ORDER`
9. `ORDER_DETAIL`
10. `ORDER_STATUS_LOG`
11. `PAYMENT_TRANSACTION`
12. `REFUND_TICKET`
13. `REFUND_DETAIL`
14. `REFUND_AUDIT_LOG`
15. `PLAYER_LIBRARY`
16. `CDKEY_BATCH`
17. `CDKEY`
18. `CDKEY_REDEEM_LOG`
19. `GAME_REVIEW`
20. `REVIEW_VERSION`
21. `ACHIEVEMENT`
22. `PLAYER_ACHIEVEMENT`
23. `ITEM_TEMPLATE`
24. `INVENTORY_ITEM`
25. `MARKET_ORDER`
26. `MARKET_TRADE`
27. `ITEM_TRANSFER_LEDGER`

落地原则：

- 使用 Oracle 类型：`VARCHAR2`、`NUMBER`、`DATE`、`TIMESTAMP`、`CLOB`。
- 所有主键、外键、唯一约束、检查约束必须写入 DDL。
- 与并发相关的 `version` 字段保留。
- 与幂等相关的 `idempotency_key` 加唯一约束。
- 账本类和日志类表原则上只追加，不物理删除。
- 金额字段使用 `NUMBER(10,2)`，C# 使用 `decimal`。
- 时间字段使用 Oracle `TIMESTAMP`，C# 使用 `DateTime` 或按需要使用 `DateTimeOffset`。
- 状态字段用 `VARCHAR2`，并用 `CHECK` 约束限定取值。

资金余额最终决策：

- 删除 `PLAYER.wallet_balance`。
- `PLAYER` 只保存玩家账号档案信息。
- `WALLET_ACCOUNT` 是资金数据唯一真相来源。
- `available_balance` 表示可用余额。
- `frozen_balance` 表示冻结余额。
- 总余额统一在查询时计算：`available_balance + frozen_balance AS total_balance`。
- 后续任何 Entity、DTO、接口、前端页面都不得把 `PLAYER.wallet_balance` 当真实字段恢复。

## 9. 核心业务事务

以下业务必须在 C# Application 层使用事务，保证 Oracle 数据一致性。

### 9.1 购买游戏

涉及表：

```text
GAME_ORDER
ORDER_DETAIL
ORDER_STATUS_LOG
PAYMENT_TRANSACTION
WALLET_ACCOUNT
WALLET_TRANSACTION
PLAYER_LIBRARY
```

要求：

- 校验玩家状态、游戏状态、是否已拥有。
- 使用幂等键防止重复购买。
- 钱包扣款与游戏入库必须同事务。
- 写订单状态日志和钱包流水。
- 失败时整体回滚。

### 9.2 钱包充值与流水

要求：

- 钱包余额变化必须写 `WALLET_TRANSACTION`。
- 流水记录变动前后余额快照。
- 使用 `decimal`，禁止使用浮点数。

### 9.3 退款审核

要求：

- 不能超额退款。
- 每次审核必须写 `REFUND_AUDIT_LOG`。
- 退款入账必须写钱包流水。
- 必要时调整 `PLAYER_LIBRARY` 资产状态。

### 9.4 CDKey 兑换

要求：

- 成功和失败兑换都写 `CDKEY_REDEEM_LOG`。
- 同一 CDKey 只能成功兑换一次。
- 同一玩家同一游戏不能重复入库。

### 9.5 评价版本

要求：

- `GAME_REVIEW` 是一人一游戏主记录。
- `REVIEW_VERSION` 只追加，不覆盖历史版本。

### 9.6 成就解锁

要求：

- `UNIQUE(user_id, ach_id)` 防止重复解锁。
- 重复上报应返回“已解锁”，不能造成重复数据。

### 9.7 饰品市场撮合

要求：

- 买单冻结资金。
- 卖单锁定饰品。
- 同一饰品同一时刻只能有一个有效卖单。
- 成交后生成 `MARKET_TRADE`。
- 资产换手写 `ITEM_TRANSFER_LEDGER`。
- 资金清算写 `WALLET_TRANSACTION`。

## 10. 前端视觉原则

目标：前台页面要接近 Steam，而不是普通后台管理系统。

建议色彩：

```text
页面背景：#171a21
主内容背景：#1b2838
卡片背景：#16202d
浅蓝强调：#66c0f4
深蓝强调：#2a475e
绿色购买：#75b022
折扣绿色：#4c6b22
正文浅色：#c7d5e0
弱文本：#8f98a0
```

主要页面：

```text
/                 首页
/store            商店列表
/games/:id        游戏详情
/library          我的游戏库
/inventory        我的饰品库存
/market           饰品市场
/account          个人中心、钱包
/login            登录
/register         注册
/developer        开发商工作台
/admin            管理员后台
```

要求：

- 首页直接展示可用产品体验，不做空泛营销页。
- 游戏卡片、折扣标签、库存格子、市场页面要重点打磨。
- 管理端可以偏表格和表单，但仍使用统一暗色主题。
- 所有金额显示两位小数。
- 前端只做展示和交互，不做最终权限判断。

## 11. API 约定

统一 API 前缀：

```text
/api
```

示例接口：

```text
POST   /api/auth/login
POST   /api/auth/register
GET    /api/games
GET    /api/games/{gameId}
POST   /api/orders
GET    /api/orders/{orderId}
POST   /api/wallet/recharge
GET    /api/wallet/transactions
POST   /api/cdkeys/redeem
POST   /api/reviews
PUT    /api/reviews/{reviewId}
POST   /api/achievements/{achId}/unlock
GET    /api/inventory
POST   /api/market/orders
POST   /api/market/match
POST   /api/refunds
POST   /api/admin/refunds/{refundId}/approve
```

统一响应格式：

```json
{
  "code": 0,
  "message": "success",
  "data": {}
}
```

错误响应：

```json
{
  "code": 40001,
  "message": "余额不足",
  "data": null
}
```

建议 HTTP 状态：

- `200`：请求成功。
- `400`：参数错误。
- `401`：未登录或 token 无效。
- `403`：无权限。
- `404`：资源不存在。
- `409`：业务冲突，例如重复购买。
- `500`：服务器内部错误。

## 12. 权限与安全

角色：

```text
PLAYER      玩家
DEVELOPER   开发商
ADMIN       管理员
AUDITOR     审计员，可选
```

安全原则：

- 密码必须使用 BCrypt 或 ASP.NET Core PasswordHasher，不存明文。
- 登录成功后使用 JWT。
- 前端只保存 JWT，不保存密码。
- 敏感接口必须后端鉴权。
- 钱包、订单、市场、退款接口必须从 token 中获取当前用户，不信任前端传入的用户 ID。
- Oracle 连接字符串、JWT 密钥、云服务器密码不得提交 Git。
- 云端 Oracle 1521 不对公网开放。

## 13. 开发顺序计划

### 第 0 阶段：项目基线

状态：已完成，但因课程提纲更新，技术栈基线已调整为 C# / ASP.NET Core。

- [x] 建立 Git 仓库。
- [x] 建立 `backend/`、`frontend/`、`database/`、`docs/` 目录。
- [x] 准备 `.gitignore`。
- [x] 读取新版课程提纲并确认 C# / Oracle / VS.NET / C/S 或 B/S 要求。
- [x] 选择 B/S 架构。
- [x] 选择阿里云作为云部署平台。

### 第 1 阶段：数据库落地

状态：数据库脚本已完成阶段性验收，后续仍需在阿里云 Oracle 环境复验。

- [x] 根据设计文档生成 Oracle `schema.sql`。
- [x] 写主键、外键、唯一约束、检查约束。
- [x] 写必要索引。
- [x] 写初始化数据 `data.sql`。
- [x] 验证 27 张表能成功创建。
- [x] 验证关键唯一约束和外键有效。
- [ ] 在阿里云 Oracle 环境重新执行 `schema.sql`、`data.sql`、`verify_phase1.sql`。

### 第 2 阶段：C# 后端基础

- 状态：已完成 Group A 最小后端骨架，位置为 `backend/SteamPlatform.Backend.sln`；当前先采用单 API 项目控制复杂度，后续功能扩大时再按五层结构拆分。
- [x] 创建 `SteamPlatform.Backend.sln`。
- [x] 创建 ASP.NET Core Web API 项目：
  - `SteamPlatform.Api`
- [x] 配置 ASP.NET Core Web API。
- [x] 配置 Oracle 连接入口。
- [x] 配置 Dapper / ODP.NET 复杂 SQL 支持。
- [x] 配置统一异常。
- [x] 配置 Swagger / OpenAPI。
- [x] 完成健康检查接口。
- [ ] 后续按模块需要再拆分 Application / Domain / Infrastructure / Shared。
- [x] 接入标准 ASP.NET Core Authentication / JWT Bearer。

### 第 3 阶段：认证与用户

- [x] 玩家注册。
- [x] 玩家登录。
- [x] JWT Bearer 签发与校验。
- [x] 管理员登录。
- [ ] 开发商登录：当前 `DEVELOPER` 表没有密码哈希字段，暂不使用 `tax_id` 作为登录密码。
- [x] 基础角色权限。
- [x] `GET /api/auth/me` 当前用户接口。

### 第 4 阶段：商店与游戏基础

- 游戏列表。
- 游戏详情。
- 开发商游戏管理。
- 管理员上下架。
- 首页推荐和折扣接口。

### 第 5 阶段：钱包与订单主链路

- 钱包账户初始化。
- 充值模拟。
- 游戏购买事务。
- 订单状态日志。
- 玩家游戏库。
- 钱包流水查询。

### 第 6 阶段：社区与成就

- 发表评价。
- 修改评价生成版本。
- 查询评价历史。
- 成就字典。
- 玩家解锁成就。

### 第 7 阶段：CDKey 与资产确权

- 开发商创建 CDKey 批次。
- 生成 CDKey 哈希。
- 玩家兑换 CDKey。
- 写兑换尝试日志。
- 入库游戏资产。

### 第 8 阶段：饰品库存与市场

- 饰品模板。
- 饰品实例。
- 玩家库存。
- 买单和卖单。
- 冻结资金与锁定饰品。
- 撮合成交。
- 市场成交记录。
- 饰品流转账本。

### 第 9 阶段：退款与审计

- 玩家申请退款。
- 管理员审核。
- 写退款明细。
- 写审核日志。
- 退款入账和流水。
- 必要时调整游戏库资产状态。

### 第 10 阶段：Vue 前端与 Steam 风格

- [x] 创建 Vue 3 + Vite + TypeScript 项目。
- [x] 配置 Pinia、Vue Router、Axios。
- [x] 完成 Group A 登录、注册、账户中心、公告管理页面。
- 商店首页。
- 游戏详情。
- 游戏库。
- 饰品库存。
- 市场交易界面。
- 个人中心与钱包。
- 管理员后台。
- 开发商后台。

### 第 11 阶段：阿里云部署

- 购买阿里云 ECS。
- 安装 Oracle。
- 安装 .NET 10 Runtime。
- 安装 Nginx。
- 部署 Oracle schema 和 seed data。
- 发布 ASP.NET Core API。
- 发布 Vue 前端 dist。
- 配置 systemd 服务。
- 配置 Nginx 反向代理。
- 配置安全组。
- 执行云端验收。

### 第 12 阶段：测试、文档和答辩

- 核心业务流程测试。
- 并发/重复提交测试。
- 数据库约束测试。
- 云端部署测试。
- 系统需求分析文档。
- 系统设计与实现文档。
- 答辩 PPT。
- 演示脚本。

## 14. 最小可演示闭环

第一闭环：

```text
注册玩家
  -> 登录
  -> 查询游戏列表
  -> 查看游戏详情
```

第二闭环：

```text
充值钱包
  -> 购买游戏
  -> 钱包扣款并写流水
  -> 游戏进入玩家库
```

完整展示闭环：

```text
注册玩家
  -> 登录
  -> 充值钱包
  -> 浏览游戏
  -> 购买游戏
  -> 游戏进入库
  -> 发表评价
  -> 修改评价生成历史版本
  -> 解锁成就
  -> 获得饰品
  -> 市场挂单
  -> 撮合成交
  -> 生成市场成交和饰品流转账本
```

退款闭环：

```text
玩家购买游戏
  -> 申请退款
  -> 管理员审核
  -> 钱包退款入账
  -> 写退款审核日志
```

## 15. 团队分工

本项目 10 人分为 4 个功能组。采用按功能模块纵向分工的方式，每组都负责自己模块的数据库理解、后端接口、前端页面、测试数据、文档片段和演示流程。

总负责人：

```text
马祥珲
```

总负责人职责：

- 维护 README、技术路线、阶段计划和决策记录。
- 把关 ASP.NET Core 五层结构和 Vue 项目整体结构。
- 统一数据库脚本变更。
- 统一接口风格、命名规范、错误码和响应格式。
- 统一 Git 分支、提交、合并和代码检查。
- 统筹云服务器部署、最终联调和答辩演示流程。
- 各组出现设计冲突时做最终裁决。

四组分工：

| Group | 成员 | 模块定位 |
|---|---|---|
| Group A | 李胤龙、元梓浩 | 基础架构、用户权限、公告 |
| Group B | 周力扬、王子轩 | 游戏商店、游戏详情、开发商游戏管理 |
| Group C | 马祥珲、胡知鱼、徐京 | 核心交易、钱包、订单、退款、CDKey、游戏库 |
| Group D | 靳岱泽、郭炫君、张茗博 | 社区互动、成就、饰品库存、市场交易 |

### 15.1 Group A：基础架构、用户权限、公告

成员：

```text
李胤龙
元梓浩
```

模块定位：项目地基组。

负责范围：

- ASP.NET Core 后端基础结构协助。
- Vue 前端基础结构协助。
- 玩家注册。
- 玩家登录。
- 管理员登录。
- 开发商登录契约预留；待 `DEVELOPER` 表增加安全凭据字段后接入。
- JWT 签发与校验。
- 角色权限控制。
- 个人资料基础接口。
- 系统公告发布、查询、置顶、过期状态。
- 前端路由守卫。
- Axios 请求封装协助。
- Swagger 接口整理协助。

涉及表：

```text
PLAYER
ADMIN_USER
DEVELOPER
SYS_NOTICE
WALLET_ACCOUNT
```

主要后端接口：

```text
POST   /api/auth/register
POST   /api/auth/login
GET    /api/auth/me
GET    /api/notices
POST   /api/admin/notices
PUT    /api/admin/notices/{noticeId}
```

主要前端页面：

```text
/login
/register
/account
/admin/notices
/developer/profile
```

组内建议细分：

- 李胤龙：认证、JWT、角色权限、后端接口。
- 元梓浩：登录注册页面、公告页面、前端路由守卫、接口联调。

### 15.2 Group B：游戏商店、游戏详情、开发商游戏管理

成员：

```text
周力扬
王子轩
```

模块定位：用户第一眼看到的门面组。

负责范围：

- 商店首页。
- 游戏列表。
- 游戏搜索和筛选。
- 游戏详情。
- 折扣展示。
- 游戏口碑展示。
- 游戏详情中的评价概览。
- 游戏详情中的成就概览。
- 开发商新增、编辑游戏。
- 管理员上下架游戏。
- Steam 风格游戏卡片和游戏详情视觉。

涉及表：

```text
GAME
DEVELOPER
SYS_NOTICE
GAME_REVIEW
REVIEW_VERSION
ACHIEVEMENT
```

主要后端接口：

```text
GET    /api/games
GET    /api/games/{gameId}
GET    /api/games/{gameId}/reviews/summary
GET    /api/games/{gameId}/achievements/summary
POST   /api/developer/games
PUT    /api/developer/games/{gameId}
POST   /api/admin/games/{gameId}/online
POST   /api/admin/games/{gameId}/offline
```

主要前端页面：

```text
/
/store
/games/:id
/developer/games
/admin/games
```

组内建议细分：

- 周力扬：游戏、开发商、管理员游戏管理后端接口。
- 王子轩：商店首页、游戏列表、详情页、Steam 风格 UI。

### 15.3 Group C：核心交易、钱包、订单、退款、CDKey、游戏库

成员：

```text
马祥珲
胡知鱼
徐京
```

模块定位：最核心、最难的主链路组。

负责范围：

- 钱包账户。
- 钱包充值模拟。
- 钱包流水。
- 游戏购买。
- 创建订单。
- 订单明细。
- 支付流水模拟。
- 订单状态日志。
- 玩家游戏库。
- 游玩时长。
- 退款申请。
- 退款审核。
- 退款明细。
- 退款审核日志。
- CDKey 批次。
- CDKey 兑换。
- CDKey 兑换日志。
- 兑换成功后游戏入库。
- 防重复购买。
- 防重复兑换。
- 幂等键。
- 事务一致性。

涉及表：

```text
WALLET_ACCOUNT
WALLET_TRANSACTION
GAME_ORDER
ORDER_DETAIL
ORDER_STATUS_LOG
PAYMENT_TRANSACTION
PLAYER_LIBRARY
REFUND_TICKET
REFUND_DETAIL
REFUND_AUDIT_LOG
CDKEY_BATCH
CDKEY
CDKEY_REDEEM_LOG
GAME
PLAYER
ADMIN_USER
```

主要后端接口：

```text
GET    /api/wallet
POST   /api/wallet/recharge
GET    /api/wallet/transactions
POST   /api/orders
GET    /api/orders
GET    /api/orders/{orderId}
GET    /api/library
POST   /api/library/{gameId}/playtime
POST   /api/refunds
GET    /api/refunds
POST   /api/admin/refunds/{refundId}/approve
POST   /api/admin/refunds/{refundId}/reject
POST   /api/developer/cdkey-batches
POST   /api/cdkeys/redeem
```

主要前端页面：

```text
/account/wallet
/orders
/orders/:id
/library
/refunds
/refunds/new
/admin/refunds
/developer/cdkey-batches
/redeem
/games/:id 的购买区域
```

组内建议细分：

- 马祥珲：核心交易总设计、购买事务、统一接口/代码规范、最终集成。
- 胡知鱼：钱包、充值、资金流水、金额校验、余额展示页面。
- 徐京：退款、CDKey、游戏库、资产确权、相关前端页面。

Group C 必须优先保证的演示链路：

```text
登录
  -> 充值钱包
  -> 购买游戏
  -> 钱包扣款
  -> 写钱包流水
  -> 生成订单
  -> 游戏进入库
  -> 申请退款
  -> 管理员审核
  -> 退款流水
```

### 15.4 Group D：社区互动、成就、饰品库存、市场交易

成员：

```text
靳岱泽
郭炫君
张茗博
```

模块定位：项目亮点组。

负责范围：

- 游戏评价。
- 评价历史版本。
- 评价点赞或隐藏。
- 成就字典。
- 玩家解锁成就。
- 饰品模板。
- 饰品实例。
- 玩家饰品库存。
- 饰品掉落模拟。
- 市场买单。
- 市场卖单。
- 冻结资金。
- 锁定饰品。
- 市场撮合。
- 市场成交记录。
- 饰品流转账本。
- 市场价格展示。

涉及表：

```text
GAME_REVIEW
REVIEW_VERSION
ACHIEVEMENT
PLAYER_ACHIEVEMENT
ITEM_TEMPLATE
INVENTORY_ITEM
MARKET_ORDER
MARKET_TRADE
ITEM_TRANSFER_LEDGER
WALLET_ACCOUNT
WALLET_TRANSACTION
GAME
PLAYER
```

主要后端接口：

```text
GET    /api/games/{gameId}/reviews
POST   /api/games/{gameId}/reviews
PUT    /api/reviews/{reviewId}
GET    /api/reviews/{reviewId}/versions
GET    /api/games/{gameId}/achievements
POST   /api/achievements/{achId}/unlock
GET    /api/inventory
POST   /api/inventory/drop
GET    /api/market
POST   /api/market/orders
POST   /api/market/orders/{marketOrderId}/cancel
POST   /api/market/match
GET    /api/market/trades
GET    /api/items/{itemId}/transfers
```

主要前端页面：

```text
/games/:id 的评价区
/games/:id 的成就区
/inventory
/market
/market/orders
/market/trades
```

组内建议细分：

- 靳岱泽：评价、评价版本、成就解锁、相关前端。
- 郭炫君：饰品模板、饰品实例、玩家库存、掉落模拟、相关前端。
- 张茗博：市场挂单、撮合成交、流转账本、市场页面。

Group D 必须优先保证的演示链路：

```text
发表评价
  -> 修改评价
  -> 查看历史版本
  -> 解锁成就
  -> 获得饰品
  -> 上架卖单
  -> 创建买单
  -> 撮合成交
  -> 饰品换手
  -> 写流转账本
```

## 16. 各组交付规范

每个 Group 的每个功能必须按纵向闭环交付，不能只交后端或只交前端。

### 16.1 功能交付清单

每个功能至少包含：

```text
1. 涉及表说明
2. 后端 Controller
3. Application Service
4. Repository / Dapper SQL / EF Core 查询
5. Request / Response DTO
6. 前端页面或组件
7. API 调用封装
8. 正常流程测试
9. 至少一个失败流程测试
10. 初始化或演示数据
11. 文档说明
12. 可演示截图或录屏
```

### 16.2 后端交付要求

后端功能交付时必须满足：

- Controller 只负责接收请求、基础参数校验和返回响应。
- 业务逻辑必须写在 Application 层。
- 数据库访问必须写在 Infrastructure 层。
- 事务边界必须清楚，核心业务用事务包裹。
- 金额必须使用 `decimal`。
- 异步数据库操作方法使用 `Async` 后缀。
- 不允许在 Controller 中拼 SQL。
- 不允许在前端传来的 `userId` 上直接信任当前用户身份，必须从 JWT 中取当前用户。
- 所有新增接口必须能在 Swagger 中看到。
- 所有业务错误必须抛业务异常或返回统一错误码，不允许随便返回字符串。

### 16.3 前端交付要求

前端功能交付时必须满足：

- 页面接入真实 API，不只是假数据。
- 必须有加载态。
- 必须有错误态。
- 必须有空状态。
- 表单必须做前端基础校验。
- 金额展示保留两位小数。
- 页面风格遵守 Steam 深色主题。
- 不允许直接在页面里散落 API URL，必须通过 `src/api/` 封装。
- 不允许前端自行决定最终权限，权限以服务端校验为准。

### 16.4 测试交付要求

每个功能至少提供：

- 1 条正常流程测试。
- 1 条失败流程测试。
- 涉及资金、订单、退款、市场撮合的功能必须提供重复提交或并发风险说明。
- 涉及数据库唯一约束、外键、检查约束的功能必须说明对应约束。
- 可使用 Apifox、Postman、Swagger 或前端页面进行演示。

### 16.5 文档交付要求

每个 Group 必须为自己模块提供文档片段：

- 模块功能说明。
- 涉及数据库表。
- 核心业务流程。
- API 列表。
- 关键事务和约束。
- 测试用例。
- 演示步骤。
- 至少 1 张界面截图。

文档片段最终由马祥珲统一整合进系统需求分析文档、系统设计与实现文档和答辩 PPT。

## 17. 统一代码规范

### 17.1 C# 后端规范

命名：

- 项目名：`SteamPlatform.Api`、`SteamPlatform.Application` 等。
- 命名空间与项目结构保持一致。
- 类名、方法名、属性名使用 `PascalCase`。
- 私有字段使用 `_camelCase`。
- 局部变量和参数使用 `camelCase`。
- 异步方法以 `Async` 结尾。
- 接口以 `I` 开头，例如 `IOrderService`。

分层：

- `Api` 只引用 `Application` 和 `Shared`。
- `Application` 可引用 `Domain` 和 `Shared`。
- `Infrastructure` 可引用 `Application`、`Domain`、`Shared`。
- `Domain` 不引用 `Infrastructure`。
- 禁止跨层乱引用。

代码风格：

- 使用 `nullable enable`。
- 优先使用构造函数注入。
- 不写魔法字符串，状态值优先使用常量或枚举映射。
- 公共方法参数必须做必要校验。
- 捕获异常时不能吞异常。
- 日志中不能输出密码、token、连接字符串。
- 单个方法过长时必须拆分。

事务规范：

- 购买、退款、CDKey 兑换、市场撮合必须显式事务。
- 事务中所有数据库写操作必须共享同一连接/事务上下文。
- 失败时必须回滚。
- 事务完成后再返回前端成功。

### 17.2 SQL 与数据库规范

- 表结构变更必须先改 `database/schema.sql`。
- 初始化数据变更必须改 `database/data.sql`。
- 验收逻辑变更必须改 `database/verify_phase1.sql`。
- 不允许绕过 README 决策恢复 `PLAYER.wallet_balance`。
- 新增表必须有主键。
- 关键外键必须写约束。
- 状态字段必须有 `CHECK` 或在后端有明确枚举校验。
- 涉及幂等的字段必须加唯一约束。
- 账本和日志表原则上只追加，不物理删除。

### 17.3 前端规范

- 页面放在 `src/views/`。
- 通用组件放在 `src/components/`。
- API 封装放在 `src/api/`。
- Pinia store 放在 `src/stores/`。
- 路由定义放在 `src/router/`。
- 样式变量和主题放在 `src/styles/`。
- 组件名使用 `PascalCase`。
- API 文件使用 `camelCase`，例如 `gameApi.ts`。
- 不在组件里硬编码重复的颜色值，优先使用主题变量或公共样式。
- 不提交无用 console 调试输出。

### 17.4 API 规范

统一前缀：

```text
/api
```

路径使用资源名复数：

```text
/api/games
/api/orders
/api/refunds
/api/market/orders
```

请求方法：

- `GET` 查询。
- `POST` 创建或业务动作。
- `PUT` 整体更新。
- `PATCH` 局部更新。
- `DELETE` 删除或取消，谨慎使用。

统一响应：

```json
{
  "code": 0,
  "message": "success",
  "data": {}
}
```

分页响应：

```json
{
  "code": 0,
  "message": "success",
  "data": {
    "items": [],
    "page": 1,
    "pageSize": 20,
    "total": 100
  }
}
```

错误响应：

```json
{
  "code": 40001,
  "message": "余额不足",
  "data": null
}
```

### 17.5 Git 协作规范

分支：

```text
main
dev
feature/group-a-auth
feature/group-b-games
feature/group-c-orders
feature/group-d-market
fix/*
docs/*
```

提交格式：

```text
feat(auth): add player login
feat(order): add purchase transaction
fix(wallet): prevent negative balance
docs(readme): update group responsibilities
style(frontend): polish game card layout
test(order): add duplicate purchase case
```

合并要求：

- 不直接向 `main` 提交。
- 功能先合并到 `dev`。
- 合并前至少自测通过。
- 涉及公共结构、数据库、接口格式的改动必须通知马祥珲。
- 发生冲突时优先保留已写入 README 的规范。

禁止提交：

- 数据库密码。
- JWT 密钥。
- 云服务器密码。
- `.env.local`。
- `appsettings.Local.json`。
- `bin/`、`obj/`、`node_modules/`、`dist/`。
- 大型安装包和个人工具目录。

## 18. 命名约定

数据库：

- 表名使用大写蛇形命名：`GAME_ORDER`。
- 字段名使用小写蛇形命名：`order_id`。
- 约束名明确表达作用。

示例：

```text
PK_PLAYER
FK_ORDER_PLAYER
UK_PLAYER_ACCOUNT
CK_WALLET_AVAILABLE_BALANCE
IDX_ORDER_USER_TIME
```

C#：

- 项目名：`SteamPlatform.Api`、`SteamPlatform.Application` 等。
- 类名：`UpperCamelCase`。
- 方法和属性：`UpperCamelCase`。
- 私有字段：`_lowerCamelCase`。
- 异步方法以 `Async` 结尾。
- Entity 示例：`GameOrder`。
- Request 示例：`CreateOrderRequest`。
- Response 示例：`GameDetailResponse`。
- Service 示例：`OrderService`。
- Repository 示例：`OrderRepository`。

前端：

- 组件：`UpperCamelCase.vue`。
- 页面：`GameDetailView.vue`。
- API 文件：`gameApi.ts`。
- Store：`userStore.ts`。

## 19. 配置文件原则

后端配置：

```text
appsettings.json
appsettings.Development.json
appsettings.Production.json
appsettings.Local.json       不提交 Git
```

前端配置：

```text
.env.development
.env.production
.env.local                   不提交 Git
```

云端敏感配置：

- 使用环境变量或服务器私有配置文件。
- 不写入仓库。
- 不写入 README。

## 20. 测试与验收重点

必须测试：

- 重复购买同一游戏。
- 余额不足购买。
- 重复提交同一幂等键。
- 钱包余额不能扣成负数。
- 同一 CDKey 重复兑换。
- 同一玩家同一成就重复解锁。
- 同一饰品重复挂卖。
- 退款不能超过订单明细实付金额。
- 市场成交后钱包、库存、流水、成交记录、流转账本一致。
- 云端部署后公网前端可访问。
- 云端 `/api/health` 可访问。
- Oracle 1521 不对公网开放。

测试方式：

- C# 单元测试：Application 层核心事务。
- 接口测试：Apifox / Postman。
- SQL 验证：SQL*Plus 查询 Oracle。
- 前端测试：浏览器手动流程。
- 云端验收：从非服务器环境访问公网地址。

## 21. 已完成验收记录

### 第 0 阶段

已完成：

- Git 仓库初始化。
- 基础目录创建。
- `.gitignore` 创建。
- 新版课程提纲已读取。
- 架构从旧 Spring Boot 方案调整为 C# / ASP.NET Core B/S 方案。
- 云平台选择阿里云。

### 第 1 阶段

数据库阶段性验收已完成：

- 27 张核心表创建。
- `PLAYER.wallet_balance` 确认不存在。
- 主键、外键、唯一约束、检查约束创建。
- 初始化数据覆盖 27 张核心表。
- 关键约束验证通过。

验收脚本：

```text
database/schema.sql
database/data.sql
database/verify_phase1.sql
database/admin/create_phase1_user.sql
database/admin/run_phase1_verification.sql
```

验收结论：

```text
core table count = 27
PLAYER.wallet_balance column count = 0
primary key constraint count = 27
foreign key constraint count = 40
unique constraint count = 13
check constraint count = 222
Phase 1 database verification passed
```

待完成：

- 在阿里云服务器 Oracle 环境重新执行数据库验收。

## 22. 当前决策记录

| 日期 | 决策 | 原因 |
|---|---|---|
| 2026-07-05 | 删除 `PLAYER.wallet_balance`，资金唯一真相放在 `WALLET_ACCOUNT` | 避免余额冗余导致账实不一致 |
| 2026-07-05 | 完成数据库第 1 阶段脚本验收 | 证明 27 张表、约束和初始化数据可执行 |
| 2026-07-06 | 根据新版课程提纲废弃 Java / Spring Boot / MyBatis 方案 | 课程要求 VS.NET、C#、Oracle、Oracle 数据访问组件或 ORM |
| 2026-07-06 | 选择 B/S 架构 | 课程允许 C/S 或 B/S；B/S 更适合 Steam 风格界面和云部署 |
| 2026-07-06 | 选择阿里云作为云部署平台 | 应用服务器和数据库均需部署到云服务器 |
| 2026-07-06 | 后端采用 ASP.NET Core Web API + 五层结构 | 符合 C# 要求，层次清晰，便于答辩说明 |
| 2026-07-06 | 数据访问采用 Oracle EF Core + Dapper / ODP.NET | 兼顾 ORM 规范性与复杂 SQL 可控性 |
| 2026-07-06 | 确定 .NET 10 SDK 与 dotnet-ef 10.x 作为开发工具链基线 | 支持 ASP.NET Core / EF Core 10 开发 |
| 2026-07-06 | 确定四组纵向功能分工，马祥珲担任唯一总负责人 | 每组同时交付前端、后端、测试和文档，降低前后端等待和集成风险 |

## 23. 变更维护规则

以下情况必须更新 README：

- 技术栈变化。
- 云平台变化。
- C/S 或 B/S 架构变化。
- 数据库表结构变化。
- API 统一格式变化。
- 权限角色变化。
- 部署方式变化。
- 实际开发顺序与计划明显不同。
- 引入 Redis、对象存储、消息队列等新基础设施。
- 任何会影响组员开发、部署、答辩说明的决策。

原则：

```text
实际怎么做，README 就怎么写。
README 写了什么，后续开发就尽量照着做。
如果二者不一致，必须尽快修正 README 或修正实现。
```

## 24. 下一步

下一步从 Group A 真实环境联调和验收开始：

1. 确认 Oracle 已加载 `schema.sql` 和 `data.sql`。
2. 配置后端 `ConnectionStrings:Oracle` 与 `Auth:SigningKey`，启动 `backend/src/SteamPlatform.Api`。
3. 在 `frontend/` 运行 `npm run dev`，通过 Vite 代理访问后端 API。
4. 使用 `alice/alice`、`bob/bob`、`rootadmin/admin` 验证注册、登录、当前用户、公告发布和公告更新流程。
5. 后续再把后端按模块规模拆分 Application / Domain / Infrastructure / Shared。
