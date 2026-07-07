# Backend

ASP.NET Core C# 后端工程目录。

本目录用于创建课程项目的 B/S 应用服务器，必须遵守课程提纲中的 VS.NET、C#、Oracle 18c+、Oracle 数据访问组件或 ORM 框架要求。

当前已落地 Group A 后端与联调基础闭环：健康检查、玩家注册、玩家登录、管理员登录、当前用户查询、公告公开查询、管理员公告发布和公告更新。

当前结构：

```text
backend/
  SteamPlatform.sln
  src/
    SteamPlatform.Api/
    SteamPlatform.Application/
    SteamPlatform.Domain/
    SteamPlatform.Infrastructure/
    SteamPlatform.Shared/
  tests/
    SteamPlatform.Api.Tests/
```

当前技术：

- C#
- .NET 10 LTS
- ASP.NET Core Web API
- Oracle.EntityFrameworkCore
- Oracle.ManagedDataAccess.Core
- Dapper
- ASP.NET Core Authentication + JWT Bearer
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
dotnet restore backend\SteamPlatform.sln
dotnet build backend\SteamPlatform.sln
dotnet test backend\SteamPlatform.sln
dotnet run --project backend\src\SteamPlatform.Api
```

本地 Oracle 连接通过 `backend/src/SteamPlatform.Api/appsettings.json` 的 `ConnectionStrings:Oracle` 或 User Secrets 配置。`Auth:SigningKey` 至少 32 字节；Development 未配置时使用进程内演示 key。

当前不允许使用 `DEVELOPER.tax_id` 作为登录密码。开发商登录需要等 `DEVELOPER` 表补充安全密码哈希字段后再接入。

初始化数据中的演示登录账号：

```text
PLAYER  alice     / alice
PLAYER  bob       / bob
ADMIN   rootadmin / admin
```

禁止把 Spring Boot、Java、MyBatis 作为本项目后端主线。
