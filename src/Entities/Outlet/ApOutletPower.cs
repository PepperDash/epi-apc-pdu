using System;
using System.Linq;
using ApcEpi.Services.PowerCommands;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Queues;

namespace ApcEpi.Entities.Outlet
{
    public class ApOutletPower : IPower, IKeyName
    {
        private readonly GenericQueue _queue;
        private readonly IBasicCommunication _coms;

        private readonly string _matchString;
        private readonly string _powerOffCommand;
        private readonly string _powerOnCommand;
        private bool _powerIsOn;

        public ApOutletPower(string key, string name, int outletIndex, CommunicationGather gather, GenericQueue queue)
        {
            _queue = queue;
            Key = key;
            Name = name;
            _coms = gather.Port as IBasicCommunication;
            _matchString = ApOutlet.GetMatchString(outletIndex);

            PowerIsOnFeedback = new BoolFeedback(
                key + "-Power",
                () => _powerIsOn);

            gather.LineReceived += GatherOnLineReceived;

            _powerOnCommand = ApOutletPowerCommands.GetPowerOnCommand(outletIndex);
            _powerOffCommand = ApOutletPowerCommands.GetPowerOffCommand(outletIndex);
        }

        private enum PowerResponseEnum
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

            _queue.Enqueue(new ComsMessage(_coms, _powerOffCommand));
        }

        public void PowerOn()
        {
            if (_powerIsOn)
                return;

            _queue.Enqueue(new ComsMessage(_coms, _powerOnCommand));
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

        private PowerResponseEnum GetOutletStatusFromResponse(string response)
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
                Debug.Console(1, this, "Could not process response : {0} {1}", response, ex.Message);
                return PowerResponseEnum.Unknown;
            }
        }

        private void GatherOnLineReceived(object sender, GenericCommMethodReceiveTextArgs args)
        {
            if (!args.Text.StartsWith(_matchString))
                return;
   
            var status = GetOutletStatusFromResponse(args.Text);

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
            }

            PowerIsOnFeedback.FireUpdate();
        }
    }
}