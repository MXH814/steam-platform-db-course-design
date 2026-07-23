import type { AchievementListItem } from '../api/types';

export interface AchievementDisplayItem extends AchievementListItem {
  iconUrl: string;
}

const defaultIcon = '/assets/achievements/default-medal.svg';

const achievementIcons: Record<string, string> = {
  ACH_DST_SURVIVE_001: '/assets/achievements/dst-first-night.svg',
  ACH_DST_SCIENCE_MACHINE: '/assets/achievements/dst-science-machine.svg',
  ACH_DST_WINTER_SURVIVOR: '/assets/achievements/dst-winter.svg',
  ACH_DST_RUINS_DIVER: '/assets/achievements/dst-ruins.svg',
  ACH_DST_SHADOW_DUEL: '/assets/achievements/dst-shadow.svg',
  ACH_DST_CELESTIAL_CARTOGRAPHER: '/assets/achievements/dst-celestial.svg',
  ACH_CS2_FIRST_ROUND: '/assets/achievements/cs2-first-round.svg',
  ACH_CS2_ACE: '/assets/achievements/cs2-ace.svg',
  ACH_CS2_BOMB_PLANT: '/assets/achievements/cs2-bomb.svg',
  ACH_CS2_DEFUSE: '/assets/achievements/cs2-defuse.svg',
  ACH_CS2_MARKET_MAKER: '/assets/achievements/cs2-market.svg'
};

export function getAchievementIcon(achievement: Pick<AchievementListItem, 'achId'>): string {
  return achievementIcons[achievement.achId] ?? defaultIcon;
}

export function withAchievementIcons(apiRows: AchievementListItem[]): AchievementDisplayItem[] {
  return apiRows.map((row) => ({
    ...row,
    iconUrl: getAchievementIcon(row)
  }));
}
