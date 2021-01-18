using System.Text;

namespace ApcEpi.Services.PowerCommands
{
    public class ApOutletPowerCommands
    {
        public const string PowerOnCommandString = "olOn";
        public const string PowerOffCommandString = "olOff";
        public const string RebootCommandString = "olOff";

        public static string GetPowerOnCommand(int outletIndex)
        {
            var builder = new StringBuilder(PowerOnCommandString);
            builder.Append(" ");
            builder.Append(outletIndex);
            builder.Append("\r");

            return builder.ToString();
        }

        public static string GetPowerOffCommand(int outletIndex)
        {
            var builder = new StringBuilder(PowerOffCommandString);
            builder.Append(" ");
            builder.Append(outletIndex);
            builder.Append("\r");

            return builder.ToString();
        }

        public static string GetRebootCommand(int outletIndex)
        {
            var builder = new StringBuilder(RebootCommandString);
            builder.Append(" ");
            builder.Append(outletIndex);
            builder.Append("\r");

            return builder.ToString();
        }
    }
}