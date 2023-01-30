using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro;
using Newtonsoft.Json;
using PepperDash.Core;

namespace ApcEpi.Config
{
    public class ApDeviceConfig
    {
        public ControlPropertiesConfig Control { get; set; }
        public int PowerCycleTimeMs { get; set; }
        public Dictionary<string, ApOutletConfig> Outlets { get; set; }
        public bool UseEssentialsJoinmap { get; set; }
        [JsonProperty("enableOutletsOverride")]
        public bool EnableOutletsOverride { get; set; }
    }

    public class ApOutletConfig
    {
        public string Name { get; set; }
        public int OutletIndex { get; set; }
        public int DelayOn { get; set; }
        public int DelayOff { get; set; }
        public bool IsInvisible { get; set; }
    }
}