import { createContext, useContext } from 'react';
import type { CurrentUser } from '../api';

export type AuthContextValue = {
  ready: boolean;
  isAuthenticated: boolean;
  user: CurrentUser | null;
  login: () => Promise<void>;
  logout: () => Promise<void>;
};

export const AuthContext = createContext<AuthContextValue | null>(null);

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used within AuthProvider');
  return ctx;
}
