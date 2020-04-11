using System;
using System.Collections.Generic;
using System.Text;
using Bot.CommandSystem;
using Bot;
using Bot.NonCommands;
using OpenMetaverse;
using OpenCollarBot.GroupCommands;


namespace OpenCollarBot.Core
{
    public class HandleMiscInputs : nCMD
    {
        [NotCommand()]
        public void handle(string text, UUID User, string agentName, MessageHandler.Destinations src, UUID originator)
        {
            OCBotMemory ocb = OCBotMemory.Memory;
            //BotSession.Instance.MHE(MessageHandler.Destinations.DEST_LOCAL, UUID.Zero, $"Got data \n\n[HandleMiscInputs.cs]:handle(\"{text}\", {User.ToString()}, \"{agentName}\", {src.ToString()}, {originator.ToString()})");
            if (ocb.ActiveReportSessions.ContainsKey(User) && ocb.ActiveReportSessions.Count > 0)
            {
                // Send report response to GitCommands
                GitCommands gc = new GitCommands();
                gc.BugResponse(originator, User, ocb.ActiveReportSessions[User].ReportStage, text, src, BotSession.Instance.MHE, agentName);
                return;
            }

            if (ocb.ActiveFeatureSessions.ContainsKey(User) && ocb.ActiveFeatureSessions.Count > 0)
            {
                GitCommands gc = new GitCommands();
                gc.FeatureResponse(originator, User, ocb.ActiveFeatureSessions[User].ReportStage, text, src, BotSession.Instance.MHE, agentName);
                return;
            }

            if (ocb.ActiveCommentSessions.ContainsKey(User) && ocb.ActiveCommentSessions.Count > 0)
            {
                GitCommands gc = new GitCommands();
                gc.comment(originator, User, ocb.ActiveCommentSessions[User].ReportStage, text, src, BotSession.Instance.MHE, agentName);
                return;
            }

            if (ocb.NoticeSessions.ContainsKey(User) && ocb.NoticeSessions.Count > 0)
            {
                GroupSystem gs = new GroupSystem();
                gs.update_notice_sess(originator, User, text, src, BotSession.Instance.MHE, agentName);
                return;
            }

            if (ocb.MailingLists.Count > 0)
            {
                // Scan all mailing lists for a session and agentKey that match.
                foreach (string sML in ocb.MailingLists.Keys)
                {
                    OCBotMemory.MailList ML = ocb.MailingLists[sML];
                    if (ML.PrepFrom == User && ML.PrepState == 1)
                    {
                        MailingLists.MailingLists cML = new MailingLists.MailingLists();
                        cML.HandleMailListData(User, originator, src, BotSession.Instance.MHE, sML, text);
                        return;
                    }
                }
            }
        }
    }
}
