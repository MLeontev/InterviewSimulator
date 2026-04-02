using FluentValidation;
using MediatR;

namespace Framework.UseCases.Behaviors;

public class ValidationBehavior<TRequest, TResponse>(IValidator<TRequest>? validator = null) 
    : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request, 
        RequestHandlerDelegate<TResponse> next, 
        CancellationToken cancellationToken)
    {
        if (validator is not null)
            await validator.ValidateAndThrowAsync(request, cancellationToken);

        return await next(cancellationToken);
    }
}