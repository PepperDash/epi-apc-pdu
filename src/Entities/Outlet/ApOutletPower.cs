using System;
using System.Linq;
using ApcEpi.Services.PowerCommands;
using ApcEpi.Services.StatusCommands;
using Crestron.SimplSharp;
using PepperDash.Core;
using PepperDash.Essentials.Core;

namespace ApcEpi.Entities.Outlet
{
    public class ApOutletPower : IPower, IKeyName
    {
        private readonly IBasicCommunication _coms;

        private readonly string _powerOffCommand;
        private readonly string _powerOnCommand;
        private bool _powerIsOn;
        private readonly CTimer _poll;

        public ApOutletPower(string key, string name, int outletIndex, IBasicCommunication coms)
        {
            Key = key;
            Name = name;
            _coms = coms;

            PowerIsOnFeedback = new BoolFeedback(
                key + "-Power",
                () => _powerIsOn);

            var pollCommand = ApOutletStatusCommands.GetOutletStatusCommand(outletIndex);
            _poll = new CTimer(o => coms.SendText(pollCommand), Timeout.Infinite);

            _powerOnCommand = ApOutletPowerCommands.GetPowerOnCommand(outletIndex);
            _powerOffCommand = ApOutletPowerCommands.GetPowerOffCommand(outletIndex);
        }

        public string Key { get; private set; }
        public string Name { get; private set; }

        public BoolFeedback PowerIsOnFeedback { get; private set; }

        public void PowerOff()
        {
            if (!_powerIsOn)
                return;

            _coms.SendText(_powerOffCommand);
            _poll.Reset(1000);
        }

        public void PowerOn()
        {
            if (_powerIsOn)
                return;

            _coms.SendText(_powerOnCommand);
            _poll.Reset(1000);
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

        public bool PowerStatus
        {
            get { return _powerIsOn; }
            set
            {
                _powerIsOn = value;
                PowerIsOnFeedback.FireUpdate();
            }
        }
    }
}