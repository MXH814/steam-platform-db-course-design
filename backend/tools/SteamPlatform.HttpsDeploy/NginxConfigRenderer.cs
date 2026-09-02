namespace SteamPlatform.HttpsDeploy;

public static class NginxConfigRenderer
{
    public static string Render(string publicIp, string certificateName = HttpsDeploymentService.ProductionCertificateName) => $$"""
        # Keep query strings out of access logs because SignalR WebSocket handshakes
        # carry the short-lived bearer token in the access_token query parameter.
        log_format steam_platform_safe '$remote_addr - $remote_user [$time_local] '
                                       '"$request_method $uri $server_protocol" $status $body_bytes_sent '
                                       '"$http_referer" "$http_user_agent"';

        server {
            listen 80 default_server;
            listen [::]:80 default_server;
            server_name {{publicIp}};

            access_log /var/log/nginx/steam-platform-access.log steam_platform_safe;

            root /opt/steam-platform/www;

            location ^~ /.well-known/acme-challenge/ {
                default_type text/plain;
                try_files $uri =404;
            }

            location / {
                return 308 https://$host$request_uri;
            }
        }

        server {
            listen 443 ssl http2 default_server;
            listen [::]:443 ssl http2 default_server;
            server_name {{publicIp}};

            access_log /var/log/nginx/steam-platform-access.log steam_platform_safe;

            ssl_certificate /etc/letsencrypt/live/{{certificateName}}/fullchain.pem;
            ssl_certificate_key /etc/letsencrypt/live/{{certificateName}}/privkey.pem;
            ssl_protocols TLSv1.2 TLSv1.3;
            ssl_session_cache shared:SteamPlatformTLS:10m;
            ssl_session_timeout 1d;
            ssl_session_tickets off;

            add_header X-Content-Type-Options nosniff always;
            add_header X-Frame-Options DENY always;
            add_header Referrer-Policy strict-origin-when-cross-origin always;
            add_header Permissions-Policy "camera=(), microphone=(), geolocation=(), payment=()" always;
            add_header Strict-Transport-Security "max-age=31536000" always;
            add_header Content-Security-Policy "default-src 'self'; base-uri 'self'; object-src 'none'; frame-ancestors 'none'; form-action 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data: blob:; media-src 'self' blob:; font-src 'self' data:; connect-src 'self' wss:" always;

            root /opt/steam-platform/www;
            index index.html;

            location /api/ {
                proxy_pass http://127.0.0.1:5253/api/;
                proxy_http_version 1.1;
                proxy_set_header Host $host;
                proxy_set_header X-Real-IP $remote_addr;
                proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
                proxy_set_header X-Forwarded-Proto $scheme;
            }

            location = /health {
                proxy_pass http://127.0.0.1:5253/health;
                proxy_http_version 1.1;
                proxy_set_header Host $host;
                proxy_set_header X-Real-IP $remote_addr;
                proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
                proxy_set_header X-Forwarded-Proto $scheme;
            }

            location = /health/database {
                proxy_pass http://127.0.0.1:5253/health/database;
                proxy_http_version 1.1;
                proxy_set_header Host $host;
                proxy_set_header X-Real-IP $remote_addr;
                proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
                proxy_set_header X-Forwarded-Proto $scheme;
            }

            location /hubs/ {
                # Nginx includes the full request target in upstream error messages.
                # Suppress routine WebSocket disconnects here so bearer tokens are
                # not persisted when the API restarts or a client loses connection.
                error_log /var/log/nginx/steam-platform-signalr-error.log crit;
                proxy_pass http://127.0.0.1:5253/hubs/;
                proxy_http_version 1.1;
                proxy_set_header Upgrade $http_upgrade;
                proxy_set_header Connection "upgrade";
                proxy_set_header Host $host;
                proxy_set_header X-Real-IP $remote_addr;
                proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
                proxy_set_header X-Forwarded-Proto $scheme;
                proxy_read_timeout 3600s;
            }

            location / {
                try_files $uri $uri/ /index.html;
            }
        }
        """;
}
