# Playwright 端到端回归与答辩录屏手册

本手册规定项目的浏览器回归、真实云端写库测试、失败证据和答辩备用录屏流程。测试目标固定为 Vue 前端、ASP.NET Core .NET 10 五层后端、腾讯云 Oracle 和 SignalR，不得用前端 Mock 代替云端业务闭环验收。

## 1. 测试分层

| Playwright 项目 | 视口 | 数据影响 | 覆盖内容 |
|---|---:|---|---|
| `desktop-chromium` | 1440 x 900 | 只读 | 启动公告、商店、CS2/DST 媒体画廊、Alice/Bob 登录基线、游戏库、库存、钱包、资料、社区、报价和市场 |
| `mobile-chromium` | Pixel 7 | 只读 | 与桌面相同的主要入口、响应式布局和页面横向溢出检查 |
| `defense-chromium` | 1440 x 900 | 写 Oracle | 临时玩家注册、CS2 免费入库、充值、DST 购买、评测、成就、退款审核、Alice 挂单、撮合、钱包、Bob 库存和流转账本 |
| `defense-recording` | 1920 x 1080 | 写 Oracle | 与固定答辩链相同，并生成连续 WebM 备用录屏 |

完整回归先完成两个只读项目，再执行 `defense-chromium`。这个依赖顺序不可移除，否则市场成交会改变 Alice、Bob 的固定库存和挂单基线，使后续只读断言失真。

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
2. 恢复工具生成快照、审计运行号并校验 42 张业务表固定基线。
3. 执行桌面只读、移动只读和真实写库答辩链。
4. 无论测试成功、失败或抛出异常，都在 `finally` 中再次执行固定基线恢复。
5. 返回 Playwright 的真实退出码；后置恢复失败时强制返回失败。

仅调试答辩链时使用 `npm run test:e2e:defense:cloud`。该命令同样执行前后恢复，不允许直接绕过恢复器运行写库用例。

## 5. 备用录屏

```bash
E2E_SSH_TARGET="ubuntu@<server>" \
E2E_SSH_KEY="<private-key-path>" \
npm run record:defense:cloud
```

录制模式在一个 1920 x 1080 浏览器上下文中依次切换临时玩家、管理员、Alice 和 Bob，避免生成无法按业务顺序播放的多段视频。稳定副本输出到：

```text
output/playwright/defense-recording/steam-platform-defense-demo.webm
```

该目录是本地验收产物并被 Git 忽略。正式答辩电脑需提前把视频复制到离线演示介质，并实际播放一次确认画面、时长和解码正常。

## 6. 报告和失败证据

| 产物 | 位置 |
|---|---|
| 完整 HTML 报告 | `output/playwright/html-report/index.html` |
| 录屏测试报告 | `output/playwright/defense-recording-report/index.html` |
| 失败截图、视频与 Trace | `output/playwright/test-results/` |
| 稳定备用录屏 | `output/playwright/defense-recording/steam-platform-defense-demo.webm` |

失败时不得只看终端最后一行。先查看截图和 `error-context.md`，再使用 `npx playwright show-trace <trace.zip>` 检查网络响应、DOM、操作时间线和控制台。修复选择器时必须继续验证原业务事实，不能为了变绿而删除关键断言。

## 7. 固定业务断言

写库答辩链至少证明：

1. 临时玩家注册后角色为 `PLAYER`，CS2 免费入库且钱包仍为 ¥0.00。
2. 充值 ¥60.00 后购买 ¥24.00 的 DST，余额为 ¥36.00，两款游戏同时在库。
3. DST 评测可发表，项目成就从 0/6 变为 1/6。
4. 管理员通过退款后余额恢复 ¥60.00，DST 从正常游戏库撤销。
5. Alice 以 ¥49.00 出售 `ITEM_CS2_002`，与 Bob 的 ¥50.00 买单按价格优先成交。
6. Alice 余额为 ¥222.55；Bob 可用余额为 ¥243.75、冻结余额为 ¥0.00。
7. Bob 获得 `ITEM_CS2_002`，流转账本显示 `P001 -> P002`。

金额、实例编号、账号或顺序变化必须先由总负责人批准，并同步修改固定数据、答辩手册、Playwright 断言和 README。

## 8. 2026-08-26 验收记录

1. 云端完整回归 11/11 通过，用时约 1.1 分钟。
2. 桌面只读 5 项、移动只读 5 项、真实 Oracle 写库答辩链 1 项全部通过。
3. 完整回归结束后的恢复运行编号为 `202608251745528ED6`。
4. 录屏链 1/1 通过，产出 1920 x 1080、25 fps、22.12 秒 WebM，文件约 2.73 MiB。
5. 录屏结束后的恢复运行编号为 `202608251746364404`。
6. 本轮自动化发现并修复了库存“模拟掉落”和“确认出售”成功提示被刷新操作立即清空的问题。
