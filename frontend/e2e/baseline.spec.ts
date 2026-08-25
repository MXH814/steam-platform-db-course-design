import { expect, test } from '@playwright/test';
import { assertNoHorizontalOverflow, loginAs } from './helpers';

test.describe('固定演示基线', () => {
  test('Alice 可以访问库、库存、个人资料、社区和交易报价', async ({ page }) => {
    await loginAs(page, 'alice', 'alice');

    await page.goto('/library');
    await expect(page.locator('.recent-game').filter({ hasText: 'Counter-Strike 2' })).toBeVisible();
    await expect(page.locator('.recent-game').filter({ hasText: '饥荒联机版' })).toBeVisible();

    await page.goto('/inventory?gameId=GAME_CS2');
    await expect(page.getByRole('heading', { name: 'alice 的库存' })).toBeVisible();
    await page.getByPlaceholder('在库存物品中搜索').fill('ITEM_CS2_002');
    await page.locator('.inventory-tile').click();
    await expect(page.getByText('ITEM_CS2_002', { exact: true })).toBeVisible();

    await page.goto('/profile');
    await expect(page.getByRole('heading', { name: /Alice|alice/i }).first()).toBeVisible();
    await expect(page.getByText(/徽章/).first()).toBeVisible();

    await page.goto('/community');
    await expect(page.getByRole('heading', { name: /社区/ }).first()).toBeVisible();
    await expect(page.getByText(/讨论/).first()).toBeVisible();

    await page.goto('/trade-offers');
    await expect(page.getByRole('heading', { name: /交易报价/ })).toBeVisible();
    await assertNoHorizontalOverflow(page);
  });

  test('Bob 的钱包冻结额与市场基线可见', async ({ page }) => {
    await loginAs(page, 'bob', 'bob');
    await page.goto('/wallet');
    await expect(page.getByText(/当前钱包余额/).first()).toBeVisible();
    await expect(page.getByText(/冻结余额/).first()).toBeVisible();

    await page.goto('/market/orders');
    await expect(page.getByText(/买单|BUY/).first()).toBeVisible();
    await assertNoHorizontalOverflow(page);
  });
});
