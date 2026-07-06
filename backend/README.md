# Backend

ASP.NET Core C# 后端工程目录。

本目录用于创建课程项目的 B/S 应用服务器，必须遵守课程提纲中的 VS.NET、C#、Oracle 18c+、Oracle 数据访问组件或 ORM 框架要求。

计划结构：

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
    SteamPlatform.Tests/
```

计划技术：

- C#
- .NET 10 LTS
- ASP.NET Core Web API
- Oracle.EntityFrameworkCore
- Oracle.ManagedDataAccess.Core
- Dapper
- JWT authentication
- Swagger / OpenAPI

禁止把 Spring Boot、Java、MyBatis 作为本项目后端主线。

