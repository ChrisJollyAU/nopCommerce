using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Plugin.Payments.BrainTree.Models;
using Nop.Web.Framework.Components;

using Nop.Core.Domain.Affiliates;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Discounts;
using Nop.Core.Domain.Localization;
using Nop.Core.Domain.Orders;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Affiliates;
using Nop.Services.Tax;
using Nop.Services.Catalog;
using Nop.Core.Domain.Shipping;
using Nop.Core.Domain.Tax;
using Nop.Services.Common;
using Nop.Services.Discounts;
using Nop.Services.Payments;
using Nop.Services.Orders;
using Nop.Services.Localization;
using System.Threading.Tasks;

namespace Nop.Plugin.Payments.BrainTree.Components
{
    [ViewComponent(Name = "PaymentBrainTree")]
    public class PaymentBrainTreeViewComponent : NopViewComponent
    {
        private readonly BrainTreePaymentSettings _brainTreePaymentSettings;

        private readonly IAffiliateService _affiliateService;
        private readonly ICheckoutAttributeFormatter _checkoutAttributeFormatter;
        private readonly ICountryService _countryService;
        private readonly ICurrencyService _currencyService;
        private readonly CurrencySettings _currencySettings;
        private readonly ICustomerService _customerService;
        private readonly ILanguageService _languageService;
        private readonly OrderSettings _orderSettings;
        private readonly IOrderTotalCalculationService _orderTotalCalculationService;
        private readonly IPaymentService _paymentService;
        private readonly IPriceFormatter _priceFormatter;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly IProductService _productService;
        private readonly ShippingSettings _shippingSettings;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IStateProvinceService _stateProvinceService;
        private readonly IStoreContext _storeContext;
        private readonly IAddressService _addressService;
        private readonly IDiscountService _discountService;
        private readonly ITaxService _taxService;
        private readonly TaxSettings _taxSettings;
        private readonly IWorkContext _workContext;
        private readonly ILocalizationService _localizationService;
        public PaymentBrainTreeViewComponent(BrainTreePaymentSettings brainTreePaymentSettings,
            IAffiliateService affiliateService,
        ICheckoutAttributeFormatter checkoutAttributeFormatter,
        ICountryService countryService,
        ICurrencyService currencyService,
        CurrencySettings currencySettings,
        ICustomerService customerService,
        ILanguageService languageService,
        OrderSettings orderSettings,
        IOrderTotalCalculationService orderTotalCalculationService,
        IPaymentService paymentService,
        IPriceFormatter priceFormatter,
        IProductAttributeParser productAttributeParser,
        IProductService productService,
        ShippingSettings shippingSettings,
        IShoppingCartService shoppingCartService,
        IStateProvinceService stateProvinceService,
        IStoreContext storeContext,
        ITaxService taxService,
        TaxSettings taxSettings,
            IGenericAttributeService genericAttributeService,
            IAddressService addressService,
            IDiscountService discountService,
        IWorkContext workContext,
        ILocalizationService localizationService
            )
        {
            _brainTreePaymentSettings = brainTreePaymentSettings;
            _workContext = workContext;
            _paymentService = paymentService;
            _localizationService = localizationService;
            _shoppingCartService = shoppingCartService;
            _customerService = customerService;
            _storeContext = storeContext;
            _currencySettings = currencySettings;
            _affiliateService = affiliateService;
            _languageService = languageService;
            _currencyService = currencyService;
            _orderSettings = orderSettings;
            _taxSettings = taxSettings;
            _priceFormatter = priceFormatter;
            _orderTotalCalculationService = orderTotalCalculationService;
            _productService = productService;
            _checkoutAttributeFormatter = checkoutAttributeFormatter;
            _genericAttributeService = genericAttributeService;
            _shippingSettings = shippingSettings;
            _countryService = countryService;
            _stateProvinceService = stateProvinceService;
            _taxService = taxService;
            _addressService = addressService;
            _discountService = discountService;
        }
        public IViewComponentResult Invoke()
        {
            var model = new PaymentInfoModel();
            var useSandBox = _brainTreePaymentSettings.UseSandBox;
            var merchantId = _brainTreePaymentSettings.MerchantId;
            var publicKey = _brainTreePaymentSettings.PublicKey;
            var privateKey = _brainTreePaymentSettings.PrivateKey;

            //new gateway
            var gateway = new Braintree.BraintreeGateway
            {
                Environment = useSandBox ? Braintree.Environment.SANDBOX : Braintree.Environment.PRODUCTION,
                MerchantId = merchantId,
                PublicKey = publicKey,
                PrivateKey = privateKey
            };
            var clientToken = gateway.ClientToken.Generate();
            model.Token = clientToken;
            var details = PreparePlaceOrderDetailsAsync().Result;
            var bstateProvince = _stateProvinceService.GetStateProvinceByAddressAsync(details.BillingAddress).Result;
            var bcountry = _countryService.GetCountryByAddressAsync(details.BillingAddress).Result;
            var sstateProvince = _stateProvinceService.GetStateProvinceByAddressAsync(details.ShippingAddress).Result;
            var scountry = _countryService.GetCountryByAddressAsync(details.ShippingAddress).Result;
            model.Amount = details.OrderTotal;
            model.Email = details.BillingAddress.Email;
            model.BillFirstName = details.BillingAddress.FirstName;
            model.BillLastName = details.BillingAddress.LastName;
            model.BillPhoneNumber = details.BillingAddress.PhoneNumber;
            model.BillAddress1 = details.BillingAddress.Address1;
            model.BillAddress2 = details.BillingAddress.Address2;
            model.BillCity = details.BillingAddress.City;
            model.BillState = bstateProvince.Abbreviation;
            model.BillPostCode = details.BillingAddress.ZipPostalCode;
            model.BillCountry = bcountry.TwoLetterIsoCode;
            model.ShipFirstName = details.ShippingAddress.FirstName;
            model.ShipLastName = details.ShippingAddress.LastName;
            model.ShipPhoneNumber = details.ShippingAddress.PhoneNumber;
            model.ShipAddress1 = details.ShippingAddress.Address1;
            model.ShipAddress2 = details.ShippingAddress.Address2;
            model.ShipCity = details.ShippingAddress.City;
            model.ShipState = sstateProvince.Abbreviation;
            model.ShipPostCode = details.ShippingAddress.ZipPostalCode;
            model.ShipCountry = scountry.TwoLetterIsoCode;
            if (details.PickupInStore)
            {
                model.ShipFirstName = model.BillFirstName;
                model.ShipLastName = model.BillLastName;
                model.ShipPhoneNumber = model.BillPhoneNumber;
                model.ShipAddress1 = model.BillAddress1;
                model.ShipAddress2 = model.BillAddress2;
                model.ShipCity = model.BillCity;
                model.ShipPostCode = model.BillPostCode;
                model.ShipState = model.BillState;
                model.ShipCountry = model.BillCountry;
            }
            return View("~/Plugins/Payments.BrainTree/Views/PaymentInfo.cshtml", model);
        }

        /// <summary>
        /// Prepare details to place an order. It also sets some properties to "processPaymentRequest"
        /// </summary>
        /// <param name="processPaymentRequest">Process payment request</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the details
        /// </returns>
        protected virtual async Task<PlaceOrderContainer> PreparePlaceOrderDetailsAsync()
        {
            var storeId = (await _storeContext.GetCurrentStoreAsync()).Id;
            var customerid = (await _workContext.GetCurrentCustomerAsync()).Id;
            var details = new PlaceOrderContainer
            {
                //customer
                Customer = await _customerService.GetCustomerByIdAsync(customerid)
            };
            if (details.Customer == null)
                throw new ArgumentException("Customer is not set");

            //affiliate
            var affiliate = await _affiliateService.GetAffiliateByIdAsync(details.Customer.AffiliateId);
            if (affiliate != null && affiliate.Active && !affiliate.Deleted)
                details.AffiliateId = affiliate.Id;

            //check whether customer is guest
            if (await _customerService.IsGuestAsync(details.Customer) && !_orderSettings.AnonymousCheckoutAllowed)
                throw new NopException("Anonymous checkout is not allowed");

            //customer currency
            var currencyTmp = await _currencyService.GetCurrencyByIdAsync(
                await _genericAttributeService.GetAttributeAsync<int>(details.Customer, NopCustomerDefaults.CurrencyIdAttribute, storeId));
            var customerCurrency = currencyTmp != null && currencyTmp.Published ? currencyTmp : await _workContext.GetWorkingCurrencyAsync();
            var primaryStoreCurrency = await _currencyService.GetCurrencyByIdAsync(_currencySettings.PrimaryStoreCurrencyId);
            details.CustomerCurrencyCode = customerCurrency.CurrencyCode;
            details.CustomerCurrencyRate = customerCurrency.Rate / primaryStoreCurrency.Rate;

            //customer language
            details.CustomerLanguage = await _languageService.GetLanguageByIdAsync(
                await _genericAttributeService.GetAttributeAsync<int>(details.Customer, NopCustomerDefaults.LanguageIdAttribute, storeId));
            if (details.CustomerLanguage == null || !details.CustomerLanguage.Published)
                details.CustomerLanguage = await _workContext.GetWorkingLanguageAsync();

            //billing address
            if (details.Customer.BillingAddressId is null)
                throw new NopException("Billing address is not provided");

            var billingAddress = await _customerService.GetCustomerBillingAddressAsync(details.Customer);

            if (!CommonHelper.IsValidEmail(billingAddress?.Email))
                throw new NopException("Email is not valid");

            details.BillingAddress = _addressService.CloneAddress(billingAddress);

            if (await _countryService.GetCountryByAddressAsync(details.BillingAddress) is Country billingCountry && !billingCountry.AllowsBilling)
                throw new NopException($"Country '{billingCountry.Name}' is not allowed for billing");

            //checkout attributes
            details.CheckoutAttributesXml = await _genericAttributeService.GetAttributeAsync<string>(details.Customer, NopCustomerDefaults.CheckoutAttributes, storeId);
            details.CheckoutAttributeDescription = await _checkoutAttributeFormatter.FormatAttributesAsync(details.CheckoutAttributesXml, details.Customer);

            //load shopping cart
            details.Cart = await _shoppingCartService.GetShoppingCartAsync(details.Customer, ShoppingCartType.ShoppingCart, storeId);

            if (!details.Cart.Any())
                throw new NopException("Cart is empty");

            //validate the entire shopping cart
            var warnings = await _shoppingCartService.GetShoppingCartWarningsAsync(details.Cart, details.CheckoutAttributesXml, true);
            if (warnings.Any())
                throw new NopException(warnings.Aggregate(string.Empty, (current, next) => $"{current}{next};"));

            //validate individual cart items
            foreach (var sci in details.Cart)
            {
                var product = await _productService.GetProductByIdAsync(sci.ProductId);

                var sciWarnings = await _shoppingCartService.GetShoppingCartItemWarningsAsync(details.Customer,
                    sci.ShoppingCartType, product, storeId, sci.AttributesXml,
                    sci.CustomerEnteredPrice, sci.RentalStartDateUtc, sci.RentalEndDateUtc, sci.Quantity, false, sci.Id);
                if (sciWarnings.Any())
                    throw new NopException(sciWarnings.Aggregate(string.Empty, (current, next) => $"{current}{next};"));
            }

            //min totals validation
            if (!await ValidateMinOrderSubtotalAmountAsync(details.Cart))
            {
                var minOrderSubtotalAmount = await _currencyService.ConvertFromPrimaryStoreCurrencyAsync(_orderSettings.MinOrderSubtotalAmount, await _workContext.GetWorkingCurrencyAsync());
                throw new NopException(string.Format(await _localizationService.GetResourceAsync("Checkout.MinOrderSubtotalAmount"),
                    await _priceFormatter.FormatPriceAsync(minOrderSubtotalAmount, true, false)));
            }

            if (!await ValidateMinOrderTotalAmountAsync(details.Cart))
            {
                var minOrderTotalAmount = await _currencyService.ConvertFromPrimaryStoreCurrencyAsync(_orderSettings.MinOrderTotalAmount, await _workContext.GetWorkingCurrencyAsync());
                throw new NopException(string.Format(await _localizationService.GetResourceAsync("Checkout.MinOrderTotalAmount"),
                    await _priceFormatter.FormatPriceAsync(minOrderTotalAmount, true, false)));
            }

            //tax display type
            if (_taxSettings.AllowCustomersToSelectTaxDisplayType)
                details.CustomerTaxDisplayType = (TaxDisplayType)await _genericAttributeService.GetAttributeAsync<int>(details.Customer, NopCustomerDefaults.TaxDisplayTypeIdAttribute, storeId);
            else
                details.CustomerTaxDisplayType = _taxSettings.TaxDisplayType;

            //sub total (incl tax)
            var (orderSubTotalDiscountAmount, orderSubTotalAppliedDiscounts, subTotalWithoutDiscountBase, _, _) = await _orderTotalCalculationService.GetShoppingCartSubTotalAsync(details.Cart, true);
            details.OrderSubTotalInclTax = subTotalWithoutDiscountBase;
            details.OrderSubTotalDiscountInclTax = orderSubTotalDiscountAmount;

            //discount history
            foreach (var disc in orderSubTotalAppliedDiscounts)
                if (!_discountService.ContainsDiscount(details.AppliedDiscounts, disc))
                    details.AppliedDiscounts.Add(disc);

            //sub total (excl tax)
            (orderSubTotalDiscountAmount, _, subTotalWithoutDiscountBase, _, _) = await _orderTotalCalculationService.GetShoppingCartSubTotalAsync(details.Cart, false);
            details.OrderSubTotalExclTax = subTotalWithoutDiscountBase;
            details.OrderSubTotalDiscountExclTax = orderSubTotalDiscountAmount;

            //shipping info
            if (await _shoppingCartService.ShoppingCartRequiresShippingAsync(details.Cart))
            {
                var pickupPoint = await _genericAttributeService.GetAttributeAsync<PickupPoint>(details.Customer,
                    NopCustomerDefaults.SelectedPickupPointAttribute, storeId);
                if (_shippingSettings.AllowPickupInStore && pickupPoint != null)
                {
                    var country = await _countryService.GetCountryByTwoLetterIsoCodeAsync(pickupPoint.CountryCode);
                    var state = await _stateProvinceService.GetStateProvinceByAbbreviationAsync(pickupPoint.StateAbbreviation, country?.Id);

                    details.PickupInStore = true;
                    details.PickupAddress = new Address
                    {
                        Address1 = pickupPoint.Address,
                        City = pickupPoint.City,
                        County = pickupPoint.County,
                        CountryId = country?.Id,
                        StateProvinceId = state?.Id,
                        ZipPostalCode = pickupPoint.ZipPostalCode,
                        CreatedOnUtc = DateTime.UtcNow
                    };
                }
                else
                {
                    if (details.Customer.ShippingAddressId == null)
                        throw new NopException("Shipping address is not provided");

                    var shippingAddress = await _customerService.GetCustomerShippingAddressAsync(details.Customer);

                    if (!CommonHelper.IsValidEmail(shippingAddress?.Email))
                        throw new NopException("Email is not valid");

                    //clone shipping address
                    details.ShippingAddress = _addressService.CloneAddress(shippingAddress);

                    if (await _countryService.GetCountryByAddressAsync(details.ShippingAddress) is Country shippingCountry && !shippingCountry.AllowsShipping)
                        throw new NopException($"Country '{shippingCountry.Name}' is not allowed for shipping");
                }

                var shippingOption = await _genericAttributeService.GetAttributeAsync<ShippingOption>(details.Customer,
                    NopCustomerDefaults.SelectedShippingOptionAttribute, storeId);
                if (shippingOption != null)
                {
                    details.ShippingMethodName = shippingOption.Name;
                    details.ShippingRateComputationMethodSystemName = shippingOption.ShippingRateComputationMethodSystemName;
                }

                details.ShippingStatus = ShippingStatus.NotYetShipped;
            }
            else
                details.ShippingStatus = ShippingStatus.ShippingNotRequired;

            //shipping total
            var (orderShippingTotalInclTax, _, shippingTotalDiscounts) = await _orderTotalCalculationService.GetShoppingCartShippingTotalAsync(details.Cart, true);
            var (orderShippingTotalExclTax, _, _) = await _orderTotalCalculationService.GetShoppingCartShippingTotalAsync(details.Cart, false);
            if (!orderShippingTotalInclTax.HasValue || !orderShippingTotalExclTax.HasValue)
                throw new NopException("Shipping total couldn't be calculated");

            details.OrderShippingTotalInclTax = orderShippingTotalInclTax.Value;
            details.OrderShippingTotalExclTax = orderShippingTotalExclTax.Value;

            foreach (var disc in shippingTotalDiscounts)
                if (!_discountService.ContainsDiscount(details.AppliedDiscounts, disc))
                    details.AppliedDiscounts.Add(disc);

            //payment total
            var paymentAdditionalFee = await _paymentService.GetAdditionalHandlingFeeAsync(details.Cart, "Payments.BrainTree");
            details.PaymentAdditionalFeeInclTax = (await _taxService.GetPaymentMethodAdditionalFeeAsync(paymentAdditionalFee, true, details.Customer)).price;
            details.PaymentAdditionalFeeExclTax = (await _taxService.GetPaymentMethodAdditionalFeeAsync(paymentAdditionalFee, false, details.Customer)).price;

            //tax amount
            SortedDictionary<decimal, decimal> taxRatesDictionary;
            (details.OrderTaxTotal, taxRatesDictionary) = await _orderTotalCalculationService.GetTaxTotalAsync(details.Cart);

            //VAT number
            var customerVatStatus = (VatNumberStatus)await _genericAttributeService.GetAttributeAsync<int>(details.Customer, NopCustomerDefaults.VatNumberStatusIdAttribute);
            if (_taxSettings.EuVatEnabled && customerVatStatus == VatNumberStatus.Valid)
                details.VatNumber = await _genericAttributeService.GetAttributeAsync<string>(details.Customer, NopCustomerDefaults.VatNumberAttribute);

            //tax rates
            details.TaxRates = taxRatesDictionary.Aggregate(string.Empty, (current, next) =>
                $"{current}{next.Key.ToString(CultureInfo.InvariantCulture)}:{next.Value.ToString(CultureInfo.InvariantCulture)};   ");

            //order total (and applied discounts, gift cards, reward points)
            var (orderTotal, orderDiscountAmount, orderAppliedDiscounts, appliedGiftCards, redeemedRewardPoints, redeemedRewardPointsAmount) = await _orderTotalCalculationService.GetShoppingCartTotalAsync(details.Cart);
            if (!orderTotal.HasValue)
                throw new NopException("Order total couldn't be calculated");

            details.OrderDiscountAmount = orderDiscountAmount;
            details.RedeemedRewardPoints = redeemedRewardPoints;
            details.RedeemedRewardPointsAmount = redeemedRewardPointsAmount;
            details.AppliedGiftCards = appliedGiftCards;
            details.OrderTotal = orderTotal.Value;

            //discount history
            foreach (var disc in orderAppliedDiscounts)
                if (!_discountService.ContainsDiscount(details.AppliedDiscounts, disc))
                    details.AppliedDiscounts.Add(disc);

            //recurring or standard shopping cart?
            details.IsRecurringShoppingCart = await _shoppingCartService.ShoppingCartIsRecurringAsync(details.Cart);
            if (!details.IsRecurringShoppingCart)
                return details;

            var (recurringCyclesError, recurringCycleLength, recurringCyclePeriod, recurringTotalCycles) = await _shoppingCartService.GetRecurringCycleInfoAsync(details.Cart);

            if (!string.IsNullOrEmpty(recurringCyclesError))
                throw new NopException(recurringCyclesError);

            return details;
        }

        /// <summary>
        /// Validate minimum order sub-total amount
        /// </summary>
        /// <param name="cart">Shopping cart</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the rue - OK; false - minimum order sub-total amount is not reached
        /// </returns>
        public virtual async Task<bool> ValidateMinOrderSubtotalAmountAsync(IList<ShoppingCartItem> cart)
        {
            if (cart == null)
                throw new ArgumentNullException(nameof(cart));

            //min order amount sub-total validation
            if (!cart.Any() || _orderSettings.MinOrderSubtotalAmount <= decimal.Zero)
                return true;

            //subtotal
            var (_, _, subTotalWithoutDiscountBase, _, _) = await _orderTotalCalculationService.GetShoppingCartSubTotalAsync(cart, _orderSettings.MinOrderSubtotalAmountIncludingTax);

            if (subTotalWithoutDiscountBase < _orderSettings.MinOrderSubtotalAmount)
                return false;

            return true;
        }

        /// <summary>
        /// Validate minimum order total amount
        /// </summary>
        /// <param name="cart">Shopping cart</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the rue - OK; false - minimum order total amount is not reached
        /// </returns>
        public virtual async Task<bool> ValidateMinOrderTotalAmountAsync(IList<ShoppingCartItem> cart)
        {
            if (cart == null)
                throw new ArgumentNullException(nameof(cart));

            if (!cart.Any() || _orderSettings.MinOrderTotalAmount <= decimal.Zero)
                return true;

            var shoppingCartTotalBase = (await _orderTotalCalculationService.GetShoppingCartTotalAsync(cart)).shoppingCartTotal;

            if (shoppingCartTotalBase.HasValue && shoppingCartTotalBase.Value < _orderSettings.MinOrderTotalAmount)
                return false;

            return true;
        }


        //nested
        protected class PlaceOrderContainer
        {
            public PlaceOrderContainer()
            {
                Cart = new List<ShoppingCartItem>();
                AppliedDiscounts = new List<Discount>();
                AppliedGiftCards = new List<AppliedGiftCard>();
            }

            /// <summary>
            /// Customer
            /// </summary>
            public Customer Customer { get; set; }

            /// <summary>
            /// Customer language
            /// </summary>
            public Language CustomerLanguage { get; set; }

            /// <summary>
            /// Affiliate identifier
            /// </summary>
            public int AffiliateId { get; set; }

            /// <summary>
            /// TAx display type
            /// </summary>
            public TaxDisplayType CustomerTaxDisplayType { get; set; }

            /// <summary>
            /// Selected currency
            /// </summary>
            public string CustomerCurrencyCode { get; set; }

            /// <summary>
            /// Customer currency rate
            /// </summary>
            public decimal CustomerCurrencyRate { get; set; }

            /// <summary>
            /// Billing address
            /// </summary>
            public Address BillingAddress { get; set; }

            /// <summary>
            /// Shipping address
            /// </summary>
            public Address ShippingAddress { get; set; }

            /// <summary>
            /// Shipping status
            /// </summary>
            public ShippingStatus ShippingStatus { get; set; }

            /// <summary>
            /// Selected shipping method
            /// </summary>
            public string ShippingMethodName { get; set; }

            /// <summary>
            /// Shipping rate computation method system name
            /// </summary>
            public string ShippingRateComputationMethodSystemName { get; set; }

            /// <summary>
            /// Is pickup in store selected?
            /// </summary>
            public bool PickupInStore { get; set; }

            /// <summary>
            /// Selected pickup address
            /// </summary>
            public Address PickupAddress { get; set; }

            /// <summary>
            /// Is recurring shopping cart
            /// </summary>
            public bool IsRecurringShoppingCart { get; set; }

            /// <summary>
            /// Initial order (used with recurring payments)
            /// </summary>
            public Order InitialOrder { get; set; }

            /// <summary>
            /// Checkout attributes
            /// </summary>
            public string CheckoutAttributeDescription { get; set; }

            /// <summary>
            /// Shopping cart
            /// </summary>
            public string CheckoutAttributesXml { get; set; }

            /// <summary>
            /// 
            /// </summary>
            public IList<ShoppingCartItem> Cart { get; set; }

            /// <summary>
            /// Applied discounts
            /// </summary>
            public List<Discount> AppliedDiscounts { get; set; }

            /// <summary>
            /// Applied gift cards
            /// </summary>
            public List<AppliedGiftCard> AppliedGiftCards { get; set; }

            /// <summary>
            /// Order subtotal (incl tax)
            /// </summary>
            public decimal OrderSubTotalInclTax { get; set; }

            /// <summary>
            /// Order subtotal (excl tax)
            /// </summary>
            public decimal OrderSubTotalExclTax { get; set; }

            /// <summary>
            /// Subtotal discount (incl tax)
            /// </summary>
            public decimal OrderSubTotalDiscountInclTax { get; set; }

            /// <summary>
            /// Subtotal discount (excl tax)
            /// </summary>
            public decimal OrderSubTotalDiscountExclTax { get; set; }

            /// <summary>
            /// Shipping (incl tax)
            /// </summary>
            public decimal OrderShippingTotalInclTax { get; set; }

            /// <summary>
            /// Shipping (excl tax)
            /// </summary>
            public decimal OrderShippingTotalExclTax { get; set; }

            /// <summary>
            /// Payment additional fee (incl tax)
            /// </summary>
            public decimal PaymentAdditionalFeeInclTax { get; set; }

            /// <summary>
            /// Payment additional fee (excl tax)
            /// </summary>
            public decimal PaymentAdditionalFeeExclTax { get; set; }

            /// <summary>
            /// Tax
            /// </summary>
            public decimal OrderTaxTotal { get; set; }

            /// <summary>
            /// VAT number
            /// </summary>
            public string VatNumber { get; set; }

            /// <summary>
            /// Tax rates
            /// </summary>
            public string TaxRates { get; set; }

            /// <summary>
            /// Order total discount amount
            /// </summary>
            public decimal OrderDiscountAmount { get; set; }

            /// <summary>
            /// Redeemed reward points
            /// </summary>
            public int RedeemedRewardPoints { get; set; }

            /// <summary>
            /// Redeemed reward points amount
            /// </summary>
            public decimal RedeemedRewardPointsAmount { get; set; }

            /// <summary>
            /// Order total
            /// </summary>
            public decimal OrderTotal { get; set; }
        }
    }
}