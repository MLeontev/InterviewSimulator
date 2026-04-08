import { useNavigate } from 'react-router-dom';
import { useInterview } from '../../features/interview/hooks/useInterview';
import { Button } from '../../shared/components/ui/Button';
import { InterviewHeader } from './InterviewHeader';

export function InterviewPage() {
  const navigate = useNavigate();
  const {
    session,
    question,
    isLoading,
    isSubmitting,
    handleStartQuestion,
    handleSubmitTheory,
    handleSubmitDraftCode,
    handleSubmitCode,
    handleSkip,
    handleFinish,
    reloadQuestion,
  } = useInterview();

  if (isLoading) {
    return (
      <div className='flex items-center justify-center min-h-screen'>
        <span className='text-gray-400 text-sm'>Загрузка...</span>
      </div>
    );
  }

  if (!session || !question) {
    return (
      <div className='flex flex-col items-center justify-center min-h-screen gap-4'>
        <div className='text-gray-400 text-sm'>Сессия не найдена</div>
        <Button variant='outline' onClick={() => navigate('/history')}>
          К истории
        </Button>
      </div>
    );
  }

  return (
    <div className='min-h-screen bg-white flex flex-col'>
      <InterviewHeader
        currentIndex={question.orderIndex}
        totalQuestions={session.totalQuestions}
        plannedEndAt={session.plannedEndAt}
        onFinish={handleFinish}
      />
    </div>
  );
}
