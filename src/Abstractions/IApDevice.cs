using PepperDash.Core;
using PepperDash.Essentials.Core;

namespace ApcEpi.Abstractions
{
    public interface IApDevice : IKeyName, IOnline, ICommunicationMonitor, IHasFeedback 
    {
          
    }
}