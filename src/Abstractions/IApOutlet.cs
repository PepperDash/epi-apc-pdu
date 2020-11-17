using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using PepperDash.Core;
using PepperDash.Essentials.Core;

namespace ApcEpi.Abstractions
{
    public interface IApOutlet : IKeyName, IOnline, IPower
    {
        int OutletIndex { get; }
        StringFeedback NameFeedback { get; }
    }
}