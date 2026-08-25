import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr';

const baseUrl = (process.env.STEAM_SMOKE_BASE_URL || 'http://127.0.0.1:5173').replace(/\/$/, '');
const sender = {
  role: 'PLAYER',
  account: process.env.STEAM_SMOKE_SENDER_ACCOUNT || 'alice',
  password: process.env.STEAM_SMOKE_SENDER_PASSWORD || 'alice'
};
const recipient = {
  role: 'PLAYER',
  account: process.env.STEAM_SMOKE_RECIPIENT_ACCOUNT || 'bob',
  password: process.env.STEAM_SMOKE_RECIPIENT_PASSWORD || 'bob'
};

async function login(credentials) {
  const response = await fetch(`${baseUrl}/api/auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(credentials)
  });
  if (!response.ok) throw new Error(`Login failed for ${credentials.account}: HTTP ${response.status}`);
  return response.json();
}

const [senderSession, recipientSession] = await Promise.all([login(sender), login(recipient)]);
const connection = new HubConnectionBuilder()
  .withUrl(`${baseUrl}/hubs/social`, { accessTokenFactory: () => recipientSession.token })
  .configureLogging(LogLevel.Error)
  .build();

const content = `SignalR smoke ${new Date().toISOString()}`;
let timeout;
const received = new Promise((resolve, reject) => {
  timeout = setTimeout(() => reject(new Error('Timed out waiting for DirectMessageReceived.')), 12000);
  connection.on('DirectMessageReceived', (message) => {
    if (message.content === content) resolve(message);
  });
});

try {
  await connection.start();
  const response = await fetch(`${baseUrl}/api/friends/${recipientSession.claims.principalId}/messages`, {
    method: 'POST',
    headers: {
      Authorization: `Bearer ${senderSession.token}`,
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({ content })
  });
  if (!response.ok) throw new Error(`Message send failed: HTTP ${response.status} ${await response.text()}`);

  const message = await received;
  console.log(JSON.stringify({
    status: 'OK',
    event: 'DirectMessageReceived',
    messageId: message.messageId,
    senderId: message.senderId,
    recipientId: recipientSession.claims.principalId
  }));
} finally {
  clearTimeout(timeout);
  await connection.stop();
}
