using PepperDash.Essentials.Core;

namespace ApcEpi.Abstractions
{
    public interface IOutletName : IApDevice
    {
        bool TryGetOutletNameFeedback(uint outletIndex, out StringFeedback result);
    }
}