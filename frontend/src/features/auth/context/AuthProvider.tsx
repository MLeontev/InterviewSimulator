import { useEffect, useMemo, useState, type ReactNode } from 'react';
import { keycloak } from '../../../shared/lib/keycloak';
import { getMe, type CurrentUser } from '../api';
import { AuthContext, type AuthContextValue } from './AuthContext';

const TOKEN_REFRESH_INTERVAL_MS = 60_000;
const TOKEN_MIN_VALIDITY_SECONDS = 70;

export function AuthProvider({ children }: { children: ReactNode }) {
  const [ready, setReady] = useState(false);
  const [isAuthenticated, setIsAuthenticated] = useState(
    !!keycloak.authenticated,
  );
  const [user, setUser] = useState<CurrentUser | null>(null);

  useEffect(() => {
    let cancelled = false;
    let refreshIntervalId: number | null = null;

    const syncUser = async () => {
      if (!keycloak.authenticated) {
        if (!cancelled) setUser(null);
        return;
      }

      try {
        const me = await getMe();
        if (!cancelled) setUser(me);
      } catch {
        if (!cancelled) setUser(null);
      }
    };

    const refreshToken = async () => {
      if (!keycloak.authenticated) return;

      try {
        await keycloak.updateToken(TOKEN_MIN_VALIDITY_SECONDS);
        if (!cancelled) {
          setIsAuthenticated(true);
        }
      } catch {
        if (!cancelled) {
          setIsAuthenticated(false);
          setUser(null);
        }
      }
    };

    const init = async () => {
      if (!cancelled) setIsAuthenticated(!!keycloak.authenticated);
      await syncUser();
      if (!cancelled) setReady(true);
    };

    keycloak.onAuthSuccess = async () => {
      if (cancelled) return;
      setIsAuthenticated(true);
      await syncUser();
    };

    keycloak.onAuthLogout = () => {
      if (cancelled) return;
      setIsAuthenticated(false);
      setUser(null);
    };

    keycloak.onTokenExpired = () => {
      void refreshToken();
    };

    refreshIntervalId = window.setInterval(() => {
      void refreshToken();
    }, TOKEN_REFRESH_INTERVAL_MS);

    void init();

    return () => {
      cancelled = true;

      if (refreshIntervalId !== null) {
        window.clearInterval(refreshIntervalId);
      }

      keycloak.onAuthSuccess = undefined;
      keycloak.onAuthLogout = undefined;
      keycloak.onTokenExpired = undefined;
    };
  }, []);

  const value = useMemo<AuthContextValue>(
    () => ({
      ready,
      isAuthenticated,
      user,
      login: async () => {
        await keycloak.login({
          redirectUri: `${window.location.origin}/history`,
        });
      },
      logout: async () => {
        if (!keycloak.authenticated) return;
        await keycloak.logout({ redirectUri: window.location.origin });
      },
    }),
    [ready, isAuthenticated, user],
  );

  if (!ready) return null;

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
