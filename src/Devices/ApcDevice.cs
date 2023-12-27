using Crestron.SimplSharp;
using Crestron.SimplSharpPro.DeviceSupport;
using PepperDash.Core;
using PepperDash.Essentials.Apc.Commands.StatusCommands;
using PepperDash.Essentials.Apc.Config;
using PepperDash.Essentials.Apc.Entities.Outlet;
using PepperDash.Essentials.Apc.Interfaces;
using PepperDash.Essentials.Apc.JoinMaps;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Bridges;
using System;
using System.Linq;
using Feedback = PepperDash.Essentials.Core.Feedback;

namespace PepperDash.Essentials.Apc.Devices
{
    public class ApcDevice : EssentialsBridgeableDevice, IOutletName, IOutletPower, IOutletOnline, IHasControlledPowerOutlets, ICommunicationMonitor
    {
        private readonly StatusMonitorBase _monitor;

        public ReadOnlyDictionary<int, IHasPowerCycle> PduOutlets { get; private set; }

        private readonly bool _useEssentialsJoinmap;
        public bool EnableAsOnline { get; private set; }

        private readonly IBasicCommunication _coms;

        public ApcDevice(string key, string name, ApcDeviceConfig config, IBasicCommunication coms):base(key, name)
        {
            Feedbacks = new FeedbackCollection<Feedback>();

            _coms = coms;

            PduOutlets = BuildOutletsFromConfig(key, config, _coms);
            _monitor = new GenericCommunicationMonitor(
                this,
                _coms,
                30000,
                120000,
                240000,
                ApcOutletStatusCommands.GetAllOutletStatusCommand());

            NameFeedback = new StringFeedback("DeviceNameFeedback", () => Name);
            Feedbacks.Add(IsOnline);
            Feedbacks.Add(NameFeedback);
            EnableAsOnline = config.EnableOutletsOverride;
            _useEssentialsJoinmap = config.UseEssentialsJoinmap;

            var gather = new CommunicationGather(_coms, "\n");
            gather.LineReceived +=
                (o, textArgs) => ProcessResponse(PduOutlets, textArgs.Text);

            CrestronEnvironment.ProgramStatusEventHandler += type =>
            {
                if (type != eProgramStatusEventType.Stopping)
                    return;

                CommunicationMonitor.Stop();
            };
        }

        public ReadOnlyDictionary<int, IHasPowerCycle> BuildOutletsFromConfig(
            string parentKey,
            ApcDeviceConfig config,
            IBasicCommunication coms)
        {

            var outlets = config
                .Outlets
                .Select(x => new ApcOutlet(x.Key, x.Value.Name, x.Value.OutletIndex, parentKey, coms, config.PowerCycleTimeMs))
                .ToDictionary<ApcOutlet, int, IHasPowerCycle>(outlet => outlet.OutletIndex, outlet => outlet);

            return new ReadOnlyDictionary<int, IHasPowerCycle>(outlets);
        }

        public void ProcessResponse(ReadOnlyDictionary<int, IHasPowerCycle> outlets, string response)
        {
            var responseToProcess = response.Trim().Split(new[] { ':' });
            if (responseToProcess.Count() != 3)
                return;

            try
            {
                var outletIndex = Convert.ToUInt32(responseToProcess[0]);
                if (!outlets.TryGetValue((int)outletIndex, out IHasPowerCycle outlet))
                    return;

                if (!(outlet is IApcOutlet apOutlet)) return;

                if (responseToProcess[2].Contains("On"))
                {
                    apOutlet.PowerStatus = true;
                }
                else if (responseToProcess[2].Contains("Off"))
                {
                    apOutlet.PowerStatus = false;
                }
                else
                {
                    Debug.Console(1,
                        "Unable to process response: {0}",
                        response);
                }

                apOutlet.SetIsOnline();
            }
            catch (Exception ex)
            {
                Debug.Console(1, "Error processing response: {0}{1}", response, ex.Message);
            }
        }

        public StatusMonitorBase CommunicationMonitor
        {
            get { return _monitor; }
        }

        public StringFeedback NameFeedback { get; private set; }
        public FeedbackCollection<Feedback> Feedbacks { get; private set; }

        public BoolFeedback IsOnline
        {
            get { return _monitor.IsOnlineFeedback; }
        }

        public override void Initialize()
        {
            try
            {
                _coms.Connect();
                CommunicationMonitor.Start();
            }
            catch (Exception ex)
            {
                Debug.Console(0,
                    this,
                    Debug.ErrorLogLevel.Error,
                    "Initialize Error : {0}",
                    ex.Message);
                Debug.Console(2, this, ex.StackTrace);
            }
        }

        public override bool CustomActivate()
        {
            foreach (var o in PduOutlets.Select(outlet => outlet.Value).OfType<IApcOutlet>())
            {
                Feedbacks.AddRange(new Feedback[]
                {
                    o.IsOnline,
                    o.NameFeedback,
                    o.PowerIsOnFeedback
                });
            }


            Feedbacks.Add(_monitor.IsOnlineFeedback);
            foreach (var feedback in Feedbacks)
            {
                feedback.OutputChange += (sender, args) =>
                    {
                        var fb = sender as Feedback;
                        if (fb != null && string.IsNullOrEmpty(fb.Key))
                            return;

                        if (fb is BoolFeedback)
                            Debug.Console(1, this, "Received update from {0} | {1}", fb.Key, fb.BoolValue);

                        if (fb is IntFeedback)
                            Debug.Console(1, this, "Received update from {0} | {1}", fb.Key, fb.IntValue);

                        if (fb is StringFeedback)
                            Debug.Console(1, this, "Received update from {0} | {1}", fb.Key, fb.StringValue);
                    };
            }

            return true;
        }

        public override void LinkToApi(BasicTriList trilist, uint joinStart, string joinMapKey, EiscApiAdvanced bridge)
        {

            if (!_useEssentialsJoinmap)
            {
                LinkToInternalJoinMap(trilist, joinStart, joinMapKey, bridge);
                return;
            }
            
            LinkToEssentialsJoinMap(trilist, joinStart, joinMapKey, bridge);
        }

        private void LinkToEssentialsJoinMap(BasicTriList trilist, uint joinStart, string joinMapKey, EiscApiAdvanced bridge)
        {
            var joinMap = new PduJoinMapBase(joinStart);

            bridge?.AddJoinMap(Key, joinMap);

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

            IsOnline.LinkInputSig(trilist.BooleanInput[joinMap.Online.JoinNumber]);
            NameFeedback.LinkInputSig(trilist.StringInput[joinMap.Name.JoinNumber]);

            for (uint x = 0; x < joinMap.OutletName.JoinSpan; x++)
            {
                var outletIndex = x + 1;
                StringFeedback feedback;
                if (!TryGetOutletNameFeedback(outletIndex, out feedback))
                    continue;

                var joinActual = outletIndex + joinMap.OutletName.JoinNumber;

                Debug.Console(2, this, "Linking Outlet Name Feedback | OutletIndex:{0}, Join:{1}", outletIndex,
                    joinActual);
                feedback.LinkInputSig(trilist.StringInput[joinActual]);
            }


            for (uint x = 0; x < joinMap.OutletPowerOn.JoinSpan; x++)
            {
                var outletIndex = x + 1;
                BoolFeedback feedback;
                if (!TryGetOutletPowerFeedback(outletIndex, out feedback))
                    continue;

                var joinActual = outletIndex + joinMap.OutletPowerOn.JoinNumber;

                Debug.Console(2, this, "Linking Outlet PowerIsOn Feedback | OutletIndex:{0}, Join:{1}", outletIndex,
                    joinActual);
                feedback.LinkInputSig(trilist.BooleanInput[joinActual]);

                Debug.Console(2, this, "Linking Outlet PowerOn Method | OutletIndex:{0}, Join:{1}", outletIndex,
                    joinActual);
                trilist.SetSigTrueAction(joinActual, () => TurnOutletOn(outletIndex));
            }

            for (uint x = 0; x < joinMap.OutletPowerOff.JoinSpan; x++)
            {
                var outletIndex = x + 1;
                BoolFeedback feedback;
                if (!TryGetOutletPowerFeedback(outletIndex, out feedback))
                    continue;

                var joinActual = outletIndex + joinMap.OutletPowerOff.JoinNumber;

                Debug.Console(2, this, "Linking Outlet PowerOff Method | OutletIndex:{0}, Join:{1}", outletIndex,
                    joinActual);
                trilist.SetSigTrueAction(joinActual, () => TurnOutletOff(outletIndex));
            }

            for (uint x = 0; x < joinMap.OutletPowerCycle.JoinSpan; x++)
            {
                var outletIndex = x + 1;
                BoolFeedback feedback;
                if (!TryGetOutletPowerFeedback(outletIndex, out feedback))
                    continue;

                var joinActual = outletIndex + joinMap.OutletPowerCycle.JoinNumber;

                Debug.Console(2, this, "Linking Outlet PowerToggle Method | OutletIndex:{0}, Join:{1}", outletIndex,
                    joinActual);
                trilist.SetSigTrueAction(joinActual, () => OutletPowerCycle(outletIndex));
            }
        }

        private void LinkToInternalJoinMap(BasicTriList trilist, uint joinStart, string joinMapKey, EiscApiAdvanced bridge)
        {
            var joinMap = new ApDeviceJoinMap(joinStart);

            bridge?.AddJoinMap(Key, joinMap);

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

                if (!TryGetOutletNameFeedback(outletIndex, out StringFeedback feedback))
                    continue;

                var joinActual = outletIndex + joinMap.OutletName.JoinNumber;

                Debug.Console(2, this, "Linking Outlet Name Feedback | OutletIndex:{0}, Join:{1}", outletIndex,
                    joinActual);
                feedback.LinkInputSig(trilist.StringInput[joinActual]);
            }

            for (uint x = 0; x < joinMap.OutletOnline.JoinSpan; x++)
            {
                var outletIndex = x + 1;
                var joinActual = outletIndex + joinMap.OutletName.JoinNumber;

                if (EnableAsOnline)
                {
                    trilist.BooleanInput[joinActual].BoolValue = PduOutlets.ContainsKey((int)outletIndex);
                    continue;
                }

                if (!TryGetOutletOnlineFeedback(outletIndex, out BoolFeedback feedback))
                    continue;
                Debug.Console(2, this, "Linking Outlet Online Feedback | OutletIndex:{0}, Join:{1}", outletIndex,
                    joinActual);
                feedback.LinkInputSig(trilist.BooleanInput[joinActual]);
            }

            for (uint x = 0; x < joinMap.OutletPowerOn.JoinSpan; x++)
            {
                var outletIndex = x + 1;

                if (!TryGetOutletPowerFeedback(outletIndex, out BoolFeedback feedback))
                    continue;

                var joinActual = outletIndex + joinMap.OutletPowerOn.JoinNumber;

                Debug.Console(2, this, "Linking Outlet PowerIsOn Feedback | OutletIndex:{0}, Join:{1}", outletIndex,
                    joinActual);
                feedback.LinkInputSig(trilist.BooleanInput[joinActual]);

                Debug.Console(2, this, "Linking Outlet PowerOn Method | OutletIndex:{0}, Join:{1}", outletIndex,
                    joinActual);
                trilist.SetSigTrueAction(joinActual, () => TurnOutletOn(outletIndex));
            }

            for (uint x = 0; x < joinMap.OutletPowerOff.JoinSpan; x++)
            {
                var outletIndex = x + 1;

                if (!TryGetOutletPowerFeedback(outletIndex, out BoolFeedback feedback))
                    continue;

                var joinActual = outletIndex + joinMap.OutletPowerOff.JoinNumber;

                Debug.Console(2, this, "Linking Outlet PowerOff Method | OutletIndex:{0}, Join:{1}", outletIndex,
                    joinActual);
                trilist.SetSigTrueAction(joinActual, () => TurnOutletOff(outletIndex));
            }

            for (uint x = 0; x < joinMap.OutletPowerToggle.JoinSpan; x++)
            {
                var outletIndex = x + 1;

                if (!TryGetOutletPowerFeedback(outletIndex, out BoolFeedback feedback))
                    continue;

                var joinActual = outletIndex + joinMap.OutletPowerToggle.JoinNumber;

                Debug.Console(2, this, "Linking Outlet PowerToggle Method | OutletIndex:{0}, Join:{1}", outletIndex,
                    joinActual);
                trilist.SetSigTrueAction(joinActual, () => ToggleOutletPower(outletIndex));
            }
        }

        public void OutletPowerCycle(uint outletIndex)
        {
            if (PduOutlets.TryGetValue((int)outletIndex, out IHasPowerCycle outlet))
            {
                outlet.PowerCycle();
                return;
            }

            Debug.Console(1, this, "Outlet at index-{0} does not exist", outletIndex);

        }

        public void ToggleOutletPower(uint outletIndex)
        {
            if (PduOutlets.TryGetValue((int)outletIndex, out IHasPowerCycle outlet))
            {
                outlet.PowerToggle();
                return;
            }

            Debug.Console(1, this, "Outlet at index-{0} does not exist", outletIndex);
        }

        public bool TryGetOutletNameFeedback(uint outletIndex, out StringFeedback result)
        {
            result = new StringFeedback(Key + "-OutletName-" + outletIndex, () => string.Empty);

            if (!PduOutlets.TryGetValue((int)outletIndex, out IHasPowerCycle outlet))
                return false;

            result = outlet is IApcOutlet apOutlet ? apOutlet.NameFeedback : new StringFeedback(() => "Unknown");

            return true;
        }

        public bool TryGetOutletOnlineFeedback(uint outletIndex, out BoolFeedback result)
        {
            result = new BoolFeedback(Key + "-OutletName-" + outletIndex, () => false);
            if (!PduOutlets.TryGetValue((int)outletIndex, out IHasPowerCycle outlet))
                return false;
            if (!(outlet is IApcOutlet apOutlet)) return false;
            result = apOutlet.IsOnline;
            return true;
        }
        public bool TryGetOutletPowerFeedback(uint outletIndex, out BoolFeedback result)
        {
            result = new BoolFeedback(Key + "-OutletPower-" + outletIndex, () => false);
            if (!PduOutlets.TryGetValue((int)outletIndex, out IHasPowerCycle outlet))
                return false;

            result = outlet is IApcOutlet apOutlet ? apOutlet.PowerIsOnFeedback : new BoolFeedback(() => false);

            return true;
        }

        public void TurnOutletOff(uint outletIndex)
        {
            if (PduOutlets.TryGetValue((int)outletIndex, out IHasPowerCycle outlet))
            {
                outlet.PowerOff();
                return;
            }

            Debug.Console(1, this, "Outlet at index-{0} does not exist", outletIndex);
        }

        public void TurnOutletOn(uint outletIndex)
        {
            if (PduOutlets.TryGetValue((int)outletIndex, out IHasPowerCycle outlet))
            {
                outlet.PowerOn();
                return;
            }

            Debug.Console(1, this, "Outlet at index-{0} does not exist", outletIndex);
        }


    }
}