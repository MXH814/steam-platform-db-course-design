# Frontend

Vue 3 + Vite + TypeScript 前端工程目录。

## 运行方式

```powershell
cd frontend
npm install
npm run dev
```

开发服务器默认地址：

```text
http://localhost:5173/
```

## 云端 API 配置

前端代码里的接口路径已经包含 `/api`，例如 `/api/games`、`/api/auth/login`。

默认情况下，本地 `npm run dev` 会把 `/api` 和 `/health` 代理到云服务器：

```text
http://124.222.213.245
```

因此组员只要拉取最新代码并运行 `npm install`、`npm run dev`，一般不需要额外配置即可访问云端数据。

如果要临时连接自己的本地后端，在 `frontend/.env.development.local` 中写：

```env
VITE_API_PROXY_TARGET=http://localhost:5253
```

如果要绕过 Vite 代理，直接让浏览器请求云端 API 主机，可以写：

```env
VITE_API_BASE_URL=http://124.222.213.245
```

不要写成 `http://124.222.213.245/api`，否则会拼成 `/api/api/...`。

## 演示账号

```text
玩家 alice / alice
玩家 bob / bob
管理员 rootadmin / admin
开发商 valve@example.com / valve
开发商 klei@example.com / klei
```

## 安全要求

- 前端只保存 JWT token，不保存密码。
- 不要把 Oracle 连接串、数据库密码、JWT SigningKey、云服务器 SSH 密码写进前端代码或 `.env` 文件。
- 组员联调只通过 API 访问云端数据，不直连 Oracle。
