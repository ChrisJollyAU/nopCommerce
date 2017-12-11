using System;
using Nop.Core;
using Nop.Core.Domain.Shipping;
using Nop.Core.Plugins;
using Nop.Services.Configuration;
using Nop.Services.Directory;
using Nop.Services.Shipping;
using Nop.Services.Shipping.Tracking;
using Nop.Services.Logging;
using System.Collections.Generic;
using System.ServiceModel;
using Nop.Plugin.Shipping.Temando.ServiceReference1;
//TODO temando login settings
namespace Nop.Plugin.Shipping.Temando
{
    /// <summary>
    /// Australia post computation method
    /// </summary>
    public class TemandoComputationMethod : BasePlugin, IShippingRateComputationMethod
    {
        #region Constants

        private const int MIN_LENGTH = 50; // 5 cm
        private const int MIN_WEIGHT = 1; // 1 g
        private const int MAX_LENGTH = 1050; // 105 cm
        private const int MAX_WEIGHT = 20000; // 20 Kg
        private const int MIN_GIRTH = 160; // 16 cm
        private const int MAX_GIRTH = 1050; // 105 cm

        #endregion

        #region Fields

        private readonly IMeasureService _measureService;
        private readonly IShippingService _shippingService;
        private readonly ISettingService _settingService;
        private readonly TemandoSettings _temandoSettings;
        private readonly ILogger _logger;
        private readonly IWebHelper _webHelper;
        private readonly List<string> postcodes;
        private readonly decimal threshold;
        #endregion

        #region Ctor
        public TemandoComputationMethod(IMeasureService measureService,
            IShippingService shippingService, ISettingService settingService,
            TemandoSettings temandoSettings, ILogger logger, IWebHelper webHelper)
        {
            this._measureService = measureService;
            this._shippingService = shippingService;
            this._settingService = settingService;
            this._temandoSettings = temandoSettings;
            this._logger = logger;
            this._webHelper = webHelper;
            postcodes = new List<string>();
            postcodes.AddRange(System.IO.File.ReadAllLines(CommonHelper.MapPath("/Plugins/Shipping.Temando/postcodes.csv")));
            threshold = _temandoSettings.Threshold;
        }
        #endregion

        #region Utilities

        #endregion

        #region Methods

        /// <summary>
        ///  Gets available shipping options
        /// </summary>
        /// <param name="getShippingOptionRequest">A request for getting shipping options</param>
        /// <returns>Represents a response of getting shipping rate options</returns>
        public GetShippingOptionResponse GetShippingOptions(GetShippingOptionRequest getShippingOptionRequest)
        {
            if (getShippingOptionRequest == null)
                throw new ArgumentNullException("getShippingOptionRequest");
            var response = new GetShippingOptionResponse();

            if (getShippingOptionRequest.Items == null)
            {
                response.AddError("No shipment items");
                return response;
            }
            if (getShippingOptionRequest.ShippingAddress == null)
            {
                response.AddError("Shipping address is not set");
                return response;
            }
            if (getShippingOptionRequest.ShippingAddress.City == null)
            {
                response.AddError("Shipping address city is not set");
                return response;
            }

            string _endpointAddress = "http://api.temando.com/soapServer.html";
            var quot = new quoting_portTypeClient(new BasicHttpBinding(),new EndpointAddress(_endpointAddress));
            quot.Endpoint.EndpointBehaviors.Add(new TemandoEndpointBehavior{UserName=_temandoSettings.Username,Password=_temandoSettings.Password});
            
            getQuotesByRequest quote = new getQuotesByRequest();
            bool freeshippostcode = postcodes.Contains(getShippingOptionRequest.ShippingAddress.ZipPostalCode);
            bool overthreshold =false;
            overthreshold = false;
            var anywhere = new Anywhere
            {
                itemNature = DeliveryNature.Domestic,
                itemMethod = DeliveryType.DoortoDoor,
                originCountry = getShippingOptionRequest.CountryFrom.TwoLetterIsoCode,
                originCode = getShippingOptionRequest.ZipPostalCodeFrom,
                originSuburb = getShippingOptionRequest.CityFrom,
                destinationCountry = getShippingOptionRequest.ShippingAddress.Country.TwoLetterIsoCode,
                destinationCode = getShippingOptionRequest.ShippingAddress.ZipPostalCode,
                destinationSuburb = getShippingOptionRequest.ShippingAddress.City,
                destinationIs = LocationType.Business,
                destinationIsSpecified = true,
                destinationBusNotifyBefore = YesNoOption.N,
                destinationBusNotifyBeforeSpecified = true,
                destinationBusLimitedAccess = YesNoOption.N,
                destinationBusLimitedAccessSpecified = true,
                originIs = LocationType.Business,
                originIsSpecified = true,
                originBusNotifyBefore = YesNoOption.Y,
                originBusNotifyBeforeSpecified = true,
                originBusLimitedAccess = YesNoOption.N,
                originBusLimitedAccessSpecified = true,
            };

            List<Anything> anythings = new List<Anything>();
            //must be after current date and not on saturday or sunday
            DateTime readydate = DateTime.Today.AddDays(1);
            if (readydate.DayOfWeek == DayOfWeek.Saturday)
            {
                readydate = readydate.AddDays(2);

            }
            else if (readydate.DayOfWeek == DayOfWeek.Sunday)
            {
                readydate = readydate.AddDays(1);

            }
            var anytime = new Anytime
            {

                readyDate = readydate.ToString("yyyy-MM-dd"),
                readyTime = ReadyTime.AM
            };

            foreach (GetShippingOptionRequest.PackageItem item in getShippingOptionRequest.Items)
            {
                bool freeship = true;
                foreach (var item2 in item.ShoppingCartItem.Product.ProductSpecificationAttributes)
                {
                    if (item2.SpecificationAttributeOption.SpecificationAttribute.Name == "ExcludeFreeShip")
                    {
                        freeship = false;
                    }
                }
                if (!overthreshold) freeship = false;
                if (item.ShoppingCartItem.Product.IsShipEnabled && !freeship)
                {
                    int quantity = item.ShoppingCartItem.Quantity;
                    if (item.OverriddenQuantity != null) quantity = item.OverriddenQuantity.Value;
                    for (int a = 0; a < quantity; a++)
                    {
                        Anything itema = new Anything();
                        itema.weight = (int)Math.Ceiling(item.ShoppingCartItem.Product.Weight);
                        itema.weightMeasurementType = WeightMeasurementType.Kilograms;
                        itema.weightMeasurementTypeSpecified = true;
                        itema.weightSpecified = true;
                        itema.width = Convert.ToInt32(item.ShoppingCartItem.Product.Width);
                        itema.widthSpecified = true;
                        itema.height = Convert.ToInt32(item.ShoppingCartItem.Product.Height);
                        itema.heightSpecified = true;
                        itema.length = Convert.ToInt32(item.ShoppingCartItem.Product.Length);
                        itema.lengthSpecified = true;
                        itema.distanceMeasurementType = DistanceMeasurementType.Centimetres;
                        itema.distanceMeasurementTypeSpecified = true;
                        itema.@class = Class.Freight;
                        itema.classSpecified = true;
                        itema.mode = Mode.Lessthanload;
                        itema.modeSpecified = true;
                        itema.packaging = Packaging.Carton;
                        itema.packagingSpecified = true;
                        itema.qualifierFreightGeneralFragile = YesNoOption.N;
                        itema.qualifierFreightGeneralFragileSpecified = true;
                        itema.quantity = quantity;
                        anythings.Add(itema);
                    }
                }
            }

            quote.anythings = anythings.ToArray();
            quote.anytime = anytime;
            quote.anywhere = anywhere;
            if (anythings.Count > 0)
            {
                try {
                    var availquotes = quot.getQuotesByRequest(quote);
                    foreach (AvailableQuote q in availquotes)
                    {
                        ShippingOption shipopt = new ShippingOption
                        {
                            Name = q.carrier.companyName.Replace("(B2B)", "") + ", " + q.deliveryMethod,
                            Rate = q.totalPrice + 5,
                            Description = $"{q.etaFrom} - {q.etaTo} Days"
                        };
                        response.ShippingOptions.Add(shipopt);
                        if (shipopt.Name.Contains("TNT"))
                        {
                            _logger.Debug("TNT Temando: " + shipopt.Rate);
                        }
                    }
                }
                catch (Exception e)
                {
                    _logger.Error(e.Message);
                }
            }
            else
            {
                ShippingOption shipopt = new ShippingOption
                {
                    Name = "Free shipping - TNT Road Express",
                    Rate = 0,
                    Description = $"{2} - {5} Days"
                };
                response.ShippingOptions.Add(shipopt);
            }
            return response;
        }

        /// <summary>
        /// Gets fixed shipping rate (if shipping rate computation method allows it and the rate can be calculated before checkout).
        /// </summary>
        /// <param name="getShippingOptionRequest">A request for getting shipping options</param>
        /// <returns>Fixed shipping rate; or null in case there's no fixed shipping rate</returns>
        public decimal? GetFixedRate(GetShippingOptionRequest getShippingOptionRequest)
        {
            return null;
        }

        /// <summary>
        /// Gets a configuration page URL
        /// </summary>
        public override string GetConfigurationPageUrl()
        {
            return $"{_webHelper.GetStoreLocation()}Admin/ShippingTemando/Configure";
        }

        /// <summary>
        /// Install plugin
        /// </summary>
        public override void Install()
        {
            //settings
            var settings = new TemandoSettings()
            {
                Username = "",Password = ""
            };
            _settingService.SaveSetting(settings);
            
            base.Install();
        }

        public override void Uninstall()
        {
            
            base.Uninstall();
        }
        #endregion

        #region Properties

        /// <summary>
        /// Gets a shipping rate computation method type
        /// </summary>
        public ShippingRateComputationMethodType ShippingRateComputationMethodType
        {
            get
            {
                return ShippingRateComputationMethodType.Realtime;
            }
        }

        /// <summary>
        /// Gets a shipment tracker
        /// </summary>
        public IShipmentTracker ShipmentTracker 
        { 
            get { return null; }
        }

        #endregion
    }
}