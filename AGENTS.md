# Codex Collaboration Notes

本文件记录 Codex 在本仓库工作时必须长期遵守的项目协作规则。

## Pull Request Review Duty

- Codex 需要定期检查本仓库是否有新的 Pull Request。
- 检查命令优先使用 `gh pr list --repo MXH814/steam-platform-db-course-design`。
- 对每个 PR，必须先阅读改动范围、运行可行的测试或构建，再决定 review 结果。
- 如果 PR 符合 README 技术路线、能通过测试、没有密钥/连接串/大文件/乱码/架构冲突，可以 approve。
- 如果 PR 违反 README、课程提纲、C#/.NET/Oracle/B/S 技术路线、分层规范、安全规范，或构建失败，必须 request changes，并写清楚阻塞原因。
- 不允许为了尽快合并而无脑 approve。
- 普通组员 PR 应保持 PR + review 流程；总负责人/仓库管理员只在 README、配置、紧急修复等维护场景下绕过保护规则。

## Current Repository Policy

- 普通组员不能直接 push `main`。
- 组员应从功能分支发 PR。
- `main` 禁止 force push 和删除。
- `_archive/` 保存我们自己做过的旧项目文件，需要提交到 GitHub。
- `_local_tools_archive/`、`*.pem`、`*.key` 不能提交。
