# Backend

ASP.NET Core C# 后端工程目录。

当前已落地 Group A 的最小后端闭环：健康检查、玩家注册、登录、当前用户查询、公告公开查询、管理员公告发布和公告更新。

当前结构：

```text
backend/
  SteamPlatform.Backend.sln
  src/
    SteamPlatform.Api/
  tests/
    SteamPlatform.Api.Tests/
```

当前技术：

- C#
- .NET 8
- ASP.NET Core Web API
- Oracle.ManagedDataAccess.Core
- Dapper
- HMAC 演示 token
- Swagger / OpenAPI

已实现接口：

```text
GET    /health
GET    /health/database
POST   /api/auth/register
POST   /api/auth/register/player
POST   /api/auth/login
GET    /api/auth/me
GET    /api/notices
POST   /api/notices
POST   /api/admin/notices
PUT    /api/admin/notices/{noticeId}
```

运行命令：

```powershell
dotnet restore backend\SteamPlatform.Backend.sln
dotnet build backend\SteamPlatform.Backend.sln
dotnet test backend\SteamPlatform.Backend.sln
dotnet run --project backend\src\SteamPlatform.Api
```

本地 Oracle 连接通过 `backend/src/SteamPlatform.Api/appsettings.json` 的 `ConnectionStrings:Oracle` 或 User Secrets 配置。`Auth:SigningKey` 至少 32 字节；Development 未配置时使用进程内演示 key。

初始化数据中的演示登录账号：

```text
PLAYER  alice     / alice
PLAYER  bob       / bob
ADMIN   rootadmin / admin
DEVELOPER dev@example.com / TAX-DEMO-001
```

禁止把 Spring Boot、Java、MyBatis 作为本项目后端主线。
