using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Queues;

namespace ApcEpi.Abstractions
{
    public interface IApDeviceBuilder : IKeyName
    {
        GenericQueue PollQueue { get; }
        IBasicCommunication Coms { get; }
        ReadOnlyDictionary<uint, IApOutlet> Outlets { get; } 
        CTimer Poll { get; }

        EssentialsDevice Build();
    }
}