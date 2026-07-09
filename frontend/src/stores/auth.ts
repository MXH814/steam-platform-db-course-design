import { defineStore } from 'pinia';
import { clearStoredToken, http, storeToken, tokenStorageKey } from '../api/http';
import type { AuthClaims, AuthResponse, LoginRequest, RegisterPlayerRequest } from '../api/types';

const adminRoles = new Set(['ADMIN', 'SUPER_ADMIN', 'RISK_ADMIN', 'CUSTOMER_SERVICE']);

interface AuthState {
  token: string | null;
  currentUser: AuthClaims | null;
  initialized: boolean;
}

export const useAuthStore = defineStore('auth', {
  state: (): AuthState => ({
    token: localStorage.getItem(tokenStorageKey),
    currentUser: null,
    initialized: false
  }),
  getters: {
    isAuthenticated: (state) => Boolean(state.token && state.currentUser),
    isAdmin: (state) => Boolean(state.currentUser && adminRoles.has(state.currentUser.role.toUpperCase())),
    isDeveloper: (state) => state.currentUser?.role.toUpperCase() === 'DEVELOPER'
  },
  actions: {
    setSession(response: AuthResponse) {
      this.token = response.token;
      this.currentUser = response.claims;
      storeToken(response.token);
    },
    async login(request: LoginRequest) {
      const { data } = await http.post<AuthResponse>('/api/auth/login', request);
      this.setSession(data);
    },
    async register(request: RegisterPlayerRequest) {
      const { data } = await http.post<AuthResponse>('/api/auth/register', request);
      this.setSession(data);
    },
    async loadCurrentUser() {
      const { data } = await http.get<AuthClaims>('/api/auth/me');
      this.currentUser = data;
    },
    async bootstrap() {
      if (!this.token) {
        this.initialized = true;
        return;
      }

      try {
        await this.loadCurrentUser();
      } catch {
        this.logout();
      } finally {
        this.initialized = true;
      }
    },
    logout() {
      this.token = null;
      this.currentUser = null;
      clearStoredToken();
    }
  }
});
