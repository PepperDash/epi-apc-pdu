using PepperDash.Essentials.Core;

namespace ApcEpi.JoinMaps
{
    public class ApDeviceJoinMap : JoinMapBaseAdvanced
    {
        [JoinName("DeviceName")] 
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

        [JoinName("DeviceOnline")] 
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

        [JoinName("OutletName")] 
        public JoinDataComplete OutletName = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 10,
                JoinSpan = 20
            },
            new JoinMetadata
            {
                Description = "Outlet Name",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Serial
            });

        [JoinName("OutletPowerOn")] 
        public JoinDataComplete OutletPowerOn = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 20,
                JoinSpan = 20
            },
            new JoinMetadata
            {
                Description = "Outlet Power On/Feedback",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("OutletPowerOff")] 
        public JoinDataComplete OutletPowerOff = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 40,
                JoinSpan = 20
            },
            new JoinMetadata
            {
                Description = "Outlet Power Off",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("OutletPowerToggle")] 
        public JoinDataComplete OutletPowerToggle = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 60,
                JoinSpan = 20
            },
            new JoinMetadata
            {
                Description = "Outlet Power Toggle",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        public ApDeviceJoinMap(uint joinStart)
            : base(joinStart, typeof(ApDeviceJoinMap))
        {

        }
    }
}