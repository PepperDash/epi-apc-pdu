namespace PepperDash.Essentials.Apc.Commands.StatusCommands
{
    public class ApcOutletStatusCommands
    {
        public const string OutletStatusCommand = "olStatus";

        public static string GetOutletStatusCommand(int outletIndex)
        {
            return $"{OutletStatusCommand} {outletIndex}\r";
        }

        public static string GetAllOutletStatusCommand()
        {
            return $"{OutletStatusCommand} all\r";
        }
    }
}