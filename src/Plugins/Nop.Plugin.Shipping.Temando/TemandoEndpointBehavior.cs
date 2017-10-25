using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

namespace Nop.Plugin.Shipping.Temando
{
    internal class TemandoEndpointBehavior : IEndpointBehavior
    {
        internal string UserName { get; set; }
        internal string Password { get; set; }

        #region IEndpointBehavior Members

        public void Validate(ServiceEndpoint endpoint)
        {
        }

        public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
        {
        }

        public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
        {
        }

        public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
        {
            var inspector = new TemandoMessageInspector { UserName = UserName, Password = Password };
            clientRuntime.MessageInspectors.Add(inspector);
        }

        #endregion
    }
}