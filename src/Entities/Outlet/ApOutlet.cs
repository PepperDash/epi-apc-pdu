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
                () => String.IsNullOrEmpty(Name) ? Key : Name);

            _online = new ApOutletOnline(key, name, outletIndex);
            _power = new ApOutletPower(key, name, outletIndex, coms);
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

        public void ProcessResponse(string response)
        {
            _online.ProcessResponse(response);
            _power.ProcessResponse(response);
        }
    }
}