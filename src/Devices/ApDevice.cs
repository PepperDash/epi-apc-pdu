using System;
using System.Linq;
using ApcEpi.Abstractions;
using ApcEpi.JoinMaps;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro.DeviceSupport;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Bridges;
using PepperDash.Essentials.Core.Queues;
using Feedback = PepperDash.Essentials.Core.Feedback;

namespace ApcEpi.Devices
{
    public class ApDevice: EssentialsBridgeableDevice, IOutletName, IOutletPower, IOutletOnline
    {
        private readonly CTimer _poll;
        private readonly StatusMonitorBase _monitor;
        private readonly ReadOnlyDictionary<uint, IApOutlet> _outlets;

        public ApDevice(IApDeviceBuilder builder)
            : base(builder.Key, builder.Name)
        {
            Feedbacks = new FeedbackCollection<Feedback>();

            _outlets = builder.Outlets;
            _poll = builder.Poll;
            _monitor = new GenericCommunicationMonitor(this, builder.Coms, 60000, 120000, 240000,
                () =>
                    {
                        var message = new ComsMessage(builder.Coms, "about\r");
                        builder.PollQueue.Enqueue(message);
                    });
             
            NameFeedback = new StringFeedback("DeviceNameFeedback", () => Name);
            Feedbacks.Add(IsOnline);
            Feedbacks.Add(NameFeedback);

            DeviceManager.AllDevicesActivated += (sender, args) =>
                {
                    try
                    {
                        var gather = new CommunicationGather(builder.Coms, "\n");
                        var queue = new GenericQueue(Key, 100);

                        gather.LineReceived +=
                            (o, textArgs) => queue.Enqueue(new ProcessApcStringMessage(_outlets, textArgs.Text));

                        queue.Enqueue(new SocketConnectMessage(builder.Coms));
                        CommunicationMonitor.Start();

                        var socket = builder.Coms as ISocketStatus;
                        if (socket != null)
                        {
                            socket.ConnectionChange += (o, eventArgs) => Debug.Console(1, this,
                                Debug.ErrorLogLevel.Notice,
                                "Socket Status : {0}",
                                eventArgs.Client.ClientStatus.ToString());
                        }

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

        class ProcessApcStringMessage : IQueueMessage
        {
            private readonly ReadOnlyDictionary<uint, IApOutlet> _outlets;
            private readonly string _response;

            public ProcessApcStringMessage(ReadOnlyDictionary<uint, IApOutlet> outlets, string response)
            {
                _outlets = outlets;
                _response = response;
            }

            public void Dispatch()
            {
                var responseToProcess = _response.Trim().Split(new[] { ':' });
                if (responseToProcess.Count() != 3)
                    return;

                try
                {
                    var outletIndex = Convert.ToUInt32(responseToProcess[0]);
                    IApOutlet outlet;
                    if (!_outlets.TryGetValue(outletIndex, out outlet))
                        return;

                    if (responseToProcess[2].Contains("On"))
                    {
                        outlet.PowerStatus = true;
                    }
                    else if (responseToProcess[2].Contains("Off"))
                    {
                        outlet.PowerStatus = false;
                    }
                    else
                    {
                        Debug.Console(1,
                            "Not sure where to go with this response : {0}",
                            _response);
                    }

                    outlet.SetIsOnline();
                }
                catch (Exception ex)
                {
                    Debug.Console(1, "Error processing response : {0}", ex.Message);
                }
            }
        }

        class SocketConnectMessage : IQueueMessage
        {
            private readonly IBasicCommunication _coms;
            public SocketConnectMessage(IBasicCommunication coms)
            {
                _coms = coms;
            }

            public void Dispatch()
            {
                try
                {
                    var socket = _coms as ISocketStatus;
                    if (socket == null)
                        return;

                    socket.Connect();
                }
                catch (Exception ex)
                {
                    Debug.Console(1, "Error connecting socket : {0}", ex.Message);
                }
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