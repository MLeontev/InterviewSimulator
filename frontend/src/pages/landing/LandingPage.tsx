import { useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../../features/auth/context/AuthContext';
import { Footer } from '../../shared/components/layout/Footer';
import { Button } from '../../shared/components/ui/Button';

interface Feature {
  title: string;
  text: string;
}

interface Step {
  num: string;
  title: string;
  text: string;
}

const features: Feature[] = [
  {
    title: 'Настройка под тебя',
    text: 'Выбери пресет под свой уровень и специализацию',
  },
  {
    title: 'Реальные условия',
    text: 'Таймер, редактор кода, последовательные вопросы',
  },
  {
    title: 'Обратная связь ИИ',
    text: 'Развернутые комментарии по каждому ответу',
  },
  { title: 'Итоговый отчет', text: 'Сильные и слабые темы, рекомендации' },
];

const steps: Step[] = [
  { num: '1', title: 'Настрой параметры', text: 'Выбери пресет собеседования' },
  {
    num: '2',
    title: 'Пройди собеседование',
    text: 'Вопросы и задачи в одной сессии',
  },
  { num: '3', title: 'Изучи результаты', text: 'Отчет с анализом и советами' },
];

export function LandingPage() {
  const { isAuthenticated, login } = useAuth();
  const navigate = useNavigate();

  useEffect(() => {
    if (isAuthenticated) {
      navigate('/history');
    }
  }, [isAuthenticated, navigate]);

  return (
    <div className='min-h-screen bg-white flex flex-col'>
      <nav className='border-b border-gray-200'>
        <div className='flex items-center justify-between max-w-5xl mx-auto py-4'>
          <span className='text-indigo-600 font-semibold'>
            Тренажер собеседований
          </span>
          <div className='flex gap-2'>
            <Button variant='outline' onClick={login}>
              Войти
            </Button>
            <Button variant='primary'>Зарегистрироваться</Button>
          </div>
        </div>
      </nav>

      <main className='flex-1'>
        <section className='bg-indigo-50 py-16 px-16'>
          <div className='text-center mb-12'>
            <h1 className='text-4xl font-bold text-gray-900 max-w-4xl mx-auto leading-tight mb-4'>
              Подготовься к техническому собеседованию
            </h1>
            <p className='text-indigo-600 text-lg max-w-xl mx-auto mb-8 leading-relaxed'>
              Проходи пробные интервью с теоретическими вопросами и
              алгоритмическими задачами. Получай обратную связь от ИИ
            </p>
            <Button
              variant='primary'
              className='px-8 py-3 text-base rounded-xl'
            >
              Начать подготовку
            </Button>
          </div>

          <div className='flex gap-4 max-w-5xl mx-auto'>
            {features.map((f) => (
              <div
                key={f.title}
                className='flex-1 bg-white border border-gray-200 rounded-xl p-6'
              >
                <div className='text-indigo-600 font-semibold text-sm mb-2'>
                  {f.title}
                </div>
                <div className='text-gray-500 text-sm leading-relaxed'>
                  {f.text}
                </div>
              </div>
            ))}
          </div>
        </section>

        <div className='border-t border-gray-200' />

        <section className='py-16 px-16'>
          <div className='flex justify-center gap-12 max-w-4xl mx-auto text-center'>
            {steps.map((s) => (
              <div key={s.num} className='flex-1'>
                <div className='w-12 h-12 rounded-full bg-indigo-600 text-white flex items-center justify-center text-lg font-bold mx-auto mb-4'>
                  {s.num}
                </div>
                <div className='font-semibold text-gray-900 mb-1'>
                  {s.title}
                </div>
                <div className='text-sm text-gray-500'>{s.text}</div>
              </div>
            ))}
          </div>
        </section>
      </main>

      <Footer />
    </div>
  );
}
