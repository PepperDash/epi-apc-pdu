using PepperDash.Essentials.Core;

namespace ApcEpi.JoinMaps
{
	public class ApDeviceJoinMap : JoinMapBaseAdvanced
	{
	    public JoinDataComplete DeviceName = new JoinDataComplete(new JoinData {JoinNumber = 1, JoinSpan = 1},
	        new JoinMetadata
	        {
	            Label = "Device Name",
	            JoinCapabilities = eJoinCapabilities.ToSIMPL,
	            JoinType = eJoinType.Serial
	        });

		public ApDeviceJoinMap(uint joinStart) 
            :base(joinStart)
		{
		}
	}
}