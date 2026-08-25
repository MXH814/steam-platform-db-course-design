# Database

Oracle 数据库脚本目录。

文件约定：

- `schema.sql`：创建 27 张核心业务表、3 张演示恢复运维审计表、约束和索引。
- `data.sql`：插入可演示的初始化数据。
- `verify_phase1.sql`：阶段 1 验收脚本，验证表数量、初始化数据和关键约束。
- `migrations/`：云端或既有库的增量迁移脚本，按文件名日期顺序执行。
- `admin/`：仅放本地开发/验收用的管理脚本。
- `demo/manifest.json`：定义答辩演示数据的表依赖顺序、基线脚本和最低行数校验。
- `logs/`：SQL*Plus 验收日志输出目录，不提交 Git。

重要决策：

- `PLAYER` 表不包含 `wallet_balance`。
- 资金唯一真相是 `WALLET_ACCOUNT.available_balance` 与 `WALLET_ACCOUNT.frozen_balance`。
- 总余额通过查询计算，不单独落库。
- `data.sql` 中玩家、管理员、开发商演示账号使用 `PBKDF2$SHA256$...` 格式哈希，不提交真实明文密码。
- `DEMO_RESET_RUN`、`DEMO_RESET_TABLE`、`DEMO_RESET_EVENT` 只记录演示恢复的快照、表映射和审计事件，不属于 27 张核心业务表。
- 演示重置必须通过 `backend/tools/SteamPlatform.DemoData` 执行；连接字符串只从服务器私有环境变量读取。
- 云端重置前必须先运行 `plan`、确认数据库健康，并由总负责人明确批准。工具会先创建同库逻辑快照，失败时回滚业务事务。
