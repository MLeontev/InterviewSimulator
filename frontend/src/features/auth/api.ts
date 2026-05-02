import { api } from '../../shared/lib/axios';

export interface CurrentUser {
  id: string;
  email: string;
  identityId: string;
}

export interface RegisterUserRequest {
  email: string;
  password: string;
}

export interface RegisterUserResponse {
  userId: string;
  identityId: string;
}

export const getMe = () =>
  api.get<CurrentUser>('/v1/users/me').then((r) => r.data);

export const registerUser = (request: RegisterUserRequest) =>
  api
    .post<RegisterUserResponse>('/v1/users', request, {
      skipErrorToast: true,
    })
    .then((r) => r.data);
