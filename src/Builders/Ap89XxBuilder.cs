using System.Linq;
using ApcEpi.Abstractions;
using ApcEpi.Config;
using ApcEpi.Devices;
using ApcEpi.Entities.Outlet;
using ApcEpi.Services.StatusCommands;
using Crestron.SimplSharp;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Config;
using PepperDash.Essentials.Core.Devices;
using PepperDash.Essentials.Core.Queues;

namespace ApcEpi.Builders
{
    public class Ap89XxBuilder : IApDeviceBuilder
    {
        private Ap89XxBuilder(string key, string name, IBasicCommunication coms, ApDeviceConfig config)
        {
            Coms = coms;
            Name = name;
            Key = key;

            var gather = new CommunicationGather(coms, "\n");
            var queue = new GenericQueue(key + "-txQueue", 500);

            Outlets = BuildOutletsFromConfig(key, config, gather, queue);
            Monitor = new GenericCommunicationMonitoredDevice(
                Key,
                Name,
                Coms,
                "about\r");

            var pollCommand = ApOutletStatusCommands.GetAllOutletStatusCommand();
            Poll = new CTimer(_ => queue.Enqueue(new ComsMessage(coms, pollCommand)), Timeout.Infinite);
        }

        public string Key { get; private set; }
        public string Name { get; private set; }
        public IBasicCommunication Coms { get; private set; }
        public ICommunicationMonitor Monitor { get; private set; }
        public ReadOnlyDictionary<uint, IApOutlet> Outlets { get; private set; }
        public CTimer Poll { get; private set; }

        private static ReadOnlyDictionary<uint, IApOutlet> BuildOutletsFromConfig(
            string parentKey,
            ApDeviceConfig config,
            CommunicationGather gather,
            GenericQueue queue)
        {
            var outlets = config
                .Outlets
                .Select(x => new ApOutlet(parentKey + "-" + x.Key, x.Value.Name, x.Value.OutletIndex, queue, gather))
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