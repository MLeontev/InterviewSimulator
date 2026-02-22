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
