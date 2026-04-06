import Keycloak from 'keycloak-js';

export const keycloak = new Keycloak({
  url: 'http://localhost:8082',
  realm: 'InterviewSimulator',
  clientId: 'interview-public-client',
});
