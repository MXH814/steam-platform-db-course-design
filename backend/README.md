# 后端工程

本目录包含 C#/.NET 10 后端解决方案、自动化测试和服务器运维工具。

## 目录

```text
backend/
  SteamPlatform.sln
  src/
    SteamPlatform.Api/             HTTP、认证授权、异常映射和 SignalR
    SteamPlatform.Application/     应用契约、服务和输入校验
    SteamPlatform.Domain/          领域实体与业务规则
    SteamPlatform.Infrastructure/  Oracle 仓储、SQL 和事务
    SteamPlatform.Shared/          公共响应与异常类型
  tests/
    SteamPlatform.Api.Tests/
    SteamPlatform.DemoData.Tests/
    SteamPlatform.HttpsDeploy.Tests/
  tools/
    SteamPlatform.DemoData/        演示数据计划、快照、恢复和审计
    SteamPlatform.HttpsDeploy/     IP HTTPS 配置、验证和回滚
```

## 技术栈

- C#、.NET 10、ASP.NET Core Web API
- Dapper、Oracle.ManagedDataAccess.Core
- JWT Bearer 认证与角色授权
- SignalR 实时消息
- xUnit 自动化测试

`Oracle.EntityFrameworkCore` 作为依赖保留，当前业务路径没有启用 `DbContext` 或实体映射。

## 业务模块

- Auth：玩家注册，多角色登录，JWT 签发和当前用户。
- Games / Notices：商店、开发商游戏维护、管理员状态治理和公告。
- CoreTransactions：钱包、订单、购买、退款、CDKey 和游戏库。
- Community：评价版本和成就。
- Inventory / Market：饰品库存、买卖挂单、撮合、成交与账本。
- Social / Engagement：好友、私信、通知、工坊、资料、徽章、报价、动态和讨论。
- Diagnostics：应用与 Oracle 健康检查。

## 本地配置

连接串与 JWT 签名密钥使用 User Secrets 或私有环境变量配置：

```powershell
dotnet user-secrets set --project backend\src\SteamPlatform.Api "ConnectionStrings:Oracle" "User Id=steam_app;Password=***;Data Source=localhost:1521/FREEPDB1"
dotnet user-secrets set --project backend\src\SteamPlatform.Api "Auth:SigningKey" "至少32字节的随机签名密钥"
```

不得将真实密码、连接串或签名密钥写入仓库。

## 构建与运行

```powershell
dotnet restore backend\SteamPlatform.sln
dotnet build backend\SteamPlatform.sln -c Release
dotnet test backend\SteamPlatform.sln -c Release
dotnet run --project backend\src\SteamPlatform.Api
```

钱包、订单、退款和市场接口使用参数化 SQL。涉及资金、权益和饰品所有权的多表写入必须在 Oracle 事务中完成，并在服务端校验角色、状态和受影响行数。
