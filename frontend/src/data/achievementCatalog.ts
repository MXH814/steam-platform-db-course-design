import type { AchievementListItem } from '../api/types';

export interface AchievementMeta {
  achId: string;
  gameId: string;
  achName: string;
  description: string;
  globalRate: number;
  iconUrl: string;
  accent: string;
}

export interface AchievementDisplayItem extends AchievementListItem {
  iconUrl: string;
  accent: string;
  source: 'api' | 'catalog';
}

const defaultIcon = '/assets/achievements/default-medal.svg';

const achievementCatalog: Record<string, AchievementMeta[]> = {
  GAME_DST: [
    {
      achId: 'ACH_DST_SURVIVE_001',
      gameId: 'GAME_DST',
      achName: 'First Night Together',
      description: '与另一名玩家一起熬过第一个夜晚。',
      globalRate: 42.5,
      iconUrl: '/assets/achievements/dst-first-night.svg',
      accent: '#d8a447'
    },
    {
      achId: 'ACH_DST_SCIENCE_MACHINE',
      gameId: 'GAME_DST',
      achName: '科学启动',
      description: '建造科学机器，解锁第一批生存配方。',
      globalRate: 37.8,
      iconUrl: '/assets/achievements/dst-science-machine.svg',
      accent: '#66c0f4'
    },
    {
      achId: 'ACH_DST_WINTER_SURVIVOR',
      gameId: 'GAME_DST',
      achName: '越冬者',
      description: '在寒冬中保持营地和队友存活。',
      globalRate: 18.4,
      iconUrl: '/assets/achievements/dst-winter.svg',
      accent: '#8fd7ff'
    },
    {
      achId: 'ACH_DST_RUINS_DIVER',
      gameId: 'GAME_DST',
      achName: '遗迹探灯',
      description: '深入地下遗迹并带回远古科技。',
      globalRate: 12.1,
      iconUrl: '/assets/achievements/dst-ruins.svg',
      accent: '#b68bff'
    },
    {
      achId: 'ACH_DST_SHADOW_DUEL',
      gameId: 'GAME_DST',
      achName: '暗影对峙',
      description: '击败一名暗影生物并稳定理智值。',
      globalRate: 21.6,
      iconUrl: '/assets/achievements/dst-shadow.svg',
      accent: '#c66cff'
    },
    {
      achId: 'ACH_DST_CELESTIAL_CARTOGRAPHER',
      gameId: 'GAME_DST',
      achName: '月岛制图师',
      description: '发现月岛并完成一次月亮科技采集。',
      globalRate: 9.7,
      iconUrl: '/assets/achievements/dst-celestial.svg',
      accent: '#9de7d7'
    }
  ],
  GAME_CS2: [
    {
      achId: 'ACH_CS2_FIRST_ROUND',
      gameId: 'GAME_CS2',
      achName: '手枪局开门红',
      description: '赢下本场对局的第一个手枪局。',
      globalRate: 55.2,
      iconUrl: '/assets/achievements/cs2-first-round.svg',
      accent: '#f4b44d'
    },
    {
      achId: 'ACH_CS2_ACE',
      gameId: 'GAME_CS2',
      achName: '一人五杀',
      description: '在一个回合中击败敌方全部五名玩家。',
      globalRate: 8.6,
      iconUrl: '/assets/achievements/cs2-ace.svg',
      accent: '#ff6b5d'
    },
    {
      achId: 'ACH_CS2_BOMB_PLANT',
      gameId: 'GAME_CS2',
      achName: '准时安装',
      description: '作为进攻方成功安放 C4。',
      globalRate: 44.3,
      iconUrl: '/assets/achievements/cs2-bomb.svg',
      accent: '#e1c15d'
    },
    {
      achId: 'ACH_CS2_DEFUSE',
      gameId: 'GAME_CS2',
      achName: '拆弹专家',
      description: '在关键回合成功拆除炸弹。',
      globalRate: 31.4,
      iconUrl: '/assets/achievements/cs2-defuse.svg',
      accent: '#66c0f4'
    },
    {
      achId: 'ACH_CS2_MARKET_MAKER',
      gameId: 'GAME_CS2',
      achName: '市场新秀',
      description: '完成一次饰品市场交易记录。',
      globalRate: 16.9,
      iconUrl: '/assets/achievements/cs2-market.svg',
      accent: '#a4d007'
    }
  ]
};

export function getAchievementCatalog(gameId: string): AchievementMeta[] {
  return achievementCatalog[gameId] ?? [];
}

export function getAchievementIcon(achievement: Pick<AchievementListItem, 'achId' | 'gameId' | 'achName'>): string {
  const match = getAchievementCatalog(achievement.gameId).find(
    (item) => item.achId === achievement.achId || item.achName === achievement.achName
  );
  return match?.iconUrl ?? defaultIcon;
}

export function mergeAchievementCatalog(gameId: string, apiRows: AchievementListItem[]): AchievementDisplayItem[] {
  const catalog = getAchievementCatalog(gameId);
  const matchedApiIds = new Set<string>();

  const catalogRows = catalog.map<AchievementDisplayItem>((meta) => {
    const apiRow = apiRows.find((row) => row.achId === meta.achId || row.achName === meta.achName);
    if (apiRow) {
      matchedApiIds.add(apiRow.achId);
    }

    return {
      achId: apiRow?.achId ?? meta.achId,
      gameId,
      achName: apiRow?.achName ?? meta.achName,
      description: apiRow?.description ?? meta.description,
      globalRate: apiRow?.globalRate ?? meta.globalRate,
      isUnlocked: apiRow?.isUnlocked ?? false,
      unlockTime: apiRow?.unlockTime ?? null,
      iconUrl: meta.iconUrl,
      accent: meta.accent,
      source: apiRow ? 'api' : 'catalog'
    };
  });

  const extraApiRows = apiRows
    .filter((row) => !matchedApiIds.has(row.achId))
    .map<AchievementDisplayItem>((row) => ({
      ...row,
      iconUrl: getAchievementIcon(row),
      accent: '#66c0f4',
      source: 'api'
    }));

  return [...catalogRows, ...extraApiRows];
}
