# HTTPS 部署与回滚手册

## 1. 当前方案

项目先使用腾讯云公网 IPv4 的可信 HTTPS：

```text
https://124.222.213.245
```

证书由 Let's Encrypt 签发，为其 `shortlived` 配置的 6 天公网 IP 证书。由于证书周期短，服务器必须保持 80 端口的 ACME challenge 可访问，并由 systemd 每小时检查续期。未来购买并完成 `steam-db-lab.com` 实名与 ICP 备案后，只替换 Nginx `server_name`、证书标识和 DNS 解析，不改变 Vue、ASP.NET Core、SignalR 或 Oracle 架构。

本方案不开放 Oracle `1521`，公网仍只使用 `22`、`80`、`443`。

## 2. 实现资产

- `backend/tools/SteamPlatform.HttpsDeploy/`：`.NET 10` 部署工具。
- `plan`：只读显示执行计划。
- `stage`：签发不受浏览器信任的测试 IP 证书，不切换 Nginx。
- `enable`：签发生产证书、备份 Nginx、启用 443、配置续期并完成验证。
- `verify`：验证 Nginx、续期 timer、HTTP 跳转、可信 TLS、前端、API 和 Oracle 健康检查。
- `rollback`：恢复启用前的 Nginx 和系统 Certbot timer 状态，保留证书与备份作为审计证据。

工具使用系统 Python 的 `pip --target` 把 Certbot `5.7.0` 隔离安装到 `/opt/steam-platform/tools/certbot/packages/`，不替换 Ubuntu 自带 Certbot，也不要求额外安装 `python3-venv`。部署状态写入 `/var/lib/steam-platform-https/state.json`；该文件不含账号密码或私钥内容。

## 3. 部署前检查

1. 腾讯云防火墙允许公网 TCP `80` 与 `443`。
2. 首次部署前确认 HTTP 健康检查正常；生产切换后以 `https://124.222.213.245/api/health` 和 `/health/database` 为正式健康检查入口。
3. `/etc/nginx/sites-available/steam-platform` 与实际加载的 `/etc/nginx/sites-enabled/steam-platform` 均存在，且 `nginx -t` 成功；本服务器两者是独立文件，工具会同时备份和更新，防止配置漂移。
4. `/opt/steam-platform/www` 是当前前端根目录。
5. ACME 联系邮箱只通过标准输入或权限为 `0600` 的临时文件传入，不写入 Git、README 或命令历史。
6. 先完成 `stage`，确认测试签发成功后才允许执行 `enable`。

## 4. 发布工具

在开发机发布 Linux 产物：

```powershell
dotnet publish backend/tools/SteamPlatform.HttpsDeploy/SteamPlatform.HttpsDeploy.csproj `
  --configuration Release `
  --runtime linux-x64 `
  --self-contained true `
  --output .deploy/https-deploy
```

将 `.deploy/https-deploy/` 上传到服务器 `/opt/steam-platform/tools/https-deploy/`。`.deploy/` 已被 Git 忽略，不提交发布产物和 SSH 私钥。

## 5. 服务器执行顺序

邮箱应由总负责人通过标准输入或权限为 `0600` 的临时文件传给工具，不写入仓库文件、环境文件或命令参数。自动化部署使用 `--email-file /tmp/<临时文件>`，执行完成后立即删除该单一文件。

```bash
export STEAM_HTTPS_PUBLIC_IP=124.222.213.245

/opt/steam-platform/tools/https-deploy/SteamPlatform.HttpsDeploy plan

sudo --preserve-env=STEAM_HTTPS_PUBLIC_IP \
  /opt/steam-platform/tools/https-deploy/SteamPlatform.HttpsDeploy \
  stage --email-stdin true --confirm STAGE_IP_HTTPS

sudo --preserve-env=STEAM_HTTPS_PUBLIC_IP \
  /opt/steam-platform/tools/https-deploy/SteamPlatform.HttpsDeploy \
  enable --email-stdin true --confirm ENABLE_IP_HTTPS
```

`enable` 会执行以下受控变化：

1. 在 `/var/lib/steam-platform-https/backups/` 生成带 UTC 时间戳的两份 Nginx 原配置备份；备份绝不写入会被 Nginx 通配加载的 `sites-enabled`。
2. 写入只允许 TLS 1.2/1.3 的站点配置。
3. 保留 `/.well-known/acme-challenge/` 的 HTTP 访问，其余 HTTP 请求返回 `308`。
4. 反向代理 `/api/`、`/health`、`/health/database` 和支持 WebSocket Upgrade 的 `/hubs/`。
5. 停用不能处理 IP 证书的系统旧 Certbot timer，启用项目专用 timer。
6. 任何配置或公网验证失败时自动恢复原 Nginx 和旧 timer 状态。

## 6. 验收

服务器内置验证：

```bash
sudo /opt/steam-platform/tools/https-deploy/SteamPlatform.HttpsDeploy \
  verify --ip 124.222.213.245
```

开发机继续执行：

```powershell
curl.exe -I http://124.222.213.245/
curl.exe https://124.222.213.245/api/health
curl.exe https://124.222.213.245/health/database

$env:E2E_BASE_URL='https://124.222.213.245'
npm --prefix frontend run test:e2e:cloud
```

验收标准：

- HTTP 返回 `308` 且 `Location` 为同路径 HTTPS。
- 浏览器和 `HttpClient` 在不跳过证书校验的情况下信任证书。
- 首页、API、Oracle 健康检查均为 `2xx`。
- 服务器内置验证保持公网 IP 作为 URL 与 TLS 标识，但 TCP 连接使用本机回环，避免云厂商不支持实例访问自身公网 IP；开发机随后执行真正公网验证。
- Nginx reload 后的短暂旧 worker 存活期内，验证会关闭 keep-alive 并以新连接有限重试；超时仍会触发完整自动回滚。
- 好友聊天与通知通过 `/hubs/social` 建立 WebSocket 或 SignalR 协商连接。
- `steam-platform-certbot-renew.timer` 为 active，并能完成 `certbot renew --dry-run --cert-name steam-platform-ip`。
- 完整云端 Playwright 回归通过，演示数据在测试前后恢复到固定基线。

## 7. 生产验收记录

2026-08-27 已完成生产切换并通过以下验证：

1. Let's Encrypt 生产证书的 SAN 为公网 IP `124.222.213.245`，有效期至 2026-09-03；浏览器、`curl` 和 .NET 均在未跳过校验的情况下信任证书。
2. HTTP `80` 返回 `308`，HTTPS `443` 正常提供 Vue 首页、`/api/health`、`/health/database` 和 `/hubs/social`。
3. 旧 `certbot.timer` 已停用；`steam-platform-certbot-renew.timer` 已启用并处于 active 状态。
4. `certbot renew --dry-run --run-deploy-hooks --no-random-sleep-on-renew --cert-name steam-platform-ip` 执行成功，证明续期和 Nginx reload 钩子可用。
5. SignalR 公网实时消息冒烟通过；完整云端 Playwright 回归 12/12 通过，测试后固定演示基线恢复成功。
6. Oracle 只读总验收 21 组断言全部通过，Oracle `1521` 继续不对公网开放。

## 8. 回滚

常规回滚：

```bash
sudo /opt/steam-platform/tools/https-deploy/SteamPlatform.HttpsDeploy \
  rollback --confirm ROLLBACK_IP_HTTPS
```

若工具本身无法运行，读取 `/var/lib/steam-platform-https/state.json` 中的 `AvailableConfigBackupPath` 与 `EnabledConfigBackupPath`，由管理员分别恢复到 `/etc/nginx/sites-available/steam-platform` 和 `/etc/nginx/sites-enabled/steam-platform`，然后依次执行 `nginx -t` 与 `systemctl reload nginx`。不得删除 `/etc/letsencrypt`、整个 Nginx 目录或服务器应用目录。

## 9. 域名阶段

`steam-db-lab.com` 是否购买、购买年限、实名主体和 ICP 备案资料仍由总负责人单独确认。域名阶段开始前不得把临时第三方通配域名写入正式配置，也不得为了域名直接暴露 Oracle。
