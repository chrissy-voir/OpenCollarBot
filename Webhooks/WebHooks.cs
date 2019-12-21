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

namespace OpenCollarBot.Webhooks
{
    class WebHooks
    {
        [WebhookAttribs("/git")]
        public WebhookRegistry.HTTPResponseData gitHook(string body, NameValueCollection headers)
        {
            GitCommands.Process(body, headers.Get("X-Github-Event"), BotSession.Instance.MHE);
            WebhookRegistry.HTTPResponseData reply = new WebhookRegistry.HTTPResponseData();
            reply.ReplyString = "Done";
            reply.Status = 200;
            CommandRegistry.Instance.RunCommand("refresh_git_nosend", UUID.Zero, 1000, BotSession.Instance.MHE, MessageHandler.Destinations.DEST_ACTION, UUID.Zero, "Console");
            return reply;
        }

        [WebhookAttribs("/eject_notification")]
        public WebhookRegistry.HTTPResponseData ejectnotif(string body, NameValueCollection headers)
        {
            string[] splitter = body.Split(new[] { '[', ']' });
            string req = "[" + splitter[1] + "]" + splitter[2];

            splitter = req.Split(new[] { ' ' });

            string KickName = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(splitter[1] + " " + splitter[2]));
            string Kicked = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(splitter[3] + " " + splitter[4]));
            req = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(req));

            string concat = Kicked + "|" + req + "|" + KickName;
            splitter = concat.Split(new[] { '|' });

            CommandRegistry.Instance.RunCommand("auto_buildnotice " + Kicked + " " + req + " " + KickName, BotSession.Instance.grid.Self.AgentID, 1000, BotSession.Instance.MHE, MessageHandler.Destinations.DEST_GROUP, BotSession.Instance.grid.Self.AgentID, BotSession.Instance.ConfigurationHandle.first + " " + BotSession.Instance.ConfigurationHandle.last);


            WebhookRegistry.HTTPResponseData reply = new WebhookRegistry.HTTPResponseData();
            reply.Status = 200;
            reply.ReplyString = "Done";
            return reply;
        }

        [WebhookAttribs("/help")]
        public WebhookRegistry.HTTPResponseData showHelp(string body, NameValueCollection headers)
        {
            WebhookRegistry.HTTPResponseData httpReply = new WebhookRegistry.HTTPResponseData();
            CommandRegistry reg = CommandRegistry.Instance;

            string Final = "<body bgcolor='black'><style type='text/css'>table.HelpTable {  border: 5px solid #1C6EA4;" +
                "  background - color: #000000; "+
                "  width: 100 %;            text - align: left;            border - collapse: collapse;"+
                "        }        table.HelpTable td, table.HelpTable th        {            border: 3px solid #AAAAAA;"+
                "  padding: 3px 2px;        }        table.HelpTable tbody td {  font-size: 19px;  color: #69FAF7;"+
                "}    table.HelpTable tr:nth-child(even)    {    background: #000000;}    table.HelpTable thead"+
                "    {        background: #26A486;  background: -moz-linear-gradient(top, #5cbba4 0%, #3bad92 66%, #26A486 100%);"+
                "  background: -webkit-linear-gradient(top, #5cbba4 0%, #3bad92 66%, #26A486 100%);"+
                "  background: linear-gradient(to bottom, #5cbba4 0%, #3bad92 66%, #26A486 100%);"+
                "  border-bottom: 2px solid #444444;}    table.HelpTable thead th {  font-size: 25px;"+
                "  font-weight: bold;  color: #FFFFFF;  text-align: center;  border-left: 2px solid #D0E4F5;"+
                "}table.HelpTable thead th:first-child {  border-left: none;}table.HelpTable tfoot td {  font-size: 14px;"+
                "}table.HelpTable tfoot.links{    text-align: right;}table.HelpTable tfoot.links a{display: inline - block;"+
                "background: #1C6EA4;  color: #FFFFFF;  padding: 2px 8px;    border - radius: 5px;}</style>";

            Final += "<table class='HelpTable'><thead><tr><th>Bot Version</th><th>5</th></tr></table><br/>";

            Final += "<table class='HelpTable'><thead><tr><th>Command</th><th>Minimum Level Required</th><th>Usage</th><th>Allowed Sources</th><th>Number of Arguments required</th></thead><tbody>";
            foreach(KeyValuePair<string, CommandGroup> cmd in reg.Cmds)
            {
                // Command
                Final += "<tr><td>" + cmd.Value.Command + "</td>";
                // Level
                Final += "<td>" + cmd.Value.minLevel.ToString() + "</td>";
                // Usage
                Final += "<td>" + cmd.Value.cmdUsage.RawUsage() + "</td>";
                // Allowed Sources
                Final += "<td>" + cmd.Value.CommandSource + "</td>";
                // # Arguments
                Final += "<td>" + cmd.Value.arguments.ToString() + "</td></tr>";
            }
            Final += "</tbody></table>";
            Final += "<br/><table class='HelpTable'><thead><tr><th>Assembly</th><th>Version</th><th># Of Commands</th><th>Total Classes</th></tr></thead><tbody>";

            foreach(Assembly A in AppDomain.CurrentDomain.GetAssemblies())
            {
                Final += "<tr><td>" + A.GetName().Name + "</td><td>" + A.GetName().Version + "</td>";
                int TotalCommandsContained = 0;
                int TotalClasses = 0;
                foreach(Type T in A.GetTypes())
                {
                    if (T.IsClass)
                    {
                        TotalClasses++;
                        foreach(MethodInfo MI in T.GetMethods())
                        {
                            CommandGroup[] CG = (CommandGroup[])MI.GetCustomAttributes(typeof(CommandGroup), false);
                            TotalCommandsContained += CG.Length;
                        }
                    }
                }

                Final += "<td>" + TotalCommandsContained.ToString() + "</td><td>" + TotalClasses.ToString() + "</td></tr>";
            }
            Final += "</tbody></table>";


            httpReply.ReplyString = Final;
            httpReply.Status = 200;

            return httpReply;
        }
    }
}
