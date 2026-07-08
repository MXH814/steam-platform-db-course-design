# Group D 社区评价与成就接口契约

本文档记录 Group D 中“游戏评价、评价历史版本、成就字典、玩家成就解锁”的接口、业务规则和测试口径。当前实现重点围绕 `GAME_DST` 的社区与自定义成就演示链路。

## 1. 模块定位

本模块负责玩家在拥有游戏后进行社区互动：发表评价、修改评价、查看评价历史版本，以及解锁课程项目自定义成就。

核心演示链路：

```text
玩家登录
  -> 拥有 Don't Starve Together / GAME_DST
  -> 发表 GAME_DST 评价
  -> 修改评价并生成历史版本
  -> 查看评价版本历史
  -> 解锁 GAME_DST 自定义成就
```

## 2. 涉及数据表

| 表名 | 用途 |
|---|---|
| `GAME` | 游戏基础信息，校验游戏是否存在 |
| `PLAYER` | 玩家账号与昵称信息 |
| `PLAYER_LIBRARY` | 玩家游戏库与资产确权，评价和成就解锁必须要求 `status = 'NORMAL'` |
| `GAME_REVIEW` | 游戏评价主记录 |
| `REVIEW_VERSION` | 评价历史版本记录 |
| `ACHIEVEMENT` | 游戏成就字典 |
| `PLAYER_ACHIEVEMENT` | 玩家成就解锁记录 |

## 3. API 列表

### 3.1 查询游戏评价

```http
GET /api/games/{gameId}/reviews?limit=50
```

说明：查询指定游戏的可见评价列表，按最新评价时间倒序返回。

路径参数：

| 参数 | 说明 |
|---|---|
| `gameId` | 游戏 ID，例如 `GAME_DST` |

查询参数：

| 参数 | 说明 |
|---|---|
| `limit` | 可选，返回数量，后端限制在 1 到 100，默认 50 |

返回字段摘要：

```json
[
  {
    "reviewId": "REV...",
    "userId": "P001",
    "nickname": "alice",
    "gameId": "GAME_DST",
    "thumbsUp": 0,
    "status": "VISIBLE",
    "versionNo": 1,
    "isRecommend": true,
    "content": "Good survival game."
  }
]
```

### 3.2 创建游戏评价

```http
POST /api/games/{gameId}/reviews
Authorization: Bearer <PLAYER_TOKEN>
```

请求体：

```json
{
  "isRecommend": true,
  "content": "Good survival game."
}
```

业务规则：

| 规则 | 说明 |
|---|---|
| 玩家鉴权 | 只能使用 `PLAYER` 角色 token |
| 游戏存在 | `GAME.game_id = gameId` 必须存在 |
| 资产确权 | 当前 `user_id + game_id` 必须在 `PLAYER_LIBRARY` 存在且 `status = 'NORMAL'` |
| 防重复评价 | 同一玩家同一游戏只能有一条评价主记录 |
| 版本初始化 | 创建评价时同步写入 `REVIEW_VERSION.version_no = 1` |

成功状态：`201 Created`

### 3.3 修改游戏评价

```http
PUT /api/reviews/{reviewId}
Authorization: Bearer <PLAYER_TOKEN>
```

请求体：

```json
{
  "isRecommend": false,
  "content": "Updated review content."
}
```

业务规则：

| 规则 | 说明 |
|---|---|
| 玩家鉴权 | 只能使用 `PLAYER` 角色 token |
| 评价存在 | `GAME_REVIEW.review_id = reviewId` 必须存在 |
| 只能改自己的评价 | 当前玩家 `user_id` 必须等于评价所属玩家 |
| 保留历史 | 修改不覆盖旧内容，而是新增一条 `REVIEW_VERSION` |

成功状态：`200 OK`

### 3.4 查询评价历史版本

```http
GET /api/reviews/{reviewId}/versions
```

说明：返回指定评价的所有历史版本，按版本号倒序。

成功状态：`200 OK`

### 3.5 查询游戏成就

```http
GET /api/games/{gameId}/achievements
Authorization: Bearer <PLAYER_TOKEN> 可选
```

说明：查询指定游戏的成就字典。如果当前请求带有玩家 token，会同时返回该玩家是否已经解锁。

返回字段摘要：

```json
[
  {
    "achId": "ACH_DST_SURVIVE_001",
    "gameId": "GAME_DST",
    "achName": "First Night",
    "description": "Survive the first night.",
    "globalRate": 35.5,
    "isUnlocked": false,
    "unlockTime": null
  }
]
```

### 3.6 解锁成就

```http
POST /api/achievements/{achId}/unlock
Authorization: Bearer <PLAYER_TOKEN>
```

业务规则：

| 规则 | 说明 |
|---|---|
| 玩家鉴权 | 只能使用 `PLAYER` 角色 token |
| 成就存在 | `ACHIEVEMENT.ach_id = achId` 必须存在 |
| 查成就所属游戏 | 先读取 `ACHIEVEMENT.game_id` |
| 资产确权 | 当前 `user_id + achievement.game_id` 必须在 `PLAYER_LIBRARY` 存在且 `status = 'NORMAL'` |
| 防重复解锁 | 已解锁时不重复插入，直接返回已有记录并标记 `alreadyUnlocked = true` |

成功状态：`200 OK`

## 4. 业务错误码

| 错误码 | HTTP 状态 | 触发场景 |
|---|---:|---|
| `GAME_NOT_OWNED` | 409 | 玩家没有正常拥有该游戏，却尝试发表评价或解锁该游戏成就 |
| `REVIEW_ALREADY_EXISTS` | 409 | 同一玩家重复评价同一游戏 |
| `Forbidden` | 403 | 非玩家角色访问玩家接口，或玩家修改他人评价 |
| `Not found` | 404 | 游戏、评价或成就不存在 |
| `Invalid request` | 400 | 必填字段为空，例如 `gameId`、`content`、`reviewId`、`achId` |

错误响应使用 ASP.NET Core `ProblemDetails`：

```json
{
  "title": "GAME_NOT_OWNED",
  "status": 409,
  "detail": "The player does not own this game."
}
```

## 5. 测试覆盖

自动测试覆盖点：

| 测试 | 目的 |
|---|---|
| `Create_review_requires_player_authentication_before_input_validation` | 未登录不能发表评价 |
| `Create_review_forbids_admin_tokens_before_opening_database` | 管理员 token 不能冒充玩家发表评价 |
| `Update_review_requires_player_authentication_before_input_validation` | 未登录不能修改评价 |
| `Unlock_achievement_requires_player_authentication_before_opening_database` | 未登录不能解锁成就 |
| `Unlock_achievement_forbids_admin_tokens_before_opening_database` | 管理员 token 不能解锁玩家成就 |
| `Create_review_rejects_unowned_game_before_writing_review` | 未拥有游戏不能发表评价，且不会写 `GAME_REVIEW` / `REVIEW_VERSION` |
| `Unlock_achievement_rejects_unowned_game_before_writing_unlock` | 未拥有成就所属游戏不能解锁成就，且不会写 `PLAYER_ACHIEVEMENT` |
| `Business_rules_map_to_409_with_error_code_title` | 业务异常会返回 409 和清晰错误码 |

验证命令：

```powershell
dotnet test backend\SteamPlatform.sln
dotnet test tests\SteamPlatform.Database.Tests\SteamPlatform.Database.Tests.csproj
cd frontend
npm.cmd run build
```

当前验证基线：

```text
backend\SteamPlatform.sln: 115 tests passed
tests\SteamPlatform.Database.Tests: 25 tests passed
frontend build: passed
```

## 6. 手动演示建议

正向流程：

```text
登录拥有 GAME_DST 的玩家
  -> 打开 /games/GAME_DST/community
  -> 发表评价
  -> 修改评价
  -> 查看评价历史版本
  -> 解锁 GAME_DST 自定义成就
```

反向流程：

```text
登录未拥有 GAME_DST 的玩家
  -> 发表 GAME_DST 评价，应返回 GAME_NOT_OWNED
  -> 解锁 GAME_DST 成就，应返回 GAME_NOT_OWNED
```

答辩说明重点：

- 评价不是覆盖更新，而是保留历史版本。
- 成就解锁具备幂等处理，重复解锁不会重复写入。
- 评价和成就都依赖 `PLAYER_LIBRARY.status = 'NORMAL'` 做资产确权，不能绕过 Group C 的购买/入库主链路。