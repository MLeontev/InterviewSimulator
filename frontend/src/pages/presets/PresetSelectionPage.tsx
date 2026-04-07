import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { toast } from 'sonner';
import {
  getInterviewPresetById,
  getInterviewPresets,
  type InterviewPresetDetails,
  type InterviewPresetListItem,
} from '../../features/interview-presets/api';
import { Button } from '../../shared/components/ui/Button';

export function PresetSelectionPage() {
  const navigate = useNavigate();

  const [presets, setPresets] = useState<InterviewPresetListItem[]>([]);
  const [selectedPresetId, setSelectedPresetId] = useState<string>('');
  const [selectedPreset, setSelectedPreset] =
    useState<InterviewPresetDetails | null>(null);

  const [isLoadingPresets, setIsLoadingPresets] = useState(true);
  const [isLoadingDetails, setIsLoadingDetails] = useState(false);
  const [isStarting, setIsStarting] = useState(false);

  const loadPresetDetails = async (presetId: string) => {
    setIsLoadingDetails(true);
    try {
      const data = await getInterviewPresetById(presetId);
      setSelectedPreset(data);
    } catch {
      toast.error('Не удалось загрузить детали пресета');
      setSelectedPreset(null);
    } finally {
      setIsLoadingDetails(false);
    }
  };

  useEffect(() => {
    const load = async () => {
      setIsLoadingPresets(true);
      try {
        const data = await getInterviewPresets();
        setPresets(data);

        if (data.length > 0) {
          const firstId = data[0].id;
          setSelectedPresetId(firstId);
          await loadPresetDetails(firstId);
        }
      } catch {
        toast.error('Не удалось загрузить список пресетов');
      } finally {
        setIsLoadingPresets(false);
      }
    };

    void load();
  }, []);

  const handlePresetChange = async (id: string) => {
    setSelectedPresetId(id);
    await loadPresetDetails(id);
  };

  const technologyNames =
    selectedPreset?.technologies?.map((x) => x.name) ?? [];

  //   const handleStartInterview = async () => {
  //     if (!selectedPresetId) return;

  //     try {
  //       setIsStarting(true);
  //       await createInterviewSession(selectedPresetId);
  //       toast.success('Сессия создана');
  //       navigate('/history');
  //     } catch {
  //       toast.error('Не удалось создать сессию');
  //     } finally {
  //       setIsStarting(false);
  //     }
  //   };

  return (
    <div className='max-w-2xl mx-auto py-12 px-8'>
      <h1 className='text-2xl font-semibold mb-2'>
        Выбор пресета собеседования
      </h1>
      <p className='text-lg mb-8'>
        Выберите готовый набор параметров для тренировочной сессии
      </p>
      {isLoadingPresets ? (
        <div className='text-gray-400 text-sm'>Загрузка пресетов...</div>
      ) : presets.length === 0 ? (
        <div className='text-gray-400 text-sm'>Пресеты пока не найдены</div>
      ) : (
        <>
          <label className='block text-sm mb-1'>Пресет</label>
          <select
            value={selectedPresetId}
            onChange={(e) => void handlePresetChange(e.target.value)}
            className='w-full border border-gray-300 rounded-xl px-3 py-3 text-gray-900 bg-white mb-4'
          >
            {presets.map((preset) => (
              <option key={preset.id} value={preset.id}>
                {preset.name}
              </option>
            ))}
          </select>
        </>
      )}

      <div className='border border-gray-300 rounded-xl p-3 mb-6 bg-white'>
        <h2 className='font-semibold text-gray-900 mb-3'>Технологии пресета</h2>
        {isLoadingDetails ? (
          <div className='text-gray-400 text-sm'>Загрузка...</div>
        ) : technologyNames.length === 0 ? (
          <div className='text-gray-400 text-sm'>Технологии не указаны</div>
        ) : (
          <div className='flex flex-wrap gap-2'>
            {technologyNames.map((name) => (
              <span
                key={name}
                className='px-3 py-1 rounded-lg bg-indigo-50 text-indigo-700 text-sm'
              >
                {name}
              </span>
            ))}
          </div>
        )}
      </div>

      <Button
        variant='primary'
        className='w-full py-3'
        disabled={!selectedPresetId || isStarting}
      >
        {isStarting ? 'Создание сессии...' : 'Начать собеседование'}
      </Button>
    </div>
  );
}
