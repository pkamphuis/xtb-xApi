namespace xAPI.Commands
{
    public class AllSymbolsCommand : BaseCommand
    {
        public AllSymbolsCommand(bool prettyPrint) : base(prettyPrint)
        {
        }

        public override string CommandName
        {
            get
            {
                return "getAllSymbols";
            }
        }

        public override string[] RequiredArguments
        {
            get
            {
                return [];
            }
        }

    }

}