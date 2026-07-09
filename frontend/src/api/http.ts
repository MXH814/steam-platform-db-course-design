import axios from 'axios';

export const tokenStorageKey = 'steam-platform-token';

export function getStoredToken(): string | null {
  return localStorage.getItem(tokenStorageKey);
}

export function storeToken(token: string): void {
  localStorage.setItem(tokenStorageKey, token);
}

export function clearStoredToken(): void {
  localStorage.removeItem(tokenStorageKey);
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

export function getApiError(error: unknown): string {
  if (axios.isAxiosError(error)) {
    const data = error.response?.data as { detail?: string; title?: string; message?: string } | string | undefined;
    if (typeof data === 'string') {
      return data;
    }

    return data?.detail || data?.title || data?.message || error.message;
  }

  return error instanceof Error ? error.message : '请求失败';
}
