using System;
using ApcEpi.Abstractions;
using ApcEpi.Services.NameCommands;
using ApcEpi.Services.PowerCommands;
using PepperDash.Core;
using PepperDash.Essentials.Core;

namespace ApcEpi.Entities.Outlet
{
    public class ApOutlet : IApOutlet
    {
        private readonly IBasicCommunication _coms;
        private readonly int _outletIndex;
        private readonly IOnline _online;
        private readonly IPower _power;

        public ApOutlet(string key, string name, int outletIndex, IBasicCommunication coms)
        {
            Key = key;
            Name = name;
            OutletIndex = outletIndex;
            _coms = coms;
            _outletIndex = outletIndex;

            NameFeedback = new StringFeedback(
                Key + "-OutletName", 
                () => String.IsNullOrEmpty(Name) ? Key : Name);

            _online = new ApOutletOnline(key, name, outletIndex, coms);
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

                        coms.SendText(outletNameCommand);
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

        public void CyclePower()
        {
            var cmd = ApOutletPowerCommands.GetRebootCommand(_outletIndex);
            _coms.SendText(cmd);
        }

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