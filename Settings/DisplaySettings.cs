﻿/*

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
    class DisplaySettings : BaseCommands
    {


        [CommandGroup("show_login_defaults", 4, 0, "Outputs region name and location vector", Destinations.DEST_AGENT | Destinations.DEST_LOCAL)]
        public void show_login_defaults(UUID client, int level, string[] additionalArgs,
                                Destinations source,
                                UUID agentKey, string agentName)
        {

            OCBotMemory ocb = OCBotMemory.Memory;
            MHE(source, client, "_\nRegion [" + ocb.DefaultRegion + "]\nLocation [" + ocb.DefaultLocation.ToString() + "]");
        }

        [CommandGroup("show_git_authed", 4, 0, "", Destinations.DEST_AGENT | Destinations.DEST_LOCAL)]
        public void show_git_authed(UUID client, int level, string[] additionalArgs,
                                Destinations source,
                                UUID agentKey, string agentName)
        {

            OCBotMemory ocb = OCBotMemory.Memory;
            foreach (string S in MainConfiguration.Instance.AuthedGithubUsers)
            {
                MHE(source, client, "[whitelisted] " + S);
            }
        }

        [CommandGroup("show_git_misc", 4, 0, "Prints the git repo, owner, and the Alert Group", Destinations.DEST_AGENT | Destinations.DEST_LOCAL)]
        public void show_git_misc(UUID client, int level, string[] additionalArgs,
                                Destinations source,
                                UUID agentKey, string agentName)
        {

            OCBotMemory ocb = OCBotMemory.Memory;
            MHE(source, client, "_\n \nGitOwner: " + ocb.gitowner + "\nRepo: " + ocb.gitrepo + "\nAlert Group: " + ocb.AlertGroup.ToString());
        }

        [CommandGroup("show_reports", 4, 0, "Outputs who has a active report", Destinations.DEST_AGENT | Destinations.DEST_LOCAL)]
        public void show_report_sessions(UUID client, int level, string[] additionalArgs,
                                Destinations source,
                                UUID agentKey, string agentName)
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



        [CommandGroup("list_limits", 4, 0, "list_limits - Lists all rate limits.", Destinations.DEST_AGENT | Destinations.DEST_LOCAL)]
        public void ListsRateLimits(UUID client, int level, string[] additionalArgs,
                                Destinations source,
                                UUID agentKey, string agentName)
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
