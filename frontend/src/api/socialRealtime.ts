import { HubConnection, HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr';
import { getStoredToken } from './http';
import type { DirectMessageItem, DiscussionTopicView, TradeOfferView, UserNotificationItem, WorkshopItemView } from './types';

export interface SocialRealtimeHandlers {
  onDirectMessage?: (message: DirectMessageItem) => void;
  onNotification?: (notification: UserNotificationItem) => void;
  onFriendChanged?: () => void;
  onWorkshopSubscriptionChanged?: (item: WorkshopItemView) => void;
  onTradeOfferChanged?: (offer: TradeOfferView, received: boolean) => void;
  onDiscussionReply?: (topic: DiscussionTopicView) => void;
}

export class SocialRealtimeClient {
  private connection: HubConnection | null = null;

  async connect(handlers: SocialRealtimeHandlers): Promise<void> {
    const token = getStoredToken();
    if (!token || this.connection?.state === HubConnectionState.Connected) {
      return;
    }

    const configuredBase = String(import.meta.env.VITE_API_BASE_URL || '').replace(/\/$/, '');
    const hubUrl = `${configuredBase}/hubs/social`;
    const connection = new HubConnectionBuilder()
      .withUrl(hubUrl, { accessTokenFactory: () => getStoredToken() || '' })
      .withAutomaticReconnect([0, 2000, 5000, 10000])
      .configureLogging(import.meta.env.DEV ? LogLevel.Warning : LogLevel.Error)
      .build();

    connection.on('DirectMessageReceived', (payload: DirectMessageItem) => handlers.onDirectMessage?.(payload));
    connection.on('NotificationReceived', (payload: UserNotificationItem) => handlers.onNotification?.(payload));
    connection.on('FriendRequestReceived', () => handlers.onFriendChanged?.());
    connection.on('FriendRequestAccepted', () => handlers.onFriendChanged?.());
    connection.on('ReviewInteractionReceived', (payload: UserNotificationItem) => handlers.onNotification?.(payload));
    connection.on('WorkshopSubscriptionChanged', (payload: WorkshopItemView) => handlers.onWorkshopSubscriptionChanged?.(payload));
    connection.on('TradeOfferReceived', (payload: TradeOfferView) => handlers.onTradeOfferChanged?.(payload, true));
    connection.on('TradeOfferChanged', (payload: TradeOfferView) => handlers.onTradeOfferChanged?.(payload, false));
    connection.on('DiscussionReplyReceived', (payload: DiscussionTopicView) => handlers.onDiscussionReply?.(payload));

    this.connection = connection;
    try {
      await connection.start();
    } catch (error) {
      this.connection = null;
      await connection.stop().catch(() => undefined);
      throw error;
    }
  }

  async disconnect(): Promise<void> {
    const connection = this.connection;
    this.connection = null;
    if (connection) {
      await connection.stop();
    }
  }
}
