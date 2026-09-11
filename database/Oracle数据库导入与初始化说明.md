# Oracle 数据库导入与初始化说明

## 1. 适用环境

- Oracle Database 18c 或更高版本
- 一个空的 Oracle 模式账号，并具有建表、创建索引和写入数据的权限
- SQL*Plus、SQLcl 或 DBeaver 等 Oracle 客户端
- 服务名示例：`FREEPDB1`

项目不提交真实数据库账号、密码或连接串。下文中的 `steam_app`、主机和服务名应替换为实际环境值。

## 2. 文件说明

| 文件或目录 | 用途 |
| --- | --- |
| `schema.sql` | 创建最终 45 张表、224 个命名约束和 49 个显式业务索引 |
| `data.sql` | 写入固定演示数据，包括玩家、开发商、管理员、CS2、饥荒联机版和各业务基线 |
| `verify_phase1.sql` | 检查模式、初始化数据和关键约束 |
| `verify_defense.sql` | 只读检查 45 表、对象状态以及钱包、订单、退款、市场和社交数据一致性 |
| `migrations/` | 已有旧数据库按日期升级时使用的增量脚本 |
| `defense/` | 执行计划和双会话行级锁演示脚本 |
| `demo/manifest.json` | C# 演示数据恢复工具使用的表依赖和基线定义 |
| `admin/` | 受控管理脚本，不参加普通初始化 |

## 3. 新建空库

### 3.1 使用 SQL*Plus 或 SQLcl

在本目录打开终端并连接目标模式。命令会提示输入密码，不要把密码写进脚本：

```text
sqlplus steam_app@//localhost:1521/FREEPDB1
```

连接成功后依次执行：

```sql
@schema.sql
@data.sql
@verify_phase1.sql
@verify_defense.sql
```

`schema.sql` 和 `data.sql` 会写入数据库；两个 `verify` 脚本只执行查询和断言。执行完成后应确认：

- 数据表为 45 张，且 45 张表均有主码。
- 命名约束为 224 个，显式业务索引为 49 个。
- 无失效对象、失效索引或禁用关系约束。
- 钱包余额、订单退款、游戏授权、市场挂单、成交、物品归属和账本满足脚本中的一致性检查。

### 3.2 使用 DBeaver

1. 新建 Oracle 连接，填写主机、端口、服务名、模式账号和密码。
2. 连接后确认当前模式是新建的空模式。
3. 按 `schema.sql`、`data.sql`、`verify_phase1.sql`、`verify_defense.sql` 的顺序分别打开并执行整个脚本。
4. 在数据库导航器中刷新 Tables 和 Indexes，核对表与索引数量。
5. 查看两个验证脚本的输出，确认没有失败项。

## 4. 连接服务器内网 Oracle

云端 Oracle 的 1521 端口不对公网开放。需要从本机管理云端数据库时，先建立 SSH 隧道：

```text
ssh -L 11521:127.0.0.1:1521 ubuntu@124.222.213.245
```

隧道保持连接期间，DBeaver 使用以下网络参数：

```text
主机：127.0.0.1
端口：11521
服务名：FREEPDB1
```

数据库账号和密码由服务器负责人通过私密方式提供，不写入仓库、截图或答辩材料。

## 5. 从旧版本升级

已有项目旧库时，不重新执行 `schema.sql` 和 `data.sql`。先备份数据库，再按文件名日期顺序执行 `migrations/` 中尚未应用的脚本，最后执行 `verify_defense.sql`。

新建空库已经包含全部迁移结果，不要在初始化后再次执行历史迁移。

## 6. 程序连接

后端读取配置键 `ConnectionStrings:Oracle`。本地开发使用 .NET User Secrets：

```powershell
dotnet user-secrets set --project backend\src\SteamPlatform.Api "ConnectionStrings:Oracle" "User Id=steam_app;Password=***;Data Source=localhost:1521/FREEPDB1"
```

服务器部署使用私有环境变量或受限配置文件。前端只调用 ASP.NET Core API，不能直接连接 Oracle。

## 7. 常见问题

- `ORA-00955`：目标模式中已有同名对象。请换用空模式；不要在现有库上重复执行建库脚本。
- `ORA-01031`：模式账号权限不足，需要授予建表或创建索引权限。
- `ORA-12514`：服务名填写错误，应核对监听器中实际注册的服务名。
- 中文显示异常：客户端编码应使用 UTF-8，脚本文件不得转换为 ANSI。
- 验证失败：先停止应用写入，记录失败查询和相关主码，再检查脚本执行顺序及是否误跑历史迁移。
