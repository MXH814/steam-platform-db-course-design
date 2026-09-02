# "Steam-" 数字游戏平台系统课程设计 README

> 本 README 是项目后续设计、开发、部署、验收和答辩的统一参考文件。  
> 任何技术选型、部署方案、数据库结构、开发计划、实际实现与本文档不一致时，必须先更新 README，再继续开发。

## 协作铁律：普通组员禁止直接推送 main

`main` 分支是项目稳定主线，必须始终保持可运行、可演示、可用于答辩。

普通组员开发功能时必须遵守以下流程：

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

- 普通组员禁止直接 push 到 `main`。
- 禁止 force push。
- 禁止删除 `main`。
- 普通组员合并前必须通过 Pull Request。
- 普通组员 Pull Request 至少需要 1 个 review。
- Pull Request 中未解决的讨论必须先处理完。
- `.github/workflows/ci.yml` 会在 Pull Request 和 `main` 更新时自动执行 C# 格式检查、.NET Release 构建与测试、演示数据基线解析、前端依赖审计和 Vue 生产构建；普通组员的 PR 必须通过该检查后才能合并。
- 总负责人马祥珲本人，或经马祥珲明确授权由 Codex 完成且已通过构建/测试/安全检查的提交，可以由管理员直接合并到 `main`，不需要等待 Pull Request 审批。
- 其他组员提交的业务代码仍必须走 Pull Request 审查流程。

任何数据库表结构、公共接口、统一响应格式、技术路线、部署方案、团队分工和交付规范变更，都必须在 Pull Request 中同步更新 README。

技术路线和分工属于总负责人统一管理的项目级决策：

- 组员不能在功能 PR 中自行修改技术路线、系统架构、团队分工、模块边界和交付规范。
- 未经马祥珲明确确认，不得把 C# / .NET / Oracle / B/S、五层结构、前端技术方向、组别职责或总负责人规则改掉。
- 未经马祥珲明确确认，不得更换最终样板游戏。当前最终样板游戏固定为 `Counter-Strike 2` 和 `Don't Starve Together / 饥荒联机版`。
- 如果确实认为既定方案需要调整，必须先提出单独讨论，不得夹带在功能代码 PR 中直接改 README。

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
- 数据库和应用服务器均部署到腾讯云云服务器。
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
- 好友关系、私聊消息、评测互动、工坊订阅与玩家通知。

### 2.1 最终样板游戏与统一业务口径

本项目最终只选用两款真实 Steam 游戏作为公开演示和测试数据口径：

| 游戏 | Steam AppID | 项目内定位 | 主要承载模块 |
|---|---:|---|---|
| `Counter-Strike 2` | `730` | 饰品经济、库存、市场交易、热门免费游戏样板 | 饰品模板、饰品实例、库存、市场卖单/买单、成交、价格历史、Steam 风格详情页 |
| `Don't Starve Together / 饥荒联机版` | `322330` | 买断制联机生存、DLC/皮肤箱、创意工坊、社区样板 | 游戏购买、游戏库、DLC/礼包、评价、公告、创意工坊入口、社区讨论、成就模拟 |

选择依据：

- `Counter-Strike 2` Steam 商店页展示了多人、跨平台多人、集换式卡牌、Steam 创意工坊、游戏内购买、VAC、统计等特性，适合作为饰品经济和市场交易样板。
- `Counter-Strike 2` 在 Steam 社区市场存在大量可交易物品，适合作为库存、挂单、价格展示、成交记录和饰品流转账本的演示数据来源。
- `Don't Starve Together / 饥荒联机版` Steam 商店页展示为多人联机生存游戏，官方描述重点是共同战斗、耕作、建造和探索，且商店页存在大量 `Chest`、`Starter Pack` 等内容，适合作为买断制游戏、DLC/礼包、皮肤箱和活动公告样板。
- `Don't Starve Together / 饥荒联机版` 在 Steam 社区市场也存在可交易物品，适合补充低价饰品、服装、节日物品等市场样例。

统一命名规则：

- 文档、接口、种子数据和页面标题中统一写 `Counter-Strike 2`，简称可写 `CS2`。
- 文档、接口、种子数据和页面标题中统一写 `Don't Starve Together / 饥荒联机版`，简称可写 `DST`。
- 数据库主键或业务编码统一使用稳定编码：`GAME_CS2`、`GAME_DST`。
- 不再使用 `Team Fortress 2` 作为演示主线或种子数据主样板。
- 不再临时增加第三款主演示游戏；如果确需增加，只能作为少量背景数据，必须先更新 README。

数据与版权边界：

- 可以使用真实游戏名称、AppID、官方公开功能特征、标签方向和业务结构作为课程设计参考。
- 本项目是课程设计和非商业答辩演示，可以使用与 `CS2`、`DST` 和 Steam 风格界面相关的图片素材来提升展示效果。
- PR 审查时不因使用游戏或 Steam 相关演示素材本身阻塞合并。
- 页面、README、项目名称和系统 Logo 不得暗示本系统由 Steam、Valve、Klei 或其他官方授权、发布或运营。
- 不把 Steam 官方商标或游戏商标当作本项目自有品牌标识。
- 图片文件体积必须适合 Git 仓库维护，不能提交明显无关、过大或影响加载体验的媒体文件。
- 价格、折扣、销量、评价数量、库存数量、成交价格均作为课程演示模拟数据，不追求实时同步 Steam。
- README 中记录的外部资料只作为设计依据，不作为爬取或同步数据源。

模块分工口径：

- Group B 负责把两款游戏在商店、搜索、详情、标签、DLC/礼包、公告入口中的展示口径做统一。
- Group C 负责围绕 `GAME_DST` 完成买断制购买、钱包扣款、游戏入库、退款、CDKey；围绕 `GAME_CS2` 完成免费游戏入库或零元入库规则。
- Group D 负责围绕 `GAME_CS2` 完成饰品库存和市场交易主链路；围绕 `GAME_DST` 完成评价、社区、成就模拟、皮肤箱/服装类饰品补充样例。

最低种子数据要求：

- `GAME` 至少包含 `GAME_CS2` 和 `GAME_DST`。
- `GAME_CS2` 的价格口径为免费入库或 0 元购买；不能被当作普通收费买断制游戏处理。
- `GAME_DST` 的价格口径为买断制游戏，并可配置折扣。
- 内容包 / 商品扩展数据至少为 `GAME_DST` 准备 2 个皮肤箱或礼包样例；当前不强制新增独立 `DLC` 表，可先复用 `GAME`、`ORDER_DETAIL`、`ITEM_TEMPLATE` 等既有结构表达。
- `ITEM_TEMPLATE` 至少包含 8 个 `GAME_CS2` 饰品模板和 4 个 `GAME_DST` 饰品模板。
- `INVENTORY_ITEM` 至少准备 2 个玩家之间可换手的 `GAME_CS2` 饰品实例。
- `MARKET_ORDER` 和 `MARKET_TRADE` 的演示数据优先使用 `GAME_CS2` 饰品。
- 如果实现成就功能，`GAME_DST` 使用课程项目自定义成就，不声称与 Steam 官方成就完全一致。

主要参考文件：

- `2026《数据库课程设计》课程提纲.doc`
- `项目文档/“Steam-”数字游戏平台系统数据库设计文档.docx`
- `图（改）/E-R图/`
- `图（改）/数据库关系图/`
- `“Steam-”数字游戏平台系统（改）.pdma`

当前项目目录保留原则：

- 根目录只保留当前正式方案需要的代码、脚本、文档、模板、图和新版模型文件。
- 当前正式 E-R 图以 `E-R图（改）.drawio` 和 `图（改）/` 为准。
- 当前正式数据库模型以 `“Steam-”数字游戏平台系统（改）.pdma` 为准。
- 旧版图、旧版 PDMaas 模型、历史实验工程、临时日志、备份文件和旧压缩包统一放入项目归档目录。
- 第三方工具解压目录和安装包不属于项目归档，不提交 GitHub。

项目归档目录：

```text
_archive/legacy-files-2026-07-07/
```

该目录用于保留我们自己做过的旧项目文件，需要提交到 GitHub，便于追溯设计演变。归档目录当前包含旧版 `图/`、旧版 `E-R图.drawio`、旧版 `pdmaas/`、`WindowsFormsApp1/` 源码、数据库验证日志、备份文件和旧压缩包。

本机工具归档目录：

```text
_local_tools_archive/
```

该目录只用于本机保留第三方工具和安装包，例如 Draw.io、PDMaas-Pro 的安装包或解压目录，已写入 `.gitignore`，不得提交到 GitHub。

云服务器 SSH 私钥不得放入 Git 仓库。`*.pem` 和 `*.key` 已写入 `.gitignore`。任何云厂商的私钥都只能作为本地私密文件保存，不能提交、不能发到群里、不能写进文档。

## 3. 最终架构选择

选择：B/S 架构。

```text
浏览器
  -> Vue 3 前端页面
  -> HTTPS（启用后 HTTP 仅用于 ACME 验证和 308 跳转）
  -> 腾讯云 Nginx
  -> ASP.NET Core Web API 应用服务器
  -> EF Core / Dapper / ODP.NET
  -> Oracle Database
```

选择 B/S 的原因：

- 课程允许 C/S 或 B/S，B/S 符合要求。
- Steam 风格界面更适合 Web 前端实现。
- 答辩演示只需浏览器访问云服务器地址。
- 应用服务器和数据库都部署在腾讯云服务器，满足云部署要求。
- 前后端分离方便团队协作。
- Oracle 端口不需要暴露给客户端，安全性明显好于桌面客户端直连数据库。

不选择纯 C/S 的原因：

- 桌面客户端直连云 Oracle 需要暴露数据库端口，安全性差。
- WinForms/WPF 做 Steam 风格界面工作量更大。
- 客户端部署、版本更新和答辩演示都更麻烦。

## 4. 项目技术栈

| 层级 | 选型 | 说明 |
|---|---|---|
| 云平台 | 腾讯云轻量应用服务器 | 运行 Oracle、ASP.NET Core API、Nginx、前端静态文件 |
| 操作系统 | Ubuntu Server 22.04 LTS 64-bit | 当前腾讯云轻量应用服务器系统镜像 |
| 数据库 | Oracle Database Free / Oracle 26ai Free，满足 Oracle 18c+ 要求 | 课程要求 Oracle 18c 或更高版本 |
| 后端语言 | C# | 课程提纲硬要求 |
| 应用服务器 | ASP.NET Core Web API on .NET 10 LTS | C# Web API，运行于 Kestrel，前置 Nginx 反向代理 |
| IDE | Visual Studio Community 2022 或更新版本 | 满足课程对 VS.NET 较新版本的要求，团队统一使用 |
| ORM | Oracle.EntityFrameworkCore | Oracle 官方 EF Core Provider |
| Oracle 数据访问 | Oracle.ManagedDataAccess.Core | Oracle 官方 ODP.NET Core 驱动 |
| 复杂 SQL | Dapper + ODP.NET | 钱包流水、市场撮合、报表查询等复杂 SQL 可控 |
| API 权限 | ASP.NET Core Authentication + JWT | 玩家、开发商、管理员分角色鉴权 |
| 实时通信 | ASP.NET Core SignalR + `@microsoft/signalr 10.x` | 好友消息、社交通知和状态变化按玩家私有分组推送 |
| 前端 | Vue 3 + Vite + TypeScript | Steam 风格 Web 界面 |
| 前端状态 | Pinia | 登录状态、用户信息、钱包、购物车等 |
| 前端路由 | Vue Router | 页面路由 |
| 前端请求 | Axios | 调用 ASP.NET Core API |
| UI 组件 | 自定义 Vue 组件 | 与 Steam 风格页面保持一致，避免引入额外大型 UI 框架 |
| CSS | 自定义 Steam 深色主题 CSS | 游戏商城、库存、市场等视觉效果 |
| Web Server | Nginx | 托管前端静态文件并反向代理 `/api` |
| API 调试 | Apifox 或 Postman | 接口测试 |
| 版本管理 | Git + GitHub | 代码提交、协作、考勤依据 |

NuGet 包版本基线：

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

开发工具基线：

| 工具 | 要求 |
|---|---|
| Visual Studio Community 2022 或更新版本 | 必须包含 ASP.NET and web development 工作负载 |
| .NET SDK | 使用 .NET 10 SDK，确保能创建和构建 ASP.NET Core Web API 项目 |
| ASP.NET Core Runtime | 与项目目标框架保持一致 |
| Entity Framework CLI | 使用 `dotnet-ef 10.x` |
| Node.js | 使用当前 LTS 或团队统一指定版本 |
| npm | 随 Node.js 安装，使用团队统一镜像源策略 |
| Git | 用于代码版本管理 |
| Oracle 客户端工具 | 使用 SQL*Plus、SQL Developer 或 DataGrip，至少保证能连接 Oracle 并执行脚本 |
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

云平台：腾讯云轻量应用服务器。

当前项目最终云平台是腾讯云。部署、联调和文档说明都以腾讯云轻量应用服务器为准。

已确定云服务器配置：

```text
规格：4 核 CPU，4GB 内存
系统盘：40GB SSD
公网带宽：3Mbps
月流量包：300GB
地域：上海
系统：Ubuntu Server 22.04 LTS 64-bit
购买时长：1 年
```

本项目按该实例完成 Oracle、ASP.NET Core API、Nginx 和 Vue 静态前端的同机部署。开发和演示阶段必须控制内存占用，避免同时运行不必要的后台服务。

云服务器部署结构：

```text
腾讯云轻量应用服务器
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
22    SSH，仅管理员维护使用；公网安全组保持最小授权，长期维护以密钥登录为准
80    HTTP
443   HTTPS；先使用可信公网 IP 证书，未来备案域名后替换主机名与证书
```

不对公网开放：

```text
1521  Oracle，只允许服务器内部访问
5000  ASP.NET Core Kestrel，只允许 Nginx 在服务器内部反向代理
```

Nginx 路由配置：

```text
/        -> Vue 前端静态文件
/api     -> ASP.NET Core Web API
/hubs    -> ASP.NET Core SignalR，Nginx 使用 WebSocket Upgrade 反向代理
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

后端目录结构：

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
- Steam 钱包支付的退款入账必须写钱包流水；外部模拟支付退款按原支付方式记录状态，不增加 Steam 钱包余额。
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

色彩规范：

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
/account          个人中心
/wallet           钱包充值
/wallet/history   消费历史记录
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
- 桌面端采用与 Steam 客户端一致的三层导航、紧凑内容密度、深色信息层级、底部状态栏和侧边抽屉交互；项目使用 `Game Deck` 自有名称与标识。
- 所有用户可见按钮、链接、图标按钮、选项卡和筛选器必须产生有效结果：完成路由跳转、状态切换、数据筛选、弹窗开关或真实业务提交，不允许保留没有响应的装饰按钮。
- 暂时只影响浏览体验的设置可以保存在 `localStorage`，JWT 只保存在当前标签页的 `sessionStorage`；购买、钱包、退款、兑换、评价、成就、库存和市场等业务状态必须通过真实 API 与 Oracle 持久化。
- 可提交操作必须提供禁用态或防重复提交状态；成功、失败和空数据都要有可见反馈。
- 新增或修改前端页面后必须在浏览器中完成桌面端和窄屏检查，并确认中文、图片、菜单浮层和长文本没有重叠或溢出。

当前已统一的前端交互基线：

- 根地址 `/` 和普通登录成功后的默认落点均为商店 `/store`，不再把独立公告列表作为首屏；启动公告以覆盖在商店之上的轮播浮窗展示，同一浏览器会话关闭后不重复弹出，并可从顶部通知入口再次打开。
- 全局客户端外壳包含顶部应用菜单、商店/库/社区/个人主导航、通知、账户菜单、下载抽屉、好友与聊天抽屉以及底部状态栏。
- 商店、游戏详情、游戏库、库内详情、库存、社区市场、社区评测与成就、钱包、消费历史、退款、CDKey 兑换、个人中心、开发商工作台和管理员后台均使用统一主题与路由。
- 游戏库支持搜索、最近游玩筛选、排序、紧凑视图、启动选项、游戏设置和本地笔记；库存支持按游戏、稀有度和状态筛选；市场支持游戏物品/平台物品视图切换；社区评测支持收藏、价值投票、欢乐和奖励状态反馈；创意工坊支持作品搜索、排序、详情和 Oracle 持久化订阅管理。
- 当前前端构建必须通过 `npm run build`，核心页面在 Playwright 桌面与移动视口检查后才能合并。

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
GET    /api/wallet/transactions?page=1&pageSize=20
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

HTTP 状态约定：

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
AUDITOR     审计员，预留角色
```

安全原则：

- 密码必须使用 BCrypt、ASP.NET Core PasswordHasher 或当前后端统一实现的 PBKDF2-SHA256 哈希，不存明文。
- 登录成功后使用 JWT；服务端同时验证固定 issuer、audience、HMAC-SHA256 签名与有效期。
- 前端只在当前标签页的 `sessionStorage` 保存 token，不保存密码；关闭标签页后登录态失效。
- 玩家注册密码长度为 8 至 128 位，账号长度为 3 至 64 位；前后端同时校验。
- 登录和注册接口按 Nginx 转发后的真实客户端 IP 限流，防止短时间暴力尝试。
- 敏感接口必须后端鉴权。
- 钱包、订单、市场、退款接口必须从 token 中获取当前用户，不信任前端传入的用户 ID。
- Oracle 连接字符串、JWT 密钥、云服务器密码不得提交 Git。
- 云端 Oracle 1521 不对公网开放。
- Nginx 必须发送 CSP、HSTS、`X-Frame-Options`、`X-Content-Type-Options`、Referrer Policy 与 Permissions Policy 安全响应头。

演示种子账号仅用于课程演示和本地/云端样例库联调，不代表真实生产密码：

```text
PLAYER      alice / alice
PLAYER      bob / bob
ADMIN       rootadmin / admin
DEVELOPER   valve@example.com / valve
DEVELOPER   klei@example.com / klei
```

## 13. 开发顺序计划

### 第 0 阶段：项目基线

状态：已完成，但因课程提纲更新，技术栈基线已调整为 C# / ASP.NET Core。

- [x] 建立 Git 仓库。
- [x] 建立 `backend/`、`frontend/`、`database/`、`docs/` 目录。
- [x] 准备 `.gitignore`。
- [x] 读取新版课程提纲并确认 C# / Oracle / VS.NET / C/S 或 B/S 要求。
- [x] 选择 B/S 架构。
- [x] 选择腾讯云作为云部署平台。

### 第 1 阶段：数据库落地

状态：数据库脚本已完成阶段性验收，并已在腾讯云 Oracle 环境完成部署验证。

- [x] 根据设计文档生成 Oracle `schema.sql`。
- [x] 写主键、外键、唯一约束、检查约束。
- [x] 写必要索引。
- [x] 写初始化数据 `data.sql`。
- [x] 验证 27 张表能成功创建。
- [x] 验证关键唯一约束和外键有效。
- [x] 在腾讯云 Oracle 环境部署并验证 `schema.sql`、`data.sql`、`verify_phase1.sql`。

### 第 2 阶段：C# 后端基础

- 创建 `SteamPlatform.sln`。
- 创建五层项目：
  - `SteamPlatform.Api`
  - `SteamPlatform.Application`
  - `SteamPlatform.Domain`
  - `SteamPlatform.Infrastructure`
  - `SteamPlatform.Shared`
- 配置 ASP.NET Core Web API。
- 配置 Oracle 连接。
- 配置 Oracle EF Core Provider。
- 配置 Dapper / ODP.NET 复杂 SQL 支持。
- 配置统一响应和统一异常。
- 配置 Swagger / OpenAPI。
- 配置 CORS。
- 完成健康检查接口。

### 第 3 阶段：认证与用户

- 玩家注册。
- 玩家登录。
- JWT 签发与校验。
- 管理员登录。
- 开发商登录。
- 基础角色权限。

### 第 4 阶段：商店与游戏基础

- 游戏列表。
- 游戏详情。
- 开发商游戏管理。
- 管理员上下架。
- 首页推荐和折扣接口。
- 固定准备 `Counter-Strike 2` 和 `Don't Starve Together / 饥荒联机版` 两款样板游戏。
- `CS2` 详情页突出免费入库、饰品经济、市场入口、创意工坊和多人竞技标签。
- `DST` 详情页突出买断制购买、联机生存、DLC/皮肤箱、创意工坊、公告和社区入口。

### 第 5 阶段：钱包与订单主链路

- 钱包账户初始化。
- 充值模拟。
- `DST` 买断制游戏购买事务。
- `CS2` 免费游戏入库或 0 元购买规则。
- 订单状态日志。
- 玩家游戏库。
- 钱包流水查询。

### 第 6 阶段：社区与成就

- 发表评价。
- 修改评价生成版本。
- 查询评价历史。
- `DST` 评价、社区讨论和公告展示优先。
- `DST` 课程项目自定义成就字典。
- 玩家解锁 `DST` 自定义成就。

### 第 7 阶段：CDKey 与资产确权

- 开发商创建 CDKey 批次。
- 生成 CDKey 哈希。
- 玩家兑换 CDKey。
- 写兑换尝试日志。
- 入库游戏资产。

### 第 8 阶段：饰品库存与市场

- `CS2` 饰品模板。
- `CS2` 饰品实例。
- 玩家库存。
- 买单和卖单。
- 冻结资金与锁定饰品。
- 撮合成交。
- 市场成交记录。
- 饰品流转账本。
- `DST` 皮肤箱、服装、节日物品作为补充饰品样例。

### 第 9 阶段：退款与审计

- 玩家申请退款。
- 管理员审核。
- 写退款明细。
- 写审核日志。
- 退款入账和流水。
- 必要时调整游戏库资产状态。

### 第 10 阶段：Vue 前端与 Steam 风格

- 创建 Vue 3 + Vite + TypeScript 项目。
- 配置 Pinia、Vue Router、Axios、自定义 Vue 组件和 Steam 深色主题 CSS。
- 商店首页。
- 游戏详情。
- 游戏库。
- 饰品库存。
- 市场交易界面。
- 个人中心与钱包。
- 管理员后台。
- 开发商后台。

### 第 11 阶段：腾讯云部署

- 购买腾讯云轻量应用服务器。
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
  -> 购买 Don't Starve Together / 饥荒联机版
  -> 钱包扣款并写流水
  -> DST 进入玩家库
```

完整展示闭环：

```text
注册玩家
  -> 登录
  -> 充值钱包
  -> 浏览 Don't Starve Together / 饥荒联机版
  -> 购买 DST
  -> DST 进入游戏库
  -> 发表 DST 评价
  -> 修改评价生成历史版本
  -> 解锁 DST 自定义成就
  -> 免费入库 Counter-Strike 2
  -> 获得 CS2 饰品
  -> CS2 饰品市场挂单
  -> 另一玩家创建买单
  -> 撮合成交
  -> 生成市场成交、钱包流水和饰品流转账本
```

退款闭环：

```text
玩家购买游戏
  -> 申请退款
  -> 管理员审核
  -> 钱包退款入账
  -> 写退款审核日志
```

演示数据固定口径：

- 购买、退款、游戏库主线默认使用 `Don't Starve Together / 饥荒联机版`。
- 饰品库存、市场挂单、撮合成交、价格走势主线默认使用 `Counter-Strike 2`。
- 评价、社区、公告、成就演示默认使用 `DST`，`CS2` 可作为市场讨论和新闻补充。

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
| Group B | 周力扬、王子轩 | 游戏商店、游戏详情、开发商游戏管理；统一展示 `CS2` 和 `DST` |
| Group C | 马祥珲、胡知鱼、徐京 | 核心交易、钱包、订单、退款、CDKey、游戏库；主线购买使用 `DST`，`CS2` 免费入库 |
| Group D | 靳岱泽、郭炫君、张茗博 | 社区互动、成就、饰品库存、市场交易；主线市场使用 `CS2`，社区成就使用 `DST` |

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
- 开发商登录。
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
```

组内分工细则：

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
- `CS2` 免费游戏详情页展示。
- `CS2` 饰品市场入口、库存入口和创意工坊入口展示。
- `DST` 买断制游戏详情页展示。
- `DST` DLC/皮肤箱/礼包区域展示。
- `DST` 更新公告、社区入口和创意工坊入口展示。
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
ITEM_TEMPLATE
```

主要后端接口：

```text
GET    /api/games
GET    /api/games/{gameId}
GET    /api/games/{gameId}/reviews/summary
GET    /api/games/{gameId}/achievements/summary
GET    /api/games/{gameId}/content-packages
GET    /api/games/{gameId}/items/summary
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

组内分工细则：

- 周力扬：游戏、开发商、管理员游戏管理后端接口。
- 王子轩：商店首页、游戏列表、详情页、Steam 风格 UI。

Group B 必须优先保证的演示口径：

```text
商店首页
  -> 展示 Counter-Strike 2 免费游戏卡片
  -> 展示 Don't Starve Together / 饥荒联机版 折扣买断制卡片
  -> 进入 CS2 详情页，看到市场、库存、创意工坊入口
  -> 进入 DST 详情页，看到购买区域、DLC/礼包、公告、社区入口
```

Group B 与其他组的边界：

- 不实现钱包扣款、订单事务、退款事务，这些归 Group C。
- 不实现饰品实例、市场撮合、饰品流转账本，这些归 Group D。
- 可以读取 Group D 提供的评价概览、成就概览、饰品概览接口，但不在详情页里自行写复杂业务逻辑。

页面职责补充：

- `/games/:id` 是商店侧游戏详情页，由 Group B 负责，面向搜索、浏览和购买前决策；页面需要展示游戏介绍、价格或免费入库状态、折扣、口碑、评价概览、成就概览，以及 CS2 市场入口、DST DLC/礼包和公告入口。
- `/games/:id` 可以读取 `/api/library` 判断当前玩家是否已入库；已入库时只展示“已在库中”和跳转 `/library/:gameId` 的入口，不在商店详情页展示玩家个人库存、个人成就进度和游玩时长。
- `/library/:gameId` 是游戏库侧详情页，由 Group C 游戏库链路和对应前端页面承接，面向已拥有游戏；页面展示玩家自己的游玩时长、最近游玩、个人成就进度，并根据游戏提供库存、市场、社区、DLC/礼包等入口。
- 商店详情页和游戏库详情页可以复用视觉组件，但业务职责必须区分，避免同一个详情页同时承担购买前商品展示和已入库后的个人数据展示。

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
- `DST` 买断制购买、退款、CDKey 兑换主链路。
- `CS2` 免费入库或 0 元订单主链路。

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
GET    /api/wallet/transactions?page=1&pageSize=20
POST   /api/orders
POST   /api/games/{gameId}/free-claim
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
/wallet
/wallet/recharge/checkout
/wallet/history
/wallet/history/:historyId
/orders -> /wallet/history
/orders/:id
/library
/refunds
/refunds/new
/admin/refunds
/developer/cdkey-batches
/redeem
/games/:id 的购买区域
```

组内分工细则：

- 马祥珲：核心交易总设计、购买事务、统一接口/代码规范、最终集成。
- 胡知鱼：钱包、充值、资金流水、金额校验、余额展示页面。
- 徐京：退款、CDKey、游戏库、资产确权、相关前端页面。

Group C 必须优先保证的演示链路：

```text
登录
  -> 充值钱包
  -> 购买 Don't Starve Together / 饥荒联机版
  -> 钱包扣款
  -> 写钱包流水
  -> 生成订单
  -> DST 进入游戏库
  -> 申请退款
  -> 管理员审核
  -> 退款流水
  -> 免费入库 Counter-Strike 2
```

Group C 钱包接口规则：

- `GET /api/wallet` 返回 `availableBalance`、`frozenBalance`、查询计算的 `totalBalance` 和 `version`。
- `POST /api/wallet/recharge` 是演示充值接口，请求字段为 `amount` 和 `idempotencyKey`。
- 充值金额必须在 `0.01` 到 `99999.99` 之间，最多两位小数。
- 充值必须在同一事务中更新 `WALLET_ACCOUNT.available_balance`、增加 `version` 并写 `WALLET_TRANSACTION`。
- 充值流水固定使用 `biz_type = RECHARGE`、`funds_direction = CREDIT`，并记录 `avail_bal_before` / `avail_bal_after`。
- 同一 `idempotencyKey` 重复提交不能重复加钱，应返回已有充值结果。
- `GET /api/wallet/transactions` 使用分页参数 `page`、`pageSize`，默认 `1`、`20`，最大 `pageSize = 100`。

Group C 与其他组的边界：

- 只负责玩家是否拥有 `CS2` / `DST`，不负责 `CS2` 饰品归属和市场换手；饰品资产归 Group D。
- 免费游戏 `CS2` 不能扣除钱包余额；可生成 0 元订单或直接写入游戏库，但实现方式必须在接口文档中写清。
- `DST` 退款成功后必须处理订单状态、退款日志、钱包流水和游戏库状态。
- CDKey 优先给 `DST` 准备兑换样例，`CS2` 不作为 CDKey 主演示对象。
- `POST /api/developer/cdkey-batches` 只允许开发商或管理员创建 `DST` CDKey 批次；明文 CDKey 只在创建响应中展示一次，数据库和兑换日志只保存哈希。
- `POST /api/cdkeys/redeem` 对无效、过期、已兑换、已拥有游戏等情况返回可解释的业务结果，并写 `CDKEY_REDEEM_LOG`，前端不要把这些情况当作网络错误处理。

Group C 详细交易契约见：

```text
docs/group-c-core-transaction-contract.md
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
- `DST` 评价、评价版本、社区讨论。
- `DST` 课程项目自定义成就字典。
- 玩家解锁 `DST` 自定义成就。
- `CS2` 饰品模板。
- `CS2` 饰品实例。
- 玩家饰品库存。
- `CS2` 饰品掉落模拟。
- `DST` 皮肤箱、服装、节日物品补充样例。
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
POST   /api/admin/reviews/{reviewId}/hide
POST   /api/admin/reviews/{reviewId}/show
GET    /api/games/{gameId}/achievements
POST   /api/achievements/{achId}/unlock
GET    /api/inventory
GET    /api/inventory?gameId=GAME_CS2
POST   /api/inventory/drop
GET    /api/market
GET    /api/market?gameId=GAME_CS2
POST   /api/market/orders
POST   /api/market/orders/{marketOrderId}/cancel
POST   /api/market/match
GET    /api/market/trades
GET    /api/market/templates/{templateId}/price-history
GET    /api/market/items/{itemId}/transfers
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

组内分工细则：

- 靳岱泽：评价、评价版本、成就解锁、相关前端。
- 郭炫君：饰品模板、饰品实例、玩家库存、掉落模拟、相关前端。
- 张茗博：市场挂单、撮合成交、流转账本、市场页面。

Group D 必须优先保证的演示链路：

```text
发表 DST 评价
  -> 修改评价
  -> 查看历史版本
  -> 解锁 DST 自定义成就
  -> 获得 CS2 饰品
  -> 上架 CS2 饰品卖单
  -> 另一玩家创建 CS2 饰品买单
  -> 撮合成交
  -> 饰品换手
  -> 写流转账本
```

Group D 与其他组的边界：

- 市场主链路只围绕 `CS2` 饰品做完整闭环，避免同时维护多套市场规则。
- `DST` 饰品只作为补充库存样例，可以展示但不要求进入市场主链路。
- `DST` 成就为课程项目自定义成就，不能写成“官方 Steam 成就完全同步”。
- 市场成交时涉及钱包冻结、解冻、转账流水，必须复用 Group C 的钱包账户和流水规则，不能另建余额字段。
- 如果市场交易需要接口调用钱包能力，优先由 Application 层服务协作，不允许前端直接组合多个危险步骤来模拟事务。

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

### 17.0 编码与文件格式规范

全项目统一使用 UTF-8 编码。

- 源码、SQL、Markdown、配置文件、Vue 文件、TypeScript 文件、C# 文件统一保存为 UTF-8。
- 不提交乱码文案。前端所有用户可见中文必须在浏览器中正常显示。
- Windows PowerShell 或终端可能因为代码页问题把正常 UTF-8 中文显示成乱码，不能仅凭终端输出判断文件是否乱码。
- Review 时如果发现疑似乱码，必须用可靠方式确认文件真实内容，例如编辑器编码状态、浏览器渲染、UTF-8 原始字节或 Unicode 码点检查。
- 只有文件真实存储内容错误时，才能以乱码为理由要求修改。
- 不要把同一个文件在 GBK、ANSI、UTF-8 之间反复转换。
- 新建文本文件时必须使用 UTF-8；工具允许时必须显式选择 `UTF-8`。
- Markdown 文档可使用 UTF-8 with BOM 以兼容 Windows 中文显示；代码文件不强制 BOM，但必须能被 .NET、Node、Vite、GitHub 正确按 UTF-8 读取。

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
- 已部署数据库的增量变更必须放入 `database/migrations/`，文件名使用 `YYYYMMDD_说明.sql`，并设计为可重复检查、尽量幂等。
- 数据库迁移随普通组员 PR 一起审查；只有在 SQL 非破坏性、事务与约束处理正确、兼容当前 Oracle Schema，并通过可行测试后才能批准。
- 需要该迁移才能完成联调的 PR 在批准并合并后，由总负责人或其授权的 Codex 部署到腾讯云 Oracle，并核对 SQL 结果、关键业务数据和公网 API。
- `database/admin/` 只存放人工管理、修复或清理脚本，不随普通迁移自动执行；需要执行时必须单独确认目的和影响范围。
- 迁移执行失败必须回滚或停止后续部署，不得在未验收的情况下宣称数据库已更新。
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
- 不在组件里硬编码重复的颜色值，统一使用主题变量或公共样式类。
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
- 云服务器 SSH 私钥，例如 `*.pem`、`*.key`。
- `.env.local`。
- `appsettings.Local.json`。
- `bin/`、`obj/`、`node_modules/`、`dist/`。
- 大型安装包和个人工具目录。
- 本机工具归档目录 `_local_tools_archive/`。
- `_archive/` 里的旧项目资料需要提交，但不得包含密码、私钥、真实连接串、第三方工具安装目录和大型安装包。

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
- 云平台选择腾讯云轻量应用服务器。

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
database/migrations/
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

补充云端验收：

- 已在腾讯云服务器 Oracle 环境完成数据库部署和基础验收。
- 已从公网访问 API，确认应用服务器可连接云端 Oracle。
- 2026-07-08 已执行 `database/migrations/20260708_developer_login_backend_completion.sql`，并将后端补全版本部署到云端。
- 2026-07-10 已执行 `database/migrations/20260709_cs2_item_template_image_assets.sql`：云端 CS2 饰品模板为 27 条且图片资源全部可访问，DST 模板图片路径已按设计清空；库存、挂单和成交数据均保留。

## 22. 当前决策记录

| 日期 | 决策 | 原因 |
|---|---|---|
| 2026-07-05 | 删除 `PLAYER.wallet_balance`，资金唯一真相放在 `WALLET_ACCOUNT` | 避免余额冗余导致账实不一致 |
| 2026-07-05 | 完成数据库第 1 阶段脚本验收 | 证明 27 张表、约束和初始化数据可执行 |
| 2026-07-06 | 根据新版课程提纲废弃 Java / Spring Boot / MyBatis 方案 | 课程要求 VS.NET、C#、Oracle、Oracle 数据访问组件或 ORM |
| 2026-07-06 | 选择 B/S 架构 | 课程允许 C/S 或 B/S；B/S 更适合 Steam 风格界面和云部署 |
| 2026-07-06 | 初步选择阿里云作为云部署平台 | 应用服务器和数据库均需部署到云服务器 |
| 2026-07-08 | 云平台调整为腾讯云轻量应用服务器并完成部署 | 因成本和配置更适合课程项目，应用服务器、Oracle、Nginx、前端静态文件均部署在云服务器 |
| 2026-07-08 | `DEVELOPER` 增加 `password_hash`，开发商使用 `contact_email + password` 登录 | 支撑开发商工作台、CDKey 批次、开发商游戏管理等权限闭环 |
| 2026-07-08 | 补齐内容包、物品摘要、市场价格历史、评价隐藏/恢复后端接口 | 保证 Steam 风格详情页、市场页、社区管理页可直接联调 |
| 2026-07-06 | 后端采用 ASP.NET Core Web API + 五层结构 | 符合 C# 要求，层次清晰，便于答辩说明 |
| 2026-07-06 | 数据访问采用 Oracle EF Core + Dapper / ODP.NET | 兼顾 ORM 规范性与复杂 SQL 可控性 |
| 2026-07-06 | 确定 .NET 10 SDK 与 dotnet-ef 10.x 作为开发工具链基线 | 支持 ASP.NET Core / EF Core 10 开发 |
| 2026-07-06 | 确定四组纵向功能分工，马祥珲担任唯一总负责人 | 每组同时交付前端、后端、测试和文档，降低前后端等待和集成风险 |
| 2026-07-07 | 建立项目归档目录 `_archive/legacy-files-2026-07-07/` | 我们自己做过的旧文件需要提交到 GitHub，第三方工具和安装包不提交 |
| 2026-07-07 | 调整 `main` 分支保护为管理员可绕过 | 普通组员仍需 PR 和 review，总负责人可处理 README、配置和紧急修复 |
| 2026-07-07 | 最终样板游戏确定为 `Counter-Strike 2` 与 `Don't Starve Together / 饥荒联机版` | `CS2` 承担饰品库存和市场交易主线，`DST` 承担买断制购买、DLC/礼包、社区、评价和自定义成就主线 |

## 23. 变更维护规则

以下情况必须更新 README：

- 技术栈变化。
- 云平台变化。
- C/S 或 B/S 架构变化。
- 数据库表结构变化。
- API 统一格式变化。
- 权限角色变化。
- 部署方式变化。
- 最终样板游戏、样例数据口径、主演示链路变化。
- `CS2` / `DST` 的模块归属、购买规则、市场规则或成就规则变化。
- 实际开发顺序与计划明显不同。
- 引入 Redis、对象存储、消息队列等新基础设施。
- 任何会影响组员开发、部署、答辩说明的决策。

原则：

```text
实际怎么做，README 就怎么写。
README 写了什么，后续开发就尽量照着做。
如果二者不一致，必须尽快修正 README 或修正实现。
```

## 24. 答辩增强计划

核心业务闭环、五层后端、Oracle 数据库、Vue 前端和腾讯云部署已经建立。答辩前按下表逐项增强；实际状态、验收证据或实现顺序变化时必须同步更新本节。

| 顺序 | 增强项 | 当前状态 | 完成标准 |
|---|---|---|---|
| 1 | 正式 HTTPS 与后续域名 | 已完成公网 IP 可信 HTTPS、自动续期、验证与回滚验收；域名购买与备案留到最后决策 | 80 自动跳转 443；可信证书自动续期；公网前端、API、Oracle 健康检查和 SignalR 均通过 HTTPS 工作；域名购买与备案另行决策 |
| 2 | 一键恢复演示数据 | 已完成并通过云端重置、恢复、再次重置验收 | .NET 工具先备份、再重置、再校验；操作有确认口令和审计日志；失败可恢复；执行前后健康检查通过 |
| 3 | 持久化增强交互与实时通知 | 已完成并通过腾讯云 Oracle、API 与 SignalR 验收 | 好友聊天、评测互动、工坊订阅写入 Oracle；C# 五层接口完整；SignalR 推送消息和状态变化；刷新或换浏览器后状态不丢失 |
| 4 | 商店媒体体验 | 已完成并通过本地及腾讯云公网验收 | CS2、DST 详情页具备视频预告片、截图画廊、缩略图切换、全屏查看、键盘操作和加载失败兜底 |
| 5 | 社交与社区扩展 | 已完成并通过腾讯云 Oracle、API、桌面与移动端验收 | 好友关系与请求、交易报价、个人资料装扮、徽章、社区动态、讨论主题与回复形成可演示闭环 |
| 6 | 固定答辩演示脚本 | 已完成，20 分钟多角色版本待集体彩排 | 使用两名现场注册玩家、Klei/Valve 两家开发商和管理员，覆盖游戏提交审核、DST 购买与 CDKey、好友聊天、评测成就、工坊、CS2 饰品交易、退款和 Oracle 证据；包含 A-G 七人分工、精确时间、金额、讲解词与故障预案 |
| 7 | Playwright 回归与备用录屏 | 已完成并通过腾讯云前后恢复保护验收 | 桌面与移动视口自动化覆盖登录、交易主链和社交社区链；测试报告可复现；提供 1080p 答辩备用录屏 |
| 8 | Oracle 数据库答辩证据 | 已完成并通过腾讯云 Oracle 只读验收 | 45 表与约束完整性、跨表一致性、复杂查询执行计划和双会话行锁均可重复演示 |

实施约束：

- 技术路线保持 `B/S + Vue + ASP.NET Core 五层结构 + Oracle + 腾讯云`，不得因增强功能改成其他后端或数据库。
- 社交、社区和实时功能必须由 C# 后端执行业务规则，前端不得继续把浏览器存储当作业务真相。
- 一键恢复默认只重置项目业务演示数据，不删除 Oracle 实例、数据库用户或表结构；任何云端重置都必须由总负责人明确确认后执行。
- 演示恢复前必须生成带时间戳的备份清单和审计记录，恢复后必须执行数据库、API 与关键页面验证。
- 媒体资源必须控制体积并提供海报或静态图兜底，不得因视频加载失败阻断商店页或答辩主流程。
- 每一项完成后必须运行对应构建与测试、更新 README、提交 GitHub，并部署到腾讯云后再标记为完成。

已确认的实施选择：

1. 当前暂无自有域名。HTTPS 采用两阶段方案：第一阶段使用 Let's Encrypt 可信公网 IP 短期证书，由 `SteamPlatform.HttpsDeploy` 自动签发、续期、验证与回滚；第二阶段是否购买并备案 `steam-db-lab.com` 仍由总负责人另行确认。域名不会改变既定 B/S、Vue、ASP.NET Core 五层结构、Oracle 或腾讯云路线。
2. 允许把当前云端业务数据整理为固定答辩基线，并在生成可恢复备份后由工具重置业务数据。
3. CS2、DST 预告片采用服务器本地压缩文件，目标为短版 720p，并保留静态海报兜底。

### 24.1 一键恢复演示数据验收记录

实现文件：

- `backend/tools/SteamPlatform.DemoData/`：.NET 10 命令行工具，支持 `plan`、`reset`、`restore`、`list`。
- `database/demo/manifest.json`：42 张业务表依赖顺序、固定基线和最低行数校验，其中包含 27 张核心表、6 张实时社交表和 9 张社区扩展表。
- `database/migrations/20260825_demo_reset_audit.sql`：创建恢复运行、快照表映射和事件审计表。
- `database/data.sql`：固定答辩基线，包含 11 个成就和 39 个 CS2/DST 物品模板。

2026-08-25 云端验收：

1. 腾讯云 Oracle 迁移成功，3 张运维审计表存在。
2. 运行 `reset`，生成快照并重置成功，运行编号 `2026082512265656AD`。
3. 按该编号运行 `restore`，当时的 27 张核心业务表逐表恢复并通过精确行数校验，运行状态为 `RESTORED`。
4. 再次运行 `reset`，最终答辩基线运行编号为 `20260825122734C1C3`，状态为 `RESET_COMPLETED`。
5. 最终关键行数：`PLAYER=2`、`GAME=2`、`ACHIEVEMENT=11`、`ITEM_TEMPLATE=39`、`MARKET_ORDER=4`、`WALLET_TRANSACTION=3`。
6. 公网 `/api/health`、`/health/database`、`/api/games` 均正常，CS2 与 DST 目录返回正确。
7. 社交与社区扩展全部接入后，恢复清单扩展到 42 张业务表、145 条基线插入。
8. 真实浏览器写入资料、动态和讨论回复后再次执行完整恢复，最终答辩基线运行编号为 `202608251626294EFF`，状态为 `RESET_COMPLETED`。
9. 当前本地验收通过：恢复工具 4 项测试、后端 188 项测试、数据库 39 项测试、Vue 生产构建。

云端恢复工具版本记录在 `/opt/steam-platform/DEMO_DATA_TOOL_COMMIT`，当前值为 `c425c59`。连接字符串仍只存在于服务器私有环境变量，不进入仓库、命令参数或日志。

### 24.2 持久化增强交互与实时通知验收记录

数据结构：

- 27 张核心业务表保持不变，新增 6 张社交增强表：`FRIEND_RELATION`、`DIRECT_MESSAGE`、`REVIEW_REACTION`、`WORKSHOP_ITEM`、`WORKSHOP_SUBSCRIPTION`、`USER_NOTIFICATION`。
- `database/migrations/20260825_social_realtime_foundation.sql` 已在腾讯云 Oracle 执行；好友二元组、状态、投票、布尔字段和通知类型均由外键、唯一约束或 `CHECK` 约束保护。
- 演示基线包含 Alice 与 Bob 的已接受好友关系、2 条历史消息、评测互动、CS2/DST 各 4 条工坊作品、2 条订阅和 2 条个人通知。

C# 五层与接口：

- Domain 定义社交领域模型；Application 定义用例、输入规范化和实时通知编排；Infrastructure 使用 Dapper/ODP.NET 与显式事务访问 Oracle；Api 暴露最小端点和 SignalR Hub；Vue 只通过 `src/api/` 调用。
- 已提供好友列表/请求/接受、私聊记录/发送、评测互动查询/更新、工坊查询/订阅、个人通知查询/已读接口。
- `/hubs/social` 使用现有 JWT 鉴权，只有 `PLAYER` 可连接；服务端按 `user:{principalId}` 私有组推送，不把聊天或通知广播给其他玩家。
- 前端好友抽屉、聊天记录、通知菜单、评测收藏/投票/欢乐/奖励和工坊订阅均以 Oracle 为业务真相，不再使用浏览器本地存储保存业务状态。

2026-08-25 云端验收：

1. 腾讯云迁移、API 与 Nginx WebSocket 反向代理部署成功，部署版本记录为 `ecf6831`。
2. 公网玩家登录后可读取好友关系、2 条历史消息、DST 评测互动、4 条工坊作品、订阅和个人通知。
3. 公网写入验证了工坊取消/恢复订阅，以及评测投票、收藏、欢乐和奖励状态修改/恢复，刷新后仍由 Oracle 返回。
4. `npm run smoke:realtime` 让 Bob 建立 SignalR 连接，再由 Alice 通过 API 发送消息；Bob 实际收到 `DirectMessageReceived`，消息 ID 为 `MSGCE5CC95E19CDB5EA633A1A0C`。
5. 冒烟产生的消息和通知已通过一键恢复清除，最终答辩基线运行编号为 `202608251417510C1E`。
6. `/api/health` 与 `/health/database` 均返回 `OK`；本地后端 188 项、数据库 36 项、恢复工具 4 项测试及 Vue 生产构建通过。

### 24.3 商店媒体体验验收记录

实现文件：

- `frontend/src/components/SteamMediaGallery.vue`：CS2 与 DST 共用的媒体画廊，提供视频播放、截图选择、前后切换、全屏查看和加载失败兜底。
- `frontend/src/data/gameCatalog.ts`：集中维护两款演示游戏的媒体清单和无障碍标签，页面组件不再分别硬编码素材。
- `frontend/public/assets/media/`：服务器本地托管 2 段 H.264/AAC 720p 短版预告片和 10 张 1280 x 720 截图。

交互与容错：

- 每款游戏固定展示 1 段视频和 5 张截图；缩略图、左右按钮、键盘方向键均可切换，图片和视频均可进入全屏查看，`Esc` 关闭全屏。
- 视频使用 `preload="metadata"`、`playsinline` 和本地海报；媒体请求失败时回退到游戏头图，不阻塞购买、入库或社区入口。
- 画廊采用稳定的 16:9 比例；缩略图在窄屏横向滚动，桌面与 390 px 手机视口均无横向页面溢出。
- 未登录用户不会请求受保护的游戏库接口；点击购买或免费入库会跳转登录页，并保留当前详情页作为登录后返回地址。

2026-08-25 验收：

1. 12 个媒体文件总计 `8,956,437` 字节；CS2 与 DST 视频分别约 3.78 MB 和 3.82 MB，适合当前 3 Mbps 答辩服务器。
2. Vue 生产构建通过；Playwright 实际验证两款视频 `readyState=4`、每款 6 个媒体入口、缩略图选择、左右切换、全屏、方向键、`Esc` 和移动端布局。
3. 腾讯云 Nginx 对 MP4 和 JPG 均返回 `206 Partial Content` 与正确 `Content-Type`，浏览器可按 Range 分段读取视频。
4. 公网页面、`/api/health`、`/health/database` 和 DST 成就摘要均返回 200；公网浏览器控制台 0 错误，详情页没有模块失败状态。
5. 同轮修复 Oracle 对循环平均值映射 `decimal` 时的溢出：成就平均达成率在 SQL 中先舍入并转换为 `NUMBER(7,2)`；新增回归守卫，数据库测试工程也统一为 `net10.0`。
6. 本地后端 188 项、数据库 36 项测试和 Vue 生产构建通过；云端部署标记为 `5bc4b40`，对应 API 修复提交为 `93c1e57`。

### 24.4 社交与社区扩展验收记录

Oracle 数据结构：

- `database/migrations/20260825_community_engagement_expansion.sql` 新增 `PLAYER_PROFILE`、`BADGE_CATALOG`、`PLAYER_BADGE`、`TRADE_OFFER`、`TRADE_OFFER_ITEM`、`COMMUNITY_POST`、`COMMUNITY_POST_REACTION`、`DISCUSSION_TOPIC`、`DISCUSSION_REPLY` 共 9 张业务表。
- 个人资料可见性、徽章稀有度、报价状态、报价物品角色、动态类型、动态可见性、反应类型和讨论状态均由外键、唯一约束或 `CHECK` 约束保护。
- 固定基线包含 2 份个人资料、4 个徽章定义、5 条玩家徽章、1 份待处理报价及 2 件已锁定物品、4 条社区动态、3 条动态反应、2 个讨论主题和 2 条回复。
- Alice 与 Bob 额外保留可自由报价的正常库存物品，既能展示待处理报价，又能现场创建第二份报价。

C# 五层与事务规则：

- Domain 定义资料、徽章、报价、动态和讨论领域模型；Application 负责输入规范化、权限和业务用例；Infrastructure 使用 Dapper/ODP.NET 与 Oracle 显式事务；Api 只暴露端点和 SignalR 通知；Vue 只调用统一 API。
- 报价只允许已接受好友之间创建；双方各选 1 至 8 件正常状态物品；创建时按稳定顺序执行行锁并转为 `LOCKED`，拒绝或撤销时释放，接受时原子交换所有权并写入资产转移账本。
- 资料预设、可见性、精选徽章、玩家搜索、报价列表与处理、社区动态与反应、讨论主题与回复均以 Oracle 为业务真相。
- 新报价、报价状态变化和讨论回复通过既有 `/hubs/social` 私有用户组实时通知，不向无关用户广播。
- API 对 Oracle 返回的无时区 `DateTime` 统一按 UTC 输出带 `Z` 的 ISO 8601 时间，避免浏览器把刚发布内容误显示为数小时前。

Vue 页面与交互：

- `/profile`、`/profiles/:userId`：公开资料、头像/背景/主题预设、签名、简介、展示游戏、资料可见性、等级、经验值、徽章和精选徽章。
- `/trade-offers`：状态筛选、报价详情、双方物品、创建器、好友选择、物品选择、留言、接受、拒绝和撤销。
- `/community`、`/community/discussions/:topicId`：动态发布、赞与奖励、CS2/DST 游戏中心、好友摘要、玩家搜索与好友请求、讨论列表、发帖和回复。
- 桌面使用 Steam 风格双栏信息密度；390 x 844 手机视口改为单栏，资料、交易和社区页面均无横向溢出。

2026-08-26 云端验收：

1. 9 表迁移在腾讯云 Oracle 执行成功；当前为 42 张业务表和 3 张演示恢复审计表。
2. 公网以 Alice 登录后读取到 3 枚徽章、1 位好友、4 条可见动态、CS2/DST 各 1 个主题和 1 份待处理报价；Bob 视角可接受该报价，Alice 视角可撤销。
3. Playwright 实际完成资料保存、好友报价双方选物、动态发布和讨论回复；发送报价前保持未提交，避免改变基线库存。
4. 动态与回复立即显示正确相对时间；浏览器控制台 0 错误、0 警告。
5. 桌面和 390 x 844 移动端截图检查通过，`documentElement.scrollWidth` 等于视口宽度。
6. 验收产生的资料更新时间、临时动态和临时回复已由一键恢复清除；最终恢复运行编号为 `202608251626294EFF`。
7. 后端 188 项、数据库 36 项、恢复工具 4 项测试和 Vue 生产构建通过；本项功能部署版本为 `c1f7c9b`。

### 24.5 固定答辩演示脚本验收记录

实现文件：

- `docs/defense-demo-runbook.md`：20 分钟五角色主演示，使用两名现场注册玩家、两家开发商和管理员，包含 A-G 七人分工、切屏顺序、逐步讲解词、固定金额、预期数据库结果和故障备用步骤。
- `backend/src/SteamPlatform.Infrastructure/CoreTransactions/CoreTransactionService.cs`：游戏库只返回 `NORMAL` 授权；退款保留 `REVOKED` 审计记录但不再错误展示。
- `backend/src/SteamPlatform.Infrastructure/Market/MarketRepository.cs`：撮合支持绑定本次买单、校验买单归属并排除自成交。
- `frontend/src/views/MarketView.vue`：市场成交页提供“执行下一笔撮合”，可现场展示价格优先的 Oracle 事务撮合。

2026-08-26 腾讯云真实业务链验收：

1. 新玩家注册后免费领取 CS2，游戏进入库但钱包不变。
2. 新玩家充值 ¥60.00，以 ¥24.00 购买 DST，钱包余额变为 ¥36.00；消费历史同时保留充值和购买流水。
3. DST 评测显示来自 `PLAYER_LIBRARY.play_minutes` 的 0.0 小时，解锁成就后进度由 0/6 变为 1/6。
4. 管理员通过退款后生成 +¥24.00 流水，余额恢复 ¥60.00，订单变为 `CLOSED / REFUNDED`，DST 授权变为 `REVOKED` 且不再出现在游戏库。
5. Alice 将 `ITEM_CS2_002` 以 ¥49.00 现场挂单，匹配 Bob 已冻结 ¥50.00 的买单；成交后 Alice 余额 ¥222.55，Bob 可用余额 ¥243.75、冻结余额 ¥0.00。
6. 成交记录和物品流转账本均显示 `ITEM_CS2_002` 从 `P001` 转移到 `P002`；Alice 的 ¥50.00 基线卖单仍等待撮合，证明价格优先正确。
7. 验收产生的临时账号、订单、评测、成就、退款、挂单和成交已由一键恢复清除，最终运行编号为 `20260825170313202A`。
8. 后端 188 项、数据库 36 项、恢复工具 4 项测试和 Vue 生产构建通过；云端部署标记为 `a1784bf`，API 与 Oracle 健康检查均为 `OK`。

### 24.6 Playwright 回归与备用录屏验收记录

实现文件：

- `frontend/playwright.config.ts`：桌面、移动、写库答辩链和 1080p 录制项目；失败时保留截图、视频和 Trace。
- `frontend/e2e/public-store.spec.ts`：商店先渲染、Oracle 启动公告浮窗、可恢复 404 页面、CS2/DST 各 1 段视频与 5 张截图、全屏查看及响应式布局。
- `frontend/e2e/baseline.spec.ts`：Alice/Bob 登录基线、游戏库、库存、钱包、个人资料、社区、交易报价和市场。
- `frontend/e2e/defense-flow.spec.ts`：注册、免费入库、充值、购买、评测、成就、退款、挂单、撮合、资金和物品账本完整链路。
- `frontend/e2e/social-community-flow.spec.ts`：资料装扮、好友申请与接受、SignalR 实时聊天、动态、讨论回复、徽章、评测互动、工坊订阅和交易报价完整链路。
- `frontend/scripts/run-cloud-e2e.mjs`：SSH 固定恢复入口、测试前后双重恢复、真实退出码和稳定录屏文件生成。
- `docs/playwright-regression-runbook.md`：执行命令、覆盖矩阵、数据安全、报告位置、固定断言和故障分析规范。

执行与数据保护：

1. `desktop-chromium` 和 `mobile-chromium` 是只读项目；两者全部结束后，依赖关系才允许 `defense-chromium` 修改 Oracle。
2. 普通 `npm run test:e2e` 不设置写库许可，完整答辩链会跳过；云端命令由受控脚本设置许可。
3. 云端脚本只能调用 `/opt/steam-platform/bin/reset-demo-data`，数据库密码继续只存在于服务器私有环境变量。
4. 写库测试前生成快照和审计运行号；测试结束、失败或异常时均在 `finally` 中恢复固定基线，后置恢复失败会强制使命令失败。
5. HTML 报告、截图、Trace 与视频位于 `output/playwright/`，作为本地验收产物被 Git 忽略，不提交缓存或私钥。

2026-08-26 腾讯云验收：

1. 完整回归 11/11 通过：桌面只读 5 项、移动只读 5 项、真实 Oracle 写库答辩链 1 项；总用时约 1.1 分钟。
2. 回归覆盖启动公告、两款游戏媒体画廊、登录、库、库存、钱包、资料、社区、报价、市场，以及固定 DST/CS2 两条业务链。
3. 完整回归结束后的恢复运行编号为 `202608251745528ED6`，固定基线恢复成功。
4. 录屏链 1/1 通过，生成 1920 x 1080、25 fps、22.12 秒、约 2.73 MiB 的 WebM 备用录屏。
5. 录屏结束后的恢复运行编号为 `202608251746364404`，临时账号、订单、退款、挂单和成交均已清理。
6. 本轮发现并修复库存模拟掉落、确认出售成功提示被后续刷新立即清空的问题；Vue 生产构建通过。

2026-08-27 社交社区补充验收：

1. 云端完整回归由 11 项增加到 12 项并全部通过：桌面只读 5 项、移动只读 5 项、交易答辩链 1 项、社交社区写库链 1 项；总用时约 1.4 分钟。
2. 新链路使用三个独立浏览器会话，真实完成临时玩家资料装扮、好友申请与接受、聊天双方在线、SignalR 即时到达和刷新后历史重载。
3. 动态、DST 讨论主题与回复、Alice 展示徽章、Bob 评测欢乐标记、工坊订阅和固定交易报价均经页面点击写入，并在刷新后从 API/Oracle 重新读取成功。
4. 单项社交回归前后恢复运行编号分别为 `202608270951137295`、`20260827095140E3BC`；完整 12 项回归前后恢复运行编号分别为 `202608270953516559`、`20260827095519A6D0`。
5. 完整回归结束后再次运行 Oracle 只读总验收，21 组断言全部通过；固定基线恢复为 2 名玩家、4 条动态、2 个主题、2 条私信和 1 笔市场成交。

### 24.7 Oracle 数据库答辩证据验收记录

实现文件：

- `database/verify_defense.sql`：45 张预期表、约束/索引/对象状态，以及订单、支付、退款、游戏库、钱包、市场、资产账本和恢复审计一致性总验收。
- `database/defense/explain_plans.sql`：订单历史、市场卖单和社区讨论三类复杂查询的 `DBMS_XPLAN` 输出，结尾回滚 `PLAN_TABLE`。
- `database/defense/lock_session_a.sql`、`lock_session_b.sql`：两个 SQL*Plus 会话复现钱包行锁、有限等待和安全回滚。
- `tests/SteamPlatform.Database.Tests/DefenseScriptContractTests.cs`：防止后续修改删掉关键断言、回滚或有限等待。
- `docs/database-defense-runbook.md`：运行方法、预期输出、索引解释、并发原理和 2 至 3 分钟答辩顺序。

2026-08-27 腾讯云 Oracle 验收：

1. 总验收识别到 45 张表、45 个主键和至少 49 个命名业务索引；禁用约束、无效索引、无效对象均为 0。
2. 订单/明细、订单/支付、退款/明细、已购权益、活动卖单资产、钱包冻结、市场成交、资产转移和钱包流水异常均为 0。
3. `PLAYER.wallet_balance` 列为 0，固定样板游戏 `GAME_CS2`、`GAME_DST` 均存在，Alice/Bob 总余额继续由钱包可用额与冻结额实时计算。
4. 订单查询实际走 `IDX_ORDER_USER_TIME` 降序范围扫描，市场查询走 `IDX_MARKET_TEMPLATE_STATUS` 范围扫描，讨论查询走 `IDX_DISCUSSION_GAME_TIME` 降序范围扫描。
5. 双会话并发测试中，会话 A 持有 `P001` 钱包行锁，会话 B 在 2 秒后收到 Oracle 23c `SQLCODE=-54`；两个会话均回滚，业务数据未改变。
6. 数据库 C# 契约测试由 36 项增加到 39 项并全部通过；详细复现步骤以数据库答辩证据手册为准。

### 24.8 HTTPS 生产部署验收记录

实现文件：

- `backend/tools/SteamPlatform.HttpsDeploy/`：`.NET 10` 运维工具，提供 `plan`、`render`、`stage`、`enable`、`verify`、`rollback`。
- `backend/tests/SteamPlatform.HttpsDeploy.Tests/`：公网 IP 校验、确认口令、Nginx 路由/TLS、systemd 续期单元和回滚状态 JSON 契约测试。
- `docs/https-deployment-runbook.md`：测试签发、生产切换、可信验证、自动续期、Playwright 回归和手动回滚说明。

2026-08-27 生产验收：

1. 工具按 `linux-x64` 自包含方式发布，运行项目锁定的 .NET 10；腾讯云服务器当前全局 .NET 9 不影响工具或现有自包含 API。
2. 服务器临时目录实测 Certbot `5.7.0`，确认支持 `--ip-address`、`--preferred-profile shortlived`、`--no-autorenew` 和 `--deploy-hook`。
3. Ubuntu 镜像没有 `python3-venv`；实现已改为 `pip --target` 项目目录隔离安装，不修改系统 Python 包，也不新增系统运行时依赖。
4. Let's Encrypt 已签发 SAN 为公网 IP `124.222.213.245` 的生产证书，由项目专用 timer 每小时检查并自动续期；开发机 `curl`、浏览器和 .NET 均在未关闭证书校验的情况下信任该证书。
5. 正式双栈 Nginx 配置通过 `nginx -t` 并已 reload；HTTP `80` 返回同路径 HTTPS `308`，HTTPS `443` 正常提供 Vue、API、Oracle 健康检查和 SignalR WebSocket。
6. 系统旧 `certbot.timer` 已停用；项目专用 `steam-platform-certbot-renew.timer` 已启用且 active。指定证书的 `renew --dry-run --run-deploy-hooks` 成功，证明短期证书续期和 Nginx reload 钩子可用。
7. 公网 HTTPS SignalR 冒烟成功收到 `DirectMessageReceived`；当前 `main` 前端部署后再次使用默认 HTTPS 地址运行完整云端 Playwright，12/12 通过，结束后的演示基线恢复运行编号为 `202608271139556A4F`。
8. HTTPS 回归后再次运行 Oracle 只读总验收，21 组断言全部通过；固定基线保持 2 名玩家、2 款样板游戏、6 件库存资产和 1 笔市场成交。
9. 本地完整后端解决方案 204 项测试通过：API 188 项、演示恢复 4 项、HTTPS 部署 12 项；构建 0 警告、0 错误。
10. 自包含工具已上传到 `/opt/steam-platform/tools/https-deploy/`，文件哈希与本地发布产物一致；当前版本标记 `/opt/steam-platform/HTTPS_TOOL_COMMIT` 为 `5cd9d00`。

### 24.9 答辩前最终质量与安全收口

2026-08-30 最终验收：

1. `SYS_NOTICE` 固定基线包含 3 条不过期公告；启动浮窗和管理员公告列表均从 Oracle/API 读取，不再因样例过期而依赖前端备用数据。
2. Vue 路由增加统一 404 页面，提供返回商店与返回上一页操作；桌面和 390 x 844 手机项目均已纳入 Playwright。
3. JWT 同时校验固定 issuer、audience、HMAC-SHA256 签名与生命周期；浏览器 token 从持久化 `localStorage` 改为标签页级 `sessionStorage`。
4. 注册接口执行账号 3 至 64 位、密码 8 至 128 位的前后端双重校验；登录和注册按真实客户端 IP 每分钟最多处理 30 次请求。云端实测前 30 次无效认证返回 `401`，第 31 次返回 `429`。
5. Nginx 公网响应已包含 CSP、HSTS、`X-Frame-Options: DENY`、`X-Content-Type-Options`、Referrer Policy 和 Permissions Policy；Oracle `1521` 与 API `5253` 继续只监听回环地址。
6. 前端完整 npm 依赖审计为 0 漏洞；C# 全解决方案格式检查通过；Release 构建 0 警告、0 错误，后端与运维工具测试 `208/208` 通过，Vue 类型检查和生产构建通过。
7. GitHub Actions 已加入 PR 与 `main` 自动质量门禁，覆盖 C# 格式、.NET 构建/测试、演示基线解析、npm 审计和 Vue 生产构建。
8. 腾讯云完整 Playwright 回归 `14/14` 通过；结束后的固定基线恢复运行编号为 `202608291704113E3B`。
9. 回归后 Oracle 只读总验收 21 组断言全部通过：45 张表、45 个主键、至少 49 个业务索引，无失效对象、账实错误、资产错配或失败恢复记录。
10. `steam-platform-api`、Nginx、项目专用证书续期 timer 均为 active，系统 failed unit 数量为 0；HTTP 跳转、HTTPS、API 与数据库健康检查全部通过。
