import { useEffect, useMemo, useState, type ReactNode } from 'react';
import { keycloak } from '../../../shared/lib/keycloak';
import { getMe, type CurrentUser } from '../api';
import { AuthContext, type AuthContextValue } from './AuthContext';

export function AuthProvider({ children }: { children: ReactNode }) {
  const [ready, setReady] = useState(false);
  const [isAuthenticated, setIsAuthenticated] = useState(
    !!keycloak.authenticated,
  );
  const [user, setUser] = useState<CurrentUser | null>(null);

  useEffect(() => {
    const syncUser = async () => {
      if (!keycloak.authenticated) {
        setUser(null);
        return;
      }

      try {
        const me = await getMe();
        setUser(me);
      } catch {
        setUser(null);
      }
    };

    const init = async () => {
      setIsAuthenticated(!!keycloak.authenticated);
      await syncUser();
      setReady(true);
    };

    keycloak.onAuthSuccess = async () => {
      setIsAuthenticated(true);
      await syncUser();
    };

    keycloak.onAuthLogout = () => {
      setIsAuthenticated(false);
      setUser(null);
    };

    void init();
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
