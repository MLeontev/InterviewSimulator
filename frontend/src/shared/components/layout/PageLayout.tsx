import { Outlet, useNavigate } from 'react-router-dom';
import { useAuth } from '../../../features/auth/context/AuthContext';
import { Button } from '../ui/Button';
import { Footer } from './Footer';

export function PageLayout() {
  const { logout } = useAuth();
  const navigate = useNavigate();

  return (
    <div className='min-h-screen bg-white flex flex-col'>
      <nav className='border-b border-slate-100'>
        <div className='flex items-center justify-between max-w-5xl mx-auto py-3.5 px-4'>
          <span
            className='text-indigo-600 font-semibold text-lg cursor-pointer hover:text-indigo-700 transition-colors'
            onClick={() => navigate('/history')}
          >
            Тренажер собеседований
          </span>
          <Button
            variant='outline'
            onClick={logout}
            className='text-xs sm:text-sm border-gray-300 text-gray-500 hover:text-red-600 hover:border-red-200 py-1.5 px-3'
          >
            Выйти
          </Button>
        </div>
      </nav>

      <main className='flex-1'>
        <Outlet />
      </main>

      <Footer />
    </div>
  );
}
