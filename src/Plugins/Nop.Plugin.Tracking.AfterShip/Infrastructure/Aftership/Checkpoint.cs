/* 
 * This code is taken form AfterShip's GitHub (https://github.com/AfterShip/aftership-sdk-net)
 * and slightly modified for our coding standards. 
 */

using System;
using Newtonsoft.Json.Linq;
using Nop.Plugin.Tracking.AfterShip.Infrastructure.Aftership.Enums;

namespace Nop.Plugin.Tracking.AfterShip.Infrastructure.Aftership
{
    public class Checkpoint
    {
        #region Fields
 
        private DateTime _createdAt;
        private string _checkpointTime;
        private string _city; 
        private Iso3Country _countryIso3; 
        private string _countryName;
        private string _message;
        private string _state; 
        private string _tag;
        private string _zip;
        private string _location;

        #endregion

        #region Ctor

        public Checkpoint(JObject checkpointJson)
        {
            CheckpointTime = checkpointJson["checkpoint_time"] == null 
                ? null 
                : checkpointJson["checkpoint_time"].ToString();
            City = checkpointJson["city"] == null 
                ? null 
                : checkpointJson["city"].ToString();
            _countryName = checkpointJson["country_name"] == null 
                ? null 
                : checkpointJson["country_name"].ToString();
            _message = checkpointJson["message"] == null 
                ? null 
                : checkpointJson["message"].ToString();
            _state = checkpointJson["state"] == null 
                ? null 
                : checkpointJson["state"].ToString();
            _tag = checkpointJson["tag"] == null 
                ? null 
                : checkpointJson["tag"].ToString();
            _zip = checkpointJson["zip"] == null 
                ? null 
                : checkpointJson["zip"].ToString();
            _location = checkpointJson["location"] == null 
                ? string.Empty
                : checkpointJson["location"].ToString();
            _countryIso3 = checkpointJson["country_iso3"] != null 
                ? checkpointJson["country_iso3"].ToString().ToIso3Enum() 
                : Iso3Country.Null;

            if (checkpointJson["created_at"] != null)
            {
                if (!DateTime.TryParse(checkpointJson["created_at"].ToString(), out _createdAt))
                    _createdAt = DateTime.MinValue;
            }
            else
            {
                _createdAt = DateTime.MinValue;
            }
        }

        #endregion

        #region Properties

        public DateTime CreatedAt
        {
            get { return _createdAt; }
            set { _createdAt = value; }
        }

        public string CheckpointTime
        {
            get { return _checkpointTime; }
            set { _checkpointTime = value; }
        }

        public string City
        {
            get { return _city; }
            set { _city = value; }
        }

        public Iso3Country CountryIso3
        {
            get { return _countryIso3; }
            set { _countryIso3 = value; }
        }

        public string CountryName
        {
            get { return _countryName; }
            set { _countryName = value; }
        }

        public string Message
        {
            get { return _message; }
            set { _message = value; }
        }

        public string State
        {
            get { return _state; }
            set { _state = value; }
        }

        public string Tag
        {
            get { return _tag; }
            set { _tag = value; }
        }

        public string Zip
        {
            get { return _zip; }
            set { _zip = value; }
        }

        public string Location
        {
            get { return _location; }
            set { _location = value; }
        }

        #endregion
    }
}