using Newtonsoft.Json;
using PepperDash.Core;
using System.Collections.Generic;

namespace PepperDash.Essentials.Apc.Config
{
    public class ApcDeviceConfig
    {
        public ControlPropertiesConfig Control { get; set; }
        public int PowerCycleTimeMs { get; set; }
        public Dictionary<string, ApcOutletConfig> Outlets { get; set; }
        public bool UseEssentialsJoinmap { get; set; }
        [JsonProperty("enableOutletsOverride")]
        public bool EnableOutletsOverride { get; set; }
    }

    public class ApcOutletConfig
    {
        public string Name { get; set; }
        public int OutletIndex { get; set; }
        public int DelayOn { get; set; }
        public int DelayOff { get; set; }
        public bool IsInvisible { get; set; }
    }
}