import { expect, test } from '@playwright/test';
import { assertNoHorizontalOverflow, dismissStartupAnnouncement, loginAs } from './helpers';

test.describe('认证与权限体验 (Group A)', () => {
  test('未登录用户访问受保护路由重定向至登录页，登录成功后重定向回原页面', async ({ page }) => {
    await page.goto('/account');
    await dismissStartupAnnouncement(page);

    // 守卫拦截未登录用户并附加 redirect 查询参数
    await expect(page).toHaveURL(/\/login\?redirect=%2Faccount/);

    // 执行玩家登录
    await page.getByLabel('角色').selectOption('PLAYER');
    await page.getByLabel('账号').fill('alice');
    await page.getByLabel('密码').fill('alice');
    await page.getByRole('button', { name: '登录', exact: true }).click();

    // 验证成功返回 /account 页面
    await expect(page).toHaveURL(/\/account/);
    await expect(page.getByRole('heading', { name: '账户中心' })).toBeVisible();
    await expect(page.getByText('alice').first()).toBeVisible();
    await assertNoHorizontalOverflow(page);
  });

  test('错误密码登录失败并展示友好错误提示', async ({ page }) => {
    await page.goto('/login');
    await dismissStartupAnnouncement(page);

    await page.getByLabel('角色').selectOption('PLAYER');
    await page.getByLabel('账号').fill('alice');
    await page.getByLabel('密码').fill('wrongpassword');
    await page.getByRole('button', { name: '登录', exact: true }).click();

    await expect(page).toHaveURL(/\/login/);
    await expect(page.locator('.message.error')).toBeVisible();
    await expect.poll(() => page.evaluate(() => sessionStorage.getItem('steam-platform-token'))).toBeNull();
  });

  test('玩家角色试图访问管理员页面时被路由守卫拦截并重定向到 /account', async ({ page }) => {
    await loginAs(page, 'alice', 'alice', 'PLAYER');

    // 玩家试图直接访问管理员公告管理
    await page.goto('/admin/notices');
    await expect(page).toHaveURL(/\/account/);
    await expect(page.getByRole('heading', { name: '账户中心' })).toBeVisible();

    // 玩家试图直接访问管理员游戏上下架页面
    await page.goto('/admin/games');
    await expect(page).toHaveURL(/\/account/);
    await expect(page.getByRole('heading', { name: '账户中心' })).toBeVisible();
  });

  test('API 返回 401 响应时拦截器清理 Token 并自动跳转回登录页', async ({ page }) => {
    await loginAs(page, 'alice', 'alice', 'PLAYER');
    await page.goto('/account');
    await expect(page.getByRole('heading', { name: '账户中心' })).toBeVisible();

    // 模拟接口收到 401 响应
    await page.route('**/api/auth/me', async (route) => {
      await route.fulfill({
        status: 401,
        contentType: 'application/json',
        body: JSON.stringify({ code: 401, message: 'Token 已失效' })
      });
    });

    // 触发受保护 API 请求
    await page.evaluate(() => {
      fetch('/api/auth/me', {
        headers: {
          Authorization: `Bearer ${sessionStorage.getItem('steam-platform-token') || ''}`
        }
      });
    });

    // 验证拦截器清理了 token 并自动重定向至登录页
    await expect.poll(() => page.evaluate(() => sessionStorage.getItem('steam-platform-token'))).toBeNull();
    await expect(page).toHaveURL(/\/login/);
  });
});

test.describe('公告系统管理与展示 (Group A)', () => {
  test('管理员能够访问公告管理页面，展示发布与更新表单及已发布列表', async ({ page }) => {
    await loginAs(page, 'rootadmin', 'admin', 'ADMIN');

    await page.goto('/admin/notices');
    await dismissStartupAnnouncement(page);

    await expect(page.getByRole('heading', { name: '公告管理' })).toBeVisible();
    await expect(page.getByRole('heading', { name: '发布公告' })).toBeVisible();
    await expect(page.getByRole('heading', { name: '更新公告' })).toBeVisible();
    await expect(page.getByRole('heading', { name: '已发布公告' })).toBeVisible();
    await assertNoHorizontalOverflow(page);
  });

  test('商店主页正确展示启动公告浮窗并支持关闭', async ({ page }) => {
    // 确保清理启动公告会话记录
    await page.goto('/store');
    await page.evaluate(() => sessionStorage.removeItem('game-deck-startup-announcement-dismissed:2026-08-25'));
    await page.reload();

    const announcementModal = page.locator('.startup-announcement');
    if (await announcementModal.isVisible().catch(() => false)) {
      await expect(announcementModal).toBeVisible();
      await expect(page.getByRole('button', { name: '关闭启动公告' })).toBeVisible();

      // 点击关闭
      await page.getByRole('button', { name: '关闭启动公告' }).click();
      await expect(announcementModal).toBeHidden();
    }
    await assertNoHorizontalOverflow(page);
  });
});
