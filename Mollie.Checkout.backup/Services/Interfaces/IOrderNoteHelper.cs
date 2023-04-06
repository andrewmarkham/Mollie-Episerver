using System;
using EPiServer.Commerce.Order;

namespace Mollie.Checkout.Services.Interfaces
{
    public interface IOrderNoteHelper
    {
        void AddNoteToOrder(IOrderGroup orderGroup, string title, string detail, Guid customerId);
    }
}
