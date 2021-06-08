using System;
using ApcEpi.Abstractions;
using ApcEpi.Services.NameCommands;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Queues;

namespace ApcEpi.Entities.Outlet
{
    public class ApOutlet : IApOutlet
    {
        private readonly IOnline _online;
        private readonly IPower _power;

        public ApOutlet(string key, string name, int outletIndex, GenericQueue txQueue, CommunicationGather gather)
        {
            Key = key;
            Name = name;
            OutletIndex = outletIndex;
            NameFeedback = new StringFeedback(
                Key + "-OutletName",
                () => string.IsNullOrEmpty(Name) ? string.Empty : Name);

            _online = new ApOutletOnline(key, name, outletIndex, gather);
            _power = new ApOutletPower(key, name, outletIndex, gather, txQueue);

            var socket = gather.Port as ISocketStatus;
            if (socket != null)
            {
                socket.ConnectionChange += (sender, args) =>
                    {
                        if (!args.Client.IsConnected)
                            return;

                        var outletNameCommand = ApOutletNameCommands
                            .GetOutletNameCommand(outletIndex, key);

                        txQueue.Enqueue(new ComsMessage(gather.Port as IBasicCommunication, outletNameCommand));
                    };
            }
        }

        public BoolFeedback IsOnline
        {
            get { return _online.IsOnline; }
        }

        public string Key { get; private set; }
        public string Name { get; private set; }
        public StringFeedback NameFeedback { get; private set; }
        public int OutletIndex { get; private set; }

        public BoolFeedback PowerIsOnFeedback
        {
            get { return _power.PowerIsOnFeedback; }
        }

        public static string GetMatchString(int outletIndex)
        {
            return Convert.ToString(outletIndex).PadLeft(2, ' ');
        }

        public void PowerOff()
        {
            _power.PowerOff();
        }

        public void PowerOn()
        {
            _power.PowerOn();
        }

        public void PowerToggle()
        {
            _power.PowerToggle();
        }
    }
}