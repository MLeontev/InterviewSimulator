import { useState } from 'react';
import { toast } from 'sonner';
import {
  QuestionStatus,
  Verdict,
  type CurrentQuestion,
} from '../../features/interview/api';
import { Button } from '../../shared/components/ui/Button';
import { Tag } from '../../shared/components/ui/Tag';

interface Props {
  question: CurrentQuestion;
  isSubmitting: boolean;
  onSubmitDraftCode: (code: string) => void;
  onSubmitFinalCode: () => void;
  onSkip: () => void;
}

function verdictLabel(verdict: Verdict) {
  switch (verdict) {
    case Verdict.OK:
      return { text: 'OK', className: 'text-green-600' };
    case Verdict.WA:
      return { text: 'WA', className: 'text-red-600' };
    case Verdict.TLE:
      return { text: 'TLE', className: 'text-orange-600' };
    case Verdict.MLE:
      return { text: 'MLE', className: 'text-orange-600' };
    case Verdict.RE:
      return { text: 'RE', className: 'text-red-600' };
    case Verdict.CE:
      return { text: 'CE', className: 'text-red-600' };
    case Verdict.FailedSystem:
      return { text: 'SYSTEM ERROR', className: 'text-red-700' };
    default:
      return { text: '—', className: 'text-gray-500' };
  }
}

export function CodingQuestion({
  question,
  isSubmitting,
  onSubmitDraftCode,
  onSubmitFinalCode,
  onSkip,
}: Props) {
  const [code, setCode] = useState(question.answer ?? '');

  const normalizedCode = code.trim();
  const lastCheckedCode = (question.answer ?? '').trim();
  const isCodeSyncedWithLastRun =
    normalizedCode.length > 0 && normalizedCode === lastCheckedCode;

  const lastRunVerdict = verdictLabel(question.overallVerdict);

  const canRunDraft =
    !isSubmitting &&
    normalizedCode.length > 0 &&
    question.status !== QuestionStatus.EvaluatingCode;

  const canSubmitFinal =
    !isSubmitting &&
    question.status === QuestionStatus.EvaluatedCode &&
    isCodeSyncedWithLastRun;

  const canSkip =
    !isSubmitting && question.status !== QuestionStatus.EvaluatingCode;

  //   const hasAnyRunResult = () =>
  //     question.testCases.some((tc) => tc.verdict !== Verdict.None);

  const handleSubmitFinalClick = () => {
    if (isSubmitting) return;

    if (question.status !== QuestionStatus.EvaluatedCode) {
      toast.error('Сначала запусти проверку кода на тестах');
      return;
    }

    if (!isCodeSyncedWithLastRun) {
      toast.error('Код изменен после последней проверки. Нажми «Запустить».');
      return;
    }

    void onSubmitFinalCode();
  };

  const timeLimitText =
    question.timeLimitMs != null
      ? `${(question.timeLimitMs / 1000).toFixed(Number.isInteger(question.timeLimitMs / 1000) ? 0 : 1)} сек`
      : 'не задано';

  const memoryLimitText =
    question.memoryLimitMb != null
      ? `${question.memoryLimitMb} МБ`
      : 'не задано';

  return (
    <div className='py-10'>
      <div className='border-l-4 border-indigo-400 bg-gray-100 rounded-xl px-5 py-4 mb-3'>
        <h2 className='text-lg font-semibold text-gray-900 mb-2'>
          {question.title || 'Алгоритмическая задача'}
        </h2>
        <p className='whitespace-pre-wrap'>{question.text}</p>
      </div>

      <div className='flex flex-wrap gap-2 mb-3'>
        <Tag className='py-2'>Ограничение времени: {timeLimitText}</Tag>
        <Tag className='py-2'>Ограничение памяти: {memoryLimitText}</Tag>
      </div>

      {question.testCases.length > 0 && (
        <div className='mb-6'>
          <h3 className='text-lg font-semibold mb-3'>Примеры</h3>

          <div>
            {question.testCases.map((tc) => (
              <div key={tc.orderIndex} className='mb-3'>
                <div className='text-sm font-medium text-gray-700 mb-1'>
                  Пример {tc.orderIndex}
                </div>

                <div className='flex gap-3'>
                  <div className='flex-1 border border-gray-200 rounded-lg p-3 bg-gray-50'>
                    <div className='text-sm text-gray-500 mb-1'>Ввод</div>
                    <pre className='text-sm text-gray-900 whitespace-pre-wrap'>
                      {tc.input}
                    </pre>
                  </div>
                  <div className='flex-1 border border-gray-200 rounded-lg p-3 bg-gray-50'>
                    <div className='text-sm text-gray-500 mb-1'>Вывод</div>
                    <pre className='text-sm text-gray-900 whitespace-pre-wrap'>
                      {tc.expectedOutput}
                    </pre>
                  </div>
                </div>
              </div>
            ))}
          </div>
        </div>
      )}

      <textarea
        value={code}
        onChange={(e) => setCode(e.target.value)}
        placeholder='Введите решение...'
        className='w-full min-h-72 border border-gray-300 rounded-xl px-5 py-4 font-mono text-sm text-gray-900 placeholder:text-gray-400 focus:outline-none focus:ring-1 focus:ring-indigo-200 focus:border-indigo-300'
      />

      {question.status === QuestionStatus.EvaluatedCode &&
        !isCodeSyncedWithLastRun && (
          <div className='text-sm text-amber-700 mb-3'>
            Код изменен после последней проверки. Перед отправкой решения нажми
            «Запустить».
          </div>
        )}

      {lastRunVerdict.text !== '—' && (
        <div className='mb-4'>
          <span>Вердикт последнего запуска – </span>
          <span className={lastRunVerdict.className}>
            {lastRunVerdict.text}
          </span>
        </div>
      )}

      <div className='flex justify-between mt-1'>
        <div className='flex gap-3'>
          <Button
            variant='primary'
            disabled={!canSubmitFinal}
            onClick={handleSubmitFinalClick}
          >
            Отправить решение
          </Button>
          <Button
            variant='outline'
            disabled={!canSkip}
            onClick={() => void onSkip()}
          >
            Пропустить
          </Button>
        </div>

        <Button
          disabled={!canRunDraft}
          onClick={() => void onSubmitDraftCode(code.trim())}
        >
          {question.status === QuestionStatus.EvaluatingCode
            ? 'Проверка...'
            : 'Запустить'}
        </Button>
      </div>
    </div>
  );
}
