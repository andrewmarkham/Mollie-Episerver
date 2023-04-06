using Mollie.Api.Models.Payment;

namespace Mollie.Checkout
{
    public static class Constants
    {
        public const string MollieCheckoutSystemKeyword = "MollieCheckout";

        public const string PaymentLinkMollie = "PaymentLinkMollie";

        public const string MollieCheckoutMethodName = "Mollie Checkout";

        


        public static class Fields
        {
            public const string EnvironmentField = "Environment";

            public const string ApiKeyField = "ApiKey";

            public const string ProfileIDField = "ProfileID";

            public const string RedirectURLField = "RedirectURL";

            public const string UseOrdersApiField = "UseOrdersApi";

            public const string UseCreditcardComponentsField = "UseCreditcardComponents";

            public const string UseApplePayDirectIntegrationField = "UseApplePayDirectIntegration";

            public const string OrderExpiresInDaysField = "OrderExpiresInDays";

            public const string EnabledMolliePaymentMethods = "EnabledMolliePaymentMethods";

            public const string DisabledMolliePaymentMethods = "DisabledMolliePaymentMethods";
        }

        public static class OtherPaymentFields
        {
            public const string LanguageId = "LanguageIdPayment";

            public const string MolliePaymentId = "MolliePaymentIdPayment";

            public const string MolliePaymentStatus = "MolliePaymentStatusPayment";

            public const string MolliePaymentMethod = "MolliePaymentMethodPayment";

            public const string MolliePaymentFullResult = "MolliePaymentFullResultPayment";

            public const string MollieIssuer = "MollieIssuerPayment";

            public const string MollieToken = "MollieToken";
        }

        public static class Cart
        {
            public const string MollieOrderStatusField = "MollieOrderStatus";
        }

        public static class MollieOrder
        {
            public const string OrderIdMollie = "OrderIdMollie";
            public const string LanguageId = "LanguageIdOrder";
            public const string PaymentMethodIdeal = PaymentMethod.Ideal;
            public const string PaymentMethodCreditCard = PaymentMethod.CreditCard;
        }

        public static class MolliePaymentStatus
        {
            public const string Open = "open";

            public const string Paid = "paid";

            public const string Pending = "pending";

            public const string Authorized = "authorized";

            public const string Canceled = "canceled";

            public const string Expired = "expired";

            public const string Failed = "failed";
        }

        public static class MollieOrderStatus
        {
            public const string Created = "created";

            public const string Pending = "pending";

            public const string Paid = "paid";

            public const string Authorized = "authorized";

            public const string Shipping = "shipping";

            public const string Canceled = "canceled";

            public const string Expired = "expired";

            public const string Completed = "completed";
        }

        public static class Webhooks
        {
            public const string MolliePaymentsWebhookUrl = "api/molliepaymentswebhook";

            public const string MollieOrdersWebhookUrl = "api/mollieorderswebhook";
        }
    }
}
