import { getApiError, http } from './http';
import type {
  ApiEnvelope,
  FallbackAwarePagedResult,
  GameAchievementSummary,
  GameContentPackage,
  GameDetail,
  GameItemSummary,
  GameListItem,
  GameQuery,
  GameReviewSummary,
  PagedResult,
} from './types';

type LibraryEntry = {
  libId?: string;
  gameId: string;
  gameName?: string;
  acquireWay?: string;
  status?: string;
  playMinutes?: number;
  lastPlayTime?: string | null;
};

export interface GameOwnershipInfo {
  owned: boolean;
  entry: LibraryEntry | null;
}

type BackendGame = {
  gameId: string;
  gameName: string;
  developerId: string;
  developerName: string;
  basePrice: number;
  discountRate: number;
  finalPrice: number;
  releaseDate: string;
  reputation: string | null;
  status: string;
};

const demoGames: GameListItem[] = [
  {
    gameId: 'GAME_CS2',
    gameName: 'Counter-Strike 2',
    shortName: 'CS2',
    developerId: 'DEV_VALVE',
    developerName: 'Valve',
    coverTone: 'cs2',
    tags: ['免费开玩', '多人竞技', '饰品市场', '库存'],
    basePrice: 0,
    discountRate: 0,
    finalPrice: 0,
    releaseDate: '2023-09-27',
    reputation: '特别好评',
    status: 'ONLINE',
    supportsMarket: true,
    hasContentPackages: false,
    summary: '免费团队竞技射击游戏，用于展示 CS2 免费入库、库存、饰品市场和成交价格摘要。'
  },
  {
    gameId: 'GAME_DST',
    gameName: "Don't Starve Together / 饥荒联机版",
    shortName: 'DST',
    developerId: 'DEV_KLEI',
    developerName: 'Klei Entertainment',
    coverTone: 'dst',
    tags: ['生存', '联机合作', '买断制', '内容包'],
    basePrice: 24,
    discountRate: 0.5,
    finalPrice: 12,
    releaseDate: '2016-04-21',
    reputation: '好评如潮',
    status: 'ONLINE',
    supportsMarket: false,
    hasContentPackages: true,
    summary: '买断制联机生存游戏，用于展示 DST 购买入口、DLC/礼包、评价概览和成就概览。'
  }
];

const demoPackages: Record<string, GameContentPackage[]> = {
  GAME_DST: [
    {
      packageId: 'PKG_DST_DLC',
      gameId: 'GAME_DST',
      packageName: 'A New Reign Content Pack',
      packageType: 'DLC',
      basePrice: 18,
      discountRate: 0.25,
      finalPrice: 13.5,
      imageUrl: null,
      sourceKind: 'DEMO'
    },
    {
      packageId: 'PKG_DST_CHEST',
      gameId: 'GAME_DST',
      packageName: 'Forest Survivors Skin Chest',
      packageType: '皮肤箱',
      basePrice: 12,
      discountRate: 0,
      finalPrice: 12,
      imageUrl: null,
      sourceKind: 'DEMO'
    },
    {
      packageId: 'PKG_DST_BUNDLE',
      gameId: 'GAME_DST',
      packageName: 'Survival Starter Bundle',
      packageType: '礼包',
      basePrice: 36,
      discountRate: 0.35,
      finalPrice: 23.4,
      imageUrl: null,
      sourceKind: 'DEMO'
    }
  ]
};

const demoItemSummary: Record<string, GameItemSummary> = {
  GAME_CS2: {
    gameId: 'GAME_CS2',
    templateCount: 8,
    inventoryItemCount: 24,
    activeBuyOrderCount: 7,
    activeSellOrderCount: 9,
    tradeCount: 18,
    highestBuyPrice: 76.5,
    lowestSellPrice: 79.9,
    lastTradePrice: 78,
    items: [
      {
        templateId: 'ITPL_CS2_AK_REDLINE',
        itemName: 'AK-47 | Redline',
        rarity: 'Classified',
        imageUrl: null,
        inventoryItemCount: 5,
        activeBuyOrderCount: 2,
        activeSellOrderCount: 3,
        tradeCount: 8,
        highestBuyPrice: 76.5,
        lowestSellPrice: 79.9,
        lastTradePrice: 78
      }
    ]
  }
};

const demoReviewSummary: Record<string, GameReviewSummary> = {
  GAME_CS2: {
    reviewCount: 120000,
    recommendCount: 104400,
    recommendRate: 87,
    latestReviewContent: '适合展示库存、饰品市场入口和玩家评价概览。',
    ratingText: '特别好评'
  },
  GAME_DST: {
    reviewCount: 98000,
    recommendCount: 93100,
    recommendRate: 95,
    latestReviewContent: '适合展示买断制购买、内容包、评价和成就。',
    ratingText: '好评如潮'
  }
};

const demoAchievementSummary: Record<string, GameAchievementSummary> = {
  GAME_CS2: {
    achievementCount: 3,
    averageGlobalRate: 42,
    achievements: [
      {
        achievementId: 'ACH_CS2_FIRST_MATCH',
        achievementName: '完成第一场比赛',
        description: '完成一次 CS2 演示对局。',
        globalRate: 68
      },
      {
        achievementId: 'ACH_CS2_FIRST_DROP',
        achievementName: '首次掉落',
        description: '获得一件饰品掉落。',
        globalRate: 31
      }
    ]
  },
  GAME_DST: {
    achievementCount: 4,
    averageGlobalRate: 36,
    achievements: [
      {
        achievementId: 'ACH_DST_FIRE',
        achievementName: '第一簇营火',
        description: '在夜晚到来前点燃营火。',
        globalRate: 54
      },
      {
        achievementId: 'ACH_DST_WINTER',
        achievementName: '越冬者',
        description: '在演示存档中度过冬季。',
        globalRate: 18
      }
    ]
  }
};

function unwrap<T>(response: ApiEnvelope<T>): T {
  if (response.code !== 0) {
    throw new Error(response.message || '请求失败');
  }
  return response.data;
}

function isRecoverableNetworkError(error: unknown): boolean {
  const message = getApiError(error).toLowerCase();
  return (
    message.includes('network') ||
    message.includes('timeout') ||
    message.includes('failed') ||
    message.includes('econnrefused') ||
    message.includes('server is not configured') ||
    message.includes('request failed with status code 500')
  );
}

function toNumber(value: number | string | null | undefined): number {
  if (value === null || value === undefined || value === '') return 0;
  return Number(value);
}

function normalizeGame(raw: BackendGame | GameListItem): GameListItem {
  if ('shortName' in raw) return raw;

  const gameId = raw.gameId;
  const name = raw.gameName;
  const lowerName = name.toLowerCase();
  const isCs2 = gameId === 'GAME_CS2' || lowerName.includes('counter-strike') || lowerName.includes('cs2');
  const isDst = gameId === 'GAME_DST' || lowerName.includes("don't starve") || name.includes('饥荒');

  return {
    gameId,
    gameName: name,
    shortName: isCs2 ? 'CS2' : isDst ? 'DST' : name,
    developerId: raw.developerId,
    developerName: raw.developerName,
    coverTone: isCs2 ? 'cs2' : isDst ? 'dst' : 'background',
    tags: isCs2 ? ['免费开玩', '多人竞技', '饰品市场', '库存'] : isDst ? ['生存', '联机合作', '买断制', '内容包'] : ['商店游戏'],
    basePrice: toNumber(raw.basePrice),
    discountRate: toNumber(raw.discountRate),
    finalPrice: toNumber(raw.finalPrice),
    releaseDate: raw.releaseDate,
    reputation: raw.reputation || '暂无评价',
    status: raw.status,
    supportsMarket: isCs2,
    hasContentPackages: isDst,
    summary: isCs2
      ? '免费团队竞技射击游戏，用于展示 CS2 免费入库、库存和饰品市场。'
      : isDst
        ? '买断制联机生存游戏，用于展示 DST 购买、内容包、评价和成就。'
        : '课程设计商店游戏数据。'
  };
}

function normalizeDetail(raw: BackendGame | GameListItem): GameDetail {
  const game = normalizeGame(raw);
  return {
    ...game,
    description:
      game.shortName === 'CS2'
        ? 'Counter-Strike 2 在本系统中用于展示免费入库、玩家库存、饰品市场、挂单成交和价格摘要。详情页只保留跳转入口，具体库存和市场业务由 Group D 页面承接。'
        : game.shortName === 'DST'
          ? "Don't Starve Together / 饥荒联机版在本系统中用于展示买断制购买、内容包、公告、社区评价和成就。真实扣款和订单由 Group C 承接。"
          : game.summary
  };
}

function applyLocalFilters(items: GameListItem[], query?: GameQuery): GameListItem[] {
  let result = [...items];
  const keyword = (query?.keyword || query?.search || '').trim().toLowerCase();

  if (keyword) {
    result = result.filter((game) =>
      [game.gameName, game.shortName, game.developerName, ...game.tags].some((text) => text.toLowerCase().includes(keyword))
    );
  }

  if (query?.tag) {
    result = result.filter((game) => game.tags.includes(query.tag || ''));
  }

  switch (query?.priceFilter) {
    case 'free':
      result = result.filter((game) => game.finalPrice <= 0);
      break;
    case 'paid':
      result = result.filter((game) => game.finalPrice > 0);
      break;
    case 'discount':
      result = result.filter((game) => game.discountRate > 0);
      break;
    case 'market':
      result = result.filter((game) => game.supportsMarket);
      break;
    case 'packages':
      result = result.filter((game) => game.hasContentPackages);
      break;
  }

  switch (query?.sort) {
    case 'price':
      result.sort((a, b) => a.finalPrice - b.finalPrice);
      break;
    case 'releaseDate':
      result.sort((a, b) => Date.parse(b.releaseDate) - Date.parse(a.releaseDate));
      break;
    case 'reputation':
      result.sort((a, b) => b.reputation.localeCompare(a.reputation, 'zh-CN'));
      break;
  }

  return result;
}

function hasLocalFilter(query?: GameQuery): boolean {
  return Boolean(
    (query?.keyword || query?.search || '').trim() ||
    (query?.priceFilter && query.priceFilter !== 'all') ||
    query?.tag
  );
}

export async function getGames(query?: GameQuery): Promise<FallbackAwarePagedResult<GameListItem>> {
  try {
    const response = await http.get<ApiEnvelope<PagedResult<BackendGame>>>('/api/games', {
      params: {
        status: query?.status,
        page: query?.page || 1,
        pageSize: query?.pageSize || 50
      }
    });
    const page = unwrap(response.data);
    const items = applyLocalFilters(page.items.map(normalizeGame), query);
    const demoMatches = applyLocalFilters(demoGames, query);
    if (!items.length && hasLocalFilter(query) && demoMatches.length) {
      return {
        ...page,
        items: demoMatches,
        total: demoMatches.length,
        source: 'fallback',
        warning: '后端当前列表没有匹配结果，已使用前端内置演示数据补足搜索展示。'
      };
    }

    return {
      ...page,
      items,
      source: 'api'
    };
  } catch (error) {
    if (!isRecoverableNetworkError(error)) throw new Error(getApiError(error));
    const items = applyLocalFilters(demoGames, query);
    return {
      items,
      page: query?.page || 1,
      pageSize: query?.pageSize || 50,
      total: items.length,
      source: 'fallback',
      warning: '后端暂时不可用，当前显示前端内置演示数据。连接恢复后会自动更新。'
    };
  }
}

export async function getGameDetail(gameId: string): Promise<GameDetail> {
  try {
    const response = await http.get<ApiEnvelope<BackendGame>>(`/api/games/${encodeURIComponent(gameId)}`);
    return normalizeDetail(unwrap(response.data));
  } catch (error) {
    if (!isRecoverableNetworkError(error)) throw new Error(getApiError(error));
    const game = demoGames.find((item) => item.gameId === gameId);
    if (!game) throw new Error('游戏不存在');
    return normalizeDetail(game);
  }
}

export async function getGameContentPackages(gameId: string): Promise<GameContentPackage[]> {
  try {
    const response = await http.get<ApiEnvelope<GameContentPackage[]>>(`/api/games/${encodeURIComponent(gameId)}/content-packages`);
    return unwrap(response.data);
  } catch (error) {
    if (!isRecoverableNetworkError(error)) throw new Error(getApiError(error));
    return demoPackages[gameId] || [];
  }
}

export async function getGameItemSummary(gameId: string): Promise<GameItemSummary> {
  try {
    const response = await http.get<ApiEnvelope<GameItemSummary>>(`/api/games/${encodeURIComponent(gameId)}/items/summary`);
    return unwrap(response.data);
  } catch (error) {
    if (!isRecoverableNetworkError(error)) throw new Error(getApiError(error));
    return (
      demoItemSummary[gameId] || {
        gameId,
        templateCount: 0,
        inventoryItemCount: 0,
        activeBuyOrderCount: 0,
        activeSellOrderCount: 0,
        tradeCount: 0,
        highestBuyPrice: null,
        lowestSellPrice: null,
        lastTradePrice: null,
        items: []
      }
    );
  }
}

export async function getGameReviewSummary(gameId: string): Promise<GameReviewSummary> {
  try {
    const response = await http.get<ApiEnvelope<Omit<GameReviewSummary, 'ratingText'>>>(`/api/games/${encodeURIComponent(gameId)}/reviews/summary`);
    const summary = unwrap(response.data);
    return {
      ...summary,
      ratingText: summary.recommendRate >= 90 ? '好评如潮' : summary.recommendRate >= 80 ? '特别好评' : '多半好评'
    };
  } catch (error) {
    if (!isRecoverableNetworkError(error)) throw new Error(getApiError(error));
    return demoReviewSummary[gameId] || { reviewCount: 0, recommendCount: 0, recommendRate: 0, latestReviewContent: null, ratingText: '暂无评价' };
  }
}

export async function getGameAchievementSummary(gameId: string): Promise<GameAchievementSummary> {
  try {
    const response = await http.get<ApiEnvelope<GameAchievementSummary>>(`/api/games/${encodeURIComponent(gameId)}/achievements/summary`);
    return unwrap(response.data);
  } catch (error) {
    if (!isRecoverableNetworkError(error)) throw new Error(getApiError(error));
    return demoAchievementSummary[gameId] || { achievementCount: 0, averageGlobalRate: null, achievements: [] };
  }
}

export async function getGameOwnership(gameId: string): Promise<GameOwnershipInfo> {
  try {
    const response = await http.get<LibraryEntry[]>('/api/library');
    const entry = response.data.find((item) => item.gameId === gameId && item.status !== 'REMOVED') || null;
    return {
      owned: Boolean(entry),
      entry
    };
  } catch (error) {
    throw new Error(getApiError(error));
  }
}
