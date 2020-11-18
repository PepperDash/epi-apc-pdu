using System;
using Crestron.SimplSharp;
using PepperDash.Core;
using PepperDash.Essentials.Core;

namespace ApcEpi.Entities.Outlet
{
    public class ApOutletOnline : IOnline, IKeyName
    {
        private readonly string _matchString;

        private readonly CTimer _offlineTimer;
        private bool _isOnline;

        public ApOutletOnline(string key, string name, int outletIndex, IBasicCommunication coms)
        {
            Key = key;
            Name = name;
            _matchString = ApOutlet.GetMatchString(outletIndex);

            IsOnline = new BoolFeedback(
                key + "-Online",
                () => _isOnline);

            InitializeGather(coms);

            _offlineTimer = new CTimer(
                _ =>
                    {
                        _isOnline = false;
                        IsOnline.FireUpdate();
                    },
                this,
                60000,
                60000);
        }

        public BoolFeedback IsOnline { get; private set; }

        public string Key { get; private set; }
        public string Name { get; private set; }

        private void GatherOnLineReceived(object sender, GenericCommMethodReceiveTextArgs args)
        {
            if (!args.Text.StartsWith(_matchString))
                return;

            _isOnline = true;
            IsOnline.FireUpdate();
            _offlineTimer.Reset(60000, 60000);
        }

        private void InitializeGather(ICommunicationReceiver coms)
        {
            var gather = new CommunicationGather(coms, "\n");
            gather.LineReceived += GatherOnLineReceived;

            CrestronEnvironment.ProgramStatusEventHandler += type =>
                {
                    switch (type)
                    {
                        case eProgramStatusEventType.Stopping:
                            gather.LineReceived -= GatherOnLineReceived;
                            gather.Stop();
                            break;
                        case eProgramStatusEventType.Paused:
                            break;
                        case eProgramStatusEventType.Resumed:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException("type");
                    }
                };
        }
    }
}