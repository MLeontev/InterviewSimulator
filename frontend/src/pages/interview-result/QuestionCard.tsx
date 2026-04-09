import {
  QuestionType,
  QuestionVerdict,
  type InterviewSessionReportQuestion,
} from '../../features/interview/api';
import { Tag } from '../../shared/components/ui/Tag';

interface Props {
  question: InterviewSessionReportQuestion;
}

function verdictView(verdict: QuestionVerdict) {
  switch (verdict) {
    case QuestionVerdict.Correct:
      return { text: 'Верно', className: 'text-green-800 bg-green-100' };
    case QuestionVerdict.PartiallyCorrect:
      return {
        text: 'Частично верно',
        className: 'text-amber-700 bg-amber-50',
      };
    case QuestionVerdict.Incorrect:
      return { text: 'Неверно', className: 'text-red-700 bg-red-50' };
    default:
      return { text: 'Не решено', className: 'text-gray-600 bg-gray-100' };
  }
}

export function QuestionCard({ question }: Props) {
  const verdict = verdictView(question.questionVerdict);

  return (
    <section className='border border-gray-200 rounded-xl p-4 bg-white'>
      <div className='flex items-center justify-between mb-3'>
        <h3 className='text-lg font-semibold'>Задание {question.orderIndex}</h3>

        <div className='flex items-center gap-2'>
          <Tag>{question.type === QuestionType.Coding ? 'Код' : 'Теория'}</Tag>
          <span className={`px-3 py-1 rounded-lg text-sm ${verdict.className}`}>
            {verdict.text}
          </span>
          {question.aiScore != null && <Tag>AI: {question.aiScore}/10</Tag>}
        </div>
      </div>

      <div className='mb-3'>
        <div className='text-sm text-gray-500 mb-1'>Вопрос</div>
        <div className='bg-gray-100 border border-gray-200 rounded-lg p-3 whitespace-pre-wrap'>
          {question.text}
        </div>
      </div>

      <div className='mb-4'>
        <div className='text-sm text-gray-500 mb-1'>Ваш ответ</div>
        <div className='bg-gray-100 border border-gray-200 rounded-lg p-3 whitespace-pre-wrap'>
          {(question.answer ?? '').trim() || 'Ответ не был дан'}
        </div>
      </div>

      {question.type === QuestionType.Coding && question.totalTests != null && (
        <div className='mb-3 flex gap-2'>
          <Tag>
            Пройдено тестов: {question.passedTests ?? 0}/{question.totalTests}
          </Tag>
        </div>
      )}

      {question.aiFeedback && (
        <div className='mb-2'>
          <div className='text-sm text-gray-500 mb-1'>Обратная связь ИИ</div>
          <div className='border-l-4 border-indigo-400 rounded-lg bg-indigo-50 p-3 whitespace-pre-wrap'>
            {question.aiFeedback}
          </div>
        </div>
      )}

      {question.errorMessage && (
        <div className='text-sm text-red-700 bg-red-50 border border-red-200 rounded-lg p-3'>
          Во время автоматической оценки произошла техническая ошибка. Этот
          ответ мог быть оценен неполно.
        </div>
      )}
    </section>
  );
}
