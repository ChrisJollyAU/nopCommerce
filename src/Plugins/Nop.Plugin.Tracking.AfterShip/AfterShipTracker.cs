using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Nop.Core;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Shipping;
using Nop.Core.Infrastructure;
using Nop.Plugin.Tracking.AfterShip.Infrastructure;
using Nop.Plugin.Tracking.AfterShip.Infrastructure.Aftership;
using Nop.Plugin.Tracking.AfterShip.Infrastructure.Aftership.Enums;
using Nop.Services.Common;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Shipping;
using Nop.Services.Shipping.Tracking;

namespace Nop.Plugin.Tracking.AfterShip
{
    public class AfterShipTracker : IShipmentTracker
    {
        #region Fields

        private readonly ICountryService _countryService;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly ILocalizationService _localizationService;
        private readonly ILogger _logger;
        private readonly IWorkContext _workContext;
        private readonly AftershipConnection _connection;
        private readonly AfterShipSettings _settings;
        private readonly IShipmentService _shipmentService;

        #endregion

        #region Ctor

        public AfterShipTracker(AfterShipSettings settings, IShipmentService shipmentService)
        {
            _countryService = EngineContext.Current.Resolve<ICountryService>();
            _genericAttributeService = EngineContext.Current.Resolve<IGenericAttributeService>();
            _localizationService = EngineContext.Current.Resolve<ILocalizationService>();
            _logger = EngineContext.Current.Resolve<ILogger>();
            _workContext = EngineContext.Current.Resolve<IWorkContext>();
            _settings = settings;
            _connection = new AftershipConnection(_settings.ApiKey);
            _shipmentService = shipmentService;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets if the current tracker can track the tracking number.
        /// </summary>
        /// <param name="trackingNumber">The tracking number to track.</param>
        /// <returns>True if the tracker can track, otherwise false.</returns>
        public bool IsMatch(string trackingNumber)
        {
            return !string.IsNullOrWhiteSpace(trackingNumber);
        }

        /// <summary>
        /// Gets a url for a page to show tracking info (third party tracking page).
        /// </summary>
        /// <param name="trackingNumber">The tracking number to track.</param>
        /// <returns>A url to a tracking page.</returns>
        public string GetUrl(string trackingNumber)
        {
            if (string.IsNullOrWhiteSpace(_settings.AfterShipUsername) || _settings.AfterShipUsername.Equals("MyAfterShipUsername"))
                return $"https://track.aftership.com/{trackingNumber}";

            return $"https://{_settings.AfterShipUsername}.aftership.com/{trackingNumber}";
        }

        /// <summary>
        /// Gets all events for a tracking number.
        /// </summary>
        /// <param name="trackingNumber">The tracking number to track.</param>
        /// <returns>List of Shipment Events.</returns>
        public IList<ShipmentStatusEvent> GetShipmentEvents(string trackingNumber)
        {
            if (string.IsNullOrWhiteSpace(_settings.ApiKey) || _settings.ApiKey.Equals("MyAfterShipAPIKey"))
                return new List<ShipmentStatusEvent>();

            if (string.IsNullOrEmpty(trackingNumber))
                return new List<ShipmentStatusEvent>();

            var shipment = _shipmentService.GetAllShipments(trackingNumber: trackingNumber)
                .FirstOrDefault(s => !s.DeliveryDateUtc.HasValue);

            if (shipment == null)
                return new List<ShipmentStatusEvent>();

            var tracker = new Infrastructure.Aftership.Tracking(trackingNumber);
            var genericAttrs = _genericAttributeService.GetAttributesForEntity(shipment.Id, "Shipment");
            var trackingInfo = genericAttrs.FirstOrDefault(a => a.Key.Equals(Constants.SHIPMENT_TRACK_ID_ATTRIBUTE_NAME));
            IList<ShipmentStatusEvent> shipmentStatusList = new List<ShipmentStatusEvent>();
            IList<Courier> couriers = null;

            if (trackingInfo != null)
            {
                try
                {
                    tracker.Id = trackingInfo.Value;
                    shipmentStatusList = GetShipmentStatusEvents(tracker);
                }
                catch (WebException)
                {
                    _logger.Error($"Error getting tracking information on Aftership events - {trackingNumber}");
                }
            }
            else
            {
                try
                {
                    // use try-catch to ensure exception won't be thrown is web service is not available
                    couriers = _connection.DetectCouriers(trackingNumber, null, null, null, null);
                }
                catch (Exception ex)
                {
                    _logger.Error("Error getting couriers information on Aftership", ex);
                }

                if (couriers == null) return shipmentStatusList;

                foreach (var courier in couriers)
                {
                    try
                    {
                        tracker.Slug = courier.Slug;
                        tracker = _connection.GetTrackingByNumber(tracker);
                        shipmentStatusList = GetShipmentStatusEvents(tracker);
                        if (shipmentStatusList.Any())
                            break;
                    }
                    catch (WebException)
                    {
                        _logger.Error($"Error getting tracking information on Aftership events - {trackingNumber}");
                    }
                }
            }

            return shipmentStatusList;
        }

        #endregion

        #region Utilites

        private IList<ShipmentStatusEvent> GetShipmentStatusEvents(Infrastructure.Aftership.Tracking tracker)
        {
            var shipmentStatusList = new List<ShipmentStatusEvent>();

            tracker = _connection.GetTrackingByNumber(tracker);

            if (tracker.Checkpoints == null || !tracker.Checkpoints.Any()) return shipmentStatusList;

            foreach (var checkpoint in tracker.Checkpoints)
            {
                Country country = null;
                if (checkpoint.CountryIso3 != Iso3Country.Null)
                    country = _countryService.GetCountryByThreeLetterIsoCode(checkpoint.CountryIso3.GetIso3Code());
                var shipmentStatus = new ShipmentStatusEvent
                {
                    CountryCode = country != null ? country.TwoLetterIsoCode : string.Empty,
                    Date = Convert.ToDateTime(checkpoint.CheckpointTime),
                    EventName = $"{checkpoint.Message} ({GetStatus(checkpoint)})",
                    Location = string.IsNullOrEmpty(checkpoint.City) ? checkpoint.Location : checkpoint.City
                };
                //// other properties (not used yet)
                //checkpoint.checkpointTime;
                //checkpoint.countryName;
                //checkpoint.state;
                //checkpoint.zip;

                shipmentStatusList.Add(shipmentStatus);
            }

            return shipmentStatusList;
        } 

        private string GetStatus(Checkpoint checkpoint)
        {
            switch (checkpoint.Tag)
            {
                case "Pending":
                    return
                        _localizationService.GetLocaleStringResourceByName(
                            "Plugins.Tracking.AfterShip.Status.Pending",
                            _workContext.WorkingLanguage.Id).ResourceValue;
                case "InfoReceived":
                    return
                        _localizationService.GetLocaleStringResourceByName(
                            "Plugins.Tracking.AfterShip.Status.InfoReceived",
                            _workContext.WorkingLanguage.Id).ResourceValue;
                case "InTransit":
                    return
                        _localizationService.GetLocaleStringResourceByName(
                            "Plugins.Tracking.AfterShip.Status.InTransit",
                            _workContext.WorkingLanguage.Id).ResourceValue;
                case "OutForDelivery":
                    return
                        _localizationService.GetLocaleStringResourceByName(
                            "Plugins.Tracking.AfterShip.Status.OutForDelivery",
                            _workContext.WorkingLanguage.Id).ResourceValue;
                case "AttemptFail":
                    return
                        _localizationService.GetLocaleStringResourceByName(
                            "Plugins.Tracking.AfterShip.Status.AttemptFail",
                            _workContext.WorkingLanguage.Id).ResourceValue;
                case "Delivered":
                    return
                        _localizationService.GetLocaleStringResourceByName(
                            "Plugins.Tracking.AfterShip.Status.Delivered",
                            _workContext.WorkingLanguage.Id).ResourceValue;
                case "Expired":
                    return
                        _localizationService.GetLocaleStringResourceByName(
                            "Plugins.Tracking.AfterShip.Status.Expired",
                            _workContext.WorkingLanguage.Id).ResourceValue;
                default:
                    return
                        _localizationService.GetLocaleStringResourceByName(
                            "Plugins.Tracking.AfterShip.Status.Exception",
                            _workContext.WorkingLanguage.Id).ResourceValue;
            }
        }

        #endregion
    }
}