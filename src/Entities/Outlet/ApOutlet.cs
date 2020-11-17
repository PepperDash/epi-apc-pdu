using ApcEpi.Abstractions;
using ApcEpi.Services.NameCommands;
using PepperDash.Core;
using PepperDash.Essentials.Core;

namespace ApcEpi.Entities.Outlet
{
    public class ApOutlet : IApOutlet
    {
        private readonly IOnline _online;
        private readonly IPower _power;

        public ApOutlet(string key, string name, int outletIndex, IBasicCommunication coms)
        {
            Key = key;
            Name = name;
            OutletIndex = outletIndex;
            NameFeedback = new StringFeedback(Key + "-OutletName-" + name, () => Name);

            _online = new ApOutletOnline(key, name, outletIndex, coms);
            _power = new ApOutletPower(key, name, outletIndex, coms);

            IsOnline.OutputChange += (sender, args) =>
                {
                    if (!args.BoolValue)
                        return;

                    var outletNameCommand = ApOutletNameCommands.GetOutletNameCommand(outletIndex, name);
                    coms.SendText(outletNameCommand);
                };
        }

        public BoolFeedback IsOnline
        {
            get { return _online.IsOnline; }
        }

        public string Key { get; private set; }
        public string Name { get; private set; }
        public int OutletIndex { get; private set; }
        public StringFeedback NameFeedback { get; private set; }

        public BoolFeedback PowerIsOnFeedback
        {
            get { return _power.PowerIsOnFeedback; }
        }

        public void PowerOff()
        {
            _power.PowerOff();
        }

        public void PowerOn()
        {
            _power.PowerOn();
        }

        public void PowerToggle()
        {
            _power.PowerToggle();
        }
    }
}