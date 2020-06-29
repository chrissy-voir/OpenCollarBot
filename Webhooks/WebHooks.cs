/*

Copyright © 2019 Tara Piccari (Aria; Tashia Redrose)
Licensed under the GPLv2

*/

using Bot;
using Bot.CommandSystem;
using OpenMetaverse;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Reflection;
using Bot.WebHookServer;

namespace OpenCollarBot.Webhooks
{
    class WebHooks
    {
        [WebhookAttribs("/git", HTTPMethod = "POST")]
        public WebhookRegistry.HTTPResponseData gitHook(List<string> arguments, string body, string method, NameValueCollection headers)
        {
            GitCommands.Process(body, headers.Get("X-Github-Event"));
            WebhookRegistry.HTTPResponseData reply = new WebhookRegistry.HTTPResponseData();
            reply.ReplyString = "Done";
            reply.Status = 200;
            // removed a line here that triggered a git refresh
            return reply;
        }

        [WebhookAttribs("/eject_notification", HTTPMethod = "POST")]
        public WebhookRegistry.HTTPResponseData ejectnotif(List<string> arguments, string body, string method, NameValueCollection headers)
        {
            string[] splitter = body.Split(new[] { '[', ']' });
            string req = "[" + splitter[1] + "]" + splitter[2];

            splitter = req.Split(new[] { ' ' });

            string KickName = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(splitter[1] + " " + splitter[2]));
            string Kicked = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(splitter[3] + " " + splitter[4]));
            req = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(req));

            string concat = Kicked + "|" + req + "|" + KickName;
            splitter = concat.Split(new[] { '|' });

            CommandRegistry.Instance.RunCommand("auto_buildnotice " + Kicked + " " + req + " " + KickName, BotSession.Instance.grid.Self.AgentID, 1000, Destinations.DEST_GROUP, BotSession.Instance.grid.Self.AgentID, BotSession.Instance.ConfigurationHandle.first + " " + BotSession.Instance.ConfigurationHandle.last);


            WebhookRegistry.HTTPResponseData reply = new WebhookRegistry.HTTPResponseData();
            reply.Status = 200;
            reply.ReplyString = "Done";
            return reply;
        }

    }
}
