import { useCallback, useEffect, useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { toast } from 'sonner';
import {
  getCurrentSession,
  getHistory,
  InterviewStatus,
  type CurrentSession,
  type HistoryItem,
} from '../../features/interview/api';
import { Button } from '../../shared/components/ui/Button';
import type { ApiError } from '../../shared/lib/apiError';

const REFRESH_MS = 5000;

const statusLabel: Record<InterviewStatus, string> = {
  [InterviewStatus.InProgress]: 'В процессе',
  [InterviewStatus.Finished]: 'Завершено',
  [InterviewStatus.EvaluatingAi]: 'Оценивается',
  [InterviewStatus.Evaluated]: 'Оценено',
  [InterviewStatus.AiEvaluationFailed]: 'Ошибка AI-оценки',
};

function formatDate(dateStr: string) {
  return new Date(dateStr).toLocaleString('ru', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
}

function formatTimeLeft(plannedEndAt: string) {
  const ms = new Date(plannedEndAt).getTime() - Date.now();
  if (ms <= 0) return 'время истекло';

  const totalMinutes = Math.ceil(ms / 60000);
  const hours = Math.floor(totalMinutes / 60);
  const minutes = totalMinutes % 60;
  return hours > 0 ? `${hours} ч ${minutes} мин` : `${minutes} мин`;
}

export function HistoryPage() {
  const navigate = useNavigate();
  const [items, setItems] = useState<HistoryItem[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [currentSession, setCurrentSession] = useState<CurrentSession | null>(
    null,
  );
  const [isLoadingCurrentSession, setIsLoadingCurrentSession] = useState(true);
  const isRefreshingRef = useRef(false);

  const loadData = useCallback(async (silent: boolean) => {
    if (isRefreshingRef.current) return;
    isRefreshingRef.current = true;

    if (!silent) {
      setIsLoading(true);
      setIsLoadingCurrentSession(true);
    }

    try {
      const [history, current] = await Promise.all([
        getHistory(),
        getCurrentSession({ skipErrorToast: true }).catch((e) => {
          const apiError = e as ApiError;
          if (apiError.code === 'SESSION_NOT_FOUND') {
            return null;
          }
          throw e;
        }),
      ]);

      setItems(history);
      setCurrentSession(current);
    } catch (e) {
      if (!silent) {
        const apiError = e as ApiError;
        toast.error(apiError.message);
      }
    } finally {
      if (!silent) {
        setIsLoading(false);
        setIsLoadingCurrentSession(false);
      }
      isRefreshingRef.current = false;
    }
  }, []);

  useEffect(() => {
    void loadData(false);
  }, [loadData]);

  useEffect(() => {
    const intervalId = window.setInterval(() => {
      if (document.visibilityState === 'visible') {
        void loadData(true);
      }
    }, REFRESH_MS);

    const onFocus = () => {
      void loadData(true);
    };

    window.addEventListener('focus', onFocus);

    return () => {
      window.clearInterval(intervalId);
      window.removeEventListener('focus', onFocus);
    };
  }, [loadData]);

  return (
    <div className='max-w-5xl mx-auto py-12'>
      <div className='flex items-center justify-between mb-8'>
        <h1 className='text-2xl font-semibold text-gray-900'>
          Мои собеседования
        </h1>
        {!currentSession && (
          <Button
            variant='primary'
            onClick={() => navigate(currentSession ? '/interview' : '/presets')}
          >
            Начать новое собеседование
          </Button>
        )}
      </div>

      {!isLoadingCurrentSession && currentSession && (
        <section className='border border-indigo-200 bg-indigo-50 rounded-xl p-4 mb-8'>
          <div className='flex items-center justify-between'>
            <div>
              <div className='text-lg font-semibold text-gray-900 mb-1'>
                Есть активная сессия
              </div>
              <div className='text-sm text-gray-700'>
                Старт: {formatDate(currentSession.startedAt)}
              </div>
              <div className='text-sm text-gray-700'>
                Прогресс: {currentSession.answeredQuestions}/
                {currentSession.totalQuestions}
              </div>
              <div className='text-sm text-gray-700'>
                Осталось времени: {formatTimeLeft(currentSession.plannedEndAt)}
              </div>
            </div>
            <Button variant='primary' onClick={() => navigate('/interview')}>
              Продолжить
            </Button>
          </div>
        </section>
      )}

      {isLoading ? (
        <div className='text-gray-400 text-sm'>Загрузка...</div>
      ) : items.length === 0 ? (
        <div className='text-center'>
          <div className='text-gray-400 text-sm mb-4'>
            У вас пока нет собеседований
          </div>
          {!currentSession && (
            <Button variant='primary' onClick={() => navigate('/presets')}>
              Начать первое собеседование
            </Button>
          )}
        </div>
      ) : (
        <div>
          <div className='grid grid-cols-4 text-sm text-gray-400 px-4 mb-2'>
            <span>Дата и время</span>
            <span>Пресет</span>
            <span>Статус</span>
            <span />
          </div>

          <div className='flex flex-col gap-3'>
            {items.map((item) => (
              <div
                key={item.sessionId}
                className='grid grid-cols-4 items-center border border-gray-200 rounded-xl px-4 py-4'
              >
                <span className='text-sm text-gray-700'>
                  {formatDate(item.finishedAt ?? item.startedAt)}
                </span>
                <span className='text-sm text-gray-700'>
                  {item.interviewPresetName}
                </span>
                <span className='text-sm text-gray-700'>
                  {statusLabel[item.status]}
                </span>
                <div className='flex justify-end'>
                  <Button
                    variant='outline'
                    onClick={() =>
                      navigate(`/interview/result/${item.sessionId}`)
                    }
                  >
                    Подробнее
                  </Button>
                </div>
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}
