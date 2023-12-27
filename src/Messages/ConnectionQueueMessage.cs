using PepperDash.Core;
using PepperDash.Essentials.Core.Queues;

namespace PepperDash.Essentials.Apc.Messages
{
    public class ConnectionQueueMessage : IQueueMessage
    {
        private readonly IBasicCommunication _coms;

        public ConnectionQueueMessage(IBasicCommunication coms)
        {
            _coms = coms;
        }

        public void Dispatch()
        {
            if (_coms == null)
                return;

            _coms.Connect();
        }
    }
}