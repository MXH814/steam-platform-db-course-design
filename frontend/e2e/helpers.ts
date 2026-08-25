import { expect, type Page } from '@playwright/test';

export type LoginRole = 'PLAYER' | 'ADMIN' | 'DEVELOPER';

export async function dismissStartupAnnouncement(page: Page) {
  const closeButton = page.getByRole('button', { name: '关闭启动公告' });
  if (await closeButton.isVisible().catch(() => false)) {
    await closeButton.click();
    await expect(closeButton).toBeHidden();
  }
}

export async function loginAs(page: Page, account: string, password: string, role: LoginRole = 'PLAYER') {
  await page.goto('/login');
  await dismissStartupAnnouncement(page);
  await page.getByLabel('角色').selectOption(role);
  await page.getByLabel('账号').fill(account);
  await page.getByLabel('密码').fill(password);
  await page.getByRole('button', { name: '登录', exact: true }).click();
  await expect(page).not.toHaveURL(/\/login(?:\?|$)/);
  await expect.poll(() => page.evaluate(() => Boolean(localStorage.getItem('steam-platform-token')))).toBeTruthy();
}

export async function assertNoHorizontalOverflow(page: Page) {
  const overflow = await page.evaluate(() => ({
    viewport: document.documentElement.clientWidth,
    content: document.documentElement.scrollWidth
  }));
  expect(overflow.content, `页面宽度 ${overflow.content}px 超出视口 ${overflow.viewport}px`).toBeLessThanOrEqual(overflow.viewport + 1);
}

export async function recordingPause(page: Page, milliseconds = 700) {
  if (process.env.E2E_RECORD === '1') {
    await page.waitForTimeout(milliseconds);
  }
}
