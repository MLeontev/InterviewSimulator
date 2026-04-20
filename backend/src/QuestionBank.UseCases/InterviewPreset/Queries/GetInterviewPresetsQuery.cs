using MediatR;
using Microsoft.EntityFrameworkCore;
using QuestionBank.Infrastructure.Interfaces.DataAccess;

namespace QuestionBank.UseCases.InterviewPreset.Queries;

public record InterviewPresetListItemDto(Guid Id, string Name);

public record GetInterviewPresetsQuery : IRequest<IReadOnlyList<InterviewPresetListItemDto>>;

internal class GetInterviewPresetsQueryHandler(IDbContext dbContext) : IRequestHandler<GetInterviewPresetsQuery, IReadOnlyList<InterviewPresetListItemDto>>
{
    public async Task<IReadOnlyList<InterviewPresetListItemDto>> Handle(GetInterviewPresetsQuery request, CancellationToken cancellationToken)
    {
        return await dbContext.InterviewPresets
            .AsNoTracking()
            .Select(x => new InterviewPresetListItemDto(x.Id, x.Name))
            .ToListAsync(cancellationToken);
    }
}
