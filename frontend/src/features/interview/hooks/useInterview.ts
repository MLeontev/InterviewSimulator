import { useCallback, useEffect, useState } from 'react';
import {
  finishSession,
  getCurrentQuestion,
  getCurrentSession,
  skipQuestion,
  startQuestion,
  submitCode,
  submitDraftCode,
  submitTheory,
  type CurrentQuestion,
  type CurrentSession,
} from '../api';

export function useInterview() {
  const [session, setSession] = useState<CurrentSession | null>(null);
  const [question, setQuestion] = useState<CurrentQuestion | null>(null);

  const [isLoading, setIsLoading] = useState(true);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const loadQuestion = useCallback(async () => {
    const q = await getCurrentQuestion();
    setQuestion(q);
  }, []);

  const loadSession = useCallback(async () => {
    const s = await getCurrentSession();
    setSession(s);
  }, []);

  useEffect(() => {
    const init = async () => {
      setIsLoading(true);
      try {
        await Promise.all([loadQuestion(), loadSession()]);
      } finally {
        setIsLoading(false);
      }
    };
    init();
  }, [loadQuestion, loadSession]);

  const handleStartQuestion = async () => {
    await startQuestion();
    await loadQuestion();
  };

  const handleSubmitTheory = async (answer: string) => {
    setIsSubmitting(true);
    try {
      await submitTheory(answer);
      await Promise.all([loadSession(), loadQuestion()]);
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleSubmitDraftCode = async (code: string) => {
    setIsSubmitting(true);
    try {
      await submitDraftCode(code);
      await loadQuestion();
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleSubmitCode = async () => {
    setIsSubmitting(true);
    try {
      await submitCode();
      await Promise.all([loadQuestion(), loadSession()]);
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleSkip = async () => {
    setIsSubmitting(true);
    try {
      await skipQuestion();
      await Promise.all([loadSession(), loadQuestion()]);
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleFinish = async () => {
    await finishSession();
  };

  return {
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
    reloadQuestion: loadQuestion,
  };
}
