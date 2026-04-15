using Backend.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using QuestionBank.ModuleContract;

namespace Backend.IntegrationTests.QuestionBank;

public sealed class QuestionBankApiTests : BaseIntegrationTest
{
    public QuestionBankApiTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetPresetAsync_ShouldReturnPreset_WhenPresetExists()
    {
        using var scope = CreateScope();
        var questionBankApi = scope.ServiceProvider.GetRequiredService<IQuestionBankApi>();

        var preset = await questionBankApi.GetPresetAsync(TestData.PythonMiddlePresetId);

        preset.Should().NotBeNull();
        preset.Id.Should().Be(TestData.PythonMiddlePresetId);
        preset.Name.Should().Be(TestData.PythonMiddlePresetName);
    }

    [Fact]
    public async Task GetPresetAsync_ShouldReturnNull_WhenPresetDoesNotExist()
    {
        using var scope = CreateScope();
        var questionBankApi = scope.ServiceProvider.GetRequiredService<IQuestionBankApi>();

        var preset = await questionBankApi.GetPresetAsync(Guid.NewGuid());

        preset.Should().BeNull();
    }

    [Fact]
    public async Task GetPresetDetailsAsync_ShouldReturnMappedPresetDetails_WhenPresetExists()
    {
        using var scope = CreateScope();
        var questionBankApi = scope.ServiceProvider.GetRequiredService<IQuestionBankApi>();

        var preset = await questionBankApi.GetPresetDetailsAsync(TestData.PythonMiddlePresetId);

        preset.Should().NotBeNull();
        preset.Id.Should().Be(TestData.PythonMiddlePresetId);
        preset.Name.Should().Be(TestData.PythonMiddlePresetName);
        preset.Technologies.Should().Contain("Python");
        preset.Competencies.Should().NotBeEmpty();
        preset.Competencies.Select(x => x.Weight).Should().BeInDescendingOrder();
        preset.Competencies.Sum(x => x.Weight).Should().BeApproximately(1.0, 0.0001);
    }

    [Fact]
    public async Task GetPresetDetailsAsync_ShouldReturnNull_WhenPresetDoesNotExist()
    {
        using var scope = CreateScope();
        var questionBankApi = scope.ServiceProvider.GetRequiredService<IQuestionBankApi>();

        var preset = await questionBankApi.GetPresetDetailsAsync(Guid.NewGuid());

        preset.Should().BeNull();
    }

    [Fact]
    public async Task GenerateInterviewQuestionsAsync_ShouldReturnOrderedTheoryAndCodingQuestions_WhenPresetExists()
    {
        using var scope = CreateScope();
        var questionBankApi = scope.ServiceProvider.GetRequiredService<IQuestionBankApi>();

        var questionSet = await questionBankApi.GenerateInterviewQuestionsAsync(
            TestData.PythonMiddlePresetId,
            theoryCount: 3,
            codingCount: 1);

        questionSet.PresetId.Should().Be(TestData.PythonMiddlePresetId);
        questionSet.Questions.Should().HaveCount(4);
        questionSet.Questions.Select(x => x.OrderIndex).Should().Equal(1, 2, 3, 4);
        questionSet.Questions.Count(x => x.Type == QuestionType.Theory).Should().Be(3);
        questionSet.Questions.Count(x => x.Type == QuestionType.Coding).Should().Be(1);

        questionSet.Questions
            .Where(x => x.Type == QuestionType.Theory)
            .Should()
            .OnlyContain(x =>
                x.ProgrammingLanguageCode == null &&
                x.TimeLimitMs == null &&
                x.MemoryLimitMb == null &&
                x.TestCases.Count == 0);

        var codingQuestion = questionSet.Questions.Single(x => x.Type == QuestionType.Coding);

        codingQuestion.ProgrammingLanguageCode.Should().NotBeNullOrWhiteSpace();
        codingQuestion.TimeLimitMs.Should().BePositive();
        codingQuestion.MemoryLimitMb.Should().BePositive();
        codingQuestion.TestCases.Should().NotBeEmpty();
        codingQuestion.TestCases.Select(x => x.OrderIndex)
            .Should()
            .Equal(Enumerable.Range(1, codingQuestion.TestCases.Count));
        codingQuestion.TestCases.Should().OnlyContain(x =>
            !string.IsNullOrWhiteSpace(x.Input) &&
            !string.IsNullOrWhiteSpace(x.ExpectedOutput));
    }

    [Fact]
    public async Task GenerateInterviewQuestionsAsync_ShouldThrow_WhenPresetDoesNotExist()
    {
        using var scope = CreateScope();
        var questionBankApi = scope.ServiceProvider.GetRequiredService<IQuestionBankApi>();

        var action = async () => await questionBankApi.GenerateInterviewQuestionsAsync(Guid.NewGuid(), 1, 1);

        await action.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("PRESET_NOT_FOUND*");
    }
}
