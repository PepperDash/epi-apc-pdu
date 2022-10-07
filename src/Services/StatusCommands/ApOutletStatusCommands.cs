using System.Text;

namespace ApcEpi.Services.StatusCommands
{
    public class ApOutletStatusCommands
    {
        public const string Command = "olStatus";

        public static string GetOutletStatusCommand(int outletIndex)
        {
            var builder = new StringBuilder(Command);
            builder.Append(" ");
            builder.Append(outletIndex);
            builder.Append("\r");
            
            return builder.ToString();
        }

        public static string GetAllOutletStatusCommand()
        {
            var builder = new StringBuilder(Command);
            builder.Append(" ");
            builder.Append("all");
            builder.Append("\r");

            return builder.ToString();
        }
    }
}