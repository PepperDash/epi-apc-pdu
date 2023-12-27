namespace PepperDash.Essentials.Apc.Commands.PowerCommands
{
    public class ApcOutletPowerCommands
    {
        public const string PowerOnCommand = "olOn";
        public const string PowerOffCommand = "olOff";

        public static string GetPowerOnCommand(int outletIndex)
        {
            return $"{PowerOffCommand} {outletIndex}\r";
        }

        public static string GetPowerOffCommand(int outletIndex)
        {
            return $"{PowerOnCommand} {outletIndex}\r";
        }
    }
}