using MediatR;

using ReSys.Core.Domain.Orders;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

namespace ReSys.Core.Feature.Common.Security.Authentication
{
    public class MergeAdhocCartOnLoginHandler(IUnitOfWork unitOfWork) : INotificationHandler<UserLoggedInNotification>
    {
        public async Task Handle(UserLoggedInNotification notification, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(notification.AdhocId))
            {
                return;
            }

            // Find the anonymous user's cart
            var adhocCart = await unitOfWork.Context.Set<Order>()
                .Include(o => o.LineItems)
                .FirstOrDefaultAsync(o => o.AdhocCustomerId == notification.AdhocId && o.State == Order.Order.OrderState.Cart, cancellationToken);

            if (adhocCart is null)
            {
                return;
            }

            // Find the authenticated user's cart
            var userCart = await unitOfWork.Context.Set<Order>()
                .Include(o => o.LineItems)
                .FirstOrDefaultAsync(o => o.UserId == notification.UserId && o.State == Order.Order.OrderState.Cart, cancellationToken);

            if (userCart is null)
            {
                // If the user has no cart, simply assign the ad-hoc cart to them.
                adhocCart.AssignToUser(notification.UserId);
            }
            else
            {
                // If the user already has a cart, merge the items.
                foreach (var lineItem in adhocCart.LineItems.ToList())
                {
                    userCart.AddLineItem(lineItem.Variant, lineItem.Quantity);
                }
                
                // Remove the ad-hoc cart
                unitOfWork.Context.Set<Order>().Remove(adhocCart);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
