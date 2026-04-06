import axios from 'axios';
import { keycloak } from './keycloak';
import { toApiError } from './apiError';
import { toast } from 'sonner';

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

api.interceptors.response.use(
  (response) => response,
  (error) => {
    const apiError = toApiError(error);

    const skipToast = !!error.config?.skipErrorToast;
    if (!skipToast) {
      if (apiError.validationMessages.length > 0) {
        toast.error(apiError.validationMessages[0]); // или склеить все
      } else {
        toast.error(apiError.message);
      }
    }

    return Promise.reject(apiError);
  },
);
