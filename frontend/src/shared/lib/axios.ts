import axios from 'axios';
import { keycloak } from './keycloak';

export const api = axios.create({
  baseURL: '/api',
});

api.interceptors.request.use(async (config) => {
  if (keycloak.authenticated) {
    await keycloak.updateToken(30);
    config.headers.Authorization = `Bearer ${keycloak.token}`;
  }
  return config;
});
