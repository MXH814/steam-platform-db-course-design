import { expect, test, type Browser, type BrowserContext, type Page } from '@playwright/test';
import { dismissStartupAnnouncement, loginAs } from './helpers';

test.describe.configure({ mode: 'serial' });
test.setTimeout(180_000);

async function openPlayerPage(browser: Browser) {
  const context = await browser.newContext({
    baseURL: process.env.E2E_BASE_URL || 'http://127.0.0.1:4173',
    locale: 'zh-CN',
    timezoneId: 'Asia/Shanghai',
    colorScheme: 'dark',
    viewport: { width: 1440, height: 900 }
  });
  return { context, page: await context.newPage() };
}

async function closeContext(context: BrowserContext) {
  await context.close().catch(() => undefined);
}

async function openFriendsDrawer(page: Page) {
  await page.getByRole('button', { name: '好友与聊天', exact: true }).click();
  const drawer = page.getByRole('complementary', { name: '好友与聊天' });
  await expect(drawer).toBeVisible();
  return drawer;
}

test('社交社区闭环：好友、实时聊天、资料、动态、讨论、互动、工坊与交易', async ({ browser }) => {
  test.skip(process.env.E2E_MUTATING !== '1', '仅由带云端前后重置保护的回归命令执行。');

  const social = await openPlayerPage(browser);
  const alice = await openPlayerPage(browser);
  const bob = await openPlayerPage(browser);

  try {
    await test.step('注册临时玩家并持久化个人资料装扮', async () => {
      await social.page.goto('/register');
      await social.page.getByLabel('账号').fill('social_demo');
      await social.page.getByLabel('昵称').fill('社交回归玩家');
      await social.page.getByLabel('密码').fill('Demo123456');
      await social.page.getByRole('button', { name: '注册并登录' }).click();
      await expect(social.page).toHaveURL(/\/account$/);

      await social.page.goto('/profile');
      await social.page.getByRole('button', { name: '编辑个人资料' }).click();
      await social.page.getByLabel('个人签名').fill('SignalR 与 Oracle 社交闭环验证');
      await social.page.getByLabel('个人简介').fill('这是一条由 Playwright 写入并在刷新后重新读取的答辩回归资料。');
      await social.page.getByRole('button', { name: '生存绿' }).click();
      await social.page.getByLabel('展示游戏').selectOption('GAME_DST');
      await social.page.getByRole('button', { name: '保存更改' }).click();
      await expect(social.page.locator('.profile-page')).toHaveClass(/survival-green/);
      await social.page.reload();
      await expect(social.page.getByText('SignalR 与 Oracle 社交闭环验证')).toBeVisible();
      await expect(social.page.getByText('这是一条由 Playwright 写入并在刷新后重新读取的答辩回归资料。')).toBeVisible();
    });

    await test.step('发送并接受好友请求', async () => {
      await social.page.goto('/community?tab=people');
      await social.page.getByPlaceholder('搜索玩家').fill('alice');
      const aliceResult = social.page.locator('.player-grid article').filter({ hasText: 'Alice' }).first();
      await expect(aliceResult).toBeVisible();
      await aliceResult.getByRole('button', { name: '添加好友' }).click();
      await expect(social.page.getByText('好友请求已发送。')).toBeVisible();

      await loginAs(alice.page, 'alice', 'alice');
      await alice.page.goto('/community?tab=people');
      const incoming = alice.page.locator('.request-list article').filter({ hasText: '社交回归玩家' });
      await expect(incoming).toBeVisible();
      await incoming.getByRole('button').click();
      await expect(alice.page.getByText('好友请求已接受。')).toBeVisible();

      await social.page.reload();
      await expect(social.page.locator('.player-grid article').filter({ hasText: 'Alice' })).toContainText('已经是好友');
    });

    await test.step('双方聊天抽屉通过 SignalR 实时收发并可刷新读取', async () => {
      const aliceDrawer = await openFriendsDrawer(alice.page);
      const aliceSocialRow = aliceDrawer.locator('.friend-row').filter({ hasText: '社交回归玩家' });
      await expect(aliceSocialRow).toBeVisible();
      await aliceSocialRow.locator('button').first().click();

      const socialDrawer = await openFriendsDrawer(social.page);
      const socialAliceRow = socialDrawer.locator('.friend-row').filter({ hasText: 'Alice' });
      await expect(socialAliceRow).toBeVisible();
      await socialAliceRow.locator('button').first().click();
      await socialDrawer.getByPlaceholder('发送消息').fill('社交回归实时消息：Oracle 已持久化');
      await socialDrawer.getByRole('button', { name: '发送消息' }).click();

      await expect(alice.page.locator('.client-toast')).toContainText('社交回归玩家 发来一条新消息');
      await expect(aliceDrawer.locator('.chat-log')).toContainText('社交回归实时消息：Oracle 已持久化');
      await alice.page.getByRole('button', { name: '关闭好友与聊天' }).click();
      await social.page.getByRole('button', { name: '关闭好友与聊天' }).click();

      await social.page.reload();
      const reopened = await openFriendsDrawer(social.page);
      await reopened.locator('.friend-row').filter({ hasText: 'Alice' }).locator('button').first().click();
      await expect(reopened.locator('.chat-log')).toContainText('社交回归实时消息：Oracle 已持久化');
      await social.page.getByRole('button', { name: '关闭好友与聊天' }).click();
    });

    let topicUrl = '';
    await test.step('发布社区动态和 DST 讨论主题', async () => {
      await social.page.goto('/community');
      await social.page.getByPlaceholder('与社区分享游戏动态...').fill('社交回归动态：前端、C# 五层与 Oracle 已贯通。');
      await social.page.getByRole('button', { name: '发布', exact: true }).click();
      await expect(social.page.getByText('社交回归动态：前端、C# 五层与 Oracle 已贯通。')).toBeVisible();

      await social.page.getByRole('button', { name: '讨论', exact: true }).click();
      await social.page.getByRole('button', { name: '饥荒联机版', exact: true }).click();
      await social.page.getByRole('button', { name: '发起新讨论' }).click();
      await social.page.getByPlaceholder('讨论标题').fill('答辩社交回归主题');
      await social.page.getByPlaceholder('详细描述你的问题或观点').fill('验证讨论主题、回复通知和 Oracle 持久化。');
      await social.page.getByRole('button', { name: '发布讨论' }).click();
      await expect(social.page.getByRole('heading', { name: '答辩社交回归主题' })).toBeVisible();
      topicUrl = social.page.url();
    });

    await test.step('好友回复讨论并由作者实时收到通知', async () => {
      await alice.page.goto('/community?tab=discussions');
      await alice.page.getByRole('button', { name: '饥荒联机版', exact: true }).click();
      await alice.page.locator('.topic-list > button').filter({ hasText: '答辩社交回归主题' }).click();
      await alice.page.getByPlaceholder('回复这个主题').fill('Alice 的实时回复已进入 Oracle。');
      await alice.page.getByRole('button', { name: '回复', exact: true }).click();
      await expect(alice.page.getByText('Alice 的实时回复已进入 Oracle。')).toBeVisible();
      await expect(social.page.locator('.client-toast')).toContainText('答辩社交回归主题');

      await social.page.goto(topicUrl);
      await expect(social.page.getByText('Alice 的实时回复已进入 Oracle。')).toBeVisible();
    });

    await test.step('Alice 切换展示徽章并在刷新后保持', async () => {
      await alice.page.goto('/profile');
      const badge = alice.page.locator('.badge-card').filter({ hasText: '平台先行者' });
      await badge.click();
      await expect(badge).toHaveClass(/featured/);
      await alice.page.reload();
      await expect(alice.page.locator('.badge-card').filter({ hasText: '平台先行者' })).toHaveClass(/featured/);
    });

    await test.step('Bob 的评测互动和工坊订阅均持久化', async () => {
      await loginAs(bob.page, 'bob', 'bob');
      await bob.page.goto('/games/GAME_DST/community');
      const review = bob.page.locator('.steam-review-card').filter({ hasText: 'Alice' }).first();
      const funny = review.getByRole('button', { name: /^欢乐 \d+$/ });
      await funny.click();
      await expect(funny).toHaveClass(/active/);
      await bob.page.reload();
      await expect(bob.page.locator('.steam-review-card').filter({ hasText: 'Alice' }).first().getByRole('button', { name: /^欢乐 \d+$/ })).toHaveClass(/active/);

      await bob.page.goto('/games/GAME_DST/community?section=workshop');
      const workshop = bob.page.locator('.workshop-card').filter({ hasText: '自动整理箱' });
      await workshop.getByRole('button', { name: '订阅', exact: true }).click();
      await expect(workshop.getByRole('button', { name: '取消订阅', exact: true })).toBeVisible();
      await bob.page.reload();
      await expect(bob.page.locator('.workshop-card').filter({ hasText: '自动整理箱' }).getByRole('button', { name: '取消订阅', exact: true })).toBeVisible();
    });

    await test.step('Bob 接受固定交易报价并写入完成状态', async () => {
      await bob.page.goto('/trade-offers');
      const offer = bob.page.locator('.offer-card').filter({ hasText: 'TO_DEMO_001' });
      await offer.getByRole('button', { name: '接受报价' }).click();
      await expect(offer.locator('.offer-status')).toHaveText('已完成');
      await bob.page.reload();
      await expect(bob.page.locator('.offer-card').filter({ hasText: 'TO_DEMO_001' }).locator('.offer-status')).toHaveText('已完成');
      await expect(alice.page.locator('.client-toast')).toContainText('交易报价状态已更新为ACCEPTED');
    });
  } finally {
    await Promise.all([closeContext(social.context), closeContext(alice.context), closeContext(bob.context)]);
  }
});
