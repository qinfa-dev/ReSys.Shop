using FluentValidation.Results;

using MediatR;

namespace ReSys.Core.Feature.Common.Behaviors;

/// <summary>
/// Pipeline behavior that runs FluentValidation validators before handling a request.
/// If validation fails, returns a list of <see cref="DbLoggerCategory.Model.Validation"/> instead of executing the handler.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse>(IValidator<TRequest>? validator = null)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : IErrorOr
{
    public async Task<TResponse> Handle(
        TRequest? request,
        RequestHandlerDelegate<TResponse>? next,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(argument: request);
        ArgumentNullException.ThrowIfNull(argument: next);

        if (validator is null)
        {
            return await next(t: cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
        }

        ValidationResult validationResult =
            await validator.ValidateAsync(instance: request,
                cancellation: cancellationToken).ConfigureAwait(continueOnCapturedContext: false);

        if (validationResult.IsValid)
        {
            return await next(t: cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
        }

        List<Error> errors = validationResult.Errors
            .ConvertAll(converter: error => Error.Validation(code: error.ErrorCode,
                description: error.ErrorMessage));

        return (dynamic)errors;
    }
}