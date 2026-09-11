# Product

## Register

product

## Users

课程设计小组成员、答辩演示者和测试验收人员。用户主要在浏览器中完成 Steam 风格商店浏览、游戏入库、游戏库查看、社区评价、成就和 CS2 饰品市场入口联调。

## Product Purpose

本项目是基于 Vue 前端、ASP.NET Core Web API 和 Oracle 数据库的 Steam 风格课程设计平台。成功标准是两款主演示游戏 `Counter-Strike 2` 和 `Don't Starve Together / 饥荒联机版` 能清楚展示商店、详情、入库、游戏库、社区、成就、库存和市场的页面职责边界。

## Brand Personality

深色、克制、信息密集。界面应接近 Steam 商店与 Steam 库的产品体验，不做营销落地页或普通后台系统。

## Anti-references

不要做普通电商卡片商城、后台管理表格首页、大面积紫色渐变、玻璃拟态、无业务意义的复杂栏目、重复入口堆叠、与 README 范围无关的 Steam 复杂功能。

## Design Principles

- 商店详情页负责商品展示、口碑摘要、购买或入库状态，不承载个人库存和市场撮合业务。
- 游戏库详情页负责已拥有游戏的个人数据，包括游玩时长、成就进度和已入库后可用入口。
- CS2 作为免费入库和饰品经济样板，DST 作为买断制、DLC/礼包、评价和成就样板。
- 所有入口必须指向已实现或明确预留的页面，避免空页面和重复跳转。
- 前端只通过 API 联动业务数据，不直接修改云端 Oracle 数据库。

## Accessibility & Inclusion

默认按产品 UI 标准处理：深色界面保证文本对比度，交互状态清晰，移动端不溢出，动效克制并支持减少动态效果偏好。
