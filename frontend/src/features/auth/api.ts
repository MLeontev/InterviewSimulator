import { api } from '../../shared/lib/axios';

export interface CurrentUser {
  id: string;
  email: string;
  identityId: string;
}

export const getMe = () =>
  api.get<CurrentUser>('/v1/users/me').then((r) => r.data);
