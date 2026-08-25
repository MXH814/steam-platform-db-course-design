# SteamPlatform.DemoData

用于答辩前生成 Oracle 业务数据快照、重置固定演示基线和按快照回滚。工具只从环境变量读取连接字符串，不接受命令行连接字符串，避免凭据进入 shell 历史。

先查看计划，不连接数据库：

```powershell
dotnet run --project backend/tools/SteamPlatform.DemoData -- plan
```

云端管理员确认迁移 `database/migrations/20260825_demo_reset_audit.sql` 已执行后，使用服务器私有环境变量运行：

```bash
export STEAM_ORACLE_ADMIN_CONNECTION='服务器私有连接字符串'
dotnet SteamPlatform.DemoData.dll reset --root /opt/steam-platform/source --actor defense-admin --confirm RESET-DEMO-DATA
dotnet SteamPlatform.DemoData.dll list --root /opt/steam-platform/source
dotnet SteamPlatform.DemoData.dll restore --root /opt/steam-platform/source --run-id 运行编号 --actor defense-admin --confirm RESTORE-DEMO-DATA
```

每次重置先为清单内每张业务表创建 `DRB_<runId>_<顺序>` 同库快照，再在单个事务内清理、写入 `database/data.sql`、验证最小数据量并提交。失败时业务变更回滚，快照保留。恢复同样在单个事务内完成，并逐表核对快照行数。

不要把连接字符串、密码或导出的私密配置写入仓库。云端执行重置前必须得到总负责人确认，并先确认 `/api/health` 与 `/health/database` 正常。
