using System.ServiceModel.Channels;
using System.Xml;

namespace Nop.Plugin.Shipping.Temando
{
    internal class TemandoMessageHeader : MessageHeader {
        internal string UserName { get; set; }
        internal string Password { get; set; }

        // The name of the element describing the security header. 
        public override string Name {
            // The empty string is not a valid local name.
            get { return "Security"; }
        }

        // Namespace of the security header. 
        public override string Namespace {
            get { return string.Empty; }
        }

        protected override void OnWriteHeaderContents(XmlDictionaryWriter writer, MessageVersion messageVersion) {
            writer.WriteStartElement("UsernameToken");
            writer.WriteElementString("Username", UserName);
            writer.WriteElementString("Password", Password);
            writer.WriteEndElement();
        }
    }
}