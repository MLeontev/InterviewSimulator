import { useCallback, useEffect, useState } from 'react';
import type { ApiError } from '../../../shared/lib/apiError';
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
  const [lastKnownSessionId, setLastKnownSessionId] = useState<string | null>(
    null,
  );

  const [session, setSession] = useState<CurrentSession | null>(null);
  const [question, setQuestion] = useState<CurrentQuestion | null>(null);

  const [isLoading, setIsLoading] = useState(true);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const loadQuestion = useCallback(async () => {
    try {
      const q = await getCurrentQuestion({ skipErrorToast: true });
      setQuestion(q);
    } catch (e) {
      const apiError = e as ApiError;
      if (
        apiError.code === 'QUESTION_NOT_FOUND' ||
        apiError.code === 'SESSION_NOT_FOUND'
      ) {
        setQuestion(null);
        return;
      }
      throw e;
    }
  }, []);

  const loadSession = useCallback(async () => {
    try {
      const s = await getCurrentSession({ skipErrorToast: true });
      setSession(s);
      setLastKnownSessionId(s.sessionId);
    } catch (e) {
      const apiError = e as ApiError;
      if (apiError.code === 'SESSION_NOT_FOUND') {
        setSession(null);
        setQuestion(null);
        return;
      }
      throw e;
    }
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

  const handleStartQuestion = useCallback(async () => {
    await startQuestion();
    await loadQuestion();
  }, [loadQuestion]);

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

  const handleFinish = useCallback(async () => {
    try {
      await finishSession();
    } catch (e) {
      const apiError = e as ApiError;
      if (apiError.code === 'SESSION_NOT_FOUND') {
        setSession(null);
        setQuestion(null);
        return;
      }
      throw e;
    }
  }, []);

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
    lastKnownSessionId,
  };
}
