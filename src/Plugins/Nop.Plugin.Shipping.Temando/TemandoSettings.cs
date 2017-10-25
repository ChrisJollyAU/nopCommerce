
using Nop.Core.Configuration;

namespace Nop.Plugin.Shipping.Temando
{
    public class TemandoSettings : ISettings
    {
        public string Username { get; set; }

        public string Password { get; set; }

        public decimal Threshold { get; set; }
    }
}