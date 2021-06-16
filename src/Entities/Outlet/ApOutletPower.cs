using System;
using System.Linq;
using ApcEpi.Services.PowerCommands;
using Crestron.SimplSharp;
using PepperDash.Core;
using PepperDash.Essentials.Core;

namespace ApcEpi.Entities.Outlet
{
    public class ApOutletPower : IPower, IKeyName
    {
        private readonly IBasicCommunication _coms;

        private readonly string _matchString;
        private readonly string _powerOffCommand;
        private readonly string _powerOnCommand;
        private bool _powerIsOn;

        public ApOutletPower(string key, string name, int outletIndex, IBasicCommunication coms)
        {
            Key = key;
            Name = name;
            _coms = coms;
            _matchString = ApOutlet.GetMatchString(outletIndex);

            PowerIsOnFeedback = new BoolFeedback(
                key + "-Power",
                () => _powerIsOn);

            _powerOnCommand = ApOutletPowerCommands.GetPowerOnCommand(outletIndex);
            _powerOffCommand = ApOutletPowerCommands.GetPowerOffCommand(outletIndex);
        }

        public enum PowerResponseEnum
        {
            On,
            Off,
            Unknown
        }

        public string Key { get; private set; }
        public string Name { get; private set; }

        public BoolFeedback PowerIsOnFeedback { get; private set; }

        public void PowerOff()
        {
            if (!_powerIsOn)
                return;

            _coms.SendText(_powerOffCommand);
        }

        public void PowerOn()
        {
            if (_powerIsOn)
                return;

            _coms.SendText(_powerOnCommand);
        }

        public void PowerToggle()
        {
            if (_powerIsOn)
                PowerOff();
            else
                PowerOn();
        }

        private static string GetDataPayloadFromResponse(string response)
        {
            response = response.Replace(" ", String.Empty);
            var splitResponse = response.Split(':');

            return splitResponse.ElementAtOrDefault(2) ?? String.Empty;
        }

        private static PowerResponseEnum GetOutletStatusFromResponse(string response)
        {
            var data = GetDataPayloadFromResponse(response);
            if (String.IsNullOrEmpty(data))
                return PowerResponseEnum.Unknown;

            try
            {
                return (PowerResponseEnum) Enum.Parse(typeof (PowerResponseEnum), data, true);
            }
            catch (Exception ex)
            {
                return PowerResponseEnum.Unknown;
            }
        }

        public void ProcessResponse(string response)
        {
            if (!response.StartsWith(_matchString))
                return;

            var status = GetOutletStatusFromResponse(response);

            switch (status)
            {
                case PowerResponseEnum.On:
                    _powerIsOn = true;
                    break;
                case PowerResponseEnum.Off:
                    _powerIsOn = false;
                    break;
                case PowerResponseEnum.Unknown:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            PowerIsOnFeedback.FireUpdate();
        }
    }
}