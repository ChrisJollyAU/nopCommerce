using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Payments.BrainTree.Models
{
    public record PaymentInfoModel : BaseNopModel
    {
        public string Token { get; set; }
        public decimal Amount { get; set; }
        public string BillFirstName { get; internal set; }
        public string BillLastName { get; internal set; }
        public string BillPhoneNumber { get; internal set; }
        public string BillAddress1 { get; internal set; }
        public string BillAddress2 { get; internal set; }
        public string BillCity { get; internal set; }
        public string BillState { get; internal set; }
        public string BillPostCode { get; internal set; }
        public string BillCountry { get; internal set; }
        public string Email { get; internal set; }
        public string ShipFirstName { get; internal set; }
        public string ShipLastName { get; internal set; }
        public string ShipPhoneNumber { get; internal set; }
        public string ShipAddress1 { get; internal set; }
        public string ShipAddress2 { get; internal set; }
        public string ShipPostCode { get; internal set; }
        public string ShipState { get; internal set; }
        public string ShipCity { get; internal set; }
        public string ShipCountry { get; internal set; }
    }
}