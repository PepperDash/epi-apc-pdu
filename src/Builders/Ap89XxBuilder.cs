using System.Linq;
using ApcEpi.Abstractions;
using ApcEpi.Config;
using ApcEpi.Devices;
using ApcEpi.Entities.Outlet;
using Crestron.SimplSharp;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Config;

namespace ApcEpi.Builders
{
    public class Ap89XxBuilder : IApDeviceBuilder
    {
        private Ap89XxBuilder(string key, string name, IBasicCommunication coms, ApDeviceConfig config)
        {
            Coms = coms;
            Name = name;
            Key = key;
            Outlets = BuildOutletsFromConfig(key, config, coms);
        }

        public string Key { get; private set; }
        public string Name { get; private set; }
        public IBasicCommunication Coms { get; private set; }

        public ReadOnlyDictionary<uint, IApOutlet> Outlets { get; private set; }

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