using Interview.Domain;

namespace Interview.UseCases.Commands;

internal sealed class InterviewSessionBuilder
{
    private readonly InterviewSession _session = new();

    public InterviewSessionBuilder WithSessionId(Guid sessionId)
    {
        _session.Id = sessionId;
        return this;
    }

    public InterviewSessionBuilder ForCandidate(Guid candidateId)
    {
        _session.CandidateId = candidateId;
        return this;
    }

    public InterviewSessionBuilder WithPresetName(string presetName)
    {
        _session.InterviewPresetName = presetName;
        return this;
    }

    public InterviewSessionBuilder StartsAt(DateTime startUtc)
    {
        _session.StartTime = startUtc;
        return this;
    }

    public InterviewSessionBuilder WithDuration(TimeSpan duration)
    {
        _session.EndTime = _session.StartTime.Add(duration);
        return this;
    }

    public InterviewSessionBuilder WithQuestions(IReadOnlyList<InterviewQuestion> questions)
    {
        _session.Questions = questions.ToList();
        return this;
    }

    public InterviewSessionBuilder InProgress()
    {
        _session.Status = InterviewStatus.InProgress;
        return this;
    }

    public InterviewSession Build()
    {
        if (_session.Id == Guid.Empty) throw new InvalidOperationException("SessionId is required.");
        if (_session.CandidateId == Guid.Empty) throw new InvalidOperationException("CandidateId is required.");
        if (string.IsNullOrWhiteSpace(_session.InterviewPresetName)) throw new InvalidOperationException("PresetName is required.");
        if (_session.EndTime == default) _session.EndTime = _session.StartTime.AddHours(1);

        return _session;
    }
}