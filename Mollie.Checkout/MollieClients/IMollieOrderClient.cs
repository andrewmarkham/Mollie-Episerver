﻿using System.Net.Http;
using System.Threading.Tasks;
using Mollie.Api.Models.Order;

namespace Mollie.Checkout.MollieClients
{
    public interface IMollieOrderClient
    {
        Task<OrderResponse> CreateOrderAsync(
            OrderRequest orderRequest,
            string apiKey);

        Task<OrderResponse> GetOrderAsync(
            string orderId,
            string apiKey);
    }
}
