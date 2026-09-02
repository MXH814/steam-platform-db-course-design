import { expect, test } from '@playwright/test';
import { assertNoHorizontalOverflow, dismissStartupAnnouncement, loginAs } from './helpers';

test.describe('认证与权限体验 (Group A)', () => {
  test('未登录用户访问受保护路由重定向至登录页，登录成功后重定向回原页面', async ({ page }) => {
    await page.goto('/account');
    await dismissStartupAnnouncement(page);

    // 守卫拦截未登录用户并附加 redirect 查询参数（通过 URL.searchParams 校验，避免字符编码表现差异）
    await expect(page).toHaveURL(/\/login(?:\?|$)/);
    await expect.poll(() => new URL(page.url()).searchParams.get('redirect')).toBe('/account');
    await expect(page.locator('.message.notice')).toContainText('该页面需要登录后访问');

    // 执行玩家登录
    await page.getByLabel('角色').selectOption('PLAYER');
    await page.getByLabel('账号').fill('alice');
    await page.getByLabel('密码').fill('alice');
    await page.getByRole('button', { name: '登录', exact: true }).click();

    // 验证成功返回 /account 页面
    await expect(page).toHaveURL(/\/account/);
    await expect(page.getByRole('heading', { name: '账户中心' })).toBeVisible();
    await expect(page.locator('.details-panel')).toContainText('alice');
    await assertNoHorizontalOverflow(page);
  });

  test('常规登录与 redirect/expired 场景下错误密码均能清晰展示错误提示', async ({ page }) => {
    // 场景一：常规无参登录页输入错误密码
    await page.goto('/login');
    await dismissStartupAnnouncement(page);

    await page.getByLabel('角色').selectOption('PLAYER');
    await page.getByLabel('账号').fill('alice');
    await page.getByLabel('密码').fill('wrongpassword');
    await page.getByRole('button', { name: '登录', exact: true }).click();

    await expect(page).toHaveURL(/\/login/);
    await expect(page.locator('.message.error')).toBeVisible();
    await expect.poll(() => page.evaluate(() => sessionStorage.getItem('steam-platform-token'))).toBeNull();

    // 场景二：带有 redirect 与 expired 参数的登录页输入错误密码
    await page.goto('/login?redirect=/account&expired=1');
    await dismissStartupAnnouncement(page);

    // 此时顶部应展示登录失效提示
    await expect(page.locator('.message.notice')).toContainText('登录状态已失效');

    // 输入错误密码提交
    await page.getByLabel('角色').selectOption('PLAYER');
    await page.getByLabel('账号').fill('alice');
    await page.getByLabel('密码').fill('wrongpassword');
    await page.getByRole('button', { name: '登录', exact: true }).click();

    // 验证错误信息正确呈现，未被 sessionNotice 遮盖；同时提示依然保留
    await expect(page.locator('.message.error')).toBeVisible();
    await expect(page.locator('.message.notice')).toContainText('登录状态已失效');
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
    await assertNoHorizontalOverflow(page);
  });

  test('真实应用请求遇到 401 响应时 Axios 拦截器清理 Token 并自动跳转回登录页', async ({ page }) => {
    await loginAs(page, 'alice', 'alice', 'PLAYER');
    await dismissStartupAnnouncement(page);

    // 拦截真实应用调用的 /api/wallet 接口，返回 401
    await page.route('**/api/wallet', async (route) => {
      await route.fulfill({
        status: 401,
        contentType: 'application/json',
        body: JSON.stringify({ message: 'Token 已过期' })
      });
    });

    // 进入 /wallet 页面，WalletView 挂载时会通过 Axios http 实例调用 /api/wallet/account
    await page.goto('/wallet');

    // 验证 Axios 响应拦截器清理了 token，并重定向至登录页，带有 redirect 与 expired 参数
    await expect(page).toHaveURL(/\/login(?:\?|$)/);
    await expect.poll(() => page.evaluate(() => sessionStorage.getItem('steam-platform-token'))).toBeNull();
    await expect.poll(() => new URL(page.url()).searchParams.get('redirect')).toBe('/wallet');
    await expect.poll(() => new URL(page.url()).searchParams.get('expired')).toBe('1');
    await expect(page.locator('.message.notice')).toContainText('登录状态已失效');

    // 恢复正常接口，重新输入正确密码登录，验证原路返回至 /wallet
    await page.unroute('**/api/wallet');
    await page.getByLabel('角色').selectOption('PLAYER');
    await page.getByLabel('账号').fill('alice');
    await page.getByLabel('密码').fill('alice');
    await page.getByRole('button', { name: '登录', exact: true }).click();

    await expect(page).toHaveURL(/\/wallet/);
    await expect(page.getByRole('heading', { name: '为您的 Steam 钱包充值' })).toBeVisible();
    await assertNoHorizontalOverflow(page);
  });
});

test.describe('公告系统管理与展示 (Group A)', () => {
  test('管理员能够访问公告管理页面，展示发布与更新表单及已发布列表', async ({ page }) => {
    await loginAs(page, 'rootadmin', 'admin', 'ADMIN');

    await page.goto('/admin/notices');
    await dismissStartupAnnouncement(page);

    // 使用 exact: true 避免“发布公告”与“已发布公告”重复匹配
    await expect(page.getByRole('heading', { name: '公告管理', exact: true })).toBeVisible();
    await expect(page.getByRole('heading', { name: '发布公告', exact: true })).toBeVisible();
    await expect(page.getByRole('heading', { name: '更新公告', exact: true })).toBeVisible();
    await expect(page.getByRole('heading', { name: '已发布公告', exact: true })).toBeVisible();
    await assertNoHorizontalOverflow(page);
  });

  test('商店主页正确展示启动公告浮窗并支持关闭', async ({ page }) => {
    // 确保全新会话未关闭过公告
    await page.goto('/store');
    await page.evaluate(() => sessionStorage.removeItem('game-deck-startup-announcement-dismissed:2026-08-25'));
    await page.reload();

    const announcementModal = page.locator('.startup-announcement');
    // 无条件断言公告必须展示
    await expect(announcementModal).toBeVisible();
    await expect(page.getByRole('button', { name: '关闭启动公告' })).toBeVisible();

    // 点击关闭
    await page.getByRole('button', { name: '关闭启动公告' }).click();
    await expect(announcementModal).toBeHidden();

    // 验证 sessionStorage 记录已关闭
    const dismissed = await page.evaluate(() =>
      sessionStorage.getItem('game-deck-startup-announcement-dismissed:2026-08-25')
    );
    expect(dismissed).toBe('true');

    // 页面刷新后同会话内不再重复弹出
    await page.reload();
    await expect(announcementModal).toBeHidden();
    await assertNoHorizontalOverflow(page);
  });
});
