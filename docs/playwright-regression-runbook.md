# Playwright 端到端回归手册

本手册规定项目的浏览器回归、真实云端写库测试和失败证据处理流程。测试目标固定为 Vue 前端、ASP.NET Core .NET 10 五层后端、腾讯云 Oracle 和 SignalR，不得用前端 Mock 代替云端业务闭环验收。

## 1. 测试分层

| Playwright 项目 | 视口 | 数据影响 | 覆盖内容 |
|---|---:|---|---|
| `desktop-chromium` | 1440 x 900 | 只读 | 启动公告、商店、CS2/DST 媒体画廊、Alice/Bob 登录基线、游戏库、库存、钱包、资料、社区、报价和市场 |
| `mobile-chromium` | Pixel 7 | 只读 | 与桌面相同的主要入口、响应式布局和页面横向溢出检查 |
| `defense-chromium` | 1440 x 900 | 写 Oracle | 交易答辩链；资料装扮、好友请求、SignalR 聊天、动态、讨论、徽章、评测互动、工坊订阅和交易报价社交链 |

完整回归先完成两个只读项目，再执行 `defense-chromium` 中的两条写库链。这个依赖顺序不可移除，否则市场成交和交易报价会改变 Alice、Bob 的固定库存和挂单基线，使后续只读断言失真。

## 2. 首次准备

在 `frontend/` 下执行：

```bash
npm ci
npx playwright install chromium
```

浏览器是本地测试工具，不提交到 Git。Playwright 版本由 `package-lock.json` 固定，升级依赖后必须重新执行浏览器安装命令。

## 3. 本地只读回归

```bash
npm run test:e2e
```

本地命令自动启动 Vite；Vite 默认把 API、健康检查和 SignalR 代理到既定腾讯云服务。由于没有设置 `E2E_MUTATING=1`，写库答辩链会安全跳过。开发者不能手工设置该变量后直接执行普通命令。

## 4. 云端完整回归

云端完整回归必须由总负责人或获授权管理员执行。执行环境只传递 SSH 目标和私钥文件位置，不传数据库密码：

```bash
E2E_SSH_TARGET="ubuntu@<server>" \
E2E_SSH_KEY="<private-key-path>" \
npm run test:e2e:cloud
```

PowerShell 使用同名环境变量后运行相同 npm 命令。脚本会依次完成：

1. 通过 SSH 调用服务器固定入口 `/opt/steam-platform/bin/reset-demo-data`。
2. 恢复工具生成快照、审计运行号并校验 45 张业务表固定基线。
3. 执行桌面只读、移动只读和真实写库答辩链。
4. 无论测试成功、失败或抛出异常，都在 `finally` 中再次执行固定基线恢复。
5. 返回 Playwright 的真实退出码；后置恢复失败时强制返回失败。

仅调试交易答辩链时使用 `npm run test:e2e:defense:cloud`，仅调试社交社区链时使用 `npm run test:e2e:social:cloud`。两个命令同样执行前后恢复，不允许直接绕过恢复器运行写库用例。

## 5. 报告和失败证据

| 产物 | 位置 |
|---|---|
| 完整 HTML 报告 | `output/playwright/html-report/index.html` |
| 失败截图、视频与 Trace | `output/playwright/test-results/` |

失败时不得只看终端最后一行。先查看截图和 `error-context.md`，再使用 `npx playwright show-trace <trace.zip>` 检查网络响应、DOM、操作时间线和控制台。修复选择器时必须继续验证原业务事实，不能为了变绿而删除关键断言。

## 6. 固定业务断言

写库答辩链至少证明：

1. 临时玩家注册后角色为 `PLAYER`，CS2 免费入库且钱包仍为 ¥0.00。
2. 充值 ¥60.00 后购买 ¥24.00 的 DST，余额为 ¥36.00，两款游戏同时在库。
3. DST 评测可发表，项目成就从 0/6 变为 1/6。
4. 管理员通过退款后余额恢复 ¥60.00，DST 从正常游戏库撤销。
5. Alice 以 ¥49.00 出售 `ITEM_CS2_002`，与 Bob 的 ¥50.00 买单按价格优先成交。
6. Alice 余额为 ¥222.55；Bob 可用余额为 ¥243.75、冻结余额为 ¥0.00。
7. Bob 获得 `ITEM_CS2_002`，流转账本显示 `P001 -> P002`。

社交社区写库链至少证明：

1. 临时玩家修改签名、简介、主题和展示游戏后，刷新页面仍能从 Oracle 读取相同资料。
2. 临时玩家向 Alice 发送好友申请，Alice 接受后双方关系均为 `ACCEPTED`。
3. 双方同时打开聊天抽屉时，消息经 SignalR 即时到达；刷新后历史消息仍可读取。
4. 社区动态、DST 讨论主题和 Alice 回复均可持久化，主题作者实时收到回复通知。
5. Alice 的展示徽章、Bob 对评测的互动和 Bob 的工坊订阅在刷新后保持。
6. Bob 接受固定报价 `TO_DEMO_001` 后，状态变为 `ACCEPTED`，双方物品由 Oracle 事务完成转移。

金额、实例编号、账号或顺序变化必须先由总负责人批准，并同步修改固定数据、答辩手册、Playwright 断言和 README。

## 7. 最终回归基线

1. 浏览器回归共 28 项：桌面只读 13 项、移动只读 13 项、交易答辩链 1 项、社交社区写库链 1 项。
2. 两条写库链只能通过带前后恢复的云端命令执行。
3. 每次完成写库回归后，必须再次运行 Oracle 总验收，确认临时账号和测试写入均已清理。
