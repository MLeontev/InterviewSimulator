import type {
  InterviewPresetDetails,
  InterviewPresetListItem,
} from '../models/interviewPreset';
import { api } from '../shared/lib/axios';

export const getInterviewPresets = async () => {
  const response = await api.get<InterviewPresetListItem[]>(
    '/v1/interview-presets',
  );
  return response.data;
};

export const getInterviewPresetById = async (id: string) => {
  const response = await api.get<InterviewPresetDetails>(
    `/v1/interview-presets/${id}`,
  );
  return response.data;
};
