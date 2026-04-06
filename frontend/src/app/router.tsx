import { createBrowserRouter } from 'react-router-dom';
import { HistoryPage } from '../pages/history/HistoryPage';
import { LandingPage } from '../pages/landing/LandingPage';
import { PageLayout } from '../shared/components/layout/PageLayout';
import { ProtectedRoute } from '../shared/components/layout/ProtectedRoute';

export const router = createBrowserRouter([
  {
    path: '/',
    element: <LandingPage />,
  },
  {
    element: <ProtectedRoute />,
    children: [
      {
        element: <PageLayout />,
        children: [{ path: '/history', element: <HistoryPage /> }],
      },
    ],
  },
]);
