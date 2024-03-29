﻿using System.Linq;
using ApcEpi.Abstractions;
using ApcEpi.Config;
using ApcEpi.Devices;
using ApcEpi.Entities.Outlet;
using Crestron.SimplSharp;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Config;
using PepperDash_Essentials_Core.Devices;

namespace ApcEpi.Builders
{
    public class Ap89XxBuilder : IApDeviceBuilder
    {
        public ApDeviceConfig Config { get; private set; }

        private Ap89XxBuilder(string key, string name, IBasicCommunication coms, ApDeviceConfig config)
        {
            Coms = coms;
            Name = name;
            Key = key;
            Config = config;

            UseEssentialsJoinMap = config.UseEssentialsJoinmap;
            Outlets = BuildOutletsFromConfig(key, config, coms);
            EnableAsOnline = config.EnableOutletsOverride;

            foreach (var outlet in Outlets)
            {
                var o = outlet;
                DeviceManager.AddDevice(o.Value);
            }
        }

        public string Key { get; private set; }
        public string Name { get; private set; }
        public IBasicCommunication Coms { get; private set; }
        public bool UseEssentialsJoinMap { get; private set; }
        public bool EnableAsOnline { get; private set; }

        public ReadOnlyDictionary<int, IHasPowerCycle> Outlets { get; private set; }

        public static ReadOnlyDictionary<int, IHasPowerCycle> BuildOutletsFromConfig(
            string parentKey,
            ApDeviceConfig config,
            IBasicCommunication coms)
        {

            var outlets = config
                .Outlets
                .Select(x => new ApOutlet(x.Key, x.Value.Name, x.Value.OutletIndex, parentKey, coms, config.PowerCycleTimeMs))
                .ToDictionary<ApOutlet, int, IHasPowerCycle>(outlet => outlet.OutletIndex, outlet => outlet);

            return new ReadOnlyDictionary<int, IHasPowerCycle>(outlets);
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