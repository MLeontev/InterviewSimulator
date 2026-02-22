import { useEffect, useState } from 'react';
import { getInterviewPresets } from '../api/interviewPresetsApi';
import type { InterviewPresetListItem } from '../models/interviewPreset';
import PresetDetails from './PresetDetails';

function PresetsList() {
  const [presets, setPresets] = useState<InterviewPresetListItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [selectedId, setSelectedId] = useState<string | null>(null);

  useEffect(() => {
    getInterviewPresets()
      .then(setPresets)
      .catch(() => setError('Не удалось загрузить пресеты собеседований'))
      .finally(() => setLoading(false));
  }, []);

  if (loading) return <p>Загрузка...</p>;
  if (error) return <p>{error}</p>;

  return (
    <div className='presets-list'>
      <h2>Пресеты собеседований</h2>
      <ul>
        {presets.map((preset) => (
          <li
            key={preset.id}
            style={{ cursor: 'pointer', marginBottom: '0.5rem' }}
            onClick={() => setSelectedId(preset.id)}
          >
            {preset.name}
          </li>
        ))}
      </ul>
      {selectedId && <PresetDetails id={selectedId} />}
      {selectedId && (
        <button onClick={() => setSelectedId(null)}>Закрыть</button>
      )}
    </div>
  );
}

export default PresetsList;
