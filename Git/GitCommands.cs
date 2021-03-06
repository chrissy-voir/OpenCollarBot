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
using Newtonsoft.Json;
using OpenMetaverse;
using Bot.CommandSystem;
using System.Net;
using System.Net.Http;
using System.IO;
using Octokit;
using System.Collections.Specialized;
using System.Threading;
using System.Collections;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using OpenCollarBot.Webhooks;


namespace OpenCollarBot
{
    public class GitCommands : BaseCommands
    {

        [CommandGroup("bug", 0, 0, "bug - Begins the process of opening a bug report on github!", Destinations.DEST_AGENT | Destinations.DEST_GROUP | Destinations.DEST_LOCAL)]
        public void BeginBugReport(UUID client, int level, string[] additionalArgs, Destinations source, UUID agentKey, string agentName)
        {
            MHE(source, client, "Hi secondlife:///app/agent/" + agentKey.ToString() + "/about\nCan you please tell me in a single sentence or less a summary of what this bug is?\n \n[Say 'cancel' at any time before I open a github ticket if you change your mind]");
            OCBotMemory bm = OCBotMemory.Memory;
            OCBotMemory.ReportData RD = new OCBotMemory.ReportData();
            RD.ReportStage = 0;
            if (!bm.ActiveReportSessions.ContainsKey(agentKey))
            {

                bm.ActiveReportSessions.Add(agentKey, RD);
                bm.Save();
            }
            else
            {
                MHE(source, client, "Seems you already had a bug report started. I'll start it from the beginning");
                bm.ActiveReportSessions.Remove(agentKey);
                bm.ActiveReportSessions.Add(agentKey, RD);
                bm.Save();
            }



        }
        [CommandGroup("get_rate", 0, 0, "get_rate - Displays your current rate usage", Destinations.DEST_AGENT | Destinations.DEST_GROUP | Destinations.DEST_LOCAL)]
        public void ShowRate(UUID client, int level, string[] additionalArgs, Destinations source, UUID agentKey, string agentName)
        {
            OCBotMemory bm = OCBotMemory.Memory;


            if (bm.RateLimiter.ContainsKey(agentKey))
                MHE(source, client, "Hi secondlife:///app/agent/" + agentKey.ToString() + "/about your current usage is [" + bm.RateLimiter[agentKey].SubmitCount.ToString() + "/" + bm.HardLimit.ToString() + "] until " + bm.RateLimiter[agentKey].Reset_At.ToString("MM/dd/yyyy HH:mm:ss tt") + " - Current timestamp: " + DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss tt"));
            else
                MHE(source, client, "No usage found");



        }
        public void BugResponse(UUID from, UUID agent, int reportStage, string reply, Destinations source, string agentName)
        {

            OCBotMemory ocb = OCBotMemory.Memory;
            OCBotMemory.ReportData RD = ocb.ActiveReportSessions[agent];
            ocb.ActiveReportSessions.Remove(agent);

            if (reply == "cancel")
            {
                MHE(source, from, "Canceled \n \n[Any entered data has been discarded]");
                ocb.Save();
                return;
            }

            if (reportStage == 0)
            {
                MHE(source, from, "Okay give me a moment");
                RD.ReportTitle = reply;
                RD.ReportStage = 1;
                ocb.ActiveReportSessions.Add(agent, RD);
                MHE(source, from, "Okay. Now please go into as much detail about the problem as you would like.  \n \n[Say '@' on a new line when you are done typing out the full description]");
            }
            else if (reportStage == 1)
            {
                if (reply == "@")
                {
                    RD.ReportStage++;
                    MHE(source, from, "Okay secondlife:///app/agent/" + agent.ToString() + "/about if you have any logs to add please add them below.\n \n[If you have no logs, or are done entering your logs, say '@']");
                }
                else
                {
                    RD.ReportBody += "\n" + reply;
                    MHE(source, from, "-Data Added-");
                }
                ocb.ActiveReportSessions.Add(agent, RD);
            }
            else if (reportStage == 2)
            {
                if (reply == "@")
                {
                    RD.ReportStage++;
                    MHE(source, from, "Okay!\n \n[If you are ready to upload this report say 'ready']");

                }
                else
                {
                    MHE(source, from, "-Data Added-");
                    RD.ReportExtraData += "<details>\n<summary>Extra Data</summary>\n\n```\n" + reply + "\n```\n</details>";
                }
                ocb.ActiveReportSessions.Add(agent, RD);
            }
            else
            {
                if (reply != "ready") return;
                int userLevel = 0;
                if (MainConfiguration.Instance.BotAdmins.ContainsKey(agent)) userLevel = MainConfiguration.Instance.BotAdmins[agent];

                if (!ocb.SubmitNewRateUsage(agent) && userLevel < 3)
                {
                    MHE(source, from, "You have hit your rate limit (5 max) for 7 days. Your rate limit resets at " + ocb.RateLimiter[agent].Reset_At.ToString());
                    ocb.Save();
                    return;
                }
                MHE(source, from, "Please stand by..");

                GitHubClient ghc = new GitHubClient(new ProductHeaderValue("OpenCollarBot"));
                Credentials cred = new Credentials("opencollarangel", Bot.Assemble.ASMInfo.GitPassword);
                ghc.Credentials = cred;
                NewIssue issueX = new NewIssue(RD.ReportTitle);

                issueX.Body = "Issue created by: " + agentName + "\n\n" + RD.ReportBody + "\n\n" + RD.ReportExtraData;
                Task<Issue> tskI = ghc.Issue.Create(ocb.gitowner, ocb.gitrepo, issueX);
                tskI.Wait();

                int issueNumber = tskI.Result.Number;

                if (tskI.Result.State == ItemState.Open)
                    MHE(source, from, "[http://github.com/" + ocb.gitowner + "/" + ocb.gitrepo + "/issues/" + issueNumber.ToString() + " Issue Created - Click Here To View]");
                else
                    MHE(source, from, "Issue creation failure. Try again later. If this issue persists please report this as a bug to group staff. Thank you");
            }

            ocb.Save();
        }

        [CommandGroup("clear_reports", 4, 0, "Clears in progress bug reports", Destinations.DEST_AGENT | Destinations.DEST_LOCAL | Destinations.DEST_GROUP)]
        public void clear_bug_reports(UUID client, int level, string[] additionalArgs, Destinations source, UUID agentKey, string agentName)
        {
            OCBotMemory ocb = OCBotMemory.Memory;

            ocb.ActiveReportSessions = new Dictionary<UUID, OCBotMemory.ReportData>();
            ocb.ActiveFeatureSessions = new Dictionary<UUID, OCBotMemory.ReportData>();
            ocb.ActiveCommentSessions = new Dictionary<UUID, OCBotMemory.ReportData>();
            ocb.Save();

            MHE(source, client, "Cleared all active reports");
        }


        [CommandGroup("set_git_owner", 5, 1, "set_git_owner [owner name] - Sets the github owner name for the repo", Destinations.DEST_AGENT | Destinations.DEST_LOCAL)]
        public void Set_GitOwner(UUID client, int level, string[] additionalArgs, Destinations source, UUID agentKey, string agentName)
        {
            OCBotMemory ocb = OCBotMemory.Memory;
            ocb.gitowner = additionalArgs[0];
            ocb.Save();

            MHE(source, client, "Repository owner set");
        }

        [CommandGroup("set_git_repo", 5, 1, "set_git_repo [repo name] - Sets the github repo", Destinations.DEST_AGENT | Destinations.DEST_LOCAL)]
        public void Set_GitRepo(UUID client, int level, string[] additionalArgs, Destinations source, UUID agentKey, string agentName)
        {
            OCBotMemory ocb = OCBotMemory.Memory;
            ocb.gitrepo = additionalArgs[0];
            ocb.Save();

            MHE(source, client, "Repository name set");
        }



        [CommandGroup("reset_rate", 4, 1, "reset_rate [uuid] - Completely reset a person's rate limit block ", Destinations.DEST_AGENT | Destinations.DEST_LOCAL)]
        public void ResetRateLimits(UUID client, int level, string[] additionalArgs, Destinations source, UUID agentKey, string agentName)
        {
            OCBotMemory ocb = OCBotMemory.Memory;

            // Reset limit!
            UUID userKey = UUID.Parse(additionalArgs[0]);
            ocb.RateLimiter.Remove(userKey);
            MHE(source, client, "User now has 5 remaining queries");
            MHE(Destinations.DEST_AGENT, userKey, "Rate blocks reset for you. You now have 5 remaining bugs/features you can submit from inworld. Remember there is not a limit if you use the github site.");

            ocb.Save();
        }


        [CommandGroup("reset_rates", 5, 0, "reset_rates - Completely reset all rate limits", Destinations.DEST_AGENT | Destinations.DEST_LOCAL)]
        public void ResetAllRateLimits(UUID client, int level, string[] additionalArgs, Destinations source, UUID agentKey, string agentName)
        {
            OCBotMemory ocb = OCBotMemory.Memory;

            // Reset limits!
            ocb.RateLimiter = new Dictionary<UUID, OCBotMemory.RateData>();
            MHE(source, client, "Reset completed");

            ocb.Save();
        }



        [CommandGroup("set_alert_group", 5, 1, "set_alert_group [uuid] - Sets the alert group for git notifications", Destinations.DEST_AGENT | Destinations.DEST_LOCAL)]
        public void SetAlertGroup(UUID client, int level, string[] additionalArgs, Destinations source, UUID agentKey, string agentName)
        {
            OCBotMemory ocb = OCBotMemory.Memory;

            ocb.AlertGroup = UUID.Parse(additionalArgs[0]);

            ocb.Save();
        }


        [CommandGroup("feature", 0, 0, "feature - Begins feature request program", Destinations.DEST_AGENT | Destinations.DEST_LOCAL | Destinations.DEST_GROUP)]
        public void FileFeatureRequest(UUID client, int level, string[] additionalArgs, Destinations source, UUID agentKey, string agentName)
        {
            OCBotMemory ocb = OCBotMemory.Memory;

            MHE(source, client, "Hi secondlife:///app/agent/" + agentKey.ToString() + "/about ! Can you give me a short summary in a sentence or less about your feature request?\n \n[You can always say 'cancel' at any time to delete this request.]\n[To create a new line type '#']");

            OCBotMemory.ReportData rd = new OCBotMemory.ReportData();
            rd.ReportStage = 0;
            if (ocb.ActiveFeatureSessions.ContainsKey(agentKey))
            {
                MHE(source, client, "* You already had a report session started. I've reset that session for you");
                ocb.ActiveFeatureSessions.Remove(agentKey);
            }

            ocb.ActiveFeatureSessions.Add(agentKey, rd);

            ocb.Save();
        }


        public void FeatureResponse(UUID from, UUID agent, int reportStage, string reply, Destinations source, string agentName)
        {
            OCBotMemory ocb = OCBotMemory.Memory;

            OCBotMemory.ReportData RD = ocb.ActiveFeatureSessions[agent];

            if (reply == "cancel")
            {
                ocb.ActiveFeatureSessions.Remove(agent);
                ocb.Save();
                MHE(source, from, "Canceled\n \n[Report Data has been deleted]");
                return;
            }
            ocb.ActiveFeatureSessions.Remove(agent);
            if (RD.ReportStage == 0)
            {
                // Save report title and prompt user for additional details
                RD.ReportTitle = reply;
                RD.ReportStage++;
                ocb.ActiveFeatureSessions.Add(agent, RD);
                MHE(source, from, "Saved. Can I get an extended description about your feature request?\n \n[Say '@' when you are ready to proceed]");
            }
            else if (RD.ReportStage == 1)
            {
                if (reply == "#")
                {
                    RD.ReportBody += "\n";
                }
                else
                    RD.ReportBody += reply;

                if (reply == "@")
                {
                    RD.ReportStage++;
                    MHE(source, from, "Saved!\n \n[If you are ready to submit this request say '@']");
                }

                ocb.ActiveFeatureSessions.Add(agent, RD);
            }
            else if (RD.ReportStage == 2)
            {
                int LVL = 0;
                if (MainConfiguration.Instance.BotAdmins.ContainsKey(agent)) LVL = MainConfiguration.Instance.BotAdmins[agent];
                if (!ocb.SubmitNewRateUsage(agent) && LVL < 3)
                {
                    // Notify user
                    ocb.Save();
                    MHE(source, from, "You have hit your rate limit (5 max) for 7 days. Your rate limit resets at " + ocb.RateLimiter[agent].Reset_At.ToString());
                    return;
                }

                GitHubClient ghc = new GitHubClient(new ProductHeaderValue("OpenCollarBot"));
                Credentials cred = new Credentials("opencollarangel", Bot.Assemble.ASMInfo.GitPassword);
                ghc.Credentials = cred;
                NewIssue NI = new NewIssue(RD.ReportTitle);
                NI.Body = "Feature Request filed by: " + agentName + "\n\n" + RD.ReportBody;

                Task<Issue> filed_issue = ghc.Issue.Create(ocb.gitowner, ocb.gitrepo, NI);

                filed_issue.Wait();

                int num = filed_issue.Result.Number;

                if (filed_issue.Result.State == ItemState.Open)
                {
                    MHE(source, from, "[http://github.com/" + ocb.gitowner + "/" + ocb.gitrepo + "/issues/" + num.ToString() + " Feature Request Created]");
                }
                else
                    MHE(source, from, "Ticket creation failure. Try again later.");
            }


            ocb.Save();
        }


        public GitCommands() { }
        
        private struct GHEvents
        {
            public string Key;
            public string Value;
        }
        public static void Process(string Response, string GHEvent)
        {

            dynamic stuff = JsonConvert.DeserializeObject(Response);

            OCBotMemory ocb = OCBotMemory.Memory;

            if (Directory.Exists("RequestLogs")) Directory.CreateDirectory("RequestLogs");
            Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            File.WriteAllText("RequestLogs/" + unixTimestamp+"_Event.txt", GHEvent);
            File.WriteAllText("RequestLogs/" + unixTimestamp + "_Resp.txt", Response);

            try
            {
                //KeyValuePair<string, string> item = new KeyValuePair<string, string>("X-Github-Event", GHEvent);
                GHEvents item = new GHEvents
                {
                    Key = "X-Github-Event",
                    Value = GHEvent
                };

                if (item.Key == "X-Github-Event")
                {
                    if (item.Value == "issues")
                    {
                        if (!MainConfiguration.Instance.Authed(Convert.ToString(stuff.issue.user.login))) return;

                        int Issue_Number = Convert.ToInt32(stuff.issue.number);
                        string HTMLUrl = Convert.ToString(stuff.issue.html_url);
                        string Title = Convert.ToString(stuff.issue.title);
                        if (stuff.action == "opened")
                        {
                            MH(Destinations.DEST_GROUP, ocb.AlertGroup, "New Issue #" + Issue_Number.ToString() + " [" + HTMLUrl + " " + Title + "]");
                        }
                        else if (stuff.action == "closed")
                        {
                            MH(Destinations.DEST_GROUP, ocb.AlertGroup, "Closed Issue #" + Issue_Number.ToString() + " [" + HTMLUrl + " " + Title + "]");
                        }
                        else if (stuff.action == "reopened")
                        {
                            MH(Destinations.DEST_GROUP, ocb.AlertGroup, "Reopened Issue #" + Issue_Number.ToString() + " [" + HTMLUrl + " " + Title + "]");
                        }
                        else if (stuff.action == "assigned")
                        {
                            MH(Destinations.DEST_GROUP, ocb.AlertGroup, "Issue #" + Issue_Number.ToString() + " has changed assigned users [" + HTMLUrl + " " + Title + "]");
                        }

                    }
                    else if (item.Value == "issue_comment")
                    {

                        if (!MainConfiguration.Instance.Authed(Convert.ToString(stuff.comment.user.login))) return;
                        if (stuff.action == "created" || stuff.action == "edited")
                        {
                            int Issue_Number = Convert.ToInt32(stuff.issue.number);
                            string HTMLUrl = Convert.ToString(stuff.issue.html_url);
                            string Title = Convert.ToString(stuff.issue.title);
                            MH(Destinations.DEST_GROUP, ocb.AlertGroup, "Issue #" + Issue_Number.ToString() + " has a new comment [" + HTMLUrl + " " + Title + "]");
                        }
                    }
                    else if (item.Value == "push")
                    {

                        if (!MainConfiguration.Instance.Authed(Convert.ToString(stuff.pusher.name))) return;
                        int I = 0;
                        bool loop = true;
                        while (loop)
                        {
                            try
                            {

                                string msg = Convert.ToString(stuff.commits[I].message);
                                if (msg.Contains("Merged")) msg = "Pull Request Merged";

                                MH(Destinations.DEST_GROUP, ocb.AlertGroup, "New Commit: " + msg);
                                I++;

                                if (stuff.commits[I] == null) loop = false;
                            }
                            catch (Exception e)
                            {
                                loop = false;
                            }
                        }

                    }
                    else if (item.Value == "pull_request")
                    {
                        int PRN = Convert.ToInt32(stuff.pull_request.number);
                        string PRU = stuff.pull_request.html_url;
                        string PRT = stuff.pull_request.title;
                        if (stuff.action == "closed")
                        {
                            if (Convert.ToBoolean(stuff.pull_request.merged)) MH(Destinations.DEST_GROUP, ocb.AlertGroup, "Pull request " + PRN + " merged and closed\n View it at: [" + PRU + " " + PRT + "]");
                            else MH(Destinations.DEST_GROUP, ocb.AlertGroup, "Pull request closed without merging\nView at [" + PRU + " " + PRT + "]");

                        }
                        else if (stuff.action == "opened")
                        {
                            MH(Destinations.DEST_GROUP, ocb.AlertGroup, "New Pull request opened #" + PRN + " [" + PRU + " " + PRT + "]");

                        }
                    }
                    else if (item.Value == "pull_request_review_comment")
                    {
                        int PRN = Convert.ToInt32(stuff.pull_request.number);
                        string PRU = stuff.pull_request.html_url;
                        string PRT = stuff.pull_request.title;
                        if (stuff.action == "created")
                        {
                            // New review comment
                            MH(Destinations.DEST_GROUP, ocb.AlertGroup, "New Pull Request Review Comment on PR #" + PRN + "\nReview Comment Author: " + stuff.comment.user.login + "\nReview Comment: " + stuff.comment.body + "\n \nView it at: [" + PRU + " " + PRT + "]");
                        }
                    }
                    else if (item.Value == "pull_request_review")
                    {
                        int PRN = Convert.ToInt32(stuff.pull_request.number);
                        string PRU = stuff.pull_request.html_url;
                        string PRT = stuff.pull_request.title;

                        MH(Destinations.DEST_GROUP, ocb.AlertGroup, "New Pull Request Review on PR #" + PRN + "\n \nView it at: [" + PRU + " " + PRT + "]");
                    }
                }
            }
            catch (Exception e)
            {
                MH(Destinations.DEST_LOCAL, UUID.Zero, "Exception caught in WebHook_Processor: " + e.Message + "\nStack: " + e.StackTrace);
                File.WriteAllText("replay.txt", Response);
                MH(Destinations.DEST_LOCAL, UUID.Zero, "Wrote JSON to local file [replay.txt]");
            }
        }




        [CommandGroup("comment", 0, 1, "comment [ticket_number] - Comments on a ticket number", Destinations.DEST_AGENT | Destinations.DEST_LOCAL | Destinations.DEST_GROUP)]
        public void DoActionComment(UUID client, int level, string[] additionalArgs, Destinations source, UUID agentKey, string agentName)
        {
            OCBotMemory ocb = OCBotMemory.Memory;

            MHE(source, client, "Okay I can post a comment on Ticket " + additionalArgs[0] + ". Please type out your comments below.\n \n[If you're done type '@', if you want to make a new line type '#', to cancel say 'cancel']");

            OCBotMemory.ReportData rd = new OCBotMemory.ReportData();
            rd.TicketNumber = int.Parse(additionalArgs[0]);
            rd.ReportStage = 0;
            ocb.ActiveCommentSessions.Add(agentKey, rd);

            ocb.Save();
        }

        public void comment(UUID from, UUID agent, int reportStage, string reply, Destinations source, string agentName)
        {
            OCBotMemory ocb = OCBotMemory.Memory;

            OCBotMemory.ReportData RD = ocb.ActiveCommentSessions[agent];
            if (reply == "cancel")
            {
                ocb.ActiveCommentSessions.Remove(agent);
                ocb.Save();
                MHE(source, from, "Canceled\n \n[Data removed]");
                return;
            }

            ocb.ActiveCommentSessions.Remove(agent);
            if (RD.ReportStage == 0)
            {
                if (reply == "#")
                {
                    RD.ReportBody += "\n";
                }
                else if (reply == "@")
                {
                    RD.ReportStage++;
                    MHE(source, from, "OK. Last question! Is this an Issue or a pull request you want to comment on? \n \n[issue/pr]");
                }
                else RD.ReportBody += reply;


            }
            else if (RD.ReportStage == 1)
            {
                if (reply.ToLower() == "issue")
                {
                    RD.ReportTitle = "issue";
                    RD.ReportStage++;
                    MHE(source, from, "OK\n \n[If you're ready to submit this say 'ready']");
                }
                else if (reply.ToLower() == "pr")
                {
                    RD.ReportTitle = "pr";
                    RD.ReportStage++;
                    MHE(source, from, "OK\n \n[If you're ready to submit this say 'ready']");
                }
                else
                {
                    MHE(source, from, "Unable to recognize reply. Try again");
                }

            }
            else if (RD.ReportStage == 2)
            {
                if (reply != "ready")
                {
                    return;
                }
                else
                {
                    GitHubClient ghc = new GitHubClient(new ProductHeaderValue("OpenCollarBot"));
                    ghc.Credentials = new Credentials("opencollarangel", "Gai3+cOLOkUUIn==");

                    if (RD.ReportTitle == "issue")
                    {
                        if (!ocb.SubmitNewRateUsage(agent))
                        {
                            MHE(source, from, "You have reached your request rate limit for the week.");
                            ocb.Save();
                            return;
                        }
                        ghc.Issue.Comment.Create(ocb.gitowner, ocb.gitrepo, RD.TicketNumber, "Comment from " + agentName + "\n \n" + RD.ReportBody);
                        MHE(source, from, "Data has been submitted!!");
                        ocb.Save();
                        return;
                    }
                    else if (RD.ReportTitle == "pr")
                    {
                        MHE(source, from, "*Error*\n \n[Only issues are supported at this time]\n[Your request limit has not been modified]");
                        ocb.Save();
                        return;
                    }
                    else
                    {
                        ocb.Save();
                        MHE(source, from, "Unrecognized request");
                        return;
                    }
                }
            }



            ocb.ActiveCommentSessions.Add(agent, RD);
            ocb.Save();
        }


        private static byte[] EncodeScript(string raw)
        {
            byte[] strByte = Encoding.UTF8.GetBytes(raw);
            byte[] assetData = new byte[strByte.Length];
            Array.Copy(strByte, 0, assetData, 0, strByte.Length);
            return assetData;
        }

        [CommandGroupMaster("Git")]
        [CommandGroup("new_scriptlist", 5, 0, "new_scriptlist - Resets the local ScriptList data file in preparation for ZI integration", Destinations.DEST_LOCAL | Destinations.DEST_AGENT)]
        public void new_scriptlist(UUID client, int level, string[] additionalArgs, Destinations source, UUID agentKey, string agentName)
        {
            if (File.Exists("ScriptList.bdf")) File.Delete("ScriptList.bdf");
            ScriptList.ResetSingleton();

            // Grab a new singleton and save the file
            ScriptList.Instance.Save();

        }


        [CommandGroupMaster("Git")]
        [CommandGroup("add_scriptlist", 5, 4, "add_scriptlist [scriptName] [scriptFolder] [assetType] [fileExt:optional] - Adds a entry to the ScriptList BDF", Destinations.DEST_LOCAL | Destinations.DEST_AGENT)]
        public void add_scriptlist(UUID client, int level, string[] additionalArgs, Destinations source, UUID agentKey, string agentName)
        {

            ScriptList sl = ScriptList.Instance;


            string ScriptName = additionalArgs[0];
            string ScriptPath = additionalArgs[1];
            string AssetType = additionalArgs[2];
            

            ScriptList.ScriptListFlags slf = new ScriptList.ScriptListFlags();
            slf.ScriptName = ScriptName;
            slf.Container = ScriptPath;
            slf.FileExt = additionalArgs[3];
            slf.Type = AssetType;


            sl.Scripts.Add(ScriptName, slf);
            sl.Save();


        }

        [CommandGroupMaster("Git")]
        [CommandGroup("get_scriptlist", 5,2,"get_scriptlist [gitowner] [branch] - Retrieves the ScriptList.bdf from a specified location for editing", Destinations.DEST_AGENT| Destinations.DEST_GROUP | Destinations.DEST_LOCAL)]
        public void get_scriptlist(UUID client, int level, string[] additionalArgs, Destinations source, UUID agentKey, string agentName)
        {
            HttpWebRequest hwr = null;
            HttpWebResponse hwresp = null;
            string baseURL = "https://raw.githubusercontent.com/" + additionalArgs[0] + "/OpenCollar/" + additionalArgs[1]+"/";
            try
            {
                hwr = (HttpWebRequest)HttpWebRequest.Create(baseURL + ".zi/ScriptList.bdf");
                hwr.Method = "GET";
                hwresp = (HttpWebResponse)hwr.GetResponse();
            }catch(Exception e)
            {
                MHE(source, client, "Error fetching the BDF");
                return;
            }

            MHE(source, client, "Saving bdf file...");
            StreamReader sr = new StreamReader(hwresp.GetResponseStream());
            File.WriteAllText("ScriptList.json", sr.ReadToEnd());

            sr.Close();
        }

        [CommandGroupMaster("Git")]
        [CommandGroup("rem_scriptlist", 5, 1, "rem_scriptlist [scriptName] - Remove entry from BDF", Destinations.DEST_LOCAL | Destinations.DEST_AGENT)]
        public void rem_scriptlist(UUID client, int level, string[] additionalArgs, Destinations source, UUID agentKey, string agentName)
        {
            ScriptList sl = ScriptList.Instance;
            sl.Scripts.Remove(additionalArgs[0]);
            sl.Save();

        }


        [CommandGroupMaster("Git")]
        [CommandGroup("dump_scriptlist", 5, 0, "dump_scriptlist - Dump BDF to human readable", Destinations.DEST_LOCAL | Destinations.DEST_AGENT)]
        public void dump_scriptlist(UUID client, int level, string[] additionalArgs, Destinations source, UUID agentKey, string agentName)
        {
            MHE(source, client, "Starting dump..");
            ScriptList sl = null;
            try
            {
                sl = ScriptList.Instance;
            }
            catch (Exception e)
            {
                MHE(source, client, "ScriptList - failure to assign singleton! (This is fatal, stopping operation)");
                return;
            }
            foreach (KeyValuePair<string, ScriptList.ScriptListFlags> kvp in sl.Scripts)
            {
                MHE(source, client, "_\nScript: " + kvp.Key + "\nData: " + kvp.Value.Dump());
            }
            MHE(source, client, "Dump successful");
        }

        [CommandGroupMaster("Git")]
        [CommandGroup("refresh_git", 0, 1, "refresh_git [gitowner] - Pulls the master branch of OpenCollar from the gitowner account", Destinations.DEST_LOCAL | Destinations.DEST_AGENT | Destinations.DEST_GROUP)]
        public void refresh_git_build(UUID client, int level, string[] additionalArgs, Destinations source, UUID agentKey, string agentName)
        {
            MHE(source, client, "* Initializing Refresh");

            HttpWebRequest hwr = null;
            HttpWebResponse hwresp = null;

            string baseURL = "https://raw.githubusercontent.com/" + additionalArgs[0] + "/OpenCollar/master/";

            try
            {
                hwr = (HttpWebRequest)HttpWebRequest.Create(baseURL + ".zi/ScriptList.bdf");
                hwr.Method = "GET";
                hwresp = (HttpWebResponse)hwr.GetResponse();
            }
            catch (Exception e) { }

            if (hwresp.StatusCode == HttpStatusCode.NotFound)
            {
                MHE(source, client, "The necessary script list BDF could not be found");
                return;
            }else if(hwresp.StatusCode == HttpStatusCode.OK)
            {
                MHE(source, client, "BDF Located\n \n[Adding to compile Queue. Scripts will be sent after verification]");
                StreamReader sr = new StreamReader(hwresp.GetResponseStream());
                StreamWriter sw = new StreamWriter("ScriptList.bdf");
                sw.Write(sr.ReadToEnd());

                sr.Close();
                sw.Close();

                ScriptList downloadedList = ScriptList.Reload(true);
                ScriptImporter.ScriptManager.AddQueue(downloadedList, client, additionalArgs[0]);
            }
        }


        [CommandGroupMaster("Git")]
        [CommandGroup("refresh_git_nosend", 1005430, 1, "refresh_git_nosend [gitowner] - Pulls the master branch of OpenCollar from the gitowner account", Destinations.DEST_AGENT)]
        public void refresh_git_build_nosend(UUID client, int level, string[] additionalArgs, Destinations source, UUID agentKey, string agentName)
        {
            MHE(source, client, "* Initializing Refresh");

            HttpWebRequest hwr = null;
            HttpWebResponse hwresp = null;

            string baseURL = "https://raw.githubusercontent.com/" + additionalArgs[0] + "/OpenCollar/master/";

            try
            {
                hwr = (HttpWebRequest)HttpWebRequest.Create(baseURL + ".zi/ScriptList.bdf");
                hwr.Method = "GET";
                hwresp = (HttpWebResponse)hwr.GetResponse();
            }
            catch (Exception e) { }

            if (hwresp.StatusCode == HttpStatusCode.NotFound)
            {
                MHE(source, client, "The necessary script list BDF could not be found");
                return;
            }
            else if (hwresp.StatusCode == HttpStatusCode.OK)
            {
                MHE(source, client, "BDF Located\n \n[Adding to compile Queue. Scripts will be sent after verification]");
                StreamReader sr = new StreamReader(hwresp.GetResponseStream());
                StreamWriter sw = new StreamWriter("ScriptList.bdf");
                sw.Write(sr.ReadToEnd());

                sr.Close();
                sw.Close();

                ScriptList downloadedList = ScriptList.Reload(true);
                ScriptImporter.ScriptManager.AddQueue(downloadedList, UUID.Zero, additionalArgs[0]);
            }
        }


        [CommandGroupMaster("Git")]
        [CommandGroup("refresh_git_branch", 0, 2, "refresh_git_branch [gitOwner] [gitbranch] - Pulls the branch of OpenCollar from the gitowner account", Destinations.DEST_AGENT | Destinations.DEST_GROUP | Destinations.DEST_LOCAL)]
        public void refresh_git_branch_build(UUID client, int level, string[] additionalArgs, Destinations source, UUID agentKey, string agentName)
        {
            MHE(source, client, "* Initializing Refresh");
            HttpWebRequest hwr = null;
            HttpWebResponse hwresp = null;
            string baseURL = "https://raw.githubusercontent.com/" + additionalArgs[0] + "/OpenCollar/" + additionalArgs[1] + "/";

            try
            {
                hwr = (HttpWebRequest)HttpWebRequest.Create(baseURL + ".zi/ScriptList.bdf");
                hwr.Method = "GET";
                hwresp = (HttpWebResponse)hwr.GetResponse();
            }catch(Exception e) { }

            if(hwresp.StatusCode == HttpStatusCode.NotFound)
            {
                MHE(source, client, "The BDF File [ScriptList.bdf] was not found!");
                return;
            }else if(hwresp.StatusCode == HttpStatusCode.OK)
            {
                MHE(source, client, "BDF Located\n \n[Adding to compile Queue. Scripts will be sent after verification]");
                StreamReader sr = new StreamReader(hwresp.GetResponseStream());
                StreamWriter sw = new StreamWriter("ScriptList.bdf");
                sw.Write(sr.ReadToEnd());
                sr.Close();
                sw.Close();

                ScriptList downloaded = ScriptList.Reload(true);
                ScriptImporter.ScriptManager.AddQueue(downloaded, client, additionalArgs[0], additionalArgs[1]);
            }
        }

    }

    [Serializable()]
    public sealed class ScriptList
    {
        private static ScriptList _inst = null;
        private static readonly object lockObj = new object();


        [Serializable()]
        public struct ScriptListFlags
        {
            public string ScriptName;
            public string Type;
            public string Container;
            public string ScriptPath;
            public string FileExt;
            public string Dump()
            {
                return "\n \nScriptPath: "+ScriptPath+"\nScriptName: "+ScriptName+"\nType: "+Type+"\nContainer Folder: "+Container;
            }
        }
        public string GitOwner;
        public string GitRepo = "OpenCollar";
        public bool IsPullRequest = false;
        public string Branch;
        public Dictionary<string, ScriptListFlags> Scripts = null;



        // No commit hash needed, will be pulled from OCBotMemory as it receives it from the git webhooks


        static ScriptList()
        {

        }

        [NonSerialized()]
        public bool SingletonInitialized = false;

        public static ScriptList Instance
        {
            get
            {
                lock (lockObj)
                {
                    if (_inst == null)
                    {

                        _inst = ScriptList.Reload();
                        Console.WriteLine("[debug] Singleton has " + _inst.Scripts.Count.ToString() + " entries before validation");
                        _inst.Validate();
                        Console.WriteLine("[debug] Singleton has " + _inst.Scripts.Count.ToString() + " entries after validation");

                        _inst.SingletonInitialized = true;
                        Console.WriteLine("=====<> Singleton for ScriptList loaded <>=====");
                    }
                    Console.WriteLine("Returning singleton with " + _inst.Scripts.Count.ToString() + " entries");
                    return _inst;
                }
            }
        }

        public static ScriptList Reload(bool fromFile = true)
        {

            Console.WriteLine("ScriptList Instance Creator Loaded");
            if (!File.Exists("ScriptList.bdf") && fromFile)
            {
                ScriptList sl = new ScriptList();
                sl.Validate();
                return sl;

            }
            try
            {

                if (_inst != null && !fromFile)
                {
                    if (_inst.SingletonInitialized) return Instance;
                }
            }
            catch (Exception e) { }
            Console.WriteLine("=====> Begin read");


            ScriptList tmpObj = JsonConvert.DeserializeObject<ScriptList>(File.ReadAllText("ScriptList.bdf"));
            Console.WriteLine("=====> End Read");
            Console.WriteLine("ScriptList Entries from BDF: " + tmpObj.Scripts.Count.ToString());
            return tmpObj;
        }

        public void Save()
        {
            string Json = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText("ScriptList.bdf", Json);
        }

        public void Validate()
        {
            if (Scripts == null) Scripts = new Dictionary<string, ScriptListFlags>();
        }

        public static void ResetSingleton()
        {
            _inst = null;
        }
    }
    
}
