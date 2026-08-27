# SteamPlatform.Database.Tests

数据库测试项目，独立于 Group A 后端 PR，可直接从目标仓库 `main` 分支运行。

默认测试只做静态验证：

- `database/schema.sql` 的 27 张核心表、15 张增强业务表和 3 张恢复审计表。
- 关键 CHECK/UNIQUE/角色状态枚举约束。
- `PLAYER.wallet_balance` 不被重新引入。
- `database/data.sql` 的种子登录账号使用 PBKDF2 哈希。
- `database/verify_phase1.sql` 保留失败即退出和核心约束检查。
- `database/verify_defense.sql` 覆盖完整 Schema 与订单、钱包、退款、市场、资产账本跨表一致性。
- 执行计划脚本必须覆盖订单、市场、社区查询并回滚 `PLAN_TABLE`，并发行锁脚本必须限制等待时间且不写业务数据。

运行：

```powershell
dotnet test tests\SteamPlatform.Database.Tests\SteamPlatform.Database.Tests.csproj
```

如需连接真实 Oracle 做冒烟验证，先配置：

```powershell
$env:STEAM_ORACLE_TEST_CONNECTION='User Id=steam_app;Password=***;Data Source=localhost:1521/FREEPDB1'
dotnet test tests\SteamPlatform.Database.Tests\SteamPlatform.Database.Tests.csproj
```

不要把真实连接串或密码提交到 Git。
