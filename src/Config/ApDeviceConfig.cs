using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro;
using PepperDash.Core;

namespace ApcEpi.Config
{
    public class ApDeviceConfig
    {
        public ControlPropertiesConfig Control { get; set; }
        public Dictionary<string, ApOutletConfig> Outlets { get; set; }
    }

    public class ApOutletConfig
    {
        public string Name { get; set; }
        public int OutletIndex { get; set; }
        public int DelayOn { get; set; }
        public int DelayOff { get; set; }
    }
}