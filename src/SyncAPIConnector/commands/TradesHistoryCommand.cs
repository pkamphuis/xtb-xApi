using System.Text.Json.Nodes;

namespace xAPI.Commands
{

    public class TradesHistoryCommand : BaseCommand
    {
        public TradesHistoryCommand(JsonObject arguments, bool prettyPrint)
            : base(arguments, prettyPrint)
        {
        }

        public override string CommandName
        {
            get
            {
                return "getTradesHistory";
            }
        }

        public override string[] RequiredArguments
        {
            get
            {
                return new string[] { "start", "end" };
            }
        }
    }

}