using PepperDash.Essentials.Core;

namespace PepperDash.Essentials.Apc.Interfaces
{
    public interface IOutletPower : IApcDevice
    {
        bool TryGetOutletPowerFeedback(uint outletIndex, out BoolFeedback result);
        void ToggleOutletPower(uint outletIndex);
        void TurnOutletOff(uint outletIndex);
        void TurnOutletOn(uint outletIndex);
    }
}