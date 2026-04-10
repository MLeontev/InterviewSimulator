import { zodResolver } from '@hookform/resolvers/zod';
import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { toast } from 'sonner';
import { z } from 'zod';
import { registerUser } from '../../features/auth/api';
import { useAuth } from '../../features/auth/context/AuthContext';
import { Footer } from '../../shared/components/layout/Footer';
import { Button } from '../../shared/components/ui/Button';
import type { ApiError } from '../../shared/lib/apiError';

const registerSchema = z
  .object({
    email: z.email('Некорректный формат email'),
    password: z.string().min(1, 'Пароль обязателен'),
    confirmPassword: z.string().min(1, 'Подтвердите пароль'),
  })
  .refine((x) => x.password === x.confirmPassword, {
    message: 'Пароли не совпадают',
    path: ['confirmPassword'],
  });

type RegisterFormValues = z.infer<typeof registerSchema>;

export function RegisterPage() {
  const { login } = useAuth();
  const [serverError, setServerError] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<RegisterFormValues>({
    resolver: zodResolver(registerSchema),
    defaultValues: {
      email: '',
      password: '',
      confirmPassword: '',
    },
  });

  const onSubmit = async (values: RegisterFormValues) => {
    setServerError(null);

    try {
      await registerUser({
        email: values.email.trim(),
        password: values.password,
      });

      toast.success('Регистрация успешна. Выполняю вход...');
      await login();
    } catch (err) {
      const apiError = err as ApiError;
      const message = apiError.validationMessages[0] ?? apiError.message;

      if (apiError.validationMessages.length > 0) {
        setError('email', { type: 'server', message });
      } else {
        setServerError(message);
      }
    }
  };

  return (
    <div className='min-h-screen bg-white flex flex-col'>
      <main className='flex-1 flex items-center justify-center px-4'>
        <div className='w-full max-w-md border border-gray-200 rounded-xl p-6'>
          <h1 className='text-xl font-semibold text-gray-900 mb-5'>
            Регистрация
          </h1>

          <form
            className='space-y-4'
            onSubmit={handleSubmit(onSubmit)}
            noValidate
          >
            <div>
              <label className='block text-sm text-gray-700 mb-1'>Email</label>
              <input
                {...register('email')}
                className='w-full border border-gray-300 rounded-lg px-3 py-2'
                placeholder='you@example.com'
              />
              {errors.email && (
                <p className='mt-1 text-sm text-red-600'>
                  {errors.email.message}
                </p>
              )}
            </div>

            <div>
              <label className='block text-sm text-gray-700 mb-1'>Пароль</label>
              <input
                {...register('password')}
                type='password'
                className='w-full border border-gray-300 rounded-lg px-3 py-2'
                placeholder='••••••••'
              />
              {errors.password && (
                <p className='mt-1 text-sm text-red-600'>
                  {errors.password.message}
                </p>
              )}
            </div>

            <div>
              <label className='block text-sm text-gray-700 mb-1'>
                Повторите пароль
              </label>
              <input
                {...register('confirmPassword')}
                type='password'
                className='w-full border border-gray-300 rounded-lg px-3 py-2'
                placeholder='••••••••'
              />
              {errors.confirmPassword && (
                <p className='mt-1 text-sm text-red-600'>
                  {errors.confirmPassword.message}
                </p>
              )}
            </div>

            {serverError && (
              <div className='text-sm text-red-700 bg-red-50 border border-red-200 rounded-lg p-3'>
                {serverError}
              </div>
            )}

            <Button
              type='submit'
              variant='primary'
              className='w-full'
              disabled={isSubmitting}
            >
              {isSubmitting ? 'Создание...' : 'Создать аккаунт'}
            </Button>

            <div className='text-sm text-center text-gray-600'>
              Уже есть аккаунт?{' '}
              <button
                type='button'
                className='text-indigo-600 hover:underline'
                onClick={() => void login()}
              >
                Войти
              </button>
            </div>
          </form>
        </div>
      </main>

      <Footer />
    </div>
  );
}
