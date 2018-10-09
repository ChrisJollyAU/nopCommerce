/* 
 * This code is taken form AfterShip's GitHub (https://github.com/AfterShip/aftership-sdk-net)
 * and slightly modified for our coding standards. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Nop.Plugin.Tracking.AfterShip.Infrastructure.Aftership.Enums;

namespace Nop.Plugin.Tracking.AfterShip.Infrastructure.Aftership
{
    public class ParametersTracking
    {
        #region Fields

        private IList<string> _slugs;
        private IList<Iso3Country> _origins;
        private IList<Iso3Country> _destinations;
        private IList<StatusTag> _tags;
        private IList<FieldTracking> _fields;
        private int _page;
        private int _limit;
        private int _total;
        private string _keyword;
        private string _lang;
        private DateTime _createdAtMin;
        private DateTime _createdAtMax;

        #endregion

        #region Ctor

        public ParametersTracking()
        {
            _page = 1;
            _limit = 100;
        }

        #endregion

        #region Properties

        public int Page
        {
            get { return _page; }
            set { _page = value; }
        }

        public int Limit
        {
            get { return _limit; }
            set { _limit = value; }
        }

        public string Keyword
        {
            get { return _keyword; }
            set { _keyword = value; }
        }

        public DateTime CreatedAtMin
        {
            get { return _createdAtMin; }
            set { _createdAtMin = value; }
        }

        public DateTime CreatedAtMax
        {
            get { return _createdAtMax; }
            set { _createdAtMax = value; }
        }

        public string Lang
        {
            get { return _lang; }
            set { _lang = value; }
        }

        public int Total
        {
            get { return _total; }
            set { _total = value; }
        }

        #endregion

        #region Methods

        public void AddSlug(string slug)
        {
            if (_slugs == null)
                _slugs = new List<string> { slug };
            else
                _slugs.Add(slug);
        }

        public void DeleteRequireField(string slug)
        {
            if (_slugs != null)
                _slugs.Remove(slug);
        }

        public void DeleteSlugs()
        {
            _slugs = null;
        }

        public void AddOrigin(Iso3Country origin)
        {
            if (_origins == null)
                _origins = new List<Iso3Country> { origin };
            else
                _origins.Add(origin);
        }

        public void DeleteOrigin(Iso3Country origin)
        {
            if (_origins != null)
                _origins.Remove(origin);
        }

        public void DeleteOrigins()
        {
            _origins = null;
        }

        public void AddDestination(Iso3Country destination)
        {
            if (_destinations == null)
                _destinations = new List<Iso3Country> { destination };
            else
                _destinations.Add(destination);
        }

        public void DeleteDestination(Iso3Country destination)
        {
            if (_destinations != null)
                _destinations.Remove(destination);
        }

        public void DeleteDestinations()
        {
            _destinations = null;
        }

        public void AddTag(StatusTag tag)
        {
            if (_tags == null)
                _tags = new List<StatusTag> { tag };
            else
                _tags.Add(tag);
        }

        public void DeletTag(StatusTag tag)
        {
            if (_tags != null)
                _tags.Remove(tag);
        }

        public void DeleteTags()
        {
            _tags = null;
        }

        public void AddField(FieldTracking field)
        {
            if (_fields == null)
                _fields = new List<FieldTracking> { field };
            else
                _fields.Add(field);
        }

        public void DeletField(FieldTracking field)
        {
            if (_fields != null)
                _fields.Remove(field);
        }

        public void DeleteFields()
        {
            _fields = null;
        }

        public string GenerateQueryString()
        {
            var qs = new QueryString("page", Page.ToString());
            qs.Add("limit", Limit.ToString());

            if (Keyword != null) qs.Add("keyword", Keyword);
            if (CreatedAtMin != default(DateTime)) qs.Add("created_at_min", CreatedAtMin.ToIso8601Short());
            if (CreatedAtMax != default(DateTime)) qs.Add("created_at_max", CreatedAtMax.ToIso8601Short());
            if (Lang != null) qs.Add("lang", Lang);
            if (_slugs != null) qs.Add("slug", _slugs);
            if (_origins != null) qs.Add("origin", _origins.Select(o => o.GetIso3Code()));
            if (_destinations != null) qs.Add("destination", _destinations.Select(d => d.GetIso3Code()));
            if (_tags != null) qs.Add("tag", _tags.Select(t => t.GetName()));
            if (_fields != null) qs.Add("fields", _fields.Select(f => f.GetName()));

            return qs.ToString();
        }

        #endregion
    }
}