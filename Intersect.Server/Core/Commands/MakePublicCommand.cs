﻿using Intersect.Server.Core.CommandParsing;
using Intersect.Server.Localization;

namespace Intersect.Server.Core.Commands
{

    internal sealed partial class MakePublicCommand : ServerCommand
    {

        public MakePublicCommand() : base(Strings.Commands.MakePublic)
        {
        }

        protected override void HandleValue(ServerContext context, ParserResult result)
        {
            Console.WriteLine($@"    {Strings.Commands.madepublic}");
            Options.Instance.AdminOnly = false;
            Options.SaveToDisk();
        }

    }

}
