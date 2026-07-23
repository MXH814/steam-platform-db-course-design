export interface GameCatalogItem {
  gameId: string;
  title: string;
  shortName: string;
  appId: string;
  subtitle: string;
  storeLine: string;
  libraryLine: string;
  capsuleLabel: string;
  heroTone: string;
  heroImage: string;
  coverImage: string;
  headerImage: string;
  tags: string[];
  links: string[];
  updates: Array<{
    date: string;
    type: string;
    title: string;
    body: string;
  }>;
}

const fallbackGame: GameCatalogItem = {
  gameId: 'GAME_DST',
  title: "Don't Starve Together / 饥荒联机版",
  shortName: 'DST',
  appId: '322330',
  subtitle: '买断制联机生存、社区评价与自定义成就',
  storeLine: '共同探索、制作、战斗和生存。在一片奇异而危险的荒野中与好友建立营地。',
  libraryLine: '加入游戏库后，可以查看游玩记录、发表评价并解锁成就。',
  capsuleLabel: 'DST',
  heroTone: 'survival',
  heroImage: '/assets/games/dst-library-hero.jpg',
  coverImage: '/assets/games/dst-library-cover.jpg',
  headerImage: '/assets/games/dst-header.jpg',
  tags: ['多人联机', '生存', '合作', '冒险', '成就'],
  links: ['访问网站', '查看更新记录', '阅读相关新闻', '查看讨论'],
  updates: [
    {
      date: '7月8日',
      type: '社区更新',
      title: '社区评价与成就现已开放',
      body: '拥有游戏的玩家可以发表评价，并在游玩过程中逐步解锁成就。'
    },
    {
      date: '7月7日',
      type: '成就更新',
      title: '新增生存挑战成就',
      body: '完成指定的生存挑战，即可在游戏库中点亮对应成就。'
    }
  ]
};

export const gameCatalog: Record<string, GameCatalogItem> = {
  GAME_DST: fallbackGame,
  GAME_CS2: {
    gameId: 'GAME_CS2',
    title: 'Counter-Strike 2',
    shortName: 'CS2',
    appId: '730',
    subtitle: '免费开玩、饰品库存与社区市场',
    storeLine: '经典团队竞技全面焕新。免费加入游戏库，并在社区市场查看与交易饰品。',
    libraryLine: '从游戏库快速开始比赛，查看游玩时长、库存、市场与成就。',
    capsuleLabel: 'CS2',
    heroTone: 'tactical',
    heroImage: '/assets/games/cs2-library-hero.jpg',
    coverImage: '/assets/games/cs2-library-cover.jpg',
    headerImage: '/assets/games/cs2-header.jpg',
    tags: ['免费游玩', '射击', '饰品', '市场', '库存'],
    links: ['访问网站', '查看库存', '进入市场', '查看交易记录'],
    updates: [
      {
        date: '7月8日',
        type: '库存更新',
        title: 'CS2 饰品库存现已开放',
        body: '前往库存查看已拥有的饰品，或进入社区市场浏览挂单和成交记录。'
      }
    ]
  }
};

export function getGameMeta(gameId: string): GameCatalogItem {
  return gameCatalog[gameId] ?? { ...fallbackGame, gameId, shortName: gameId, title: gameId };
}
