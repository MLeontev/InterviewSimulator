import { api } from '../../shared/lib/axios';

export const InterviewStatus = {
  InProgress: 'InProgress',
  Finished: 'Finished',
  EvaluatingAi: 'EvaluatingAi',
  Evaluated: 'Evaluated',
} as const;

export type InterviewStatus =
  (typeof InterviewStatus)[keyof typeof InterviewStatus];

export const SessionVerdict = {
  None: 'None',
  Passed: 'Passed',
  Borderline: 'Borderline',
  Failed: 'Failed',
} as const;

export type SessionVerdict =
  (typeof SessionVerdict)[keyof typeof SessionVerdict];

export const QuestionType = {
  Theory: 'Theory',
  Coding: 'Coding',
} as const;
export type QuestionType = (typeof QuestionType)[keyof typeof QuestionType];

export const QuestionStatus = {
  NotStarted: 'NotStarted',
  InProgress: 'InProgress',
  Skipped: 'Skipped',
  EvaluatingCode: 'EvaluatingCode',
  EvaluatedCode: 'EvaluatedCode',
  Submitted: 'Submitted',
  EvaluatingAi: 'EvaluatingAi',
  EvaluatedAi: 'EvaluatedAi',
} as const;
export type QuestionStatus =
  (typeof QuestionStatus)[keyof typeof QuestionStatus];

export const Verdict = {
  None: 'None',
  FailedSystem: 'FailedSystem',
  OK: 'OK',
  WA: 'WA',
  TLE: 'TLE',
  MLE: 'MLE',
  RE: 'RE',
  CE: 'CE',
} as const;
export type Verdict = (typeof Verdict)[keyof typeof Verdict];

export const QuestionVerdict = {
  None: 'None',
  Correct: 'Correct',
  PartiallyCorrect: 'PartiallyCorrect',
  Incorrect: 'Incorrect',
} as const;
export type QuestionVerdict =
  (typeof QuestionVerdict)[keyof typeof QuestionVerdict];

export interface HistoryItem {
  sessionId: string;
  interviewPresetName: string;
  status: InterviewStatus;
  sessionVerdict: SessionVerdict;
  startedAt: string;
  plannedEndAt: string;
  finishedAt: string | null;
  totalQuestions: number;
  completedQuestions: number;
}

export interface CurrentSession {
  sessionId: string;
  status: InterviewStatus;
  startedAt: string;
  plannedEndAt: string;
  totalQuestions: number;
  answeredQuestions: number;
}

export interface CurrentQuestion {
  questionId: string;
  orderIndex: number;
  type: QuestionType;
  title: string;
  text: string;
  programmingLanguageCode: string | null;
  status: QuestionStatus;
  answer: string | null;
  timeLimitMs: number | null;
  memoryLimitMb: number | null;
  overallVerdict: Verdict;
  errorMessage: string | null;
  testCases: TestCase[];
}

export interface TestCase {
  orderIndex: number;
  input: string;
  expectedOutput: string;
  actualOutput: string | null;
  verdict: Verdict;
  executionTimeMs: number | null;
  memoryUsedMb: number | null;
  errorMessage: string | null;
}

export interface InterviewSessionReportQuestion {
  questionId: string;
  orderIndex: number;
  type: QuestionType;
  status: QuestionStatus;
  questionVerdict: QuestionVerdict;
  overallVerdict: Verdict;
  title: string;
  text: string;
  answer: string | null;
  programmingLanguageCode: string | null;
  startedAt: string | null;
  submittedAt: string | null;
  evaluatedAt: string | null;
  timeLimitMs: number | null;
  memoryLimitMb: number | null;
  errorMessage: string | null;
  aiScore: number | null;
  aiFeedback: string | null;
  passedTests: number | null;
  totalTests: number | null;
}

export interface InterviewSessionReport {
  sessionId: string;
  candidateId: string;
  interviewPresetId: string;
  interviewPresetName: string;
  status: InterviewStatus;
  sessionVerdict: SessionVerdict;
  startedAt: string;
  plannedEndAt: string;
  finishedAt: string | null;
  totalQuestions: number;
  answeredQuestions: number;
  averageQuestionAiScore: number | null;
  sessionSummary: string | null;
  sessionStrengths: string[];
  sessionWeaknesses: string[];
  sessionRecommendations: string[];
  questions: InterviewSessionReportQuestion[];
}

export const createSession = (presetId: string) =>
  api.post('/v1/interview-session', { interviewPresetId: presetId });

export const getCurrentSession = () =>
  api.get<CurrentSession>('/v1/interview-session').then((r) => r.data);

export const getCurrentQuestion = () =>
  api
    .get<CurrentQuestion>('/v1/interview-session/question')
    .then((r) => r.data);

export const startQuestion = () =>
  api.post('/v1/interview-session/question/start');

export const submitTheory = (answer: string) =>
  api.post('/v1/interview-session/question/submit-theory', { answer });

export const submitDraftCode = (code: string) =>
  api.post('/v1/interview-session/question/submit-draft-code', { code });

export const submitCode = () =>
  api.post('/v1/interview-session/question/submit-code');

export const skipQuestion = () =>
  api.post('/v1/interview-session/question/skip');

export const finishSession = () => api.post('/v1/interview-session/finish');

export const getHistory = () =>
  api.get<HistoryItem[]>('/v1/interview-sessions/history').then((r) => r.data);

export const getSessionReport = (
  sessionId: string,
  options?: { skipErrorToast?: boolean },
) =>
  api
    .get<InterviewSessionReport>(`/v1/interview-sessions/${sessionId}/report`, {
      skipErrorToast: options?.skipErrorToast,
    })
    .then((r) => r.data);
