using System;
using System.Linq;
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
    public class ApDevice: EssentialsDevice, IOutletName, IOutletPower, IOutletOnline, IBridgeAdvanced
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

            var socket = builder.Coms as ISocketStatus;
            if (socket != null)
            {
                socket.ConnectionChange +=
                    (sender, args) =>
                        {
                            if (args.Client.IsConnected)
                                _poll.Reset(2000, 30000);
                            else
                            {
                                _poll.Stop();
                            }
                        };
            }

            AddPostActivationAction(() => builder.Coms.Connect());

            NameFeedback = new StringFeedback("DeviceNameFeedback", () => builder.Name);
            Feedbacks.Add(IsOnline);
            Feedbacks.Add(NameFeedback);
        }

        public StatusMonitorBase CommunicationMonitor
        {
            get { return _monitor.CommunicationMonitor; }
        }

        public StringFeedback NameFeedback { get; private set; }
        public FeedbackCollection<Feedback> Feedbacks { get; private set; }

        public BoolFeedback IsOnline
        {
            get { return _monitor.CommunicationMonitor.IsOnlineFeedback; }
        }

        public override bool CustomActivate()
        {
            foreach (var apOutletFeedbacks in _outlets
                .Values
                .Select(x => new Feedback[]
                    {
                        x.IsOnline, 
                        x.NameFeedback, 
                        x.PowerIsOnFeedback
                    }))
            {
                Feedbacks.AddRange(apOutletFeedbacks);
            }

            foreach (var feedback in Feedbacks)
            {
                feedback.OutputChange += (sender, args) =>
                    {
                        var fb = sender as Feedback;
                        if (fb != null && String.IsNullOrEmpty(fb.Key))
                            return;

                        if (fb is BoolFeedback)
                            Debug.Console(1, this, "Received update from {0} | {1}", fb.Key, fb.BoolValue);

                        if (fb is IntFeedback)
                            Debug.Console(1, this, "Received update from {0} | {1}", fb.Key, fb.IntValue);

                        if (fb is StringFeedback)
                            Debug.Console(1, this, "Received update from {0} | {1}", fb.Key, fb.StringValue);
                    };
            }

            CommunicationMonitor.Start();

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
            Debug.Console(1, "Linking to Bridge Type {0}", GetType().Name);

            trilist.OnlineStatusChange += (device, args) =>
                {
                    if (!args.DeviceOnLine)
                        return;

                    foreach (var feedback in Feedbacks)
                        feedback.FireUpdate();
                };

            IsOnline.LinkInputSig(trilist.BooleanInput[joinMap.DeviceOnline.JoinNumber]);
            NameFeedback.LinkInputSig(trilist.StringInput[joinMap.DeviceName.JoinNumber]);

            for (uint x = 0; x < joinMap.OutletName.JoinSpan; x++)
            {
                var outletIndex = x + 1;
                StringFeedback feedback;
                if (!TryGetOutletNameFeedback(outletIndex, out feedback))
                    continue;
     
                var joinActual = outletIndex + joinMap.OutletName.JoinNumber;

                Debug.Console(2, this, "Linking Outlet Name Feedback | OutletIndex:{0}, Join:{1}", outletIndex, joinActual);
                feedback.LinkInputSig(trilist.StringInput[joinActual]);
            }

            for (uint x = 0; x < joinMap.OutletOnline.JoinSpan; x++)
            {
                var outletIndex = x + 1;
                BoolFeedback feedback;
                if (!TryGetOutletOnlineFeedback(outletIndex, out feedback))
                    continue;

                var joinActual = outletIndex + joinMap.OutletName.JoinNumber;

                Debug.Console(2, this, "Linking Outlet Online Feedback | OutletIndex:{0}, Join:{1}", outletIndex, joinActual);
                feedback.LinkInputSig(trilist.BooleanInput[joinActual]);
            }

            for (uint x = 0; x < joinMap.OutletPowerOn.JoinSpan; x++)
            {
                var outletIndex = x + 1;
                BoolFeedback feedback;
                if (!TryGetOutletPowerFeedback(outletIndex, out feedback))
                    continue;

                var joinActual = outletIndex + joinMap.OutletPowerOn.JoinNumber;

                Debug.Console(2, this, "Linking Outlet PowerIsOn Feedback | OutletIndex:{0}, Join:{1}", outletIndex, joinActual);
                feedback.LinkInputSig(trilist.BooleanInput[joinActual]);

                Debug.Console(2, this, "Linking Outlet PowerOn Method | OutletIndex:{0}, Join:{1}", outletIndex, joinActual);
                trilist.SetSigTrueAction(joinActual, () => TurnOutletOn(outletIndex));
            }

            for (uint x = 0; x < joinMap.OutletPowerOff.JoinSpan; x++)
            {
                var outletIndex = x + 1;
                BoolFeedback feedback;
                if (!TryGetOutletPowerFeedback(outletIndex, out feedback))
                    continue;

                var joinActual = outletIndex + joinMap.OutletPowerOff.JoinNumber;

                Debug.Console(2, this, "Linking Outlet PowerOff Method | OutletIndex:{0}, Join:{1}", outletIndex, joinActual);
                trilist.SetSigTrueAction(joinActual, () => TurnOutletOff(outletIndex));
            }

            for (uint x = 0; x < joinMap.OutletPowerToggle.JoinSpan; x++)
            {
                var outletIndex = x + 1;
                BoolFeedback feedback;
                if (!TryGetOutletPowerFeedback(outletIndex, out feedback))
                    continue;

                var joinActual = outletIndex + joinMap.OutletPowerToggle.JoinNumber;

                Debug.Console(2, this, "Linking Outlet PowerToggle Method | OutletIndex:{0}, Join:{1}", outletIndex, joinActual);
                trilist.SetSigTrueAction(joinActual, () => ToggleOutletPower(outletIndex));
            }
        }

        public void ToggleOutletPower(uint outletIndex)
        {
            IApOutlet outlet;
            if (_outlets.TryGetValue(outletIndex, out outlet))
            {
                outlet.PowerToggle();
                ResetPoll();
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
                ResetPoll();
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
                ResetPoll();
                return;
            }

            Debug.Console(1, this, "Outlet at index-{0} does not exist", outletIndex);
        }

        private void ResetPoll()
        {
            _poll.Reset(1000, 10000);
        }
    }
}