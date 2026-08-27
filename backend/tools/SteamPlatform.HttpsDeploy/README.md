# SteamPlatform.HttpsDeploy

`.NET 10` HTTPS 运维工具，用于腾讯云 Ubuntu 服务器的可信公网 IP 证书部署。工具不会接触 Oracle 连接串、SSH 私钥或应用密钥。

云端按 `linux-x64` 自包含方式发布，因此执行的是项目锁定的 .NET 10，不依赖服务器全局安装的旧运行时。

完整操作顺序、环境变量、验收和回滚方式见 `docs/https-deployment-runbook.md`。`render` 可在不修改系统的情况下输出待部署 Nginx 配置。实际执行必须遵守以下约束：

- `stage`、`enable`、`rollback` 都需要独立确认口令。
- 正式邮箱可通过 `--email-stdin true` 从标准输入读取，不进入命令参数或仓库文件。
- Certbot 固定安装在 `/opt/steam-platform/tools/certbot/packages/`，不替换 Ubuntu 系统包，也不要求镜像额外安装 `python3-venv`。
- `enable` 在改写 Nginx 前保存原配置；配置校验或公网验证失败时自动恢复。
- 生产证书为 Let's Encrypt 短期 IP 证书，由独立 systemd timer 每小时检查续期。
- 80 端口保留 ACME challenge，其余请求以 `308` 跳转 HTTPS；Oracle `1521` 不开放。
