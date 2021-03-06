﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using SteamKit2;

namespace SteamIrcBot
{
    class GameIDCommand : Command
    {
        public GameIDCommand()
        {
            Triggers.Add( "!gameid" );
            HelpText = "!gameid <gameid> - Expands GameID";
        }


        protected override void OnRun( CommandDetails details )
        {
            if ( details.Args.Length == 0 )
            {
                IRC.Instance.Send( details.Channel, "{0}: GameID argument required", details.Sender.Nickname );
                return;
            }

            ulong gameId;
            if ( !ulong.TryParse( details.Args[ 0 ], out gameId ) )
            {
                IRC.Instance.Send( details.Channel, "{0}: Invalid GameID", details.Sender.Nickname );
                return;
            }

            IRC.Instance.Send( details.Channel, "{0}: {1}", details.Sender.Nickname, SteamUtils.ExpandGameID( gameId ) );
        }
    }

    class AppInfoCommand : Command<AppInfoCommand.Request>
    {
        public class Request : BaseRequest
        {
            public JobID JobID { get; set; }

            public uint AppID { get; set; }
        }


        public AppInfoCommand()
        {
            Triggers.Add( "!appinfo" );
            HelpText = "!appinfo <appid> - Requests app info for a given app, and provides it";

            Steam.Instance.CallbackManager.Subscribe<SteamApps.PICSProductInfoCallback>( OnProductInfo );
        }

        protected override void OnRun( CommandDetails details )
        {
            if ( !Settings.Current.IsWebEnabled )
            {
                IRC.Instance.Send( details.Channel, "{0}: Web support is disabled", details.Sender.Nickname );
                return;
            }

            if ( details.Args.Length == 0 )
            {
                IRC.Instance.Send( details.Channel, "{0}: AppID argument required", details.Sender.Nickname );
                return;
            }

            uint appId;
            if ( !uint.TryParse( details.Args[ 0 ], out appId ) )
            {
                IRC.Instance.Send( details.Channel, "{0}: Invalid AppID", details.Sender.Nickname );
                return;
            }

            if ( !Steam.Instance.Connected )
            {
                IRC.Instance.Send( details.Channel, details.Sender.Nickname + ": Unable to request app name: not connected to Steam!" );
                return;
            }

            var jobId = Steam.Instance.Apps.PICSGetProductInfo( appId, null, false, false );
            AddRequest( details, new Request { JobID = jobId, AppID = appId } );
        }

        void OnProductInfo( SteamApps.PICSProductInfoCallback callback )
        {
            if ( callback.ResponsePending )
                return;

            var req = GetRequest( r => r.JobID == callback.JobID );

            if ( req == null )
                return;

            bool isUnknownApp = callback.UnknownApps.Contains( req.AppID ) || !callback.Apps.ContainsKey( req.AppID );

            if ( isUnknownApp )
            {
                IRC.Instance.Send( req.Channel, "{0}: Unable to request appinfo for {1}: unknown AppID", req.Requester.Nickname, req.AppID );
                return;
            }

            var appInfo = callback.Apps[ req.AppID ];

            var path = Path.Combine( "appinfo", string.Format( "{0}.vdf", req.AppID ) );
            var fsPath = Path.Combine( Settings.Current.WebPath, path );


            var webUri = new Uri( new Uri( Settings.Current.WebURL ), path );

            try
            {
                appInfo.KeyValues.SaveToFile( fsPath, false );
            }
            catch ( IOException ex )
            {
                IRC.Instance.Send( req.Channel, "{0}: Unable to save appinfo for {1} to web path!", req.Requester.Nickname, req.AppID );
                Log.WriteError( "AppInfoCommand", "Unable to save appinfo for {0} to web path: {1}", req.AppID, ex );

                return;
            }

            IRC.Instance.Send( req.Channel, "{0}: {1} {2}", req.Requester.Nickname, webUri, appInfo.MissingToken ? "(requires token)" : "" );
        }
    }
}
