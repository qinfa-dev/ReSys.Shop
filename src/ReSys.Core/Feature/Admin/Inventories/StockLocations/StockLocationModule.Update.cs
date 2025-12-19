using MapsterMapper;

using ReSys.Core.Domain.Inventories.Locations;
using ReSys.Core.Domain.Location.Countries; 
using ReSys.Core.Domain.Location.States;    
using ReSys.Core.Feature.Common.Persistence.Interfaces;

namespace ReSys.Core.Feature.Admin.Inventories.StockLocations;

public static partial class StockLocationModule
{
    public static class Update
    {
        public record Request : Models.Parameter; 
        public record Result : Models.ListItem;

        public sealed record Command(Guid Id, Request Request) : ICommand<Result>;

        public sealed class CommandValidator : AbstractValidator<Command>
        {
            public CommandValidator()
            {
                RuleFor(expression: x => x.Id).NotEmpty();
                RuleFor(expression: x => x.Request).SetValidator(validator: new Models.ParameterValidator());
            }
        }

        public sealed class CommandHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper)
            : ICommandHandler<Command, Result>
        {
            public async Task<ErrorOr<Result>> Handle(Command command, CancellationToken ct)
            {
                var request = command.Request;
                var location = await unitOfWork.Context.Set<StockLocation>()
                    .FindAsync(keyValues: [command.Id], cancellationToken: ct);
                if (location == null)
                    return StockLocation.Errors.NotFound(id: command.Id);

                if (location.Name != request.Name)
                {
                    var uniqueNameCheck = await unitOfWork.Context.Set<StockLocation>()
                        .Where(predicate: m => m.Id != location.Id)
                        .CheckNameIsUniqueAsync<StockLocation, Guid>(name: request.Name, prefix: nameof(StockLocation),
                            cancellationToken: ct);
                    if (uniqueNameCheck.IsError)
                        return uniqueNameCheck.Errors;
                }

                // Validate Country and State if provided
                if (request.CountryId.HasValue)
                {
                    var country = await unitOfWork.Context.Set<Country>()
                        .FindAsync(keyValues: [request.CountryId.Value], cancellationToken: ct);
                    if (country == null)
                        return Error.NotFound("Country.NotFound",
                            $"Country with ID '{request.CountryId.Value}' was not found.");
                }

                if (request.StateId.HasValue)
                {
                    var state = await unitOfWork.Context.Set<State>()
                        .FindAsync(keyValues: [request.StateId.Value], cancellationToken: ct);
                    if (state == null)
                        return Error.NotFound("State.NotFound",
                            $"State with ID '{request.StateId.Value}' was not found.");
                }


                await unitOfWork.BeginTransactionAsync(cancellationToken: ct);

                var updateResult = location.Update(
                    name: request.Name,
                    presentation: request.Presentation,
                    active: request.Active,
                    address1: request.Address1,
                    address2: request.Address2,
                    city: request.City,
                    zipcode: request.ZipCode,
                    countryId: request.CountryId,
                    stateId: request.StateId,
                    phone: request.Phone,
                    company: request.Company,
                    publicMetadata: request.PublicMetadata,
                    privateMetadata: request.PrivateMetadata);

                if (updateResult.IsError) return updateResult.Errors;

                await unitOfWork.SaveChangesAsync(cancellationToken: ct);
                await unitOfWork.CommitTransactionAsync(cancellationToken: ct);

                return mapper.Map<Result>(source: updateResult.Value);
            }
        }
    }
}
