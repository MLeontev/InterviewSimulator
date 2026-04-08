import { useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { QuestionStatus, QuestionType } from '../../features/interview/api';
import { useInterview } from '../../features/interview/hooks/useInterview';
import { Button } from '../../shared/components/ui/Button';
import { CodingQuestion } from './CodingQuestion';
import { InterviewHeader } from './InterviewHeader';
import { TheoryQuestion } from './TheoryQuestion';

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

  useEffect(() => {
    const questionId = question?.questionId;
    const status = question?.status;

    if (!questionId) return;
    if (status !== QuestionStatus.EvaluatingCode) return;

    const id = window.setInterval(() => {
      void reloadQuestion();
    }, 1000);

    return () => window.clearInterval(id);
  }, [question?.questionId, question?.status, reloadQuestion]);

  useEffect(() => {
    if (!question?.questionId) return;
    if (question.status !== QuestionStatus.NotStarted) return;

    void handleStartQuestion();
  }, [question?.questionId, question?.status, handleStartQuestion]);

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

      <div className='w-full max-w-4xl mx-auto'>
        {question!.type === QuestionType.Theory ? (
          <TheoryQuestion
            key={question.questionId}
            question={question!}
            isSubmitting={isSubmitting}
            onSubmit={handleSubmitTheory}
            onSkip={handleSkip}
          />
        ) : (
          <CodingQuestion
            key={question.questionId}
            question={question}
            isSubmitting={isSubmitting}
            onSubmitDraftCode={handleSubmitDraftCode}
            onSubmitFinalCode={handleSubmitCode}
            onSkip={handleSkip}
          />
        )}
      </div>
    </div>
  );
}
