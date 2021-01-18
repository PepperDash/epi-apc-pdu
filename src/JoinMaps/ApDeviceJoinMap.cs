using PepperDash.Essentials.Core;

namespace ApcEpi.JoinMaps
{
    public class ApDeviceJoinMap : JoinMapBaseAdvanced
    {
        public JoinDataComplete DeviceName = new JoinDataComplete(
            new JoinData
                {
                    JoinNumber = 1, 
                    JoinSpan = 1
                },
            new JoinMetadata
                {
                    Description = "Device Name",
                    JoinCapabilities = eJoinCapabilities.ToSIMPL,
                    JoinType = eJoinType.Serial
                });

        public JoinDataComplete DeviceOnline = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 1,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Device Online",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Digital
            });

        public JoinDataComplete OutletName = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 50,
                JoinSpan = 50
            },
            new JoinMetadata
            {
                Description = "Device Name",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Serial
            });

        public JoinDataComplete OutletOnline = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 50,
                JoinSpan = 50
            },
            new JoinMetadata
            {
                Description = "Outlet Online",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Digital
            });

        public JoinDataComplete OutletPowerOn = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 100,
                JoinSpan = 50
            },
            new JoinMetadata
            {
                Description = "Outlet Power On/Feedback",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital
            });

        public JoinDataComplete OutletPowerOff = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 150,
                JoinSpan = 150
            },
            new JoinMetadata
            {
                Description = "Outlet Power Off",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        public JoinDataComplete OutletPowerToggle = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 200,
                JoinSpan = 50
            },
            new JoinMetadata
            {
                Description = "Outlet Power Toggle",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        public JoinDataComplete OutletPowerCycle = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 250,
                JoinSpan = 50
            },
            new JoinMetadata
            {
                Description = "Outlet Power Cycle",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        public ApDeviceJoinMap(uint joinStart)
            : base(joinStart, typeof(ApDeviceJoinMap)) { }
    }
}