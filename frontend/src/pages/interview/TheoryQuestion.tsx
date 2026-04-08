import { useState } from 'react';
import type { CurrentQuestion } from '../../features/interview/api';
import { Button } from '../../shared/components/ui/Button';

interface Props {
  question: CurrentQuestion;
  isSubmitting: boolean;
  onSubmit: (answer: string) => void;
  onSkip: () => void;
}

export function TheoryQuestion({
  question,
  isSubmitting,
  onSubmit,
  onSkip,
}: Props) {
  const [answer, setAnswer] = useState(() => question.answer ?? '');

  return (
    <div className='py-10'>
      <div className='border-l-4 border-indigo-400 bg-gray-100 rounded-xl px-5 py-4 mb-6'>
        <h2 className='text-lg font-semibold text-gray-900 mb-2'>
          {question.title || 'Теоретический вопрос'}
        </h2>
        <p className='whitespace-pre-wrap'>{question.text}</p>
      </div>

      <textarea
        value={answer}
        onChange={(e) => setAnswer(e.target.value)}
        placeholder='Введите ваш ответ...'
        className='w-full min-h-56 border border-gray-300 rounded-xl px-5 py-4 placeholder:text-gray-400 focus:outline-none focus:ring-1 focus:ring-indigo-200 focus:border-indigo-300'
      />

      <div className='flex gap-3 mt-1'>
        <Button
          variant='primary'
          disabled={isSubmitting || answer.trim().length === 0}
          onClick={() => void onSubmit(answer.trim())}
        >
          Отправить ответ
        </Button>
        <Button
          variant='outline'
          disabled={isSubmitting}
          onClick={() => void onSkip()}
        >
          Пропустить
        </Button>
      </div>
    </div>
  );
}
