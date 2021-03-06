﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamIrcBot
{
    class BeastCommand : Command<BeastCommand.Request>
    {
        public class Request : BaseRequest
        {
        }

        public BeastCommand()
        {
            Triggers.Add( "!beast" );

            HelpText = "!beast - Shows how much time is remaining until the next Dota New Bloom beast time";
        }

        protected override void OnRun( CommandDetails details )
        {
            IRC.Instance.Send( details.Channel, "{0}: It's dead bro", details.Sender.Nickname );
        }
    }
}
