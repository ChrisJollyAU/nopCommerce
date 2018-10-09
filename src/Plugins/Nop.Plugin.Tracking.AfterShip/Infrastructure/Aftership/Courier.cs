/* 
 * This code is taken form AfterShip's GitHub (https://github.com/AfterShip/aftership-sdk-net)
 * and slightly modified for our coding standards. 
 */

using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Nop.Plugin.Tracking.AfterShip.Infrastructure.Aftership
{
    public class Courier
    {
        #region Fields

        private string _slug;
        private string _name;
        private string _phone;
        private string _otherName;
        private string _webUrl;
        private IList<string> _requireFields;

        #endregion

        #region Ctors

        public Courier(string webUrl, string slug, string name, string phone, string otherName)
        {
            _webUrl = webUrl;
            _slug = slug;
            _name = name;
            _phone = phone;
            _otherName = otherName;
        }

        public Courier(JObject jsonCourier)
        {
            _webUrl = jsonCourier["web_url"] == null 
                ? null 
                : jsonCourier["web_url"].ToString();
            _slug = jsonCourier["slug"] == null 
                ? null 
                : jsonCourier["slug"].ToString();
            _name = jsonCourier["name"] == null 
                ? null 
                : jsonCourier["name"].ToString();
            _phone = jsonCourier["phone"] == null 
                ? null 
                : jsonCourier["phone"].ToString();
            _otherName = jsonCourier["other_name"] == null 
                ? null 
                : jsonCourier["other_name"].ToString();

            var requireFieldsArray = jsonCourier["required_fields"] == null 
                ? null 
                : (JArray)jsonCourier["required_fields"];

            if (requireFieldsArray == null || !requireFieldsArray.Any()) return;

            _requireFields = new List<string>();

            foreach (var token in requireFieldsArray)
            {
                _requireFields.Add(token.ToString());
            }
        }

        #endregion

        #region Fields

        [JsonProperty("slug")]
        public string Slug
        {
            get { return _slug; }
            set { _slug = value; }
        }

        [JsonProperty("name")]
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        [JsonProperty("phone")]
        public string Phone
        {
            get { return _phone; }
            set { _phone = value; }
        }

        [JsonProperty("other_name")]
        public string OtherName
        {
            get { return _otherName; }
            set { _otherName = value; }
        }

        [JsonProperty("web_url")]
        public string WebUrl
        {
            get { return _webUrl; }
            set { _webUrl = value; }
        }

        [JsonIgnore]
        public IList<string> RequireFields
        {
            get { return _requireFields; }
            set { _requireFields = value; }
        }

        #endregion

        #region Methods

        public void AddRequireField(string requierField)
        {
            if (_requireFields == null)
                _requireFields = new List<string> { requierField };
            else
                _requireFields.Add(requierField);
        }

        public void DeleteRequireField(string requireField)
        {
            if (_requireFields != null)
                _requireFields.Remove(requireField);
        }

        public void DeleteRequireFields()
        {
            _requireFields = null;
        }

        public override string ToString()
        {
            var courierJson = new JObject { Slug, Name, Phone, OtherName, WebUrl };

            return string.Format("Courier{0}", courierJson);
        }

        #endregion
    }
}