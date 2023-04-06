using EPiServer.Commerce.Order;
using EPiServer.Security;
using Mediachase.Commerce.Orders;
using Mediachase.Commerce.Security;
using System;
using EPiServer.ServiceLocation;
using Mollie.Checkout.Services.Interfaces;

namespace Mollie.Checkout.Helpers
{
    [ServiceConfiguration(typeof(IOrderNoteHelper))]
    public class OrderNoteHelper : IOrderNoteHelper
    {
        public void AddNoteToOrder(IOrderGroup orderGroup, string title, string detail, Guid customerId)
        {
            var note = orderGroup.CreateOrderNote();

            note.Type = OrderNoteTypes.System.ToString();
            note.CustomerId = customerId != Guid.Empty ?
                customerId :
                PrincipalInfo.CurrentPrincipal.GetContactId();
            note.Title = !string.IsNullOrEmpty(title) ?
                title :
                detail.Substring(0, Math.Min(detail.Length, 24)) + "...";
            note.Detail = detail;
            note.Created = DateTime.UtcNow;

            orderGroup.Notes.Add(note);
        }
    }
}
