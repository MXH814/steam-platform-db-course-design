import { expect, test, type Browser, type Page } from '@playwright/test';
import { dismissStartupAnnouncement, loginAs, recordingPause } from './helpers';

test.describe.configure({ mode: 'serial' });

async function newPage(browser: Browser) {
  const context = await browser.newContext({
    baseURL: process.env.E2E_BASE_URL || 'http://127.0.0.1:4173',
    locale: 'zh-CN',
    timezoneId: 'Asia/Shanghai',
    colorScheme: 'dark',
    viewport: process.env.E2E_RECORD === '1' ? { width: 1920, height: 1080 } : { width: 1440, height: 900 },
    recordVideo: process.env.E2E_RECORD === '1'
      ? { dir: '../output/playwright/raw-video', size: { width: 1920, height: 1080 } }
      : undefined
  });
  return { context, page: await context.newPage() };
}

async function expectWalletBalance(page: Page, available: string, frozen = '¥ 0.00') {
  await page.goto('/wallet');
  const accountCard = page.locator('.account-card');
  await expect(accountCard.locator('.account-balance strong')).toHaveText(available);
  await expect(accountCard.locator('.account-balance-details div').filter({ hasText: '冻结余额' }).locator('dd')).toHaveText(frozen);
}

async function logout(page: Page) {
  await page.locator('.client-actions > button.account-link').click();
  await page.locator('.account-popover').getByRole('button', { name: '退出账户' }).click();
  await expect(page).toHaveURL(/\/login$/);
}

test('固定答辩链：注册、购买退款、库存挂单、撮合与账本', async ({ browser }) => {
  test.skip(process.env.E2E_MUTATING !== '1', '仅由带云端前后重置保护的回归命令执行。');

  const player = await newPage(browser);
  await test.step('商店启动公告覆盖在已渲染的首页上', async () => {
    await player.page.goto('/');
    await expect(player.page.getByText('精选与推荐')).toBeVisible();
    await expect(player.page.getByRole('dialog')).toBeVisible();
    await recordingPause(player.page, 1_100);
    await dismissStartupAnnouncement(player.page);
  });

  await test.step('注册临时玩家并确认 PLAYER 主体', async () => {
    await player.page.goto('/register');
    await player.page.getByLabel('账号').fill('defense_demo');
    await player.page.getByLabel('昵称').fill('答辩演示');
    await player.page.getByLabel('密码').fill('Demo123456');
    await player.page.getByRole('button', { name: '注册并登录' }).click();
    await expect(player.page).toHaveURL(/\/account$/);
    await expect(player.page.getByRole('heading', { name: '账户中心' })).toBeVisible();
    const accountDetails = player.page.locator('.details-panel');
    await expect(accountDetails.getByText('defense_demo', { exact: true })).toBeVisible();
    await expect(accountDetails.getByText('PLAYER', { exact: true })).toBeVisible();
    await recordingPause(player.page);
  });

  await test.step('CS2 免费入库不影响钱包', async () => {
    await player.page.goto('/games/GAME_CS2');
    await expect(player.page.getByLabel('Counter-Strike 2 媒体画廊')).toBeVisible();
    await player.page.getByRole('button', { name: '免费入库', exact: true }).click();
    await expect(player.page.getByText(/已免费入库/)).toBeVisible();
    await expect(player.page.getByRole('link', { name: '开始游戏' })).toBeVisible();
    await recordingPause(player.page);
    await expectWalletBalance(player.page, '¥ 0.00');
  });

  await test.step('微信模拟充值 60 元并生成资金流水', async () => {
    await player.page.goto('/wallet/recharge/checkout?amount=60');
    await expect(player.page.locator('.payment-select select')).toHaveValue('WECHAT_PAY');
    await expect(player.page.locator('.selected-method, .payment-notice').getByText('微信支付', { exact: true })).toBeVisible();
    await player.page.getByRole('button', { name: '继续' }).click();
    await expect(player.page.getByText(/充值成功，当前钱包余额 ¥ 60\.00/)).toBeVisible();
    await player.page.waitForURL(/\/wallet\/history$/);
    await expect(player.page.getByText('充值', { exact: true }).first()).toBeVisible();
    await recordingPause(player.page);
  });

  await test.step('以钱包购买 DST 并写入游戏库', async () => {
    await player.page.goto('/games/GAME_DST');
    await expect(player.page.getByLabel('饥荒联机版 媒体画廊')).toBeVisible();
    await player.page.getByRole('button', { name: '购买游戏' }).click();
    await expect(player.page).toHaveURL(/\/checkout\/game\/GAME_DST$/);
    await expect(player.page.getByText('¥ 24.00', { exact: true }).first()).toBeVisible();
    await player.page.getByRole('button', { name: '支付' }).click();
    await player.page.waitForURL(/\/wallet\/history$/);
    await expect(player.page.getByText(/饥荒联机版/).first()).toBeVisible();
    await expectWalletBalance(player.page, '¥ 36.00');
    await player.page.goto('/library');
    await expect(player.page.locator('.recent-game').filter({ hasText: 'Counter-Strike 2' })).toBeVisible();
    await expect(player.page.locator('.recent-game').filter({ hasText: '饥荒联机版' })).toBeVisible();
    await recordingPause(player.page);
  });

  await test.step('发表 DST 评测并解锁一个项目成就', async () => {
    await player.page.goto('/games/GAME_DST/community');
    await player.page.getByPlaceholder('告诉其他玩家这款游戏哪里值得推荐，或哪里需要谨慎。').fill('答辩自动回归：联机生存内容丰富，购买、评价与成就链路完整。');
    await player.page.getByRole('button', { name: '发表评测' }).click();
    await expect(player.page.getByText('评价已发表。')).toBeVisible();
    const unlock = player.page.getByRole('button', { name: '解锁', exact: true }).first();
    await expect(unlock).toBeEnabled();
    await unlock.click();
    await expect(player.page.getByText('成就已解锁。')).toBeVisible();
    await expect(player.page.getByText(/1\/6 已解锁/)).toBeVisible();
    await recordingPause(player.page);
  });

  await test.step('提交 DST 退款并由管理员审核通过', async () => {
    await player.page.goto('/refunds');
    const dstOption = player.page.locator('select option').filter({ hasText: '饥荒联机版' });
    const orderId = await dstOption.getAttribute('value');
    expect(orderId).toBeTruthy();
    await player.page.getByLabel('选择订单').selectOption(orderId!);
    await player.page.getByLabel('退款原因').fill('答辩自动回归退款，验证资金和游戏库权益回滚。');
    await player.page.getByRole('button', { name: '提交退款' }).click();
    await expect(player.page.getByText(/退款申请已提交/)).toBeVisible();
    await expect(player.page.getByText('PENDING', { exact: true })).toBeVisible();

    await logout(player.page);
    await loginAs(player.page, 'rootadmin', 'admin', 'ADMIN');
    await player.page.goto('/admin/refunds');
    const approve = player.page.locator('button.secondary-button:not([disabled])').filter({ hasText: '通过' }).first();
    await expect(approve).toBeEnabled();
    await approve.click();
    await expect(player.page.getByText(/已更新为 APPROVED/)).toBeVisible();

    await logout(player.page);
    await loginAs(player.page, 'defense_demo', 'Demo123456');
    await expectWalletBalance(player.page, '¥ 60.00');
    await player.page.goto('/library');
    await expect(player.page.locator('.recent-game').filter({ hasText: 'Counter-Strike 2' })).toBeVisible();
    await expect(player.page.locator('.recent-game').filter({ hasText: '饥荒联机版' })).toHaveCount(0);
    await recordingPause(player.page);
  });

  await test.step('Alice 将指定 CS2 饰品以 49 元挂单', async () => {
    await logout(player.page);
    await loginAs(player.page, 'alice', 'alice');
    await player.page.goto('/inventory?gameId=GAME_CS2');
    await player.page.getByPlaceholder('在库存物品中搜索').fill('ITEM_CS2_002');
    await player.page.locator('.inventory-tile').click();
    await expect(player.page.getByText('ITEM_CS2_002', { exact: true })).toBeVisible();
    await player.page.getByRole('button', { name: '出售', exact: true }).click();
    const sellDialog = player.page.getByRole('dialog', { name: /AK-47 \| Redline/ });
    await sellDialog.getByLabel('出售价格').fill('49');
    await sellDialog.getByRole('button', { name: '确认出售' }).click();
    await expect(player.page.getByText(/已为 AK-47 \| Redline 创建出售挂单/)).toBeVisible();
    await recordingPause(player.page);
  });

  await test.step('按价格优先撮合 Bob 买单并检查卖方账本', async () => {
    await player.page.goto('/market/trades');
    await player.page.getByRole('button', { name: '执行下一笔撮合' }).click();
    const matchMessage = player.page.locator('.market-message.success');
    await expect(matchMessage).toContainText('撮合完成');
    await expect(matchMessage).toContainText('AK-47 | Redline');
    await expect(matchMessage).toContainText('P001 → P002');
    await expect(matchMessage).toContainText('¥49.00');
    await expect(player.page.getByText(/ITEM_CS2_002/).first()).toBeVisible();
    await expectWalletBalance(player.page, '¥ 222.55');

    await player.page.goto('/market/transfers');
    await player.page.getByPlaceholder('输入物品编号').fill('ITEM_CS2_002');
    await player.page.getByRole('button', { name: '查询' }).click();
    await expect(player.page.getByText('P001 → P002', { exact: true })).toBeVisible();
    await recordingPause(player.page);
  });

  await test.step('Bob 获得饰品且 50 元冻结额释放并返还 1 元差额', async () => {
    await logout(player.page);
    await loginAs(player.page, 'bob', 'bob');
    await expectWalletBalance(player.page, '¥ 243.75', '¥ 0.00');
    await player.page.goto('/inventory?gameId=GAME_CS2');
    await player.page.getByPlaceholder('在库存物品中搜索').fill('ITEM_CS2_002');
    await expect(player.page.locator('.inventory-tile')).toHaveCount(1);
    await player.page.locator('.inventory-tile').click();
    await expect(player.page.getByText('ITEM_CS2_002', { exact: true })).toBeVisible();
    await recordingPause(player.page, 1_100);
  });
  await player.context.close();
});
