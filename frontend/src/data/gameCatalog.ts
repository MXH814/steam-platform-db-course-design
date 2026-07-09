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
  subtitle: '买断制联机生存、社区评价与课程自定义成就样板',
  storeLine: '共同探索、制作、战斗和生存。这里展示课程项目中的商店、评价与成就链路。',
  libraryLine: '已加入游戏库后，玩家可以发表评价、查看版本历史并解锁自定义成就。',
  capsuleLabel: 'DST',
  heroTone: 'survival',
  tags: ['多人联机', '生存', '合作', '冒险', '成就'],
  links: ['访问网站', '查看更新记录', '阅读相关新闻', '查看讨论'],
  updates: [
    {
      date: '7月8日',
      type: '社区更新',
      title: '评价与成就模块接入游戏库确权',
      body: '玩家必须拥有 GAME_DST 后才能发表评价或解锁对应成就，社区互动与购买入库链路保持一致。'
    },
    {
      date: '7月7日',
      type: '课程自定义成就',
      title: '新增 DST 自定义成就演示',
      body: '成就解锁具备幂等处理，重复解锁不会重复写入 PLAYER_ACHIEVEMENT。'
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
    subtitle: '免费入库、饰品库存与市场交易样板',
    storeLine: '围绕饰品库存、掉落模拟和市场交易构建课程项目中的 CS2 经济系统。',
    libraryLine: '库内重点展示库存、掉落和市场入口；评价与成就主链路优先围绕 GAME_DST。',
    capsuleLabel: 'CS2',
    heroTone: 'tactical',
    tags: ['免费游玩', '射击', '饰品', '市场', '库存'],
    links: ['访问网站', '查看库存', '进入市场', '查看交易记录'],
    updates: [
      {
        date: '7月8日',
        type: '库存更新',
        title: 'CS2 饰品库存入口已接入',
        body: '库内页面保留 CS2 演示位置，后续可接入饰品掉落、挂单和成交记录。'
      }
    ]
  }
};

export function getGameMeta(gameId: string): GameCatalogItem {
  return gameCatalog[gameId] ?? { ...fallbackGame, gameId, shortName: gameId, title: gameId };
}
