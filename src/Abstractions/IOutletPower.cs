using PepperDash.Essentials.Core;

namespace ApcEpi.Abstractions
{
    public interface IOutletPower : IApDevice
    {
        bool TryGetOutletPowerFeedback(uint outletIndex, out BoolFeedback result);
        void ToggleOutletPower(uint outletIndex);
        void TurnOutletOff(uint outletIndex);
        void TurnOutletOn(uint outletIndex);
    }
}