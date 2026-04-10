import { useEffect, useState } from 'react';
import { Button } from '../../shared/components/ui/Button';

interface Props {
  currentIndex: number;
  totalQuestions: number;
  plannedEndAt: string;
  onFinish: () => void;
}

function useTimer(plannedEndAt: string) {
  const [secondsLeft, setSecondsLeft] = useState(() => {
    const diff = new Date(plannedEndAt).getTime() - Date.now();
    return Math.max(0, Math.floor(diff / 1000));
  });

  useEffect(() => {
    const interval = setInterval(() => {
      setSecondsLeft((prev) => Math.max(0, prev - 1));
    }, 1000);
    return () => clearInterval(interval);
  }, [plannedEndAt]);

  const minutes = String(Math.floor(secondsLeft / 60)).padStart(2, '0');
  const seconds = String(secondsLeft % 60).padStart(2, '0');
  const isUrgent = secondsLeft < 300;

  return { display: `${minutes}:${seconds}`, isUrgent };
}

export function InterviewHeader({
  currentIndex,
  totalQuestions,
  plannedEndAt,
  onFinish,
}: Props) {
  const { display, isUrgent } = useTimer(plannedEndAt);

  return (
    <nav className='bg-indigo-50 border-b border-indigo-100'>
      <div className='flex items-center justify-between  max-w-4xl mx-auto py-4'>
        <span className='text-indigo-600 font-semibold'>
          Задание {currentIndex} из {totalQuestions}
        </span>
        <span
          className={`border rounded-lg px-3 py-1 text-sm font-medium font-mono
        ${
          isUrgent
            ? 'border-red-300 text-red-600 bg-white'
            : 'border-gray-300 text-gray-700 bg-white'
        }`}
        >
          {display}
        </span>
        <Button onClick={onFinish}>Завершить сессию досрочно</Button>
      </div>
    </nav>
  );
}
