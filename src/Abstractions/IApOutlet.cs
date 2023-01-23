using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash_Essentials_Core.Devices;

namespace ApcEpi.Abstractions
{
    public interface IApOutlet : IOnline, IHasPowerCycle
    {
        int OutletIndex { get; }
        StringFeedback NameFeedback { get; }
        bool PowerStatus { get; set; }
        void SetIsOnline();
    }
}