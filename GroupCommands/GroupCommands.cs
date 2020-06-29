/*

Copyright © 2019 Tara Piccari (Aria; Tashia Redrose)
Licensed under the GPLv2

*/


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Bot;
using Bot.CommandSystem;
using System.IO;
using OpenMetaverse;
using OpenMetaverse.Packets;
using OpenMetaverse.ImportExport.Collada14;

namespace OpenCollarBot.GroupCommands
{
    public class GroupSystem : BaseCommands
    {
        [CommandGroup("create_notice", 5, 1, "create_notice [noticeName] - Creates a new scheduled notice", Destinations.DEST_AGENT | Destinations.DEST_GROUP | Destinations.DEST_LOCAL)]
        public void create_notice(UUID client, int level,  string[] additionalArgs,
                                Destinations source,
                                 UUID agentKey, string agentName)
        {
            OCBotMemory ocb = OCBotMemory.Memory;
            if (ocb.NoticeSessions.ContainsKey(agentKey))
            {
                // Remove session
                MHE(source, client, "You already had a notice creation session in progress, restarting session");
                ocb.NoticeSessions.Remove(agentKey);

            }

            OCBotMemory.NoticeCreationSessions nCS = new OCBotMemory.NoticeCreationSessions();
            nCS.SessionAv = agentKey;
            nCS.State = 0;
            nCS.TemporaryNotice = new OCBotMemory.Notices();
            nCS.TemporaryNotice.InternalName = additionalArgs[0];

            ocb.NoticeSessions.Add(agentKey, nCS);
            ocb.Save();

            MHE(source, client, "Okay! Notice session started! To stop at any time say 'cancel'\n \n[Please submit a summary of the notice!]");
        }

        [CommandGroup("modify_notice", 5, 1, "modify_notice [noticeName] - Modifies a existing notice", Destinations.DEST_AGENT | Destinations.DEST_GROUP | Destinations.DEST_LOCAL)]
        public void modify_existing_notice(UUID client, int level,  string[] additionalArgs,
                                Destinations source,
                                 UUID agentKey, string agentName)
        {
            // This command will modify an existing notice
            if (OCBotMemory.Memory.NoticeLists.ContainsKey(additionalArgs[0]))
            {
                OCBotMemory.Notices existingNotice = OCBotMemory.Memory.NoticeLists[additionalArgs[0]];
                MHE(source, client, $"OK!\n \nHere is the information currently set\nDescription: {existingNotice.NoticeSummary}\nBody: {existingNotice.NoticeDescription}\nGroup Key: {existingNotice.GroupKey.ToString()}\nHas Attachment? [{existingNotice.HasAttachment.ToString()}]\nAttachment UUID: {existingNotice.NoticeAttachment.ToString()}\nRepeats: [{existingNotice.Repeats.ToString()}]");
                OCBotMemory.NoticeCreationSessions newSess = new OCBotMemory.NoticeCreationSessions();
                newSess.State = 0;
                newSess.TemporaryNotice = existingNotice;
                newSess.SessionAv = agentKey;

                MHE(source, client, "You will be asked for the various different properties. To skip over something say: skip");



                if (OCBotMemory.Memory.NoticeSessions.ContainsKey(agentKey))
                {
                    MHE(source, client, "You appear to have already had a notice creator session running. Restarting");
                    OCBotMemory.Memory.NoticeSessions.Remove(agentKey);
                }
                OCBotMemory.Memory.NoticeSessions.Add(agentKey, newSess);
                MHE(source, client, "Please submit a summary of the notice");

            }
            else
            {
                MHE(source, client, "There is no such notice with that ID");
            }

        }


        public void update_notice_sess(UUID fromID, UUID agentKey, string request, Destinations sourceLoc, string agentName)
        {
            OCBotMemory ocb = OCBotMemory.Memory;
            OCBotMemory.NoticeCreationSessions nCS = ocb.NoticeSessions[agentKey];

            if (request.ToLower() == "cancel")
            {
                ocb.NoticeSessions.Remove(agentKey);
                ocb.Save();
                MHE(sourceLoc, fromID, "Canceled");
                return;
            } else if(request.ToLower() == "skip")
            {
                MHE(sourceLoc, fromID, "Stand by...");
                switch (nCS.State)
                {
                    case 0:
                        {
                            MHE(sourceLoc, fromID, $"Notice summary remains set to: {nCS.TemporaryNotice.NoticeSummary}");
                            nCS.State++;
                            MHE(sourceLoc, fromID, "Notice summary set!\n \n[Type out the body of the notice]");
                            break;
                        }
                    case 1:
                        {
                            MHE(sourceLoc, fromID, $"Notice description remains set to: {nCS.TemporaryNotice.NoticeDescription}");
                            nCS.State++;
                            MHE(sourceLoc, fromID, "Notice description set!\n \n[Should the notice repeat every 2 weeks? (y/n)]");
                            break;
                        }
                    case 2:
                        {
                            MHE(sourceLoc, fromID, $"Repeat remains set to: {nCS.TemporaryNotice.Repeats.ToString()}");
                            nCS.State++;
                            MHE(sourceLoc, fromID, "Notice will " + (nCS.TemporaryNotice.Repeats ? "" : " -not- ") + " repeat every 2 weeks\n \n[What group should this notice be sent in? (expect:UUID)]");
                            break;
                        }
                    case 3:
                        {
                            MHE(sourceLoc, fromID, $"Group Key remains set to: {nCS.TemporaryNotice.GroupKey.ToString()}");
                            nCS.State++;
                            MHE(sourceLoc, fromID, "Group set! (Will verify that I am in this group after all data is set)\n \n[Want an attachment set? (y/n)]");
                            break;
                        }
                    case 4:
                        {
                            MHE(sourceLoc, fromID, $"Attachment UUID remains set to: {nCS.TemporaryNotice.NoticeAttachment.ToString()}");
                            nCS.State = 6;
                            MHE(sourceLoc, fromID, "Here's the details of the built notice, please verify it is correct\n \nNotice Summary: " + nCS.TemporaryNotice.NoticeSummary + "\nNotice Description: " + nCS.TemporaryNotice.NoticeDescription + "\nNotice has attachment: " + nCS.TemporaryNotice.HasAttachment.ToString() + "\nNotice Attachment ID: " + nCS.TemporaryNotice.NoticeAttachment.ToString() + "\nRepeats: " + nCS.TemporaryNotice.Repeats.ToString() + "\n \n[To confirm this, say 'confirm']");
                            break;
                        }
                }

                ocb.NoticeSessions[agentKey] = nCS;
                ocb.Save();
                return;
            }

            if (nCS.State == 0)
            {
                nCS.State++;
                nCS.TemporaryNotice.NoticeSummary = request;
                ocb.NoticeSessions[agentKey] = nCS;
                ocb.Save();

                MHE(sourceLoc, fromID, "Notice summary set!\n \n[Type out the body of the notice]");
            }
            else if (nCS.State == 1)
            {
                nCS.State++;
                nCS.TemporaryNotice.NoticeDescription = request;
                ocb.NoticeSessions[agentKey] = nCS;
                ocb.Save();

                MHE(sourceLoc, fromID, "Notice description set!\n \n[Should the notice repeat every 2 weeks? (y/n)]");
            }
            else if (nCS.State == 2)
            {
                nCS.State++;
                if (request.ToLower() == "y")
                    nCS.TemporaryNotice.Repeats = true;
                else nCS.TemporaryNotice.Repeats = false;
                ocb.NoticeSessions[agentKey] = nCS;
                ocb.Save();

                MHE(sourceLoc, fromID, "Notice will " + (nCS.TemporaryNotice.Repeats ? "" : " -not- ") + " repeat every 2 weeks\n \n[What group should this notice be sent in? (expect:UUID)]");
            }
            else if (nCS.State == 3)
            {
                nCS.State++;
                nCS.TemporaryNotice.GroupKey = UUID.Parse(request);
                ocb.NoticeSessions[agentKey] = nCS;
                ocb.Save();

                MHE(sourceLoc, fromID, "Group set! (Will verify that I am in this group after all data is set)\n \n[Want an attachment set? (y/n)]");

            }
            else if (nCS.State == 4)
            {
                nCS.State++;
                if (request.ToLower() == "y")
                    nCS.TemporaryNotice.HasAttachment = true;
                else nCS.TemporaryNotice.HasAttachment = false;
                if (nCS.TemporaryNotice.HasAttachment)
                    MHE(sourceLoc, fromID, "Okay!\n \n[Send me the inventory item you want on the notice (must have transfer and copy permissions!!)]");
                else
                {
                    nCS.State = 6;
                    MHE(sourceLoc, fromID, "Here's the details of the built notice, please verify it is correct\n \nNotice Summary: " + nCS.TemporaryNotice.NoticeSummary + "\nNotice Description: " + nCS.TemporaryNotice.NoticeDescription + "\nNotice has attachment: " + nCS.TemporaryNotice.HasAttachment.ToString() + "\nNotice Attachment ID: " + nCS.TemporaryNotice.NoticeAttachment.ToString() + "\nRepeats: " + nCS.TemporaryNotice.Repeats.ToString() + "\n \n[To confirm this, say 'confirm']");
                }

                ocb.NoticeSessions[agentKey] = nCS;
                ocb.Save();
            }
            else if (nCS.State == 6)
            {
                if (request.ToLower() == "confirm")
                {

                    MHE(sourceLoc, fromID, "Okay! Scheduling.. Stand by");
                    // Adding notice to task scheduler
                    if (!ocb.NoticeLists.ContainsKey(nCS.TemporaryNotice.InternalName))
                        ocb.NoticeLists.Add(nCS.TemporaryNotice.InternalName, ocb.NoticeSessions[agentKey].TemporaryNotice);
                    else ocb.NoticeLists[nCS.TemporaryNotice.InternalName] = ocb.NoticeSessions[agentKey].TemporaryNotice;

                    // Notice is now scheduled, remove session
                    ocb.NoticeSessions.Remove(agentKey);
                    MHE(sourceLoc, fromID, "Scheduled notice to be dispatched, please wait a few moments");
                    ocb.Save();
                }
            }
        }

        [CommandGroup("list_notices", 5, 0, "list_notices - List all notices", Destinations.DEST_AGENT | Destinations.DEST_GROUP | Destinations.DEST_LOCAL)]
        public void list_notices(UUID client, int level,  string[] additionalArgs,
                                Destinations source,
                                 UUID agentKey, string agentName)
        {
            OCBotMemory ocb = OCBotMemory.Memory;
            foreach (KeyValuePair<string, OCBotMemory.Notices> entry in ocb.NoticeLists)
            {
                MHE(source, client, "_\n[Notice Session]\n Name: " + entry.Key + "\n Repeats: " + entry.Value.Repeats.ToString() + "\n Group ID: " + entry.Value.GroupKey.ToString() + "\n Notice Summary: " + entry.Value.NoticeSummary + "\n Notice NextDate: " + entry.Value.LastSent.ToString());
            }
        }



        [CommandGroup("rm_notice", 5, 1, "rm_notices [noticeName] - Removes a notice", Destinations.DEST_AGENT | Destinations.DEST_GROUP | Destinations.DEST_LOCAL)]
        public void rm_notice(UUID client, int level,  string[] additionalArgs,
                                Destinations source,
                                 UUID agentKey, string agentName)
        {
            OCBotMemory ocb = OCBotMemory.Memory;
            if (ocb.NoticeLists.ContainsKey(additionalArgs[0]))
                ocb.NoticeLists.Remove(additionalArgs[0]);

            ocb.Save();
        }




        [CommandGroup("clear_mknotice", 5, 0, "clear_mknotice - Clears notice creator sessions", Destinations.DEST_AGENT | Destinations.DEST_GROUP | Destinations.DEST_LOCAL)]
        public void reset_notice_sess(UUID client, int level,  string[] additionalArgs,
                                Destinations source,
                                 UUID agentKey, string agentName)
        {
            OCBotMemory ocb = OCBotMemory.Memory;

            ocb.NoticeSessions = new Dictionary<UUID, OCBotMemory.NoticeCreationSessions>();

            ocb.Save();
        }


        [CommandGroup("check_notices", 5, 0, "check_notices - Checks the notice queue for pending dispatches", Destinations.DEST_AGENT | Destinations.DEST_GROUP | Destinations.DEST_LOCAL)]
        public void check_notice_queue(UUID client, int level,  string[] additionalArgs,
                                Destinations source,
                                 UUID agentKey, string agentName)
        {
            PerformCheck(DateTime.Now - TimeSpan.FromSeconds(30));
        }

        public static void PerformCheck( DateTime LastScheduleCheck)
        {
            if (DateTime.Now > LastScheduleCheck)
            {
                LastScheduleCheck = DateTime.Now + TimeSpan.FromMinutes(5);
                OCBotMemory bm = OCBotMemory.Memory;
                Dictionary<string, OCBotMemory.Notices> NoticeLists = bm.NoticeLists;
                Dictionary<int, OCBotMemory.Notices> NoticeEditQueue = new Dictionary<int, OCBotMemory.Notices>();

                foreach (KeyValuePair<string, OCBotMemory.Notices> entry in NoticeLists)
                {
                    // check notice information
                    OCBotMemory.Notices Notice = bm.NoticeLists[entry.Key];
                    if (entry.Value.Repeats)
                    {
                        if (entry.Value.LastSent == null) Notice.LastSent = DateTime.Now - TimeSpan.FromMinutes(90);
                        // Check datetime
                        if (Notice.LastSent < DateTime.Now)
                        {
                            MH(Destinations.DEST_LOCAL, UUID.Zero, "Dispatching scheduled notice");
                            // Send notice and update information
                            GroupNotice NewNotice = new GroupNotice();
                            if (Notice.HasAttachment)
                                NewNotice.AttachmentID = Notice.NoticeAttachment;
                            else NewNotice.AttachmentID = UUID.Zero;
                            NewNotice.Message = Notice.NoticeDescription;
                            NewNotice.OwnerID = BotSession.Instance.grid.Self.AgentID;
                            NewNotice.Subject = Notice.NoticeSummary;


                            BotSession.Instance.grid.Groups.SendGroupNotice(Notice.GroupKey, NewNotice);


                            Notice.LastSent = DateTime.Now + TimeSpan.FromDays(14);
                            NoticeEditQueue.Add(1, Notice); // This will edit the entry
                            break;
                        }
                    }
                    else
                    {

                        GroupNotice NewNotice = new GroupNotice();
                        if (Notice.HasAttachment)
                            NewNotice.AttachmentID = Notice.NoticeAttachment;
                        else NewNotice.AttachmentID = UUID.Zero;
                        NewNotice.Message = Notice.NoticeDescription;
                        NewNotice.OwnerID = BotSession.Instance.grid.Self.AgentID;
                        NewNotice.Subject = Notice.NoticeSummary;

                        BotSession.Instance.grid.Groups.SendGroupNotice(Notice.GroupKey, NewNotice);

                        NoticeEditQueue.Add(2, Notice); // Will delete the notice
                        MH(Destinations.DEST_LOCAL, UUID.Zero, "Dispatching single-use notice");
                        break;
                    }
                }

                foreach (KeyValuePair<int, OCBotMemory.Notices> entry in NoticeEditQueue)
                {
                    if (entry.Key == 1)
                    {
                        bm.NoticeLists[entry.Value.InternalName] = entry.Value;
                    }
                    else if (entry.Key == 2)
                    {
                        bm.NoticeLists.Remove(entry.Value.InternalName);
                    }
                }

                bm.Save();
            }
        }
    }
}
