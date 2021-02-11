﻿using EPiServer.Commerce.Order;

namespace Mollie.Checkout.Services
{
    public interface IMollieCheckoutService
    {
        void HandlePaymentSuccess(IOrderGroup orderGroup, IPayment payment);

        void HandlePaymentFailure(IOrderGroup orderGroup, IPayment payment);

        void UpdateOrder(IOrderGroup orderGroup, string mollieStatus, string mollieOrderId);
    }    
}
