import type { AxiosError } from 'axios';
import { useEffect, useState } from 'react';
import { getInterviewPresetById } from '../api/interviewPresetsApi';
import type { InterviewPresetDetails } from '../models/interviewPreset';

interface PresetDetailsProps {
  id: string;
}

function PresetDetails({ id }: PresetDetailsProps) {
  const [preset, setPreset] = useState<InterviewPresetDetails | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    getInterviewPresetById(id)
      .then(setPreset)
      .catch((err: unknown) => {
        const axiosError = err as AxiosError<{
          code: string;
          description: string;
        }>;
        setError(
          axiosError.response?.data?.description ??
            'Не удалось загрузить пресет',
        );
      })
      .finally(() => setLoading(false));
  }, [id]);

  if (loading) return <p>Загрузка...</p>;
  if (error) return <p>{error}</p>;
  if (!preset) return <p>Пресет не найден</p>;

  return (
    <div>
      <h3>{preset.name}</h3>
      <p>Уровень: {preset.grade}</p>
      <p>Специализация: {preset.specialization}</p>
      <h4>Стек технологий:</h4>
      <ul>
        {preset.technologies.map((tech) => (
          <li key={tech.id}>
            {tech.name} ({tech.category})
          </li>
        ))}
      </ul>
    </div>
  );
}

export default PresetDetails;
