import { api } from '../../shared/lib/axios';

export interface InterviewPresetListItem {
  id: string;
  name: string;
}

export interface Technology {
  id: string;
  name: string;
  category: string;
}

export interface InterviewPresetDetails {
  id: string;
  name: string;
  grade: string;
  specialization: string;
  technologies: Technology[];
}

export const getInterviewPresets = () =>
  api
    .get<InterviewPresetListItem[]>('/v1/interview-presets')
    .then((r) => r.data);

export const getInterviewPresetById = (id: string) =>
  api
    .get<InterviewPresetDetails>(`/v1/interview-presets/${id}`)
    .then((r) => r.data);
