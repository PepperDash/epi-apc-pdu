using System.Collections.Generic;
using ApcEpi.Builders;
using ApcEpi.Devices;
using PepperDash.Essentials.Core;

namespace ApcEpi.Factories
{
    public class Ap89XxFactory : EssentialsPluginDeviceFactory<ApDevice>
    {
        public Ap89XxFactory()
        {
            MinimumEssentialsFrameworkVersion = "1.8.0";
            TypeNames = new List<string>() { "Ap89xx" };
        }

        public override EssentialsDevice BuildDevice(PepperDash.Essentials.Core.Config.DeviceConfig dc)
        {
            return Ap89XxBuilder
                .GetFromDeviceConfig(dc)
                .Build();
        }
    }
}