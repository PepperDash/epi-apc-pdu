using System;
using ApcEpi.Abstractions;
using ApcEpi.JoinMaps;
using ApcEpi.Services.StatusCommands;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro.DeviceSupport;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Bridges;
using Feedback = PepperDash.Essentials.Core.Feedback;

namespace ApcEpi.Devices
{
    public class ApDevice: EssentialsDevice, IOutletName, IOutletPower, IOutletOnline
    {
        private readonly ICommunicationMonitor _monitor;
        private readonly CTimer _poll;
        private readonly ReadOnlyDictionary<uint, IApOutlet> _outlets;

        public ApDevice(IApDeviceBuilder builder)
            : base(builder.Key, builder.Name)
        {
            Feedbacks = new FeedbackCollection<Feedback>();

            _outlets = builder.Outlets;
            _monitor = builder.Monitor;
            _poll = builder.Poll;

            Feedbacks.Add(IsOnline);
        }

        public StatusMonitorBase CommunicationMonitor
        {
            get { return _monitor.CommunicationMonitor; }
        }

        public FeedbackCollection<Feedback> Feedbacks { get; private set; }

        public BoolFeedback IsOnline
        {
            get { return _monitor.CommunicationMonitor.IsOnlineFeedback; }
        }

        public override bool CustomActivate()
        {
            CommunicationMonitor.Start();
            _poll.Reset(2000, 5000);

            foreach (var apOutlet in _outlets.Values)
            {
                Feedbacks.AddRange(new Feedback[]
                    {
                        apOutlet.IsOnline,
                        apOutlet.NameFeedback,
                        apOutlet.PowerIsOnFeedback
                    });
            }

            return true;
        }

        public void LinkToApi(BasicTriList trilist, uint joinStart, string joinMapKey, EiscApiAdvanced bridge)
        {
            var joinMap = new ApDeviceJoinMap(joinStart);

            if (bridge != null)
                bridge.AddJoinMap(Key, joinMap);

            var customJoins = JoinMapHelper.TryGetJoinMapAdvancedForDevice(joinMapKey);

            if (customJoins != null)
                joinMap.SetCustomJoinData(customJoins);

            Debug.Console(1, "Linking to Trilist '{0}'", trilist.ID.ToString("X"));
            Debug.Console(0, "Linking to Bridge Type {0}", GetType().Name);

            trilist.OnlineStatusChange += (device, args) =>
                {
                    if (!args.DeviceOnLine)
                        return;

                    foreach (var feedback in Feedbacks)
                        feedback.FireUpdate();
                };
        }

        public void ToggleOutletPower(uint outletIndex)
        {
            IApOutlet outlet;
            if (_outlets.TryGetValue(outletIndex, out outlet))
            {
                outlet.PowerToggle();
                return;
            }

            Debug.Console(1, this, "Outlet at index-{0} does not exist", outletIndex);
        }

        public bool TryGetOutletNameFeedback(uint outletIndex, out StringFeedback result)
        {
            result = new StringFeedback(Key + "-OutletName-" + outletIndex, () => String.Empty);
            IApOutlet outlet;
            if (!_outlets.TryGetValue(outletIndex, out outlet))
                return false;

            result = outlet.NameFeedback;
            return true;
        }

        public bool TryGetOutletOnlineFeedback(uint outletIndex, out BoolFeedback result)
        {
            result = new BoolFeedback(Key + "-OutletName-" + outletIndex, () => false);
            IApOutlet outlet;
            if (!_outlets.TryGetValue(outletIndex, out outlet))
                return false;

            result = outlet.IsOnline;
            return true;
        }

        public bool TryGetOutletPowerFeedback(uint outletIndex, out BoolFeedback result)
        {
            result = new BoolFeedback(Key + "-OutletPower-" + outletIndex, () => false);
            IApOutlet outlet;
            if (!_outlets.TryGetValue(outletIndex, out outlet))
                return false;

            result = outlet.PowerIsOnFeedback;
            return true;
        }

        public void TurnOutletOff(uint outletIndex)
        {
            IApOutlet outlet;
            if (_outlets.TryGetValue(outletIndex, out outlet))
            {
                outlet.PowerOff();
                return;
            }

            Debug.Console(1, this, "Outlet at index-{0} does not exist", outletIndex);
        }

        public void TurnOutletOn(uint outletIndex)
        {
            IApOutlet outlet;
            if (_outlets.TryGetValue(outletIndex, out outlet))
            {
                outlet.PowerOn();
                return;
            }

            Debug.Console(1, this, "Outlet at index-{0} does not exist", outletIndex);
        }

        public static void PollDevice(IBasicCommunication coms)
        {
            if (coms == null)
                return;

            var command = ApOutletStatusCommands.GetAllOutletStatusCommand();
            coms.SendText(command);
        }
    }
}