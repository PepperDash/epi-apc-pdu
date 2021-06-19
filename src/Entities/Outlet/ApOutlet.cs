using System;
using ApcEpi.Abstractions;
using ApcEpi.Services.NameCommands;
using PepperDash.Core;
using PepperDash.Essentials.Core;

namespace ApcEpi.Entities.Outlet
{

    public class ApOutlet : IApOutlet
    {
        private readonly ApOutletOnline _online;
        private readonly ApOutletPower _power;

        public ApOutlet(string key, string name, int outletIndex, string parentDeviceKey, IBasicCommunication coms)
        {
            Key = key;
            Name = name;
            OutletIndex = outletIndex;
            NameFeedback = new StringFeedback(
                parentDeviceKey + "-" + Key + "-OutletName", 
                () => String.IsNullOrEmpty(Name) ? string.Empty : Name);

            _online = new ApOutletOnline(key, name, outletIndex);
            _power = new ApOutletPower(key, name, outletIndex, coms);

            var socket = coms as ISocketStatus;
            if (socket != null)
            {
                socket.ConnectionChange += (sender, args) =>
                {
                    if (!args.Client.IsConnected)
                        return;

                    var outletNameCommand = ApOutletNameCommands
                        .GetOutletNameCommand(outletIndex, key);

                    //coms.SendText(outletNameCommand);
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

        public bool PowerStatus
        {
            get { return _power.PowerStatus; }
            set
            {
                _power.PowerStatus = value;
            }
        }

        public void SetIsOnline()
        {
            _online.SetIsOnline();
        }
    }
}