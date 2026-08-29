import { expect, test } from '@playwright/test';
import { assertNoHorizontalOverflow, dismissStartupAnnouncement } from './helpers';

test.describe('公开商店与媒体画廊', () => {
  test('首页先渲染商店，再以前置浮窗显示启动公告', async ({ page, request }) => {
    const health = await request.get('/health');
    expect(health.ok()).toBeTruthy();

    await page.goto('/');
    await expect(page).toHaveURL(/\/store$/);
    await expect(page.getByText('精选与推荐')).toBeVisible();
    await expect(page.getByRole('heading', { name: '浏览热门游戏' })).toBeVisible();

    const announcement = page.getByRole('dialog', { name: /荒野生存特别活动现已开放|饰品市场/ });
    await expect(announcement).toBeVisible();
    await dismissStartupAnnouncement(page);
    await expect(announcement).toBeHidden();
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
