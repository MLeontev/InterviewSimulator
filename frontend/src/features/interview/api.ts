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

export const getHistory = () =>
  api.get<HistoryItem[]>('/v1/interview-sessions/history').then((r) => r.data);
