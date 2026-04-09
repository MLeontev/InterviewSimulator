import { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { toast } from 'sonner';
import {
  getSessionReport,
  SessionVerdict,
  type InterviewSessionReport,
} from '../../features/interview/api';
import { Button } from '../../shared/components/ui/Button';
import { Tag } from '../../shared/components/ui/Tag';
import type { ApiError } from '../../shared/lib/apiError';
import { QuestionCard } from './QuestionCard';

const POLLING_INTERVAL_MS = 1000;

function formatDate(dateStr: string | null) {
  if (!dateStr) return '–';
  return new Date(dateStr).toLocaleString('ru', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
}

function formatDuration(startedAt: string, finishedAt: string | null) {
  const start = new Date(startedAt).getTime();
  const end = finishedAt ? new Date(finishedAt).getTime() : Date.now();
  const minutes = Math.max(0, Math.floor((end - start) / 60000));
  const h = Math.floor(minutes / 60);
  const m = minutes % 60;
  return h > 0 ? `${h} ч ${m} мин` : `${m} мин`;
}

function sessionVerdictView(v: SessionVerdict) {
  switch (v) {
    case SessionVerdict.Passed:
      return { text: 'Пройдено', className: 'text-green-700 bg-green-50' };
    case SessionVerdict.Borderline:
      return { text: 'Погранично', className: 'text-amber-700 bg-amber-50' };
    case SessionVerdict.Failed:
      return { text: 'Не пройдено', className: 'text-red-700 bg-red-50' };
    default:
      return { text: '—', className: 'text-gray-600 bg-gray-100' };
  }
}

export function InterviewResultPage() {
  const navigate = useNavigate();
  const { sessionId } = useParams<{ sessionId: string }>();

  const [report, setReport] = useState<InterviewSessionReport | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isPolling, setIsPolling] = useState(false);
  const [errorText, setErrorText] = useState<string | null>(null);

  useEffect(() => {
    if (!sessionId) {
      setErrorText('Некорректный идентификатор сессии');
      setIsLoading(false);
      return;
    }

    let cancelled = false;
    let timerId: number | undefined;

    const load = async (initial: boolean) => {
      if (cancelled) return;
      if (initial) {
        setIsLoading(true);
        setErrorText(null);
      }

      try {
        const data = await getSessionReport(sessionId, {
          skipErrorToast: true,
        });
        if (cancelled) return;

        setReport(data);
        setIsPolling(false);
      } catch (e) {
        if (cancelled) return;

        const apiError = e as ApiError;
        if (apiError.code === 'SESSION_NOT_FINISHED') {
          setIsPolling(true);
          timerId = window.setTimeout(() => {
            void load(false);
          }, POLLING_INTERVAL_MS);
          return;
        }

        setErrorText(apiError.message);
        toast.error(apiError.message);
      } finally {
        if (initial && !cancelled) {
          setIsLoading(false);
        }
      }
    };

    void load(true);

    return () => {
      cancelled = true;
      if (timerId) {
        window.clearTimeout(timerId);
      }
    };
  }, [sessionId]);

  if (isLoading) {
    return (
      <div className='max-w-5xl mx-auto py-12 flex justify-center'>
        <div className='text-gray-400 text-sm'>Загрузка отчета...</div>
      </div>
    );
  }

  if (errorText || !report) {
    return (
      <div className='flex flex-col items-center justify-center gap-4 py-12'>
        <div className='text-gray-400 text-sm text-center'>
          {errorText ?? 'Не удалось загрузить отчет'}
        </div>
        <Button variant='outline' onClick={() => navigate('/history')}>
          К списку сессий
        </Button>
      </div>
    );
  }

  const verdict = sessionVerdictView(report.sessionVerdict);

  return (
    <div className='max-w-5xl mx-auto py-10'>
      <div className='flex items-center justify-between mb-6'>
        <h1 className='text-2xl font-semibold text-gray-900'>
          Отчет по сессии собеседования
        </h1>
        <Button variant='outline' onClick={() => navigate('/history')}>
          К списку сессий
        </Button>
      </div>

      {isPolling && (
        <div className='mb-4 text-sm text-indigo-700 bg-indigo-50 border border-indigo-200 rounded-xl px-4 py-3'>
          Сессия еще оценивается ИИ. Обновляю...
        </div>
      )}

      <section className='border border-indigo-200 bg-indigo-50 rounded-xl p-4 mb-4'>
        <h2 className='text-xl font-semibold mb-2'>
          {report.interviewPresetName}
        </h2>

        <div className='flex gap-3 mb-3 text-sm text-gray-800'>
          <div className='flex-1 bg-white/70 rounded-lg px-3 py-2 text-center'>
            Старт: {formatDate(report.startedAt)}
          </div>
          <div className='flex-1 bg-white/70 rounded-lg px-3 py-2 text-center'>
            Завершение: {formatDate(report.finishedAt)}
          </div>
          <div className='flex-1 bg-white/70 rounded-lg px-3 py-2 text-center'>
            Длительность: {formatDuration(report.startedAt, report.finishedAt)}
          </div>
          <div className='flex-1 bg-white/70 rounded-lg px-3 py-2 text-center'>
            Выполнено заданий: {report.answeredQuestions}/
            {report.totalQuestions}
          </div>
        </div>

        <div className='flex items-center gap-2 flex-wrap'>
          <span className={`px-3 py-1 rounded-lg text-sm ${verdict.className}`}>
            Итог: {verdict.text}
          </span>
          {report.averageQuestionAiScore != null && (
            <Tag>Средний балл: {report.averageQuestionAiScore}/10</Tag>
          )}
        </div>
      </section>

      <div className='flex gap-4 mb-4'>
        <section className='flex-1 border border-green-200 rounded-xl bg-green-50 p-4'>
          <h3 className='text-xl font-semibold mb-2'>Сильные темы</h3>
          {report.sessionStrengths.length === 0 ? (
            <div className='text-sm text-gray-500'>Пока нет данных</div>
          ) : (
            <ul className='list-disc list-outside pl-4 text-gray-800'>
              {report.sessionStrengths.map((x, i) => (
                <li key={`${x}-${i}`}>{x}</li>
              ))}
            </ul>
          )}
        </section>
        <section className='flex-1 border border-red-200 rounded-xl bg-red-50 p-4'>
          <h3 className='text-xl font-semibold mb-2'>Слабые темы</h3>
          {report.sessionWeaknesses.length === 0 ? (
            <div className='text-sm text-gray-500'>Пока нет данных</div>
          ) : (
            <ul className='list-disc list-outside pl-4 text-gray-800'>
              {report.sessionWeaknesses.map((x, i) => (
                <li key={`${x}-${i}`}>{x}</li>
              ))}
            </ul>
          )}
        </section>
      </div>

      <section className='border border-amber-200 rounded-xl bg-amber-50 p-4 mb-6'>
        <h3 className='text-xl font-semibold mb-2'>Рекомендации</h3>

        {report.sessionSummary && (
          <p className='text-gray-900 whitespace-pre-wrap mb-2'>
            {report.sessionSummary}
          </p>
        )}

        {report.sessionRecommendations.length > 0 && (
          <ul className='list-disc list-outside pl-4 text-gray-800'>
            {report.sessionRecommendations.map((x, i) => (
              <li key={`${x}-${i}`}>{x}</li>
            ))}
          </ul>
        )}
      </section>

      <div className='flex flex-col gap-4'>
        {report.questions.map((q) => (
          <QuestionCard key={q.questionId} question={q} />
        ))}
      </div>
    </div>
  );
}
