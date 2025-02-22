# Mollie Payments for Episerver
<hr/>

## Intro

The Mollie.Checkout Package helps with the Implementation of [Mollie](https://www.mollie.com/) for accepting Online Payments in a Episerver Commerce Website. 

## Packages

[Mollie.Checkout] is the Package for Integration of Mollie Checkout in a Episerver Commerce Website.  
[Mollie.Checkout.CommerceManager] contains the UserControl for Configuration of the Payment Method in Episerver Commerce Manager.


## How it works / Flow

- Customer adds a Product to the Shopping Cart and navigates to the Checkout Page.
- On the Checkout Page the Payment Option 'Mollie Checkout' is available, and the Customer selects this.
    - The Customer selects the preferred Payment Method (optional)
    - The Customer enters Creditcard Information (optional - if using Creditcard Components)
- The Customer clicks on 'PLACE ORDER'
    - Depending on the selected Option in Settings a Payment or Order is created using Mollie APIs.
        (an Url is returned to redirect the Customer to if needed)
    - The Customer is redirected to a Mollie Page to complete the Payment. (if needed)
- The Customer completes the Payment (if needed)
- (Background) Updates on the Payment or Order are sent to a Webhook by Mollie
    - When the Payment is successful an Order is created for the Cart in Episerver.
- The Customer is redirected to the 'Redirect Page' specified in the Mollie Configuration.
    - This could be the Order Confirmation Page
    - This also could be a Page that waits for the Payment to be processed, and redirects the User if a Payment is received.


## Integration in Foundation 

<details><summary>1. Install Packages</summary>
<p>

Install Package [Mollie.Checkout] in the __Foundation__ Project and the __Foundation.CommerceManager__ Project  
Install Package [Mollie.Checkout.CommerceManager] in the __Foundation.CommerceManager__ Project

</p>
</details>

<details><summary>2. Configure Payment in CommerceManager</summary>
<p>

When starting the Website for the first time after installing the Package the Mollie Checkout Payment Method should be added to the System for all Markets and Languages. To complete the Configuration of the Payment Method in Episerver Commerce Manager go to Administration >> Order System >> Payments >> _language_  

Select the Payment Method named 'Mollie Checkout'

Verify/Fill the following Fields:
#### On the Overview Tab:_
- Name 
- System Keyword: Type __MollieCheckout__ 
- Language
- Class Name: Select __Mollie.Checkout.MollieCheckoutGateway__
- Payment Class: Select __Mediachase.Commerce.Orders.OtherPayment__
- IsActive: Select __Yes__
#### On the Markets Tab:
- Select Markets to enable this Payment Method for.
#### On the Parameters tab: 
- Api Key
- Profile ID (Required when using Creditcard Components)
- Redirect URL 

</p>
</details>

<details><summary>3a. Create MollieCheckout Payment method (Minimal version example)</summary>
<p>

In this 'Minimal version' __Mollie Checkout__ is selectable as Payment Option the Checkout Page. When this Option is selected, the Customer is redirected to a series of Mollie Hosted Pages to select the Payment Method (iDeal, Creditcard etc.) and complete the Payment on placing the Order.


In __Foundation\\Features\\Checkout\\Payments__ Add a new Class __MollieCheckoutPaymentOption.cs__

```csharp
    public class MollieCheckoutPaymentOption : PaymentOptionBase
    {
        public override string SystemKeyword => "MollieCheckout";

        protected readonly LanguageService _languageService;

        public MollieCheckoutPaymentOption()
            : this(LocalizationService.Current, 
                ServiceLocator.Current.GetInstance<IOrderGroupFactory>(), 
                ServiceLocator.Current.GetInstance<ICurrentMarket>(), 
                ServiceLocator.Current.GetInstance<LanguageService>(), 
                ServiceLocator.Current.GetInstance<IPaymentService>())
        { }

        public MollieCheckoutPaymentOption(
            LocalizationService localizationService,
            IOrderGroupFactory orderGroupFactory,
            ICurrentMarket currentMarket,
            LanguageService languageService,
            IPaymentService paymentService)
        : base(localizationService, orderGroupFactory, currentMarket, languageService, paymentService)
        {
            _languageService = languageService;
        }

        public override bool ValidateData() => true;

        public override IPayment CreatePayment(decimal amount, IOrderGroup orderGroup)
        {
            var languageId = _languageService.GetCurrentLanguage().Name;

            var payment = orderGroup.CreatePayment(OrderGroupFactory);

            payment.PaymentType = PaymentType.Other;
            payment.PaymentMethodId = PaymentMethodId;
            payment.PaymentMethodName = SystemKeyword;
            payment.Amount = amount;
            payment.Status = PaymentStatus.Pending.ToString();
            payment.TransactionType = TransactionType.Sale.ToString();

            payment.Properties.Add(Mollie.Checkout.Constants.OtherPaymentFields.LanguageId, languageId);

            return payment;
        }
    }
``` 

In __Foundation\\Features\\Checkout__ Add a new View ___MollieCheckoutPaymentMethod.cshtml__

```html

@model  Foundation.Features.Checkout.Payments.MollieCheckoutPaymentOption

@Html.HiddenFor(model => model.PaymentMethodId)

<br />
<div class="row">
    <div class="col-12">
        <div class="alert alert-info square-box">
            Mollie Payment method
        </div>
    </div>
</div>

```

In __Foundation\\Infrastructure\\InitializeSite.cs__ add

```csharp
   _services.AddTransient<IPaymentMethod, MollieCheckoutPaymentOption>();
```

</p>
</details>

<details><summary>3b. Create MollieCheckout Payment method (Complete version example)</summary>
<p>

In this 'Complete Version' __Mollie Checkout__ is selectable as Payment Option the Checkout Page. When this Option is selected, the Customer can see the available Mollie Payment Methods and select one on the Checkout Page. If Creditcard Components is used, also Creditcard Information can be entered before completing the Order.


In __Foundation\\Features\\Checkout\\Payments__ Add a new Class __MollieCheckoutPaymentOption.cs__

```csharp
    public class MollieCheckoutPaymentOption : PaymentOptionBase
    {
        public override string SystemKeyword => "MollieCheckout";

        protected readonly LanguageService _languageService;
        protected readonly ICheckoutConfigurationLoader _checkoutConfigurationLoader;
        private readonly IPaymentMethodsService _paymentMethodsService;
        private readonly ICartService _cartService;
        private readonly ICurrentMarket _currentMarket;

        private string _subPaymentMethodId;
        
        public MollieCheckoutPaymentOption()
            : this(LocalizationService.Current,
                ServiceLocator.Current.GetInstance<IOrderGroupFactory>(),
                ServiceLocator.Current.GetInstance<ICurrentMarket>(),
                ServiceLocator.Current.GetInstance<LanguageService>(),
                ServiceLocator.Current.GetInstance<IPaymentService>(),
                ServiceLocator.Current.GetInstance<ICheckoutConfigurationLoader>(),
                ServiceLocator.Current.GetInstance<IPaymentMethodsService>(),
                ServiceLocator.Current.GetInstance<ICartService>())
        { }

        public MollieCheckoutPaymentOption(
            LocalizationService localizationService,
            IOrderGroupFactory orderGroupFactory,
            ICurrentMarket currentMarket,
            LanguageService languageService,
            IPaymentService paymentService,
            ICheckoutConfigurationLoader checkoutConfigurationLoader,
            IPaymentMethodsService paymentMethodsService,
            ICartService cartService)
            : base(localizationService, orderGroupFactory, currentMarket, languageService, paymentService)
        {
            _languageService = languageService;
            _checkoutConfigurationLoader = checkoutConfigurationLoader;
            _paymentMethodsService = paymentMethodsService;
            _cartService = cartService;
            _currentMarket = currentMarket;

            InitValues();
        }

        public IEnumerable<PaymentMethod> SubPaymentMethods { get; private set; }
        public CheckoutConfiguration Configuration { get; private set; }


        public void InitValues()
        {
            var languageId = _languageService.GetCurrentLanguage().Name;

            Configuration = _checkoutConfigurationLoader.GetConfiguration(languageId);

            var cart = _cartService.LoadCart(_cartService.DefaultCartName, false)?.Cart;

            if (cart != null)
            {
                var countryCode = GetCountryCode(cart);

                SubPaymentMethods = AsyncHelper.RunSync(() =>
                    _paymentMethodsService.LoadMethods(
                        cart.MarketId.Value,
                        languageId, 
                        cart.GetTotal(), 
                        countryCode));
            }
            else
            {
                SubPaymentMethods = AsyncHelper.RunSync(() =>
                    _paymentMethodsService.LoadMethods(
                        languageId));
            }
        }


        private string GetCountryCode(ICart cart)
        {
            if (cart.GetFirstForm().Payments.Any(p =>
                p.BillingAddress != null && !string.IsNullOrWhiteSpace(p.BillingAddress.CountryCode)))
            {
                return cart.GetFirstForm().Payments
                    .First(p => p.BillingAddress != null && !string.IsNullOrWhiteSpace(p.BillingAddress.CountryCode))
                    .BillingAddress.CountryCode;
            }

            if (cart.GetFirstForm().Shipments.Any(s =>
                s.ShippingAddress != null && !string.IsNullOrWhiteSpace(s.ShippingAddress.CountryCode)))
            {
                return cart.GetFirstForm().Shipments
                    .First(s => s.ShippingAddress != null && !string.IsNullOrWhiteSpace(s.ShippingAddress.CountryCode))
                    .ShippingAddress.CountryCode;
            }

            return _currentMarket.GetCurrentMarket().Countries.FirstOrDefault();
        }

        public override bool ValidateData() => true;

        public override IPayment CreatePayment(decimal amount, IOrderGroup orderGroup)
        {
            var languageId = _languageService.GetCurrentLanguage().Name;

            var payment = orderGroup.CreatePayment(OrderGroupFactory);

            payment.PaymentType = PaymentType.Other;
            payment.PaymentMethodId = PaymentMethodId;
            payment.PaymentMethodName = SystemKeyword;
            payment.Amount = amount;
            payment.Status = PaymentStatus.Pending.ToString();
            payment.TransactionType = TransactionType.Sale.ToString();

            payment.Properties.Add(Mollie.Checkout.Constants.OtherPaymentFields.LanguageId, languageId);
            
            if (!string.IsNullOrWhiteSpace(SubPaymentMethod))
            {
                payment.Properties.Add(Mollie.Checkout.Constants.OtherPaymentFields.MolliePaymentMethod, SubPaymentMethod);

                if (SubPaymentMethod.Equals(Mollie.Checkout.Constants.MollieOrder.PaymentMethodIdeal,   StringComparison.InvariantCultureIgnoreCase) && !string.IsNullOrWhiteSpace(ActiveIssuer))
                {
                    payment.Properties.Add(Mollie.Checkout.Constants.OtherPaymentFields.MollieIssuer, ActiveIssuer);
                }

                if (SubPaymentMethod.Equals(Mollie.Checkout.Constants.MollieOrder.PaymentMethodCreditCard, StringComparison.InvariantCultureIgnoreCase) && !string.IsNullOrWhiteSpace(CreditCardComponentToken))
                {
                    payment.Properties.Add(Mollie.Checkout.Constants.OtherPaymentFields.MollieToken, CreditCardComponentToken);
                }
            }

            return payment;
        }

        public string SubPaymentMethod 
        {
            get 
            {
                if (string.IsNullOrWhiteSpace(_subPaymentMethodId))
                {
                    var cartPayment = _cartService.LoadCart(_cartService.DefaultCartName, false)?.Cart?.GetFirstForm()?.Payments
                        .FirstOrDefault(p => p.PaymentMethodId == PaymentMethodId);

                    _subPaymentMethodId = cartPayment?.Properties[Mollie.Checkout.Constants.OtherPaymentFields.MolliePaymentMethod] as string;
                }
                return _subPaymentMethodId;
            }
            set => _subPaymentMethodId = value;
        }

        public string CreditCardComponentToken { get; set; }

        public string ActiveIssuer { get; set; }

        public string MollieDescription
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(SubPaymentMethod))
                {
                    return base.Description + " " + SubPaymentMethods.FirstOrDefault(x => x.Id.Equals(SubPaymentMethod,
                        StringComparison.InvariantCultureIgnoreCase))?.Description;
                }

                return base.Description;
            }
        }


        public string Locale => LanguageUtils.GetLocale(_languageService.GetCurrentLanguage().Name);
    }
``` 

In __Foundation\\Features\\Checkout__ Add a new View ___MollieCheckoutPaymentMethod.cshtml__

```html

@using Foundation.Features.Checkout.Payments

@model MollieCheckoutPaymentOption

<link href="~/Assets/css/mollie.checkout.css" rel="stylesheet" type="text/css" />

<div class="row">
    <div class="col-md-12 checkout-mollie">
        <div id="accordion" class="accordion molliePaymentMethods" style="padding: 20px;">

            @Html.HiddenFor(model => model.PaymentMethodId)

            @{
                var activeSubPaymentMethod = true;
            }

            @foreach (var method in Model.SubPaymentMethods)
            {
                if (!string.IsNullOrWhiteSpace(Model.SubPaymentMethod))
                {
                    activeSubPaymentMethod = method.Id.Equals(Model.SubPaymentMethod, StringComparison.InvariantCultureIgnoreCase);
                }

                <div class="card">
                    <div class="card-header" id="head-@method.Id">
                        <label class="checkbox">
                            <input type="radio" name="subPaymentMethod" value="@method.Id" @(activeSubPaymentMethod ? "checked" : string.Empty)
                                   data-toggle="collapse" data-target="#collapse-@method.Id" aria-expanded="true" aria-controls="collapse-@method.Id" />
                            <img src="@method.ImageSize1X" alt="@method.Description" />
                            @method.Description
                            <span class="checkmark"></span>
                        </label>
                    </div>
                </div>

                <div id="collapse-@method.Id" class="collapse @(activeSubPaymentMethod ? "show" : string.Empty)" aria-labelledby="head-@method.Id" data-parent="#accordion">

                    @if (method.Issuers != null)
                    {
                        <div class="card-body">
                            @RenderIssuersList(method.Issuers)
                        </div>
                    }

                    @if (method.Id.Equals("creditcard", StringComparison.InvariantCultureIgnoreCase) && Model.Configuration.UseCreditcardComponents)
                    {
                        <div class="card-body">
                            @RenderCreditCardComponents()
                        </div>
                    }

                </div>

                activeSubPaymentMethod = false;
            }
        </div>
    </div>
</div>


@helper RenderIssuersList(IEnumerable<Mollie.Api.Models.Issuer.IssuerResponse> issuers)
{
    var first = true;
    <ul id="issuersList" style="list-style: none;">
        @foreach (var issuer in issuers)
        {
            <li>
                <label class="checkbox">
                    @if (first)
                    {
                        @Html.RadioButtonFor(m => m.ActiveIssuer, issuer.Id, new { id = issuer.Id, @checked = "checked" })
                    }
                    else
                    {
                        @Html.RadioButtonFor(m => m.ActiveIssuer, issuer.Id, new { id = issuer.Id })
                    }
                    <img src="@issuer.Image.Size1x" alt="@issuer.Name" />
                    @issuer.Name
                    <span class="checkmark"></span>
                </label>
            </li>
            first = false;
        }
    </ul>
}


@helper RenderCreditCardComponents()
{
    @Html.HiddenFor(model => model.CreditCardComponentToken)

    <div class="wrapper">
        <div class="form-fields">
            <div class="form-group form-group--card-holder">
                <label class="label" for="card-holder">Card holder</label>
                <div id="card-holder"></div>
                <div id="card-holder-error" class="field-error" role="alert"></div>
                <input type="checkbox" id="card-holder-valid" style="display: none;" />
            </div>
            <div class="form-group form-group--card-number">
                <label class="label" for="card-number">Card number</label>
                <div id="card-number"></div>
                <div id="card-number-error" class="field-error" role="alert"></div>
                <input type="checkbox" id="card-number-valid" style="display: none;" />
            </div>
            <div class="form-group form-group--expiry-date">
                <label class="label" for="expiry-date">Expiry date</label>
                <div id="expiry-date"></div>
                <div id="expiry-date-error" class="field-error" role="alert"></div>
                <input type="checkbox" id="expiry-date-valid" style="display: none;" />
            </div>
            <div class="form-group form-group--verification-code">
                <label class="label" for="verification-code">Verification code</label>
                <div id="verification-code"></div>
                <div id="verification-code-error" class="field-error" role="alert"></div>
                <input type="checkbox" id="verification-code-valid" style="display: none;" />
            </div>
        </div>

        <div id="form-error" class="form-error" role="alert"></div>
    </div>
}

@if (Model.Configuration.UseCreditcardComponents)
{
    <script type="text/javascript">
        var mollieData = mollieData || {};

        mollieData.ProfileId = '@Model.Configuration.ProfileId';
        mollieData.Locale = '@Model.Locale';
        mollieData.Test = Boolean('@Model.Configuration.Environment.Equals("test", StringComparison.InvariantCultureIgnoreCase)');

    </script>

}



```

In __Foundation\\Assets\\js__ Add a new File __mollie.checkout.js__

```javascript

function MollieCheckout(profileId, locale, testmode) {

    this.mollie = Mollie(profileId, { locale: locale, testmode: testmode });

    this.initComponents = function () {
        var cardNumber = this.mollie.createComponent('cardNumber');
        cardNumber.mount('#card-number');

        var cardHolder = this.mollie.createComponent('cardHolder');
        cardHolder.mount('#card-holder');

        var expiryDate = this.mollie.createComponent('expiryDate');
        expiryDate.mount('#expiry-date');

        var verificationCode = this.mollie.createComponent('verificationCode');
        verificationCode.mount('#verification-code');

        var tokenField = document.querySelector('#CreditCardComponentToken');

        var cardNumberValid = document.querySelector('#card-number-valid');
        var cardNumberError = document.querySelector('#card-number-error');
        cardNumber.addEventListener('change', async event => {
            if (event.error && event.touched) {
                cardNumberError.textContent = event.error;
                cardNumberValid.checked = false;
                tokenField.value = '';
                return;
            } else if (event.touched && !event.error) {
                cardNumberError.textContent = '';
                cardNumberValid.checked = true;
                await this.tryGetToken();
            }
        });


        var cardHolderValid = document.querySelector('#card-holder-valid');
        var cardHolderError = document.querySelector('#card-holder-error');
        cardHolder.addEventListener('change', async event => {
            if (event.error && event.touched) {
                cardHolderError.textContent = event.error;
                cardHolderValid.checked = false;
                tokenField.value = '';
                return;
            } else if (event.touched && !event.error) {
                cardHolderError.textContent = '';
                cardHolderValid.checked = true;
                await this.tryGetToken();
            }
        });

        var expiryDateValid = document.querySelector('#expiry-date-valid');
        var expiryDateError = document.querySelector('#expiry-date-error');
        expiryDate.addEventListener('change', async event => {
            if (event.error && event.touched) {
                expiryDateError.textContent = event.error;
                expiryDateValid.checked = false;
                tokenField.value = '';
                return;
            } else if (event.touched && !event.error) {
                expiryDateError.textContent = '';
                expiryDateValid.checked = true;
                await this.tryGetToken();
            }
        });

        var verificationCodeValid = document.querySelector('#verification-code-valid');
        var verificationCodeError = document.querySelector('#verification-code-error');
        verificationCode.addEventListener('change', async event => {
            if (event.error && event.touched) {
                verificationCodeError.textContent = event.error;
                verificationCodeValid.checked = false;
                tokenField.value = '';
                return;
            } else if (event.touched && !event.error) {
                verificationCodeError.textContent = '';
                verificationCodeValid.checked = true;
                await this.tryGetToken();
            }
        });
    }


    this.tryGetToken = async function () {
        var a = document.querySelector('#card-holder-valid');
        var b = document.querySelector('#card-number-valid');
        var c = document.querySelector('#expiry-date-valid');
        var d = document.querySelector('#verification-code-valid');

        if (a.checked === false || b.checked === false || c.checked === false || d.checked === false) {
            return;
        }

        const { token, error } = await this.mollie.createToken();

        if (error) {
            alert(error.message);
            // Something wrong happened while creating the token. Handle this situation gracefully.
            return;
        }

        if (token) {
            var tokenField = document.querySelector('#CreditCardComponentToken');
            tokenField.value = token;
        }
    }
}

```


In __Foundation\\Infrastructure\\InitializeSite.cs__ add

```csharp
   _services.AddTransient<IPaymentMethod, MollieCheckoutPaymentOption>();
```

In __Foundation\\Features\\Shared\\Views\\_Layout.cshtml__ add (directly below main.min.js file ref)

```html
<script src="~/Assets/js/main.min.js"></script>

<script src="https://js.mollie.com/v1/mollie.js"></script>
<script src="~/Assets/js/mollie.checkout.js"></script>
<script type="text/javascript">

    if (mollieData !== undefined && mollieData !== null) {
        var mc = new MollieCheckout(mollieData.ProfileId, mollieData.Locale, mollieData.Test);
        mc.initComponents();
    }

</script>

```

</p>
</details>


<details><summary>4. Handle redirect to Mollie</summary>
<p>

After the processing of the Payments by Episerver, the Mollie Checkout Payment will return a PaymentProcessingResult with IsSuccessful = true and a RedirectUrl.
In Foundation the User needs to be redirected to this Redirect Url (Url to the Mollie Checkout Page)

See the [CheckoutService.cs](https://dev.azure.com/arlanet/Mollie/_git/Mollie?path=%2FFoundation%2FFeatures%2FCheckout%2FServices%2FCheckoutService.cs) for an Example of this on line 208

```csharp

    // Do we need a redirect to payment provider
    if (processPayments.Any(x => x.IsSuccessful && !string.IsNullOrWhiteSpace(x.RedirectUrl)))
    {
        var payment = processPayments.First(x => x.IsSuccessful && !string.IsNullOrWhiteSpace(x.RedirectUrl));
        HttpContext.Current.Response.Redirect(payment.RedirectUrl, true);
        return null;
    }

```

</p>
</details>


<details><summary>5. Implement IMollieCheckoutService</summary>
<p>

When a Payment Status Update (paid, cancelled, etc..) is received from Mollie this Service is called. 
Implement Logic here to convert the Cart to an Order when the Payment was successful.

See an sample Implementation here:

```csharp

    [ServiceConfiguration(typeof(IMollieCheckoutService))]
    public class MollieCheckoutService : IMollieCheckoutService
    {
        private readonly IOrderGroupCalculator _orderGroupCalculator;
        private readonly IOrderRepository _orderRepository;
        private readonly IOrderNoteHelper _orderNoteHelper;

        public MollieCheckoutService(IOrderGroupCalculator orderGroupCalculator, IOrderRepository orderRepository,
            IOrderNoteHelper orderNoteHelper)
        {
            _orderGroupCalculator = orderGroupCalculator;
            _orderRepository = orderRepository;
            _orderNoteHelper = orderNoteHelper;
        }

        public void HandlePaymentSuccess(IOrderGroup orderGroup, IPayment payment)
        {
            var cart = orderGroup as ICart;

            if (cart != null)
            {
                var processedPayments = orderGroup.GetFirstForm().Payments
                    .Where(x => x.Status.Equals(PaymentStatus.Processed.ToString()));

                var totalProcessedAmount = processedPayments.Sum(x => x.Amount);

                // If the Cart is completely paid
                if (totalProcessedAmount == orderGroup.GetTotal(_orderGroupCalculator).Amount)
                {
                    // Create order
                    var orderReference = (cart.Properties["IsUsePaymentPlan"] != null &&
                        cart.Properties["IsUsePaymentPlan"].Equals(true)) ?
                            SaveAsPaymentPlan(cart) :
                            _orderRepository.SaveAsPurchaseOrder(cart);

                    var purchaseOrder = _orderRepository.Load<IPurchaseOrder>(orderReference.OrderGroupId);

                    var message = "Converted to order by HandlePaymentSuccess initiated by webhook";
                    _orderNoteHelper.AddNoteToOrder(purchaseOrder, message, message, Guid.Empty);

                    purchaseOrder.Properties[MollieOrder.OrderIdMollie] = cart.Properties[MollieOrder.OrderIdMollie];
                    purchaseOrder.Properties[PaymentLinkMollie] = cart.Properties[PaymentLinkMollie];
                    purchaseOrder.Properties[MollieOrder.LanguageId] = payment.Properties[OtherPaymentFields.LanguageId];

                    _orderRepository.Save(purchaseOrder);

                    // Delete cart
                    _orderRepository.Delete(cart.OrderLink);

                    cart.AdjustInventoryOrRemoveLineItems((item, validationIssue) => { });
                }
            }
        }

        public void HandleOrderStatusUpdate(
            IOrderGroup orderGroup, 
            string mollieStatus, 
            string mollieOrderId)
        {
            if(orderGroup == null)
            {
                throw new ArgumentNullException(nameof(orderGroup));
            }

            if(string.IsNullOrEmpty(mollieStatus))
            {
                throw new ArgumentException(nameof(mollieStatus));
            }

            if (string.IsNullOrEmpty(mollieOrderId))
            {
                throw new ArgumentException(nameof(mollieOrderId));
            }

            switch (mollieStatus)
            {
                case MollieOrderStatus.Created:
                case MollieOrderStatus.Pending:
                case MollieOrderStatus.Authorized:
                case MollieOrderStatus.Paid:
                case MollieOrderStatus.Shipping:
                    orderGroup.OrderStatus = OrderStatus.InProgress;
                    break;
                case MollieOrderStatus.Completed:
                    orderGroup.OrderStatus = OrderStatus.Completed;
                    break;
                case MollieOrderStatus.Canceled:
                case MollieOrderStatus.Expired:
                    orderGroup.OrderStatus = OrderStatus.Cancelled;
                    break;
                default:
                    break;
            }

            orderGroup.Properties[Constants.Cart.MollieOrderStatusField] = mollieStatus;
            orderGroup.Properties[MollieOrder.OrderIdMollie] = mollieOrderId;

            _orderRepository.Save(orderGroup);
        }

        public void HandlePaymentFailure(IOrderGroup orderGroup, IPayment payment)
        {
            // Do nothing, leave cart as is with failed payment.
        }

        private OrderReference SaveAsPaymentPlan(ICart cart)
        {
            throw new NotImplementedException("");
        }
    }

```

</p>
</details>


<details><summary>6. Change the Foundation Order-Confirmation controller</summary>
<p>

Change the Foundation Order Confirmation Page to accept the Order Trackingnumber instead of the Order Id. \
See a sample of the changed OrderConfirmationController here:

```csharp

    public class OrderConfirmationController : OrderConfirmationControllerBase<OrderConfirmationPage>
    {
        private readonly ICampaignService _campaignService;
        private readonly IPurchaseOrderRepository _purchaseOrderRepository;
        public OrderConfirmationController(
            ICampaignService campaignService,
            ConfirmationService confirmationService,
            IAddressBookService addressBookService,
            IOrderGroupCalculator orderGroupCalculator,
            UrlResolver urlResolver, 
            ICustomerService customerService,
            IPurchaseOrderRepository purchaseOrderRepository) :
            base(confirmationService, addressBookService, orderGroupCalculator, urlResolver, customerService)
        {
            _campaignService = campaignService;
            _purchaseOrderRepository = purchaseOrderRepository;
        }
        public ActionResult Index(OrderConfirmationPage currentPage, string notificationMessage, string orderNumber)
        {
            IPurchaseOrder order = null;
            if (PageEditing.PageIsInEditMode)
            {
                order = _confirmationService.CreateFakePurchaseOrder();
            }
            else if (!string.IsNullOrWhiteSpace(orderNumber))
            {
                if (int.TryParse(orderNumber, out int orderId))
                {
                    order = _confirmationService.GetOrder(orderId);
                }
                else
                {
                    order = _purchaseOrderRepository.Load(orderNumber);
                }
            }

            if (order != null && order.CustomerId == _customerService.CurrentContactId)
            {
                var viewModel = CreateViewModel(currentPage, order);
                viewModel.NotificationMessage = notificationMessage;

                _campaignService.UpdateLastOrderDate();
                _campaignService.UpdatePoint(decimal.ToInt16(viewModel.SubTotal.Amount));

                return View(viewModel);
            }

            return Redirect(Url.ContentUrl(ContentReference.StartPage));
        }
    }

```

</p>
</details>


<details><summary>7. Add a payment confirmation view</summary>
<p>
    
On the Foundation order-confirmation page a view is shown with some information about the payments for order.

Add a new View ___MollieCheckoutConfirmation.cshtml__ to __Foundation\\Features\\MyAccount\\OrderConfirmation__
```html

@model EPiServer.Commerce.Order.IPayment 

<div>
    <h4>@Html.Translate("/OrderConfirmation/PaymentDetails")</h4>
    <p>
        @{ 
            var method = Model.Properties[Mollie.Checkout.Constants.OtherPaymentFields.MolliePaymentMethod] as string;
        }
        Paid by:  @(method ?? "Mollie Checkout")
        
    </p>
</div>

```

</p>
</details>