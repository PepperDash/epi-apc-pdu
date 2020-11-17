using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using PepperDash.Core;
using PepperDash.Essentials.Core;

namespace ApcEpi.Abstractions
{
    public interface IApDeviceBuilder : IKeyName
    {
        ICommunicationMonitor Monitor { get; }
        IBasicCommunication Coms { get; }
        ReadOnlyDictionary<uint, IApOutlet> Outlets { get; } 
        CTimer Poll { get; }

        EssentialsDevice Build();
    }
}