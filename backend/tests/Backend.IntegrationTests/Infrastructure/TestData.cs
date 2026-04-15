namespace Backend.IntegrationTests.Infrastructure;

internal static class TestData
{
    public static string UniqueEmail()
    {
        return $"test-{Guid.NewGuid():N}@example.com";
    }

    public static string DefaultPassword()
    {
        return "Qwerty123!";
    }
}
