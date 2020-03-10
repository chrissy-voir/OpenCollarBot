/*

Copyright © 2019 Tara Piccari (Aria; Tashia Redrose)
Licensed under the GPLv2

*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bot;
using Bot.Assemble;
using Bot.CommandSystem;
using OpenMetaverse;


namespace OpenCollarBot.Settings
{
    class DisplaySettings
    {

        [CommandGroup("show_level", 0, 0, "This command shows your current auth level if any.", MessageHandler.Destinations.DEST_AGENT | MessageHandler.Destinations.DEST_LOCAL | MessageHandler.Destinations.DEST_GROUP)]
        public void show_level(UUID client, int level, GridClient grid, string[] additionalArgs,
                                MessageHandler.MessageHandleEvent MHE, MessageHandler.Destinations source,
                                CommandRegistry registry, UUID agentKey, string agentName)
        {
            MHE(source, client, "Hi secondlife:///app/agent/" + agentKey.ToString() + "/about !! Your authorization level is " + level.ToString());
        }

        [CommandGroup("show_version", 0, 0, "Outputs the bot version", MessageHandler.Destinations.DEST_AGENT | MessageHandler.Destinations.DEST_LOCAL)]
        public void show_version(UUID client, int level, GridClient grid, string[] additionalArgs,
                                MessageHandler.MessageHandleEvent MHE, MessageHandler.Destinations source,
                                CommandRegistry registry, UUID agentKey, string agentName)
        {
            MHE(source, client, "Version " + ASMInfo.BotVer.ToString());
        }



        [CommandGroup("show_admins", 4, 0, "Outputs all admin users", MessageHandler.Destinations.DEST_AGENT | MessageHandler.Destinations.DEST_LOCAL)]
        public void show_admins(UUID client, int level, GridClient grid, string[] additionalArgs,
                                MessageHandler.MessageHandleEvent MHE, MessageHandler.Destinations source,
                                CommandRegistry registry, UUID agentKey, string agentName)
        {

            for (int i = 0; i < MainConfiguration.Instance.BotAdmins.Count; i++)
            {
                MHE(source, client, "secondlife:///app/agent/" + MainConfiguration.Instance.BotAdmins.ElementAt(i).Key.ToString() + "/about [" + MainConfiguration.Instance.BotAdmins.ElementAt(i).Value.ToString() + "] " + MainConfiguration.Instance.BotAdmins.ElementAt(i).Key.ToString());
            }
        }

        [CommandGroup("show_login_defaults", 4, 0, "Outputs region name and location vector", MessageHandler.Destinations.DEST_AGENT | MessageHandler.Destinations.DEST_LOCAL)]
        public void show_login_defaults(UUID client, int level, GridClient grid, string[] additionalArgs,
                                MessageHandler.MessageHandleEvent MHE, MessageHandler.Destinations source,
                                CommandRegistry registry, UUID agentKey, string agentName)
        {

            OCBotMemory ocb = OCBotMemory.Memory;
            MHE(source, client, "_\nRegion [" + ocb.DefaultRegion + "]\nLocation [" + ocb.DefaultLocation.ToString() + "]");
        }

        [CommandGroup("show_git_authed", 4, 0, "", MessageHandler.Destinations.DEST_AGENT | MessageHandler.Destinations.DEST_LOCAL)]
        public void show_git_authed(UUID client, int level, GridClient grid, string[] additionalArgs,
                                MessageHandler.MessageHandleEvent MHE, MessageHandler.Destinations source,
                                CommandRegistry registry, UUID agentKey, string agentName)
        {

            OCBotMemory ocb = OCBotMemory.Memory;
            foreach (string S in ocb.AuthedGithubUsers)
            {
                MHE(source, client, "[whitelisted] " + S);
            }
        }

        [CommandGroup("show_git_misc", 4, 0, "Prints the git repo, owner, and the Alert Group", MessageHandler.Destinations.DEST_AGENT | MessageHandler.Destinations.DEST_LOCAL)]
        public void show_git_misc(UUID client, int level, GridClient grid, string[] additionalArgs,
                                MessageHandler.MessageHandleEvent MHE, MessageHandler.Destinations source,
                                CommandRegistry registry, UUID agentKey, string agentName)
        {

            OCBotMemory ocb = OCBotMemory.Memory;
            MHE(source, client, "_\n \nGitOwner: " + ocb.gitowner + "\nRepo: " + ocb.gitrepo + "\nAlert Group: " + ocb.AlertGroup.ToString());
        }

        [CommandGroup("show_reports", 4, 0, "Outputs who has a active report", MessageHandler.Destinations.DEST_AGENT | MessageHandler.Destinations.DEST_LOCAL)]
        public void show_report_sessions(UUID client, int level, GridClient grid, string[] additionalArgs,
                                MessageHandler.MessageHandleEvent MHE, MessageHandler.Destinations source,
                                CommandRegistry registry, UUID agentKey, string agentName)
        {
            OCBotMemory ocb = OCBotMemory.Memory;

            MHE(source, client, "[Bug Report Sessions]");
            foreach (KeyValuePair<UUID, OCBotMemory.ReportData> entry in ocb.ActiveReportSessions)
            {
                UUID k = entry.Key;
                int ReportStage = entry.Value.ReportStage;
                MHE(source, client, "secondlife:///app/agent/" + k.ToString() + "/about [ACTIVE|ReportStage: " + ReportStage.ToString() + "]");
            }

            MHE(source, client, "[Feature Request Sessions]");
            foreach (KeyValuePair<UUID, OCBotMemory.ReportData> entry in ocb.ActiveFeatureSessions)
            {
                UUID k = entry.Key;
                int ReportStage = entry.Value.ReportStage;
                MHE(source, client, "secondlife:///app/agent/" + k.ToString() + "/about [ACTIVE|ReportStage: " + ReportStage.ToString() + "]");
            }

            MHE(source, client, "[Comment Sessions]");
            foreach (KeyValuePair<UUID, OCBotMemory.ReportData> entry in ocb.ActiveCommentSessions)
            {
                UUID k = entry.Key;
                int ReportStage = entry.Value.ReportStage;
                MHE(source, client, "secondlife:///app/agent/" + k.ToString() + "/about [ACTIVE|ReportStage: " + ReportStage.ToString() + "]");
            }

            MHE(source, client, "[Notice Sessions]");
            foreach (KeyValuePair<UUID, OCBotMemory.NoticeCreationSessions> entry in ocb.NoticeSessions)
            {
                UUID k = entry.Key;
                MHE(source, client, "secondlife:///app/agent/" + k.ToString() + "/about [ACTIVE|ReportStage: " + entry.Value.State.ToString() + "]");
            }

        }



        [CommandGroup("list_limits", 4, 0, "list_limits - Lists all rate limits.", MessageHandler.Destinations.DEST_AGENT | MessageHandler.Destinations.DEST_LOCAL)]
        public void ListsRateLimits(UUID client, int level, GridClient grid, string[] additionalArgs,
                                MessageHandler.MessageHandleEvent MHE, MessageHandler.Destinations source,
                                CommandRegistry registry, UUID agentKey, string agentName)
        {
            OCBotMemory ocb = OCBotMemory.Memory;

            // list!
            if (ocb.RateLimiter != null)
            {
                if (ocb.RateLimiter.Count > 0)
                {

                    foreach (KeyValuePair<UUID, OCBotMemory.RateData> kvp in ocb.RateLimiter)
                    {
                        MHE(source, agentKey, "secondlife:///app/agent/" + kvp.Key.ToString() + "/about [ResetDate: " + kvp.Value.Reset_At.ToString() + "; RequestsSubmitted: " + kvp.Value.SubmitCount.ToString() + "]");
                    }
                }
                else
                {
                    MHE(source, agentKey, "Nothing to list.");
                }
            }
            else
            {
                MHE(source, agentKey, "CRITICAL FAILURE!\n \n[ Error. Rate limit array is NULL ]");
            }

            ocb.Save();
        }
    }
}
