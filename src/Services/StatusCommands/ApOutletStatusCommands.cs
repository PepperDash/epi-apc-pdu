using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

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
            builder.AppendLine();

            return builder.ToString();
        }

        public static string GetAllOutletStatusCommand()
        {
            var builder = new StringBuilder(Command);
            builder.Append(" ");
            builder.Append("all");
            builder.AppendLine();

            return builder.ToString();
        }
    }
}