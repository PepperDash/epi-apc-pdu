using System;
using Crestron.SimplSharp;
using PepperDash.Core;
using PepperDash.Essentials.Core;

namespace PepperDash.Essentials.Apc.Entities.Outlet
{
    public class ApOutletOnline : IOnline, IKeyName
    {
        private readonly string _matchString;

        private readonly CTimer _offlineTimer;
        private bool _isOnline;

        public ApOutletOnline(string key, string name, int outletIndex)
        {
            Key = key;
            Name = name;
            _matchString = ApcOutlet.GetMatchString(outletIndex);

            IsOnline = new BoolFeedback(
                key + "-Online",
                () => _isOnline);

            _offlineTimer = new CTimer(
                _ =>
                    {
                        _isOnline = false;
                        IsOnline.FireUpdate();
                    },
                this,
                60000,
                60000);
        }

        public BoolFeedback IsOnline { get; private set; }

        public string Key { get; private set; }
        public string Name { get; private set; }

        public void SetIsOnline()
        {
            _isOnline = true;
            IsOnline.FireUpdate();
            _offlineTimer.Reset(60000, 60000);
        }
    }
}