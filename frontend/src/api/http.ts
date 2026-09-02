import axios from 'axios';

export const tokenStorageKey = 'steam-platform-token';

export function getStoredToken(): string | null {
  return sessionStorage.getItem(tokenStorageKey);
}

export function storeToken(token: string): void {
  sessionStorage.setItem(tokenStorageKey, token);
}

export function clearStoredToken(): void {
  sessionStorage.removeItem(tokenStorageKey);
}

export const http = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || '',
  timeout: 10000
});

http.interceptors.request.use((config) => {
  const token = getStoredToken();
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }

  return config;
});

let isHandlingUnauthorized = false;

http.interceptors.response.use(
  (response) => response,
  async (error) => {
    if (axios.isAxiosError(error) && error.response) {
      const status = error.response.status;
      const requestUrl = error.config?.url || '';
      const isAuthEndpoint =
        requestUrl.includes('/api/auth/login') ||
        requestUrl.includes('/api/auth/register');

      if (status === 401 && !isAuthEndpoint) {
        clearStoredToken();
        window.dispatchEvent(new CustomEvent('auth-unauthorized'));

        if (!isHandlingUnauthorized) {
          isHandlingUnauthorized = true;
          window.dispatchEvent(
            new CustomEvent('app-toast', { detail: '登录状态已失效，请重新登录。' })
          );

          try {
            const { router } = await import('../router');
            const currentRoute = router.currentRoute.value;
            const fullPath =
              currentRoute?.fullPath ||
              window.location.pathname + window.location.search;

            if (
              currentRoute?.name !== 'login' &&
              !window.location.pathname.startsWith('/login')
            ) {
              await router.push({
                name: 'login',
                query: { redirect: fullPath }
              });
            }
          } catch {
            const currentPath =
              window.location.pathname + window.location.search;
            if (!window.location.pathname.startsWith('/login')) {
              window.location.assign(`/login?redirect=${encodeURIComponent(currentPath)}`);
            }
          } finally {
            setTimeout(() => {
              isHandlingUnauthorized = false;
            }, 1200);
          }
        }
      } else if (status === 403) {
        const errorMsg = getApiError(error);
        const detailMsg =
          errorMsg && errorMsg !== '请求失败'
            ? errorMsg
            : '操作失败：当前账号没有执行此操作的权限。';

        window.dispatchEvent(
          new CustomEvent('app-toast', { detail: detailMsg })
        );
      }
    }

    return Promise.reject(error);
  }
);

export function getApiError(error: unknown): string {
  if (axios.isAxiosError(error)) {
    const data = error.response?.data;
    if (typeof data === 'string') {
      return data;
    }

    if (typeof data === 'object' && data !== null) {
      const apiData = data as { code?: number; message?: string; detail?: string; title?: string };
      if (apiData.code !== undefined && apiData.message !== undefined) {
        return apiData.message;
      }
      return apiData.detail || apiData.title || apiData.message || '';
    }

    return error.message;
  }

  return error instanceof Error ? error.message : '请求失败';
}
