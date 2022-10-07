using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Config;
using PepperDash.Essentials.Core.Devices;
using PepperDash.Essentials.Core.Queues;


namespace ApcEpi.Messages
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