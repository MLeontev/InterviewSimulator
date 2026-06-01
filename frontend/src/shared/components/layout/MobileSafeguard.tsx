import { useNavigate } from 'react-router-dom';
import { Button } from '../ui/Button';

export function MobileSafeguard() {
  const navigate = useNavigate();

  return (
    <div className='flex flex-col items-center justify-center min-h-dvh p-6 text-center md:hidden bg-slate-50'>
      <div className='max-w-md bg-white rounded-2xl border border-indigo-100 shadow-xl p-8 flex flex-col items-center'>
        <h2 className='text-2xl font-bold text-gray-900 mb-3 tracking-tight'>
          Нужен большой экран
        </h2>
        <p className='text-gray-600 text-sm leading-relaxed mb-8'>
          Экран симулятора недоступен на мобильных устройствах. Практическая
          часть интервью, ответы на теоретические вопросы и написание кода
          требуют полноценной клавиатуры и большого экрана. Пожалуйста, откройте
          эту ссылку на компьютере или ноутбуке.
        </p>
        <div className='flex flex-col gap-3 w-full'>
          <Button
            variant='primary'
            onClick={() => navigate('/history')}
            className='w-full py-3'
          >
            К списку сессий
          </Button>
          <Button
            variant='outline'
            onClick={() => navigate('/')}
            className='w-full py-3'
          >
            На главную
          </Button>
        </div>
      </div>
    </div>
  );
}
