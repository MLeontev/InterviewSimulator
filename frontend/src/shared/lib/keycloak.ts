import Keycloak from 'keycloak-js';

export const keycloak = new Keycloak({
  url: 'http://localhost:8082',
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
