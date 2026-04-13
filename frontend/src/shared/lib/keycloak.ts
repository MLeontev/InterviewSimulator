import Keycloak from 'keycloak-js';

const keycloakUrl =
  import.meta.env.VITE_KEYCLOAK_URL || 'http://localhost:8082';

export const keycloak = new Keycloak({
  url: keycloakUrl,
  realm: 'InterviewSimulator',
  clientId: 'interview-public-client',
});

let initPromise: Promise<boolean> | null = null;

export function initKeycloak() {
  if (!initPromise) {
    initPromise = keycloak.init({ onLoad: 'check-sso' }).catch((e) => {
      initPromise = null;
      throw e;
    });
  }
  return initPromise;
}
