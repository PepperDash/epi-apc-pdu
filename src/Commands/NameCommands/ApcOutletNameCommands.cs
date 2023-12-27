using System.Text;

namespace PepperDash.Essentials.Apc.Commands.NameCommands
{
    public class ApcOutletNameCommands
    {
        public const string OutletNameCommand = "olName";

        public static string GetOutletNameCommand(int outletNumber, string name)
        {
            return $"{OutletNameCommand} {outletNumber} {name}\r";
        }
    }
}