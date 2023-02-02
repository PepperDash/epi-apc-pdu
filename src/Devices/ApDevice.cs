using System;
using System.Linq;
using ApcEpi.Abstractions;
using ApcEpi.Entities.Outlet;
using ApcEpi.JoinMaps;
using ApcEpi.Services.StatusCommands;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro.DeviceSupport;
using Crestron.SimplSharp.Ssh;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Bridges;
using Feedback = PepperDash.Essentials.Core.Feedback;
using PepperDash_Essentials_Core.Devices;


namespace ApcEpi.Devices
{
    public class ApDevice : EssentialsBridgeableDevice, IOutletName, IOutletPower, IOutletOnline, IHasControlledPowerOutlets
    {
        private readonly StatusMonitorBase _monitor;
        public ReadOnlyDictionary<int, IHasPowerCycle> PduOutlets { get; private set; }
        bool _useEssentialsJoinmap;
        public bool EnableAsOnline { get; private set; }

        public ApDevice(IApDeviceBuilder builder)
            : base(builder.Key, builder.Name)
        {
            Feedbacks = new FeedbackCollection<Feedback>();

            PduOutlets = builder.Outlets;
            _monitor = new GenericCommunicationMonitor(
                this, 
                builder.Coms, 
                30000, 
                120000, 
                240000,
                ApOutletStatusCommands.GetAllOutletStatusCommand());
             
            NameFeedback = new StringFeedback("DeviceNameFeedback", () => Name);
            Feedbacks.Add(IsOnline);
            Feedbacks.Add(NameFeedback);
            EnableAsOnline = builder.EnableAsOnline;
            _useEssentialsJoinmap = builder.UseEssentialsJoinMap;

            var gather = new CommunicationGather(builder.Coms, "\n");
            gather.LineReceived +=
                (o, textArgs) => ProcessResponse(PduOutlets, textArgs.Text);

            /*
            var socket = builder.Coms as ISocketStatus;
            if (socket != null)
            {
                socket.ConnectionChange +=
                    (sender, args) =>
                        Debug.Console(1, this, Debug.ErrorLogLevel.Notice, "ConnectionStatus:{0}",
                            args.Client.ClientStatus.ToString());
            }*/

            CrestronEnvironment.ProgramStatusEventHandler += type =>
            {
                if (type != eProgramStatusEventType.Stopping)
                    return;

                CommunicationMonitor.Stop();
            };

            DeviceManager.AllDevicesActivated += (sender, args) =>
                {
                    try
                    {
                        builder.Coms.Connect();
                        CommunicationMonitor.Start();
                    }
                    catch (Exception ex)
                    {
                        Debug.Console(0,
                            this,
                            Debug.ErrorLogLevel.Error,
                            "Error handling all devices activated : {0}",
                            ex.Message);
                    }
                };
        }

        public static void ProcessResponse(ReadOnlyDictionary<int, IHasPowerCycle> outlets, string response)
        {
            var responseToProcess = response.Trim().Split(new[] { ':' });
            if (responseToProcess.Count() != 3)
                return;

            try
            {
                var outletIndex = Convert.ToUInt32(responseToProcess[0]);
                IHasPowerCycle outlet;
                if (!outlets.TryGetValue((int)outletIndex, out outlet))
                    return;
                var apOutlet = outlet as IApOutlet;
                if (apOutlet == null) return;
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
                        "Not sure where to go with this response: {0}",
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

        public override bool CustomActivate()
        {
            foreach (var o in PduOutlets.Select(outlet => outlet.Value).OfType<IApOutlet>())
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

            return true;
        }

        public override void LinkToApi(BasicTriList trilist, uint joinStart, string joinMapKey, EiscApiAdvanced bridge)
        {

            if (!_useEssentialsJoinmap)
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

                    Debug.Console(2, this, "Linking Outlet Name Feedback | OutletIndex:{0}, Join:{1}", outletIndex,
                        joinActual);
                    feedback.LinkInputSig(trilist.StringInput[joinActual]);
                }

                for (uint x = 0; x < joinMap.OutletOnline.JoinSpan; x++)
                {
                    var outletIndex = x + 1;
                    var joinActual = outletIndex + joinMap.OutletName.JoinNumber;

                    if (!EnableAsOnline)
                    {
                        BoolFeedback feedback;
                        if (!TryGetOutletOnlineFeedback(outletIndex, out feedback))
                            continue;
                        Debug.Console(2, this, "Linking Outlet Online Feedback | OutletIndex:{0}, Join:{1}", outletIndex,
                            joinActual);
                        feedback.LinkInputSig(trilist.BooleanInput[joinActual]);
                        continue;
                    }
                    trilist.BooleanInput[joinActual].BoolValue = PduOutlets.ContainsKey((int)outletIndex);

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

                for (uint x = 0; x < joinMap.OutletPowerToggle.JoinSpan; x++)
                {
                    var outletIndex = x + 1;
                    BoolFeedback feedback;
                    if (!TryGetOutletPowerFeedback(outletIndex, out feedback))
                        continue;

                    var joinActual = outletIndex + joinMap.OutletPowerToggle.JoinNumber;

                    Debug.Console(2, this, "Linking Outlet PowerToggle Method | OutletIndex:{0}, Join:{1}", outletIndex,
                        joinActual);
                    trilist.SetSigTrueAction(joinActual, () => ToggleOutletPower(outletIndex));
                }
            }
            else
            {
                var joinMap = new PduJoinMapBase(joinStart);

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
        }

        public void OutletPowerCycle(uint outletIndex)
        {
            IHasPowerCycle outlet;
            if (PduOutlets.TryGetValue((int)outletIndex, out outlet))
            {
                outlet.PowerCycle();
                return;
            }

            Debug.Console(1, this, "Outlet at index-{0} does not exist", outletIndex);

        }

        public void ToggleOutletPower(uint outletIndex)
        {
            IHasPowerCycle outlet;
            if (PduOutlets.TryGetValue((int)outletIndex, out outlet))
            {
                outlet.PowerToggle();
                return;
            }

            Debug.Console(1, this, "Outlet at index-{0} does not exist", outletIndex);
        }

        public bool TryGetOutletNameFeedback(uint outletIndex, out StringFeedback result)
        {
            result = new StringFeedback(Key + "-OutletName-" + outletIndex, () => String.Empty);
            IHasPowerCycle outlet;
            if (!PduOutlets.TryGetValue((int)outletIndex, out outlet))
                return false;
            var apOutlet = outlet as IApOutlet;
            result = apOutlet != null ? apOutlet.NameFeedback : new StringFeedback(() => "Unknown");

            return true;
        }

        public bool TryGetOutletOnlineFeedback(uint outletIndex, out BoolFeedback result)
        {
            result = new BoolFeedback(Key + "-OutletName-" + outletIndex, () => false);
            IHasPowerCycle outlet;
            if (!PduOutlets.TryGetValue((int)outletIndex, out outlet))
                return false;
            var apOutlet = outlet as IApOutlet;
            if (apOutlet == null) return false;
            result = apOutlet.IsOnline;
            return true;
        }
        public bool TryGetOutletPowerFeedback(uint outletIndex, out BoolFeedback result)
        {
            result = new BoolFeedback(Key + "-OutletPower-" + outletIndex, () => false);
            IHasPowerCycle outlet;
            if (!PduOutlets.TryGetValue((int)outletIndex, out outlet))
                return false;
            var apOutlet = outlet as IApOutlet;
            result = apOutlet != null ? apOutlet.PowerIsOnFeedback : new BoolFeedback(() => false);

            return true;
        }

        public void TurnOutletOff(uint outletIndex)
        {
            IHasPowerCycle outlet;
            if (PduOutlets.TryGetValue((int)outletIndex, out outlet))
            {
                outlet.PowerOff();
                return;
            }

            Debug.Console(1, this, "Outlet at index-{0} does not exist", outletIndex);
        }

        public void TurnOutletOn(uint outletIndex)
        {
            IHasPowerCycle outlet;
            if (PduOutlets.TryGetValue((int)outletIndex, out outlet))
            {
                outlet.PowerOn();
                return;
            }

            Debug.Console(1, this, "Outlet at index-{0} does not exist", outletIndex);
        }


    }
}