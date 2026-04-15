using System.Net;
using System.Net.Http.Json;
using Backend.IntegrationTests.Infrastructure;
using FluentAssertions;

namespace Backend.IntegrationTests.QuestionBank;

public sealed class InterviewPresetsEndpointTests : BaseIntegrationTest
{
    public InterviewPresetsEndpointTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetAll_ShouldReturnSeededInterviewPresets()
    {
        using var client = CreateApiClient();

        using var response = await client.GetAsync("/api/v1/interview-presets");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<IReadOnlyList<InterviewPresetListItemResponse>>();

        payload.Should().NotBeNull();
        payload.Should().ContainSingle(x =>
            x.Id == TestData.PythonMiddlePresetId &&
            x.Name == TestData.PythonMiddlePresetName);
    }

    [Fact]
    public async Task GetById_ShouldReturnPresetDetails_WhenPresetExists()
    {
        using var client = CreateApiClient();

        using var response = await client.GetAsync($"/api/v1/interview-presets/{TestData.PythonMiddlePresetId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<InterviewPresetResponse>();

        payload.Should().NotBeNull();
        payload!.Id.Should().Be(TestData.PythonMiddlePresetId);
        payload.Name.Should().Be(TestData.PythonMiddlePresetName);
        payload.Grade.Should().Be("Middle");
        payload.Specialization.Should().Be("Algorithms and Data Structures");
        payload.Technologies.Should().ContainSingle(x =>
            x.Name == "Python" &&
            x.Category == "ProgrammingLanguage");
    }

    [Fact]
    public async Task GetById_ShouldReturnNotFound_WhenPresetDoesNotExist()
    {
        using var client = CreateApiClient();

        using var response = await client.GetAsync($"/api/v1/interview-presets/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();

        payload.Should().NotBeNull();
        payload!.Code.Should().Be("PRESET_NOT_FOUND");
        payload.Description.Should().Be("Пресет интервью не найден");
    }

    private sealed record InterviewPresetListItemResponse(Guid Id, string Name);

    private sealed record InterviewPresetResponse(
        Guid Id,
        string Name,
        string Grade,
        string Specialization,
        IReadOnlyList<TechnologyResponse> Technologies);

    private sealed record TechnologyResponse(
        Guid Id,
        string Name,
        string Category);

    private sealed record ErrorResponse(string Code, string Description);
}
