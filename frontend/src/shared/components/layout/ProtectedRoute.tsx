import { Navigate, Outlet } from 'react-router-dom';
import { useAuth } from '../../../features/auth/context/AuthContext';

export function ProtectedRoute() {
  const { isAuthenticated } = useAuth();

  if (!isAuthenticated) {
    return <Navigate to='/' replace />;
  }

  return <Outlet />;
}
