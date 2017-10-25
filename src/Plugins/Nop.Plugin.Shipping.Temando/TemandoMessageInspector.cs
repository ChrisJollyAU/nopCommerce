using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;

namespace Nop.Plugin.Shipping.Temando
{
    internal class TemandoMessageInspector : IClientMessageInspector {
        internal string UserName { get; set; }
        internal string Password { get; set; }

        #region IClientMessageInspector Members

        public void AfterReceiveReply(ref Message reply, object correlationState) {
        }

        public object BeforeSendRequest(ref Message request, IClientChannel channel) {
            if (request != null) {
                request.Headers.Add(new TemandoMessageHeader {UserName = UserName, Password = Password});
            }
            return null;
        }

        #endregion
    }
}