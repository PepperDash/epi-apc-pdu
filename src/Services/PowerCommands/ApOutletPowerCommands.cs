using System.Text;

namespace ApcEpi.Services.PowerCommands
{
    public class ApOutletPowerCommands
    {
        public const string PowerOnCommand = "olOn";
        public const string PowerOffCommand = "olOff";

        public static string GetPowerOnCommand(int outletIndex)
        {
            var builder = new StringBuilder(PowerOnCommand);
            builder.Append(" ");
            builder.Append(outletIndex);
            builder.Append("\r");

            return builder.ToString();
        }

        public static string GetPowerOffCommand(int outletIndex)
        {
            var builder = new StringBuilder(PowerOffCommand);
            builder.Append(" ");
            builder.Append(outletIndex);
            builder.Append("\r");

            return builder.ToString();
        }
    }
}