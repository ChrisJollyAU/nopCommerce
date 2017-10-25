using Nop.Services.Shipping.Tracking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Plugin.Shipping.Temando
{
    class TemandoShipmentTracker : IShipmentTracker
    {
        public bool IsMatch(string trackingNumber)
        {
            throw new NotImplementedException();
        }

        public string GetUrl(string trackingNumber)
        {
            throw new NotImplementedException();
        }

        public IList<ShipmentStatusEvent> GetShipmentEvents(string trackingNumber)
        {
            throw new NotImplementedException();
        }
    }
}
