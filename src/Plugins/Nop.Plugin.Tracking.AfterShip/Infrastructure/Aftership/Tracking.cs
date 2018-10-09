/* 
 * This code is taken form AfterShip's GitHub (https://github.com/AfterShip/aftership-sdk-net)
 * and slightly modified for our coding standards. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Nop.Plugin.Tracking.AfterShip.Infrastructure.Aftership.Enums;

namespace Nop.Plugin.Tracking.AfterShip.Infrastructure.Aftership
{
    /// <summary>
    /// Tracking. Keep instances of trackings
    /// </summary>
    public class Tracking
    {
        #region Fields

        private string _id;
        private string _trackingNumber;
        private string _slug;
        private IList<string> _emails;
        private IList<string> _phones;
        private string _title;
        private string _customerName;
        private Iso3Country _destinationCountryIso3;
        private Iso3Country _originCountryIso3;
        private string _orderId;
        private string _orderIdPath;
        private Dictionary<string, string> _customFields; 
        private DateTime _createdAt; 
        private DateTime _updatedAt; 
        private bool _active; 
        private string _expectedDelivery; 
        private int _shipmentPackageCount; 
        private string _shipmentType; 
        private string _signedBy;  
        private string _source; 
        private StatusTag _tag;
        private int _trackedCount; 
        private IList<Checkpoint> _checkpoints;
        private string _uniqueToken;
        private string _trackingAccountNumber;
        private string _trackingPostalCode;
        private string _trackingShipDate;

        #endregion

        #region Ctors

        public Tracking(string trackingNumber)
        {
            _trackingNumber = trackingNumber;
            _title = trackingNumber;
            _checkpoints = new List<Checkpoint>();
            _emails = new List<string>();
            _phones = new List<string>();
        }

        public Tracking(JObject trackingJson)
        {
            _id = trackingJson["id"] == null 
                ? null 
                : trackingJson["id"].ToString();
            _trackingNumber = trackingJson["tracking_number"] == null
                ? null
                : trackingJson["tracking_number"].ToString();
            _slug = trackingJson["slug"] == null 
                ? null 
                : trackingJson["slug"].ToString();
            _title = trackingJson["title"] == null 
                ? null 
                : trackingJson["title"].ToString();
            _customerName = trackingJson["customer_name"] == null 
                ? null 
                : trackingJson["customer_name"].ToString();
            _expectedDelivery = trackingJson["expected_delivery"] == null 
                ? null 
                : trackingJson["expected_delivery"].ToString();
            _orderId = trackingJson["order_id"] == null 
                ? null 
                : trackingJson["order_id"].ToString();
            _orderIdPath = trackingJson["order_id_path"] == null 
                ? null 
                : trackingJson["order_id_path"].ToString();
            _trackingAccountNumber = trackingJson["tracking_account_number"] == null 
                ? null 
                : trackingJson["tracking_account_number"].ToString();
            _trackingPostalCode = trackingJson["tracking_postal_code"] == null 
                ? null 
                : trackingJson["tracking_postal_code"].ToString();
            _trackingShipDate = trackingJson["tracking_ship_date"] == null 
                ? null 
                : trackingJson["tracking_ship_date"].ToString();
            _shipmentType = trackingJson["shipment_type"] == null 
                ? null 
                : trackingJson["shipment_type"].ToString();
            _signedBy = trackingJson["singned_by"] == null 
                ? null 
                : trackingJson["signed_by"].ToString();
            _source = trackingJson["source"] == null 
                ? null 
                : trackingJson["source"].ToString();
            _uniqueToken = trackingJson["unique_token"] == null 
                ? null 
                : trackingJson["unique_token"].ToString();

            var smsesArray = trackingJson["smses"] == null 
                ? null 
                : (JArray)trackingJson["smses"];
            var emailsArray = trackingJson["emails"] == null 
                ? null 
                : (JArray)trackingJson["emails"];
            var destinationCountryIso3 = trackingJson["destination_country_iso3"] == null 
                ? null 
                : trackingJson["destination_country_iso3"].ToString();
            var originCountryIso3 = trackingJson["origin_country_iso3"] == null 
                ? null 
                : trackingJson["origin_country_iso3"].ToString();
            var customFieldsJson = (trackingJson["custom_fields"] == null || !trackingJson["custom_fields"].HasValues)
                ? null
                : (JObject)trackingJson["custom_fields"];
            var tag = trackingJson["tag"] == null 
                ? null 
                : trackingJson["tag"].ToString();
            var checkpointsArray = trackingJson["checkpoints"] == null 
                ? null 
                : (JArray)trackingJson["checkpoints"];

            #region Non-string values

            if (!string.IsNullOrEmpty(destinationCountryIso3))
            {
                if (!Enum.TryParse(destinationCountryIso3, out _destinationCountryIso3))
                    _destinationCountryIso3 = Iso3Country.Null;
            }
            else
            {
                _destinationCountryIso3 = Iso3Country.Null;
            }

            if (smsesArray != null && smsesArray.Count != 0)
            {
                _phones = new List<string>();
                foreach (var token in smsesArray)
                {
                    _phones.Add(token.ToString());
                }
            }

            if (emailsArray != null && emailsArray.Count != 0)
            {
                _emails = new List<string>();
                foreach (var token in emailsArray)
                {
                    _emails.Add(token.ToString());
                }
            }

            if (customFieldsJson != null)
            {
                _customFields = new Dictionary<string, string>();
                var keys = customFieldsJson.Properties();
                foreach (var item in keys)
                {
                    _customFields.Add(item.Name, customFieldsJson[item.Name].ToString());
                }
            }

            if (trackingJson["created_at"] != null)
            {
                if (!DateTime.TryParse(trackingJson["created_at"].ToString(), out _createdAt))
                    _createdAt = DateTime.MinValue;
            }
            else
            {
                _createdAt = DateTime.MinValue;
            }

            if (trackingJson["updated_at"] != null)
            {
                if (!DateTime.TryParse(trackingJson["updated_at"].ToString(), out _updatedAt))
                    _updatedAt = DateTime.MinValue;
            }
            else
            {
                _updatedAt = DateTime.MinValue;
            }

            if (trackingJson["active"] != null)
            {
                if (!bool.TryParse(trackingJson["active"].ToString(), out _active))
                    _active = false;
            }
            else
            {
                _active = false;
            }

            if (!string.IsNullOrEmpty(originCountryIso3))
            {
                if (!Enum.TryParse(originCountryIso3, out _originCountryIso3))
                    _originCountryIso3 = Iso3Country.Null;
            }
            else
            {
                _originCountryIso3 = Iso3Country.Null;
            }

            if (trackingJson["shipment_package_count"] != null)
            {
                if (int.TryParse(trackingJson["shipment_package_count"].ToString(), out _shipmentPackageCount))
                    _shipmentPackageCount = 0;
            }
            else
            {
                _shipmentPackageCount = 0;
            }

            if (!string.IsNullOrEmpty(tag))
            {
                if (!Enum.TryParse(tag, out _tag))
                    _tag = StatusTag.Unknown;
            }
            else
            {
                _tag = StatusTag.Unknown;
            }

            if (trackingJson["tracked_count"] != null)
            {
                if (int.TryParse(trackingJson["tracked_count"].ToString(), out _trackedCount))
                    _trackedCount = 0;
            }
            else
            {
                _trackedCount = 0;
            }

            #endregion

            if (checkpointsArray == null) return;

            _checkpoints = new List<Checkpoint>();

            foreach (var token in checkpointsArray)
            {
                _checkpoints.Add(new Checkpoint((JObject)token));
            }
        }

        #endregion

        #region Properties

        public string Id
        {
            get { return _id; }
            set { _id = value; }
        }

        public string TrackingNumber
        {
            get { return _trackingNumber; }
            set { _trackingNumber = value; }
        }

        public string Slug
        {
            get { return _slug; }
            set { _slug = value; }
        }

        public IList<string> Emails
        {
            get { return _emails; }
            set { _emails = value; }
        }

        public IList<string> Phones
        {
            get { return _phones; }
            set { _phones = value; }
        }

        public string Title
        {
            get { return _title; }
            set { _title = value; }
        }

        public string CustomerName
        {
            get { return _customerName; }
            set { _customerName = value; }
        }

        public Iso3Country DestinationCountryIso3
        {
            get { return _destinationCountryIso3; }
            set { _destinationCountryIso3 = value; }
        }

        public string OrderId
        {
            get { return _orderId; }
            set { _orderId = value; }
        }

        public string OrderIdPath
        {
            get { return _orderIdPath; }
            set { _orderIdPath = value; }
        }

        public Dictionary<string, string> CustomFields
        {
            get { return _customFields; }
            set { _customFields = value; }
        }

        public DateTime CreatedAt
        {
            get { return _createdAt; }
            set { _createdAt = value; }
        }

        public DateTime UpdatedAt
        {
            get { return _updatedAt; }
            set { _updatedAt = value; }
        }

        public bool Active
        {
            get { return _active; }
            set { _active = value; }
        }

        public string ExpectedDelivery
        {
            get { return _expectedDelivery; }
            set { _expectedDelivery = value; }
        }

        public Iso3Country OriginCountryIso3
        {
            get { return _originCountryIso3; }
            set { _originCountryIso3 = value; }
        }

        public int ShipmentPackageCount
        {
            get { return _shipmentPackageCount; }
            set { _shipmentPackageCount = value; }
        }

        public int TrackedCount
        {
            get { return _trackedCount; }
            set { _trackedCount = value; }
        }

        public string ShipmentType
        {
            get { return _shipmentType; }
            set { _shipmentType = value; }
        }

        public string SignedBy
        {
            get { return _signedBy; }
            set { _signedBy = value; }
        }

        public string Source
        {
            get { return _source; }
            set { _source = value; }
        }

        public StatusTag Tag
        {
            get { return _tag; }
            set { _tag = value; }
        }

        public string UniqueToken
        {
            get { return _uniqueToken; }
            set { _uniqueToken = value; }
        }

        public string TrackingAccountNumber
        {
            get { return _trackingAccountNumber; }
            set { _trackingAccountNumber = value; }
        }

        public string TrackingPostalCode
        {
            get { return _trackingPostalCode; }
            set { _trackingPostalCode = value; }
        }

        public string TrackingShipDate
        {
            get { return _trackingShipDate; }
            set { _trackingShipDate = value; }
        }

        public IList<Checkpoint> Checkpoints
        {
            get { return _checkpoints; }
            set { _checkpoints = value; }
        }

        #endregion

        #region Methods

        public void AddEmails(params string[] emails)
        {
            foreach (var email in emails)
            {
                _emails.Add(email);
            }
        }

        public void DeleteEmail(string email)
        {
            _emails.Remove(email);
        }

        public void AddPhones(params string[] phones)
        {
            foreach (var phone in phones)
            {
                _phones.Add(phone);
            }
        }

        public void DeletePhone(string phone)
        {
            _phones.Remove(phone);
        }

        public void AddCustomFields(string field, string value)
        {
            if (_customFields == null)
            {
                _customFields = new Dictionary<string, string>();
            }

            CustomFields.Add(field, value);
        }

        public void DeleteCustomFields(string field)
        {
            if (CustomFields != null)
            {
                CustomFields.Remove(field);
            }
        }

        public string GetJsonPost()
        {
            var trackingJson = new JObject { { "tracking_number", new JValue(_trackingNumber) } };
            if (_slug != null) trackingJson.Add("slug", new JValue(_slug));
            if (_title != null) trackingJson.Add("title", new JValue(_title));
            if (_emails != null)
            {
                var emailsJson = new JArray(_emails);
                trackingJson["emails"] = emailsJson;
            }

            if (_phones != null)
            {
                var phonesJson = new JArray(_phones);
                trackingJson["smses"] = phonesJson;
            }

            if (_customerName != null) trackingJson.Add("customer_name", new JValue(_customerName));
            if (_destinationCountryIso3 != 0) trackingJson.Add("destination_country_iso3", new JValue(_destinationCountryIso3.ToString()));
            if (_orderId != null) trackingJson.Add("order_id", new JValue(_orderId));
            if (_orderIdPath != null) trackingJson.Add("order_id_path", new JValue(_orderIdPath));
            if (_trackingAccountNumber != null) trackingJson.Add("tracking_account_number", new JValue(_trackingAccountNumber));
            if (_trackingPostalCode != null) trackingJson.Add("tracking_postal_code", new JValue(TrackingPostalCode));
            if (_trackingShipDate != null) trackingJson.Add("tracking_ship_date", new JValue(TrackingShipDate));

            if (_customFields != null)
            {
                var customFieldsJson = new JObject();
                foreach (var pair in _customFields)
                {
                    customFieldsJson.Add(pair.Key, new JValue(pair.Value));
                }

                trackingJson["custom_fields"] = customFieldsJson;
            }

            var globalJson = new JObject { ["tracking"] = trackingJson };

            return globalJson.ToString();
        }

        public string GeneratePutJson()
        {
            var globalJson = new JObject();
            var trackingJson = new JObject();

            if (_title != null) trackingJson.Add("title", new JValue(_title));
            if (_emails != null && _emails.Any())
            {
                var emailsJson = new JArray(_emails);
                trackingJson["emails"] = emailsJson;
            }

            if (_phones != null && _phones.Any())
            {
                var phonesJson = new JArray(_phones);
                trackingJson["smses"] = phonesJson;
            }

            if (_customerName != null) trackingJson.Add("customer_name", new JValue(_customerName));
            if (_orderId != null) trackingJson.Add("order_id", new JValue(_orderId));
            if (_orderIdPath != null) trackingJson.Add("order_id_path", new JValue(_orderIdPath));
            if (_customFields != null)
            {
                var customFieldsJson = new JObject();

                foreach (var pair in _customFields)
                {
                    customFieldsJson.Add(pair.Key, new JValue(pair.Value));
                }

                trackingJson["custom_fields"] = customFieldsJson;
            }

            globalJson["tracking"] = trackingJson;

            return globalJson.ToString();
        }

        public string GetQueryRequiredFields()
        {
            var containsInfo = false;
            var qs = new QueryString();

            if (_trackingAccountNumber != null)
            {
                containsInfo = true;
                qs.Add("tracking_account_number", _trackingAccountNumber);
            }

            if (_trackingPostalCode != null)
            {
                qs.Add("tracking_postal_code", _trackingPostalCode);
                containsInfo = true;
            }

            if (_trackingShipDate != null)
            {
                qs.Add("tracking_ship_date", _trackingShipDate);
                containsInfo = true;
            }

            return containsInfo ? qs.ToString() : string.Empty;
        }

        public override string ToString()
        {
            return string.Format("_id: {0} \n_trackingNumber: {1} \n_slug: {2}", _id, _trackingNumber, _slug);
        }

        #endregion
    }
}