using System.Text;

namespace ApcEpi.Services.NameCommands
{
    public class ApOutletNameCommands
    {
        public const string Command = "olName";

        public static string GetOutletNameCommand(int outletNumber, string name)
        {
            var builder = new StringBuilder(Command);
            builder.Append(" ");
            builder.Append(outletNumber);
            builder.Append(" ");
            builder.Append(name);
            builder.AppendLine();

            return builder.ToString();
        }

        public static string GetOutletPollCommand(int outletNumber)
        {
            var builder = new StringBuilder(Command);
            builder.Append(" ");
            builder.Append(outletNumber);
            builder.AppendLine();

            return builder.ToString();
        }
    }
}