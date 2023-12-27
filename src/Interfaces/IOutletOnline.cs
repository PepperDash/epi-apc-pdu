using PepperDash.Essentials.Core;

namespace PepperDash.Essentials.Apc.Interfaces
{
    public interface IOutletOnline : IApcDevice
    {
        bool TryGetOutletOnlineFeedback(uint outletIndex, out BoolFeedback result);
    }
}