/* 
 * This code is taken form AfterShip's GitHub (https://github.com/AfterShip/aftership-sdk-net)
 * and slightly modified for our coding standards. 
 */

using System.Collections.Generic;
using System.Text;

namespace Nop.Plugin.Tracking.AfterShip.Infrastructure.Aftership
{
    /// <summary>
    /// Creates a url friendly String
    /// </summary>
    public class QueryString
    {
        #region Fields

        private string _query = string.Empty;

        #endregion

        #region Ctors

        public QueryString()
        {
        }

        public QueryString(string name, string value)
        {
            Encode(name, value);
        }

        #endregion

        #region Methods

        public void Add(string name, IEnumerable<string> list)
        {
            _query += "&";

            var value = string.Join(",", list);
            Encode(name, value);
        }

        public void Add(string name, string value)
        {
            _query += "&";
            Encode(name, value);
        }

        public override string ToString()
        {
            return _query;
        }

        #endregion

        #region Utils

        private void Encode(string name, string value)
        {
            var sb = new StringBuilder();
            sb.Append(System.Uri.EscapeDataString(name));
            sb.Append("=");
            sb.Append(System.Uri.EscapeDataString(value));
            _query += sb.ToString();
        }

        #endregion
    }
}