using PepperDash.Essentials.Core;

namespace PepperDash.Essentials.Apc.Interfaces
{
    public interface IOutletName : IApcDevice
    {
        bool TryGetOutletNameFeedback(uint outletIndex, out StringFeedback result);
    }
}