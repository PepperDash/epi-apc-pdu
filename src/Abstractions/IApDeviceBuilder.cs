using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Queues;
using PepperDash_Essentials_Core.Devices;


namespace ApcEpi.Abstractions
{
    public interface IApDeviceBuilder : IKeyName
    {
        IBasicCommunication Coms { get; }
        ReadOnlyDictionary<int, IHasPowerCycle> Outlets { get; } 
        EssentialsDevice Build();
        bool UseEssentialsJoinMap { get;  }
    }
}