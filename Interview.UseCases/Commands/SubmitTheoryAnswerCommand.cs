using FluentValidation;
using Framework.Domain;
using Interview.Domain;
using Interview.Infrastructure.Interfaces.DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Interview.UseCases.Commands;

public record SubmitTheoryAnswerCommand(Guid QuestionId, string Answer) : IRequest<Result>;

internal class SubmitTheoryAnswerCommandValidator : AbstractValidator<SubmitTheoryAnswerCommand>
{
    public SubmitTheoryAnswerCommandValidator()
    {
        RuleFor(x => x.Answer)
            .NotEmpty().WithMessage("Ответ не может быть пустым");
    }
}

internal class SubmitTheoryAnswerCommandHandler(IDbContext dbContext) : IRequestHandler<SubmitTheoryAnswerCommand, Result>
{
    public async Task<Result> Handle(SubmitTheoryAnswerCommand request, CancellationToken cancellationToken)
    {
        var question = await dbContext.InterviewQuestions
            .Include(x => x.InterviewSession)
            .FirstOrDefaultAsync(x => x.Id == request.QuestionId, cancellationToken);
        
        if (question is null)
            return Result.Failure(Error.NotFound("QUESTION_NOT_FOUND", "Задание не найдено"));
        
        if (question.Type != QuestionType.Theory)
            return Result.Failure(Error.Business("QUESTION_NOT_THEORY", "Задание не является теоретическим"));

        if (question.InterviewSession.Status != InterviewStatus.InProgress)
            return Result.Failure(Error.Business("SESSION_NOT_ACTIVE", "Сессия уже завершена"));
        
        if (question.Status >= QuestionStatus.Skipped)
            return Result.Failure(Error.Business("QUESTION_COMPLETED", "Задание уже решено или пропущено"));
        
        question.Answer = request.Answer.Trim();
        question.SubmittedAt = DateTime.UtcNow;
        question.EvaluatedAt = null;
        question.AiFeedbackJson = null;
        question.ErrorMessage = null;
        question.QuestionVerdict = QuestionVerdict.None;
        question.Status = QuestionStatus.Submitted;
        
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}