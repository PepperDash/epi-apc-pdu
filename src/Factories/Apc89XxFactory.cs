using PepperDash.Core;
using PepperDash.Essentials.Apc.Communications;
using PepperDash.Essentials.Apc.Config;
using PepperDash.Essentials.Apc.Devices;
using PepperDash.Essentials.Core;
using System.Collections.Generic;

namespace PepperDash.Essentials.Apc.Factories
{
    public class Apc89XxFactory : EssentialsPluginDeviceFactory<ApcDevice>
    {
        public Apc89XxFactory()
        {
            MinimumEssentialsFrameworkVersion = "1.11.0";
            TypeNames = new List<string>() { "Ap89xx" };
        }

        public override EssentialsDevice BuildDevice(Core.Config.DeviceConfig dc)
        {

            var config = dc.Properties.ToObject<ApcDeviceConfig>();

            IBasicCommunication coms;

            if(config.Control.Method == eControlMethod.Ssh)
            {
                coms = new ApcSshClient($"{dc.Key}-ssh", config.Control.TcpSshProperties.Address, config.Control.TcpSshProperties.Port, config.Control.TcpSshProperties.Username, config.Control.TcpSshProperties.Password);
            } else
            {
                coms = CommFactory.CreateCommForDevice(dc);
            }

            return new ApcDevice(dc.Key, dc.Name, config, coms);
        }


    }
}