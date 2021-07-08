using System.Linq;
using ApcEpi.Abstractions;
using ApcEpi.Config;
using ApcEpi.Devices;
using ApcEpi.Entities.Outlet;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro.CrestronThread;
using Crestron.SimplSharpPro.Diagnostics;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Queues;
using PepperDash.Essentials.Core.Config;
using ApcEpi.Services.StatusCommands;

namespace ApcEpi.Builders
{
    public class Ap89XxBuilder : IApDeviceBuilder
    {
        private static GenericQueue _pollQueue;

        private Ap89XxBuilder(string key, string name, IBasicCommunication coms, ApDeviceConfig config)
        {
            Coms = coms;
            Name = name;
            Key = key;
            Outlets = BuildOutletsFromConfig(key, config, coms);

            if (_pollQueue == null)
                _pollQueue = new GenericQueue("ApcPollQueue", Thread.eThreadPriority.LowestPriority, 200);

            PollQueue = _pollQueue;

            Outlets = BuildOutletsFromConfig(key, config, coms);
            var pollCommand = ApOutletStatusCommands.GetAllOutletStatusCommand();

            Poll = new CTimer(_ =>
                {
                    if (!SystemMonitor.ProgramInitialization.ProgramInitializationComplete)
                        return;

                    _pollQueue.Enqueue(new ComsMessage(coms, pollCommand));
                }, Timeout.Infinite);

            var socket = coms as ISocketStatus;
            if (socket != null)
            {
                socket.ConnectionChange +=
                    (sender, args) =>
                    {
                        if (args.Client.IsConnected)
                            Poll.Reset(2000, 30000);
                        else
                        {
                            Poll.Stop();
                        }
                    };
            }

        }

        public string Key { get; private set; }
        public string Name { get; private set; }
        public GenericQueue PollQueue { get; private set; }
        public IBasicCommunication Coms { get; private set; }

        public ReadOnlyDictionary<uint, IApOutlet> Outlets { get; private set; }
        public CTimer Poll { get; private set; }

        public static ReadOnlyDictionary<uint, IApOutlet> BuildOutletsFromConfig(
            string parentKey,
            ApDeviceConfig config,
            IBasicCommunication coms)
        {
            var outlets = config
                .Outlets
                .Select(x => new ApOutlet(x.Key, x.Value.Name, x.Value.OutletIndex, parentKey, coms))
                .ToDictionary<ApOutlet, uint, IApOutlet>(outlet => (uint) outlet.OutletIndex, outlet => outlet);

            return new ReadOnlyDictionary<uint, IApOutlet>(outlets);
        }

        public static IApDeviceBuilder GetFromDeviceConfig(DeviceConfig dc)
        {
            var config = dc.Properties.ToObject<ApDeviceConfig>();
            var coms = CommFactory.CreateCommForDevice(dc);

            return new Ap89XxBuilder(dc.Key, dc.Name, coms, config);
        }

        public EssentialsDevice Build()
        {
            return new ApDevice(this);
        }
    }
}