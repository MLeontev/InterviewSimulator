import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  getHistory,
  type HistoryItem,
  InterviewStatus,
} from '../../features/interview/api';
import { Button } from '../../shared/components/ui/Button';

const statusLabel: Record<InterviewStatus, string> = {
  [InterviewStatus.InProgress]: 'В процессе',
  [InterviewStatus.Finished]: 'Завершено',
  [InterviewStatus.EvaluatingAi]: 'Оценивается',
  [InterviewStatus.Evaluated]: 'Оценено',
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

export function HistoryPage() {
  const navigate = useNavigate();
  const [items, setItems] = useState<HistoryItem[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    getHistory()
      .then(setItems)
      .finally(() => setIsLoading(false));
  }, []);

  return (
    <div className='max-w-5xl mx-auto py-12'>
      <div className='flex items-center justify-between mb-8'>
        <h1 className='text-2xl font-semibold text-gray-900'>
          Мои собеседования
        </h1>
        <Button variant='primary' onClick={() => navigate('/presets')}>
          Начать новое собеседование
        </Button>
      </div>

      {isLoading ? (
        <div className='text-gray-400 text-sm'>Загрузка...</div>
      ) : items.length === 0 ? (
        <div className='text-center'>
          <div className='text-gray-400 text-sm mb-4'>
            У вас пока нет собеседований
          </div>
          <Button variant='primary' onClick={() => navigate('/presets')}>
            Начать первое собеседование
          </Button>
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
