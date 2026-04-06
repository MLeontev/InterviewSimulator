import { useEffect, useState, type ReactNode } from 'react';
import { AuthProvider } from '../features/auth/context/AuthProvider';
import { initKeycloak } from '../shared/lib/keycloak';

interface Props {
  children: ReactNode;
}
export function Providers({ children }: Props) {
  const [ready, setReady] = useState(false);

  useEffect(() => {
    initKeycloak()
      .then(() => setReady(true))
      .catch((e) => {
        console.error('Keycloak init failed', e);
      });
  }, []);

  if (!ready) return null;

  return <AuthProvider>{children}</AuthProvider>;
}
