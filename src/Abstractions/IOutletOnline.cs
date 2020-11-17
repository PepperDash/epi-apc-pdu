using PepperDash.Essentials.Core;

namespace ApcEpi.Abstractions
{
    public interface IOutletOnline : IApDevice
    {
        bool TryGetOutletOnlineFeedback(uint outletIndex, out BoolFeedback result); 
    }
}