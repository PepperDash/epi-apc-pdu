using PepperDash.Essentials.Core;

namespace PepperDash.Essentials.Apc.Interfaces
{
    public interface IApcOutlet : IOnline, IHasPowerCycle
    {
        int OutletIndex { get; }
        StringFeedback NameFeedback { get; }
        bool PowerStatus { get; set; }
        void SetIsOnline();
    }
}