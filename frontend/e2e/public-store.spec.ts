import { expect, test } from '@playwright/test';
import { assertNoHorizontalOverflow, dismissStartupAnnouncement } from './helpers';

test.describe('公开商店与媒体画廊', () => {
  test('新上架游戏追加在首页末尾且不占用固定精选位', async ({ page }) => {
    await page.route((url) => url.pathname === '/api/games', async (route) => {
      await route.fulfill({
        contentType: 'application/json',
        body: JSON.stringify({
          code: 0,
          message: 'success',
          data: {
            items: [
              {
                gameId: 'GAME_SURVIVAL_LAB',
                gameName: 'Survival Lab',
                developerId: 'DEV_KLEI',
                developerName: 'Klei Entertainment',
                basePrice: 68,
                discountRate: 0.2,
                finalPrice: 54.4,
                releaseDate: '2026-09-11T00:00:00',
                reputation: null,
                status: 'ONLINE'
              },
              {
                gameId: 'GAME_CS2',
                gameName: 'Counter-Strike 2',
                developerId: 'DEV_VALVE',
                developerName: 'Valve',
                basePrice: 0,
                discountRate: 0,
                finalPrice: 0,
                releaseDate: '2023-09-27T00:00:00',
                reputation: '特别好评',
                status: 'ONLINE'
              },
              {
                gameId: 'GAME_DST',
                gameName: "Don't Starve Together / 饥荒联机版",
                developerId: 'DEV_KLEI',
                developerName: 'Klei Entertainment',
                basePrice: 48,
                discountRate: 0.5,
                finalPrice: 24,
                releaseDate: '2016-04-21T00:00:00',
                reputation: '好评如潮',
                status: 'ONLINE'
              }
            ],
            page: 1,
            pageSize: 50,
            total: 3
          }
        })
      });
    });

    await page.goto('/store');
    await dismissStartupAnnouncement(page);

    await expect(page.locator('.feature-summary h1')).toHaveText("Don't Starve Together / 饥荒联机版");
    await expect(page.locator('.game-grid .game-card h3')).toHaveText([
      'Counter-Strike 2',
      "Don't Starve Together / 饥荒联机版",
      'Survival Lab'
    ]);
  });

  test('首页先渲染商店，再以前置浮窗显示启动公告', async ({ page, request }) => {
    const health = await request.get('/health');
    expect(health.ok()).toBeTruthy();

    await page.goto('/');
    await expect(page).toHaveURL(/\/store$/);
    await expect(page.getByText('精选与推荐')).toBeVisible();
    await expect(page.getByRole('heading', { name: '浏览热门游戏' })).toBeVisible();

    const announcement = page.locator('.startup-announcement');
    await expect(announcement).toBeVisible();
    await announcement.getByRole('button', { name: '查看第 3 条公告' }).click();
    await expect(announcement.locator('.announcement-media img')).toHaveAttribute('src', '/assets/media/workshop-cosmetics-banner.jpg');
    await dismissStartupAnnouncement(page);
    await expect(announcement).toBeHidden();

    const search = page.getByRole('searchbox', { name: '搜索商店' });
    await search.fill('不存在的游戏名称');
    await page.waitForTimeout(350);
    await expect(page.getByRole('status').getByText('没有找到游戏', { exact: true })).toHaveCount(0);
    await page.getByRole('button', { name: '搜索', exact: true }).click();
    await expect(page.getByRole('status').getByText('没有找到游戏', { exact: true })).toBeVisible();
  });

  test('未知地址显示可恢复的 404 页面', async ({ page }) => {
    await page.goto('/this-route-does-not-exist');

    await expect(page.getByRole('heading', { name: '找不到这个页面' })).toBeVisible();
    await page.getByRole('link', { name: '返回商店' }).click();
    await expect(page).toHaveURL(/\/store$/);
  });

  for (const game of [
    { id: 'GAME_CS2', title: 'Counter-Strike 2', screenshot: /CS2 游戏截图/ },
    { id: 'GAME_DST', title: '饥荒联机版', screenshot: /饥荒联机版游戏截图/ }
  ]) {
    test(`${game.title} 提供预告片、五张截图与全屏查看`, async ({ page }) => {
      await page.goto(`/games/${game.id}`);
      await dismissStartupAnnouncement(page);

      const gallery = page.getByLabel(`${game.title} 媒体画廊`);
      await expect(gallery).toBeVisible();
      await expect(gallery.getByRole('listitem')).toHaveCount(6);

      await gallery.getByRole('listitem', { name: game.screenshot }).nth(0).click();
      await gallery.getByRole('button', { name: '全屏查看当前媒体' }).click();

      const viewer = page.getByRole('dialog', { name: `${game.title} 全屏媒体查看` });
      await expect(viewer).toBeVisible();
      await viewer.getByRole('button', { name: '下一个媒体' }).click();
      await expect(viewer.getByText(/\/ 6/)).toBeVisible();
      await viewer.getByRole('button', { name: '关闭全屏媒体' }).click();
      await expect(viewer).toBeHidden();
      await assertNoHorizontalOverflow(page);
    });
  }
});
