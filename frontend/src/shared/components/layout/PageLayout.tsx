import { Outlet, useNavigate } from 'react-router-dom';
import { useAuth } from '../../../features/auth/context/AuthContext';
import { Button } from '../ui/Button';
import { Footer } from './Footer';

export function PageLayout() {
  const { logout } = useAuth();
  const navigate = useNavigate();

  return (
    <div className='min-h-screen bg-white flex flex-col'>
      <nav className='border-b border-gray-200'>
        <div className='flex items-center justify-between max-w-5xl mx-auto py-4'>
          <span
            className='text-indigo-600 font-semibold cursor-pointer'
            onClick={() => navigate('/history')}
          >
            Тренажер собеседований
          </span>
          <Button variant='outline' onClick={logout}>
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
