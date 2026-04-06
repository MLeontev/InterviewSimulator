import type { AxiosError } from 'axios';

type ValidationErrors = Record<string, string[]>;

type ErrorPayload = {
  code?: string;
  description?: string;
  errors?: ValidationErrors;
};

export type ApiError = {
  status: number;
  code: string;
  message: string;
  validationMessages: string[];
  raw?: unknown;
};

export function toApiError(err: unknown): ApiError {
  const axiosErr = err as AxiosError<ErrorPayload>;
  const status = axiosErr.response?.status ?? 0;
  const data = axiosErr.response?.data;

  const code = data?.code ?? 'UNKNOWN_ERROR';
  const message = data?.description ?? 'Произошла ошибка';

  const validationMessages = data?.errors
    ? Object.values(data.errors).flat()
    : [];

  return {
    status,
    code,
    message,
    validationMessages,
    raw: data,
  };
}
