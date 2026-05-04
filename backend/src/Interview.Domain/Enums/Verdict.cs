namespace Interview.Domain.Enums;

/// <summary>
/// Вердикт автоматической проверки программного кода
/// </summary>
public enum Verdict
{
    None,
    FailedSystem,
    OK,
    CE,
    RE,
    TLE,
    MLE,
    WA
}
