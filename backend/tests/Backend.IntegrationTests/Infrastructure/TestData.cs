namespace Backend.IntegrationTests.Infrastructure;

internal static class TestData
{
    public static readonly Guid PythonMiddlePresetId = Guid.Parse("00000005-0000-0000-0000-000000000001");
    public const string PythonMiddlePresetName = "Python-разработчик (Middle)";
    public static readonly Guid CSharpJuniorPresetId = Guid.Parse("00000005-0000-0000-0000-000000000002");
    public const string CSharpJuniorPresetName = ".NET backend-разработчик (Junior)";

    public static string UniqueEmail()
    {
        return $"test-{Guid.NewGuid():N}@example.com";
    }

    public static string DefaultPassword()
    {
        return "Qwerty123!";
    }
}
