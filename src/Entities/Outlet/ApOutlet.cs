﻿using System;
using ApcEpi.Abstractions;
using ApcEpi.Services.NameCommands;
using Crestron.SimplSharp;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash_Essentials_Core.Devices;


namespace ApcEpi.Entities.Outlet
{

    public class ApOutlet : IApOutlet
    {
        private readonly ApOutletOnline _online;
        private readonly ApOutletPower _power;
        public int PowerCycleTimeMs { get; private set; }
        private readonly CTimer _powerCycleTimer;


        public ApOutlet(string key, string name, int outletIndex, string parentDeviceKey, IBasicCommunication coms, int powerCycleTimeMs)
        {
            Key = parentDeviceKey + "-" + key;
            Name = name;
            OutletIndex = outletIndex;
            PowerCycleTimeMs = powerCycleTimeMs;
            NameFeedback = new StringFeedback(
                name, 
                () => String.IsNullOrEmpty(Name) ? string.Empty : Name);

            _online = new ApOutletOnline(key, name, outletIndex);
            _power = new ApOutletPower(key, name, outletIndex, coms);

            var socket = coms as ISocketStatus;
            if (socket != null)
            {
                socket.ConnectionChange += (sender, args) =>
                {
                    if (!args.Client.IsConnected)
                        return;

                    ApOutletNameCommands.GetOutletNameCommand(outletIndex, key);

                };
            }
            _powerCycleTimer = new CTimer(PowerOnDue, Timeout.Infinite);
        }

        public BoolFeedback IsOnline
        {
            get { return _online.IsOnline; }
        }

        public string Key { get; private set; }
        public string Name { get; private set; }
        public StringFeedback NameFeedback { get; private set; }

        public int OutletIndex { get; private set; }

        public BoolFeedback PowerIsOnFeedback
        {
            get { return _power.PowerIsOnFeedback; }
        }

        public static string GetMatchString(int outletIndex)
        {
            return Convert.ToString(outletIndex).PadLeft(2, ' ');
        }

        public void PowerOff()
        {
            _power.PowerOff();
        }

        public void PowerOn()
        {
            _power.PowerOn();
        }

        private void PowerOnDue(object obj)
        {
            _power.PowerOn();
            _powerCycleTimer.Reset(Timeout.Infinite);
        }
        public void PowerToggle()
        {
            _power.PowerToggle();
        }

        public bool PowerStatus
        {
            get { return _power.PowerStatus; }
            set
            {
                _power.PowerStatus = value;
            }
        }

        public void SetIsOnline()
        {
            _online.SetIsOnline();
        }


        public void PowerCycle()
        {
            PowerOff();
            _powerCycleTimer.Reset(PowerCycleTimeMs);
        }


    }
}