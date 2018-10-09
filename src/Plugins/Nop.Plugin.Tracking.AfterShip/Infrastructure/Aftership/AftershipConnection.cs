/* 
 * This code is taken form AfterShip's GitHub (https://github.com/AfterShip/aftership-sdk-net)
 * and slightly modified for our coding standards. 
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Newtonsoft.Json.Linq;
using Nop.Plugin.Tracking.AfterShip.Infrastructure.Aftership.Enums;

namespace Nop.Plugin.Tracking.AfterShip.Infrastructure.Aftership
{
    /// <summary>
    /// Connection API. Connect with the API of Afthership
    /// </summary>
    public class AftershipConnection
    {
        #region Fields

        private static string URL_SERVER = "https://api.aftership.com/";
        private static string VERSION_API = "v4";

        private readonly string _tokenAftership;
        private readonly string _url;

        #endregion

        #region Ctor

        /// <summary>
        /// Constructor ConnectionAPI.
        /// </summary>
        /// <param name="tokenAfthership"> Afthership token for the connection.</param>
        /// <param name="url">Afthership API url.</param>
        /// <returns></returns>
        public AftershipConnection(string tokenAfthership, string url = null)
        {
            _tokenAftership = tokenAfthership;
            _url = url ?? URL_SERVER;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Updates a tracking of your account.
        /// </summary>
        /// <param name="tracking">
        /// A Tracking object with the information to update
        /// The fields trackingNumber and slug SHOULD be informed, otherwise an exception will be thrown
        /// The fields an user can update are: smses, emails, title, customerName, orderID, orderIDPath,
        /// customFields.
        /// </param>
        /// <returns>The last Checkpoint object.</returns>
        public Tracking PutTracking(Tracking tracking)
        {
            string parametersExtra;
            if (!string.IsNullOrEmpty(tracking.Id))
            {
                parametersExtra = tracking.Id;
            }
            else
            {
                var paramRequiredFields = ReplaceFirst(tracking.GetQueryRequiredFields(), "&", "?");
                parametersExtra = string.Format("{0}/{1}{2}", tracking.Slug, tracking.TrackingNumber, paramRequiredFields);
            }

            var response = Request("PUT", string.Format("/trackings/{0}", parametersExtra), tracking.GeneratePutJson());

            return new Tracking((JObject)response["data"]["tracking"]);
        }

        /// <summary>
        /// Return the tracking information of the last checkpoint of a single tracking.
        /// </summary>
        /// <param name="tracking">A Tracking to get the last checkpoint of, it should have tracking number and slug at least.</param>
        /// <returns>The last Checkpoint object.</returns>
        public Checkpoint GetLastCheckpoint(Tracking tracking)
        {
            string parametersExtra;
            Checkpoint checkpoint = null;

            if (!string.IsNullOrEmpty(tracking.Id))
            {
                parametersExtra = tracking.Id;
            }
            else
            {
                var paramRequiredFields = ReplaceFirst(tracking.GetQueryRequiredFields(), "&", "?");
                parametersExtra = string.Format("{0}/{1}{2}", tracking.Slug, tracking.TrackingNumber, paramRequiredFields);
            }

            var response = Request("GET", string.Format("/last_checkpoint/{0}", parametersExtra), null);
            var checkpointJson = (JObject)response["data"]["checkpoint"];

            if (checkpointJson.Count != 0)
            {
                checkpoint = new Checkpoint(checkpointJson);
            }

            return checkpoint;
        }

        /// <summary>
        /// Return the tracking information of the last checkpoint of a single tracking.
        /// </summary>
        /// <param name="tracking">A Tracking to get the last checkpoint of, it should have tracking number and slug at least.</param>
        /// <param name="fields">A list of fields of checkpoint wanted to be in the response.</param>
        /// <param name="lang">
        /// A string with the language desired. Support Chinese to English translation
        /// for china-ems and china-post only.
        /// </param>
        /// <returns>The last Checkpoint object.</returns>
        public Checkpoint GetLastCheckpoint(Tracking tracking, List<FieldCheckpoint> fields, string lang)
        {
            var qs = new QueryString();
            string parametersExtra;
            Checkpoint checkpoint = null;

            if (fields != null) qs.Add("fields", fields.Select(f => f.GetName()));
            if (string.IsNullOrEmpty(lang)) qs.Add("lang", lang);

            var parameters = ReplaceFirst(qs.ToString(), "&", "?");
            
            if (!string.IsNullOrEmpty(tracking.Id))
            {
                parametersExtra = string.Format("{0}{1}", tracking.Id, parameters);
            }
            else
            {
                var paramRequiredFields = tracking.GetQueryRequiredFields();
                parametersExtra = string.Format("{0}/{1}{2}{3}", tracking.Slug, tracking.TrackingNumber, parameters, paramRequiredFields);
            }

            var response = Request("GET", string.Format("/last_checkpoint/{0}", parametersExtra), null);
            var checkpointJson = (JObject)response["data"]["checkpoint"];
            
            if (checkpointJson.Count != 0)
                checkpoint = new Checkpoint(checkpointJson);

            return checkpoint;
        }

        /// <summary>
        /// Retrack an expired tracking once.
        /// </summary>
        /// <param name="tracking">A Tracking to reactivate, it should have tracking number and slug at least.</param>
        /// <returns></returns>
        public bool Retrack(Tracking tracking)
        {
            var paramRequiredFields = ReplaceFirst(tracking.GetQueryRequiredFields(), "&", "?");
            var response = Request(
                "POST",
                string.Format("/trackings/{0}/{1}/retrack{2}", tracking.Slug, tracking.TrackingNumber, paramRequiredFields), 
                null);

            if ((int)response["meta"]["code"] == 200)
            {
                return (bool)response["data"]["tracking"]["active"];
            }

            return false;
        }

        /// <summary>
        /// Returns a list of trackers on the page number.
        /// </summary>
        /// <param name="page">Page number.</param>
        /// <returns>List of trackers.</returns>
        public IList<Tracking> GetTrackings(int page)
        {
            IList<Tracking> trackingList = null;

            var response = Request(
                "GET", 
                string.Format("/trackings?limit=100&page={0}", page), 
                null);
            var trackingJson = (JArray)response["data"]["trackings"];
            if (trackingJson.Count != 0)
            {
                trackingList = trackingJson.Select(t => new Tracking((JObject)t)).ToList();
            }

            return trackingList;
        }

        /// <summary>
        /// Get trackings from your account with the ParametersTracking defined in the params.
        /// </summary>
        /// <param name="parameters">ParametersTracking Object, with the information to get.</param>
        /// <returns>A List of Tracking Objects from your account.</returns>
        public IList<Tracking> GetTrackings(ParametersTracking parameters)
        {
            IList<Tracking> trackingList = null;
            var response = Request("GET", string.Format("/trackings?{0}", parameters.GenerateQueryString()), null);
            var trackingJson = (JArray)response["data"]["trackings"];
            if (trackingJson.Count != 0)
            {
                var size = (int)response["data"]["count"];
                trackingList = new List<Tracking>();
                foreach (var token in trackingJson)
                {
                    trackingList.Add(new Tracking((JObject)token));
                }

                parameters.Total = size;
            }

            return trackingList;
        }

        /// <summary>
        /// Return a list of couriers supported by AfterShip along with their names, URLs and slugs.
        /// </summary>
        /// <returns>A list of Object Courier, with all the couriers supported by the API.</returns>
        public IList<Courier> GetAllCouriers()
        { 
            var response = Request("GET", "/couriers/all", null);
            var couriersJson = (JArray)response["data"]["couriers"];
            var couriers = new List<Courier>(couriersJson.Count);

            foreach (JToken token in couriersJson)
            {
                var element = (JObject)token;
                var newCourier = new Courier(element);
                couriers.Add(newCourier);
            }

            return couriers;
        }

        /// <summary>
        /// Return a list of user couriers enabled by user's account along their names, URLs, slugs, required fields.
        /// </summary>
        /// <returns>A list of Object Courier, with all the couriers supported by the API.</returns>
        public IList<Courier> GetCouriers()
        {
            var response = Request("GET", "/couriers", null);
            var couriersJson = (JArray)response["data"]["couriers"];
            var couriers = new List<Courier>(couriersJson.Count);

            foreach (var token in couriersJson)
            {
                var element = (JObject)token;
                var newCourier = new Courier(element);
                couriers.Add(newCourier);
            }

            return couriers;
        }

        /// <summary>
        /// Get a list of matched couriers for a tracking number based on the tracking number format 
        /// Note, only check the couriers you have defined in your account.
        /// </summary>
        /// <param name="trackingNumber"> tracking number to match with couriers.</param>
        /// <returns>A List of Couriers objects that match the provided trackingNumber.</returns>
        public IList<Courier> DetectCouriers(string trackingNumber)
        {
            var body = new JObject();
            var tracking = new JObject();
            var couriers = new List<Courier>();

            if (string.IsNullOrEmpty(trackingNumber))
                throw new ArgumentException("The tracking number should be always informed for the method detectCouriers");

            tracking.Add("tracking_number", new JValue(trackingNumber));
            body.Add("tracking", tracking);

            var response = Request("POST", "/couriers/detect", body.ToString());
            var couriersJson = (JArray)response["data"]["couriers"];

            foreach (JToken token in couriersJson)
            {
                var element = (JObject)token;
                var newCourier = new Courier(element);
                couriers.Add(newCourier);
            }

            return couriers;
        }

        /// <summary>
        /// Get a list of matched couriers for a tracking number based on the tracking number format Note, only check the couriers you have defined in your account
        /// Note, only check the couriers you have defined in your account.
        /// </summary>
        /// <param name="trackingNumber">Tracking number to match with couriers (mandatory).</param>
        /// <param name="trackingPostalCode">Tracking number to match with couriers.</param>
        /// <param name="trackingShipDate">
        /// Sually it is refer to the posting date of the shipment, format in YYYYMMDD.
        /// Required by some couriers, such as 'deutsch-post'.(optional)
        /// </param>
        /// <param name="trackingAccountNumber">
        /// The account number for particular courier. Required by some couriers, 
        /// such as 'dynamic-logistics'.(optional)
        /// </param>
        /// <param name="slugs">The slug of couriers to detect.</param>
        /// <returns>A List of Couriers objects that match the provided trackingNumber.</returns>
        public IList<Courier> DetectCouriers(
            string trackingNumber, 
            string trackingPostalCode, 
            string trackingShipDate,
            string trackingAccountNumber, 
            params string[] slugs)
        {
            var body = new JObject();
            var tracking = new JObject();
            var couriers = new List<Courier>();

            if (string.IsNullOrEmpty(trackingNumber))
                throw new ArgumentException("Tracking number should be always informed for the method detectCouriers");

            tracking.Add("tracking_number", new JValue(trackingNumber));

            if (string.IsNullOrEmpty(trackingPostalCode))
                tracking.Add("tracking_postal_code", new JValue(trackingPostalCode));

            if (string.IsNullOrEmpty(trackingShipDate))
                tracking.Add("tracking_ship_date", new JValue(trackingShipDate));

            if (string.IsNullOrEmpty(trackingAccountNumber))
                tracking.Add("tracking_account_number", new JValue(trackingAccountNumber));

            if (slugs != null && slugs.Length != 0)
            {
                var slugsJson = new JArray(slugs);
                tracking.Add("slug", slugsJson);
            }

            body.Add("tracking", tracking);

            var response = Request("POST", "/couriers/detect", body.ToString());
            var couriersJson = (JArray)response["data"]["couriers"];

            foreach (var token in couriersJson)
            {
                var element = (JObject)token;
                var newCourier = new Courier(element);
                couriers.Add(newCourier);
            }

            return couriers;
        }

        /// <summary>
        /// Get next page of Trackings from your account with the ParametersTracking defined in the params.
        /// </summary>
        /// <param name="parameters">ParametersTracking Object, with the information to get.</param>
        /// <returns> The next page of Tracking Objects from your account.</returns>
        public IList<Tracking> GetTrackingsNext(ParametersTracking parameters)
        {
            parameters.Page = parameters.Page + 1;
            return GetTrackings(parameters);
        }

        /// <summary>
        /// A Tracking object with the information to creates
        /// The field trackingNumber SHOULD be informed, otherwise an exception will be thrown
        /// The fields an user can add are: Slug, Phones, Emails, Title, CustomerName, OrderId, OrderIdPath,
        /// CustomFields, DestinationCountryIso3 (the others are provided by the Server).
        /// </summary>
        /// <param name="tracking">Tracking.</param>
        /// <returns> 
        /// A Tracking object with the fields in the same state as the server, if a field has an error,
        /// it won't be added, and won't be shown in the response (for example if the smses
        /// phone number is not valid). This response doesn't have checkpoints informed!
        /// </returns>
        public Tracking CreateTracking(Tracking tracking)
        {
            var trackingJson = tracking.GetJsonPost();
            var response = Request("POST", "/trackings", trackingJson);

            return new Tracking((JObject)response["data"]["tracking"]);
        }

        /// <summary>
        /// Delete a tracking from your account, if the tracking.id property is defined
        /// it will delete that tracking from the system, if not it will take the 
        /// tracking tracking.Number and the tracking.Slug for identify the tracking.
        /// </summary>
        /// <param name="tracking">A Tracking to delete</param>
        /// <returns>A boolean, true if delete correctly, and false otherwise</returns>
        public bool DeleteTracking(Tracking tracking)
        {
            var parametersAll = !string.IsNullOrEmpty(tracking.Id) 
                ? tracking.Id
                : string.Format("{0}/{1}", tracking.Slug, tracking.TrackingNumber);

            var response = Request("DELETE", string.Format("/trackings/{0}", parametersAll), null);

            return Convert.ToInt32(response["meta"]["code"].ToString()) == 200;
        }

        /// <summary>
        /// Get a specific tracking from your account. If the trackingGet.id property 
        /// is defined it will get that tracking from the system, if not it will take 
        /// the tracking tracking.number and the tracking.slug for identify the tracking.
        /// </summary>
        /// <param name="tracking">A Tracking to get.</param>
        /// <returns>A Tracking object with the response.</returns>
        public Tracking GetTrackingByNumber(Tracking tracking)
        {
            string parametersExtra;
            if (!string.IsNullOrEmpty(tracking.Id))
            {
                parametersExtra = tracking.Id;
            }
            else
            {
                var paramRequiredFields = ReplaceFirst(tracking.GetQueryRequiredFields(), "&", "?");

                parametersExtra = string.Format("{0}/{1}{2}", tracking.Slug, tracking.TrackingNumber, paramRequiredFields);
            }

            var response = Request("GET", string.Format("/trackings/{0}", parametersExtra), null);
            var trackingJson = (JObject)response["data"]["tracking"];

            return trackingJson.Count != 0 ? new Tracking(trackingJson) : null;
        }

        /// <summary>
        /// Get a specific tracking from your account. If the trackingGet.id property 
        /// is defined it will get that tracking from the system, if not it will take 
        /// the tracking tracking.number and the tracking.slug for identify the tracking.
        /// </summary>
        /// <param name="tracking">A Tracking to get.</param>
        /// <param name="fields">A list of fields wanted to be in the response.</param>
        /// <param name="lang">
        /// A string with the language desired. Support Chinese to 
        /// English translation for china-ems and china-post only.
        /// </param>
        /// <returns></returns>
        public Tracking GetTrackingByNumber(Tracking tracking, List<FieldTracking> fields, string lang)
        {
            string parametersAll;
            var qs = new QueryString();

            if (fields != null) qs.Add("fields", ParseListFieldTracking(fields));
            if (!string.IsNullOrEmpty(lang)) qs.Add("lang", lang);

            var paramsQuery = ReplaceFirst(qs.ToString(), "&", "?");

            if (!string.IsNullOrEmpty(tracking.Id))
            {
                parametersAll = string.Format("{0}{1}", tracking.Id, paramsQuery);
            }
            else
            {
                var paramRequiredFields = tracking.GetQueryRequiredFields();
                parametersAll = string.Format("{0}/{1}{2}{3}", tracking.Slug, tracking.TrackingNumber, paramsQuery, paramRequiredFields);
            }

            var response = Request("GET", string.Format("/trackings/{0}", parametersAll), null);
            var trackingJson = (JObject)response["data"]["tracking"];

            return trackingJson.Count != 0 ? new Tracking(trackingJson) : null;
        }

        public IList<string> ParseListFieldTracking(IList<FieldTracking> list)
        {
            return list.Select(element => element.ToString()).ToList();
        }

        public string ReplaceFirst(string text, string search, string replace)
        {
            var pos = text.IndexOf(search, StringComparison.Ordinal);
            return pos < 0 ? text : text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
        }

        #endregion

        #region Utils

        /// <summary>
        /// Make a request to the HTTP API of Aftership
        /// </summary>
        /// <param name="method">string with the method of the request: GET, POST, PUT, DELETE</param> 
        /// <param name="urlResource">string with the URL of the request</param> 
        /// <param name="body">string JSON with the body of the request, 
        /// if the request doesn't need body "GET/DELETE", the bodywould be null</param> 
        /// <returns>A string JSON with the response of the request</returns>
        private JObject Request(string method, string urlResource, string body)
        {
            var url = string.Format("{0}{1}{2}", _url, VERSION_API, urlResource);
            var jsonResponse = string.Empty;
            var header = new WebHeaderCollection { { "aftership-api-key", _tokenAftership } };
            var request = (HttpWebRequest)WebRequest.Create(url);

            request.Timeout = 150000;
            request.Headers = header;
            request.ContentType = "application/json";
            request.Method = method;

            if (body != null)
            {
                using (var streamWriter = new StreamWriter(request.GetRequestStream()))
                {
                    streamWriter.Write(body);
                    streamWriter.Flush();
                    streamWriter.Close();
                }
            }

            try
            {
                var response = (HttpWebResponse)request.GetResponse();
                var stream = response.GetResponseStream();
                if (stream != null)
                {
                    using (var reader = new StreamReader(stream))
                    {
                        jsonResponse = reader.ReadToEnd();
                    }
                }
            }
            catch (WebException ex)
            {
                if (ex.Response == null)
                {
                    throw ex;
                }

                var response = ex.Response as HttpWebResponse;
                if (response == null) return JObject.Parse(jsonResponse);

                using (var stream = response.GetResponseStream())
                {
                    if (stream == null) return JObject.Parse(jsonResponse);

                    using (var reader = new StreamReader(stream))
                    {
                        var text = reader.ReadToEnd();
                        throw new WebException(text, ex);
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return JObject.Parse(jsonResponse);
        }

        #endregion
    }
}