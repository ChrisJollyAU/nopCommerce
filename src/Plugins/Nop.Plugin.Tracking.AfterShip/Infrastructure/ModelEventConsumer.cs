using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Nop.Core;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Shipping;
using Nop.Core.Events;
using Nop.Plugin.Tracking.AfterShip.Infrastructure.Aftership;
using Nop.Services.Common;
using Nop.Services.Events;
using Nop.Services.Logging;

namespace Nop.Plugin.Tracking.AfterShip.Infrastructure
{
    public class ModelEventConsumer : IConsumer<EntityUpdated<Shipment>>, IConsumer<EntityInserted<Shipment>>, IConsumer<EntityDeleted<Shipment>>
    {
        #region Fields

        private readonly IGenericAttributeService _genericAttributeService;
        private readonly IStoreContext _storeContext;
        private readonly ILogger _logger;
        private readonly AftershipConnection _connection;
        private readonly AfterShipSettings _settings;

        #endregion

        #region Ctor

        public ModelEventConsumer(IGenericAttributeService genericAttributeService, IStoreContext storeContext, ILogger logger, AfterShipSettings settings)
        {
            _genericAttributeService = genericAttributeService;
            _storeContext = storeContext;
            _logger = logger;
            _settings = settings;

            _connection = new AftershipConnection(_settings.ApiKey);
        }

        #endregion

        #region Methods

        public void HandleEvent(EntityInserted<Shipment> eventMessage)
        {
            OnShipmentInsert(eventMessage.Entity);
        }

        public void HandleEvent(EntityUpdated<Shipment> eventMessage)
        {
            OnShipmentChange(eventMessage.Entity);
        }

        public void HandleEvent(EntityDeleted<Shipment> eventMessage)
        {
            OnShipmentDeleted(eventMessage.Entity);
        }

        #endregion

        #region Utils

        private void OnShipmentInsert(Shipment args)
        {
            if (!string.IsNullOrEmpty(args.TrackingNumber))
                CreateTracking(args);
        }

        private void OnShipmentChange(Shipment args)
        {
            if (string.IsNullOrWhiteSpace(args.TrackingNumber)) return;

            var genericAttrs = _genericAttributeService.GetAttributesForEntity(args.Id, "Shipment");

            // Events for tracking is not yet registered. We register and save this entry in the generic attributes
            if (genericAttrs.Any(g => g.Value.Equals(args.TrackingNumber))) return;

            //remove the old tracker
            var oldTrackAttr = genericAttrs.FirstOrDefault(g => g.Key.Equals(Constants.SHIPMENT_TRACK_NUMBER_ATTRIBUTE_NAME));
            if (oldTrackAttr != null && !oldTrackAttr.Value.Equals(args.TrackingNumber))
            {
                if (RemoveTracking(oldTrackAttr.Value))
                {
                    foreach (var genericAttribute in genericAttrs)
                    {
                        _genericAttributeService.DeleteAttribute(genericAttribute);
                    }
                }
            }

            //create the new tracking in Aftarship
            CreateTracking(args);
        }

        private void OnShipmentDeleted(Shipment args)
        {
            if (!string.IsNullOrEmpty(args.TrackingNumber))
                RemoveTracking(args.TrackingNumber);
        }

        private void CreateTracking(Shipment shipment)
        {
            var order = shipment.Order;
            var customer = shipment.Order.Customer;
            var customerFullName = $"{order.ShippingAddress.FirstName} {order.ShippingAddress.LastName}".Trim();

            //create the new tracking
            var track = new Aftership.Tracking(shipment.TrackingNumber)
            {
                CustomerName = customerFullName,
                OrderId = $"ID {order.Id}",
                OrderIdPath = $"{_storeContext.CurrentStore.Url}orderdetails/{order.Id}"
            };

            if (_settings.AllowCustomerNotification)
            {
                track.Emails = new List<string> { customer.Email };
            }

            try
            {
                track = _connection.CreateTracking(track);
                _genericAttributeService.InsertAttribute(new GenericAttribute
                {
                    EntityId = shipment.Id,
                    Key = Constants.SHIPMENT_NOTIFICATION_ATTRIBUTE_NAME,
                    KeyGroup = "Shipment",
                    StoreId = 0,
                    Value = "True"
                });
                _genericAttributeService.InsertAttribute(new GenericAttribute
                {
                    EntityId = shipment.Id,
                    Key = Constants.SHIPMENT_TRACK_NUMBER_ATTRIBUTE_NAME,
                    KeyGroup = "Shipment",
                    StoreId = 0,
                    Value = shipment.TrackingNumber
                });
                _genericAttributeService.InsertAttribute(new GenericAttribute
                {
                    EntityId = shipment.Id,
                    Key = Constants.SHIPMENT_TRACK_ID_ATTRIBUTE_NAME,
                    KeyGroup = "Shipment",
                    StoreId = 0,
                    Value = track.Id
                });
            }
            catch (WebException ex)
            {
                _logger.Error($"Cannot registration tracking with number - {shipment.TrackingNumber}", new Exception(ex.Message));
            }
        }

        private bool RemoveTracking(string trackingNumber)
        {
            var result = false;
            try
            {
                var track = new Aftership.Tracking(trackingNumber);
                var couriers = _connection.DetectCouriers(trackingNumber);
                foreach (var courier in couriers)
                {
                    track.Slug = courier.Slug;
                    result = _connection.DeleteTracking(track);
                    if (result)
                        break;
                }
            }
            catch (WebException ex)
            {
                _logger.Warning($"Cannot delete tracking with number - {trackingNumber}", new Exception(ex.Message));
            }

            return result;
        }
        
        #endregion
    }
}