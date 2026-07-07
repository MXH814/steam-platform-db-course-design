# Frontend

Vue 3 + Vite + TypeScript 前端工程目录。

当前已落地 Group A 的基础页面和接口封装：

- `/`：公开公告列表。
- `/login`：玩家/管理员登录。
- `/register`：玩家注册。
- `/account`：当前用户信息。
- `/admin/notices`：管理员公告创建与更新。

本地运行：

```powershell
cd frontend
npm install
npm run dev
npm run build
```

Vite 开发服务器会把 `/api` 和 `/health` 代理到 `http://localhost:5253`。如需连接其他 API 地址，可配置 `VITE_API_BASE_URL`。

前端只保存 JWT，不保存密码；最终权限判断以后端为准。
