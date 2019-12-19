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
using Bot.CommandSystem;
using OpenMetaverse;
using Newtonsoft.Json;
using System.IO;
using System.Net.Http;
using System.Net;

namespace OpenCollarBot.MailingLists
{
    public class MailingLists
    {
        [CommandGroup("mkmaillist", 4, 2, "mkmaillist [list_name] [allow_optOut:(y/n)]", MessageHandler.Destinations.DEST_AGENT | MessageHandler.Destinations.DEST_LOCAL)]
        public void MakeMailingList(UUID client, int level, GridClient grid, string[] additionalArgs,
                                SysOut log, MessageHandler.MessageHandleEvent MHE, MessageHandler.Destinations source,
                                CommandRegistry registry, UUID agentKey, string agentName)
        {
            // Create the new mailing list
            OCBotMemory ocb = OCBotMemory.Memory;

            OCBotMemory.MailList ML = new OCBotMemory.MailList();
            ML.Members = new List<OCBotMemory.MailListMember>();
            ML.MailListOwner = agentKey;
            ML.ListName = additionalArgs[0];
            ML.PrepFrom = UUID.Zero;
            ML.PrepMsg = "";
            ML.PrepState = 0;

            if (additionalArgs[1].ToLower() == "y")
            {
                ML.AllowOptOut = true;
            }
            else
                ML.AllowOptOut = false;

            MHE(source, client, "Mailing list '" + additionalArgs[0] + "' has been created successfully.\n \n[Note: Regardless of whether you enabled opt out. The first time a message is sent to this person, info will be included on how to opt out of all bot related mailing lists. These changes are reflected in a separate list.]");
            ocb.MailingLists.Add(additionalArgs[0], ML);
            ocb.Save();

        }

        [CommandGroup("maillist_add", 4, 2, "maillist_add [list_name] [UUID]", MessageHandler.Destinations.DEST_AGENT | MessageHandler.Destinations.DEST_LOCAL)]
        public void AddListMember(UUID client, int level, GridClient grid, string[] additionalArgs,
                                SysOut log, MessageHandler.MessageHandleEvent MHE, MessageHandler.Destinations source,
                                CommandRegistry registry, UUID agentKey, string agentName)
        {
            // Create the new mailing list
            OCBotMemory ocb = OCBotMemory.Memory;

            UUID add_member = UUID.Parse(additionalArgs[1]);
            // Lets check data
            OCBotMemory.MailList ML = ocb.MailingLists[additionalArgs[0]];
            List<OCBotMemory.MailListMember> Members = ML.Members;
            if (Members == null) Members = new List<OCBotMemory.MailListMember>();
            foreach (OCBotMemory.MailListMember Member in Members)
            {

                if (Member.MemberID == add_member)
                {
                    MHE(source, client, "This member is already added to the list");
                    if (ocb.BlacklistMailingList.Contains(add_member))
                    {
                        MHE(source, client, "If this person is having trouble getting mailing list notifications, have them opt back into mailing lists. They are currently opted out of all mailing list functionality.");
                    }

                    return;
                }
            }

            OCBotMemory.MailListMember NewMember = new OCBotMemory.MailListMember();
            NewMember.OptOut = false;
            NewMember.Informed = false;
            NewMember.MemberID = add_member;

            Members.Add(NewMember);
            ML.Members = Members;

            ocb.MailingLists[additionalArgs[0]] = ML;

            MHE(source, client, "Added.");


            ocb.Save();

        }


        [CommandGroup("maillist_rem", 4, 2, "maillist_rem [list_name] [UUID]", MessageHandler.Destinations.DEST_AGENT | MessageHandler.Destinations.DEST_LOCAL)]
        public void RemListMember(UUID client, int level, GridClient grid, string[] additionalArgs,
                                SysOut log, MessageHandler.MessageHandleEvent MHE, MessageHandler.Destinations source,
                                CommandRegistry registry, UUID agentKey, string agentName)
        {
            // Create the new mailing list
            OCBotMemory ocb = OCBotMemory.Memory;

            UUID TheMember = UUID.Parse(additionalArgs[1]);
            OCBotMemory.MailList ML = ocb.MailingLists[additionalArgs[0]];
            List<OCBotMemory.MailListMember> Members = ML.Members;

            foreach (OCBotMemory.MailListMember Member in Members)
            {
                if (Member.MemberID == TheMember)
                {
                    Members.Remove(Member);
                    ML.Members = Members;
                    ocb.MailingLists[additionalArgs[0]] = ML;
                    ocb.Save();

                    MHE(source, client, "The member has been removed from the mailing list '" + additionalArgs[0] + "'");
                    return;

                }
            }
            MHE(source, client, "No changes made. Could not find that member in the list");

        }




        [CommandGroup("maillist_replace", 4, 2, "maillist_replace [list_name] [URL] - Replaces the members list with the CSV at [URL]", MessageHandler.Destinations.DEST_AGENT | MessageHandler.Destinations.DEST_LOCAL)]
        public void ReplaceMailListMembers(UUID client, int level, GridClient grid, string[] additionalArgs,
                                SysOut log, MessageHandler.MessageHandleEvent MHE, MessageHandler.Destinations source,
                                CommandRegistry registry, UUID agentKey, string agentName)
        {
            // Create the new mailing list
            OCBotMemory ocb = OCBotMemory.Memory;
            MHE(source, client, "Stand by..");
            OCBotMemory.MailList ML = ocb.MailingLists[additionalArgs[0]];
            if (ML.MailListOwner != agentKey)
            {
                MHE(source, client, "PERMISSION ERROR: You are not the MailList Owner. Please only edit mailing lists you own or have created yourself");
                return;
            }
            List<OCBotMemory.MailListMember> Members = new List<OCBotMemory.MailListMember>();

            string sURL = additionalArgs[1];
            WebRequest WR = WebRequest.Create(sURL);
            WebResponse resp = WR.GetResponse();

            Stream S = resp.GetResponseStream();
            StreamReader SR = new StreamReader(S);
            string CSV = SR.ReadToEnd();
            string[] CSVList = new string[] { ",", ", " };
            string[] listData = CSV.Split(CSVList, StringSplitOptions.RemoveEmptyEntries);
            foreach (string D in listData)
            {
                OCBotMemory.MailListMember NewMember = new OCBotMemory.MailListMember();
                NewMember.Informed = false;
                NewMember.OptOut = false;
                NewMember.MemberID = UUID.Parse(D);
                Members.Add(NewMember);
            }

            ML.Members = Members;
            ocb.MailingLists[additionalArgs[0]] = ML;

            ocb.Save();

            MHE(source, client, "Mailing list member list completely replaced by CSV contents");


        }

        string tf(bool V)
        {
            if (V) return "true";
            else return "false";
        }

        [CommandGroup("maillist", 4, 1, "maillist [list_name] - Lists all data about a mailing list (DANGER: Can be long.)", MessageHandler.Destinations.DEST_AGENT | MessageHandler.Destinations.DEST_LOCAL)]
        public void GetMailListMembers(UUID client, int level, GridClient grid, string[] additionalArgs,
                                SysOut log, MessageHandler.MessageHandleEvent MHE, MessageHandler.Destinations source,
                                CommandRegistry registry, UUID agentKey, string agentName)
        {
            OCBotMemory ocb = OCBotMemory.Memory;

            OCBotMemory.MailList ML = ocb.MailingLists[additionalArgs[0]];
            MHE(source, client, "Mailing List Name: " + ML.ListName);
            MHE(source, client, "Mailing List Owner: secondlife:///app/agent/" + ML.MailListOwner.ToString() + "/about [" + ML.MailListOwner.ToString() + "]");
            MHE(source, client, ">Allow Opt Out: " + tf(ML.AllowOptOut));
            MHE(source, client, ">>Members");

            foreach (OCBotMemory.MailListMember Member in ML.Members)
            {
                MHE(source, client, "[Member] secondlife:///app/agent/" + Member.MemberID.ToString() + "/about [" + Member.MemberID.ToString() + "] OptedOut:" + tf(Member.OptOut) + ", FirstMsgSent:" + tf(Member.Informed));
            }

            MHE(source, client, ">Completed Mailing List Dump<");
        }


        [CommandGroup("lsmaillist", 4, 0, "lsmaillist - Lists all mailinglist names", MessageHandler.Destinations.DEST_AGENT | MessageHandler.Destinations.DEST_LOCAL)]
        public void GetMailLists(UUID client, int level, GridClient grid, string[] additionalArgs,
                                SysOut log, MessageHandler.MessageHandleEvent MHE, MessageHandler.Destinations source,
                                CommandRegistry registry, UUID agentKey, string agentName)
        {
            OCBotMemory ocb = OCBotMemory.Memory;

            foreach (OCBotMemory.MailList ML in ocb.MailingLists.Values)
            {
                MHE(source, client, "List: " + ML.ListName);
            }

            MHE(source, client, ">Completed Mailing List Dump<");
        }


        [CommandGroup("rmmaillist", 4, 1, "rmmaillist [list_name] - Deletes the mailing list if authorized", MessageHandler.Destinations.DEST_AGENT | MessageHandler.Destinations.DEST_LOCAL)]
        public void RMMailLists(UUID client, int level, GridClient grid, string[] additionalArgs,
                                SysOut log, MessageHandler.MessageHandleEvent MHE, MessageHandler.Destinations source,
                                CommandRegistry registry, UUID agentKey, string agentName)
        {
            OCBotMemory ocb = OCBotMemory.Memory;


            OCBotMemory.MailList ML = ocb.MailingLists[additionalArgs[0]];

            if (ML.MailListOwner == agentKey)
            {
                ocb.MailingLists.Remove(additionalArgs[0]);
                ocb.Save();
                MHE(source, client, "Deleted mail list");
            }
        }


        [CommandGroup("maillist_prepare", 4, 1, "maillist_prepare [list_name] - Begins preparing for a message to be sent", MessageHandler.Destinations.DEST_AGENT | MessageHandler.Destinations.DEST_LOCAL)]
        public void StartNewDispatchedMessage(UUID client, int level, GridClient grid, string[] additionalArgs,
                        SysOut log, MessageHandler.MessageHandleEvent MHE, MessageHandler.Destinations source,
                        CommandRegistry registry, UUID agentKey, string agentName)
        {
            OCBotMemory ocb = OCBotMemory.Memory;

            OCBotMemory.MailList ML = ocb.MailingLists[additionalArgs[0]];

            ML.PrepState = 1;
            ML.PrepFrom = agentKey;
            ML.PrepMsg = "";

            ocb.MailingLists[additionalArgs[0]] = ML;
            ocb.Save();
            MHE(source, client, "Okay you can type out your message now. \n \n[To indicate completion type '@', and for a new line type '#', and to cancel: 'cancel']");
        }

        public void HandleMailListData(UUID agent, UUID from, MessageHandler.Destinations source, MessageHandler.MessageHandleEvent MHE, string MailingListName, string reply)
        {
            OCBotMemory ocb = OCBotMemory.Memory;
            OCBotMemory.MailList ML = ocb.MailingLists[MailingListName];

            if (reply == "cancel")
            {
                ML.PrepFrom = UUID.Zero;
                ML.PrepMsg = "";
                ML.PrepState = 0;
                MHE(source, from, "OK. Deleted the prepared data. Resuming normal operations");
                ocb.MailingLists[MailingListName] = ML;
                ocb.Save();
                return;
            }

            if (reply == "#")
            {
                ML.PrepMsg += "\n";
                ocb.MailingLists[MailingListName] = ML;
                ocb.Save();
                return;

            }

            if (reply == "@")
            {

                MHE(source, from, "Okay! Dispatching a example message to you. If it looks right then use the -maillist_send- command. View the help data for more info. If it does not look right.. Just start the process over, it'll erase the existing data");
                ML.PrepMsg = "_\n \n[Mailing List Notification]\n[List: " + ML.ListName + "]\n*You are receiving this message because you are a member of the mailing list\n \n" + ML.PrepMsg;
                ML.PrepState = 2;
                MHE(MessageHandler.Destinations.DEST_AGENT, agent, ML.PrepMsg);
                ocb.MailingLists[MailingListName] = ML;
                ocb.Save();
                return;
            }

            if (ML.PrepState == 1)
            {

                ML.PrepMsg += reply;
                ocb.MailingLists[MailingListName] = ML;
                ocb.Save();
            }
        }


        [CommandGroup("maillist_send", 4, 1, "maillist_send [list_name] - Sends Prepared data", MessageHandler.Destinations.DEST_AGENT | MessageHandler.Destinations.DEST_LOCAL)]
        public void Dispatch(UUID client, int level, GridClient grid, string[] additionalArgs,
                        SysOut log, MessageHandler.MessageHandleEvent MHE, MessageHandler.Destinations source,
                        CommandRegistry registry, UUID agentKey, string agentName)
        {
            OCBotMemory ocb = OCBotMemory.Memory;

            OCBotMemory.MailList ML = ocb.MailingLists[additionalArgs[0]];
            List<OCBotMemory.MailListMember> Members = ML.Members;

            Console.WriteLine("Enter foreach with " + Members.Count.ToString() + " members");

            for (int i = 0; i < Members.Count; i++)
            {
                OCBotMemory.MailListMember Member = Members[i];
                Console.WriteLine("ENTER Blacklist check for " + Member.MemberID.ToString());
                if (ocb.BlacklistMailingList.Contains(Member.MemberID))
                {
                    // Totally ignore sending a message to this user.
                }
                else
                {
                    string PreparedStatement = ML.PrepMsg;
                    if (Member.Informed)
                    {

                        bool CanSend = true;
                        if (ML.AllowOptOut)
                        {
                            if (Member.OptOut) CanSend = false;

                        }
                        if (CanSend)
                            MHE(MessageHandler.Destinations.DEST_AGENT, Member.MemberID, ML.PrepMsg);
                    }
                    else
                    {
                        bool CanSend = true;
                        if (ML.AllowOptOut)
                        {
                            if (Member.OptOut) CanSend = false;
                        }
                        if (CanSend)
                        {

                            MHE(MessageHandler.Destinations.DEST_AGENT, Member.MemberID, ML.PrepMsg + "\n \n[I see this is the first time you are receiving a message on this mailing list]\n[If you want to opt out from this mailing list, you can send the command: maillist_opt [list_name]]\n[If you want to opt out from all mailing lists you can send: maillist_off/maillist_on]");

                            Console.WriteLine("Begin replace");
                            Member.setInformed();
                            ML.Members[i] = Member;
                            Console.WriteLine("Replaced maillist entry");
                        }
                    }
                }

                Console.WriteLine("EXIT Blacklist Check for " + Member.MemberID.ToString());
            }
            Console.WriteLine("Foreach complete");


            MHE(MessageHandler.Destinations.DEST_AGENT, ML.MailListOwner, ML.PrepMsg);

            ML.PrepFrom = UUID.Zero;
            ML.PrepMsg = "";
            ML.PrepState = 0;

            ocb.MailingLists[additionalArgs[0]] = ML;
            ocb.Save();
        }




        [CommandGroup("maillist_opt", 0, 1, "maillist_opt [list_name] - Toggles the OptOut Status if list allows for it", MessageHandler.Destinations.DEST_AGENT | MessageHandler.Destinations.DEST_LOCAL)]
        public void ToggleMaillist(UUID client, int level, GridClient grid, string[] additionalArgs,
                        SysOut log, MessageHandler.MessageHandleEvent MHE, MessageHandler.Destinations source,
                        CommandRegistry registry, UUID agentKey, string agentName)
        {
            OCBotMemory ocb = OCBotMemory.Memory;

            OCBotMemory.MailList ML = ocb.MailingLists[additionalArgs[0]];

            List<OCBotMemory.MailListMember> Members = ML.Members;

            foreach (OCBotMemory.MailListMember Member in Members)
            {
                if (Member.MemberID == agentKey)
                {
                    // Flip status
                    OCBotMemory.MailListMember M = Member;
                    M.FlipOpt();
                    ML.Members.Remove(Member);
                    ML.Members.Add(M);
                    ocb.MailingLists[additionalArgs[0]] = ML;
                    ocb.Save();
                    MHE(source, client, "Successfully flipped your opt-in status to " + tf(M.OptOut));
                    return;
                }
            }

            MHE(source, client, "Could not locate entry in that mailing list");


        }

        [CommandGroup("maillist_allowopt", 4, 2, "maillist_allowopt [list_name] [allowOpt:y/n] - Toggles the OptOut Status for the entire list", MessageHandler.Destinations.DEST_AGENT | MessageHandler.Destinations.DEST_LOCAL)]
        public void ToggleMaillistOpt(UUID client, int level, GridClient grid, string[] additionalArgs,
                        SysOut log, MessageHandler.MessageHandleEvent MHE, MessageHandler.Destinations source,
                        CommandRegistry registry, UUID agentKey, string agentName)
        {
            OCBotMemory ocb = OCBotMemory.Memory;

            OCBotMemory.MailList ML = ocb.MailingLists[additionalArgs[0]];

            if (agentKey != ML.MailListOwner)
            {
                MHE(source, client, "You must be the maillist owner to change that");
                return;
            }
            if (additionalArgs[1].ToLower() == "y")
            {
                ML.AllowOptOut = true;
            }
            else
            {
                ML.AllowOptOut = false;
            }
            MHE(source, client, "Updated");
            ocb.MailingLists[additionalArgs[0]] = ML;

            ocb.Save();


        }



        [CommandGroup("maillist_off", 0, 0, "maillist_off - Turns all mailing lists off for you", MessageHandler.Destinations.DEST_AGENT | MessageHandler.Destinations.DEST_LOCAL)]
        public void ToggleMaillistOff(UUID client, int level, GridClient grid, string[] additionalArgs,
                        SysOut log, MessageHandler.MessageHandleEvent MHE, MessageHandler.Destinations source,
                        CommandRegistry registry, UUID agentKey, string agentName)
        {
            OCBotMemory ocb = OCBotMemory.Memory;

            if (!ocb.BlacklistMailingList.Contains(agentKey)) ocb.BlacklistMailingList.Add(agentKey);

            ocb.Save();
            MHE(source, client, "OK");

        }

        [CommandGroup("maillist_on", 0, 0, "maillist_on - Turns all mailing lists on for you", MessageHandler.Destinations.DEST_AGENT | MessageHandler.Destinations.DEST_LOCAL)]
        public void ToggleMaillistOn(UUID client, int level, GridClient grid, string[] additionalArgs,
                        SysOut log, MessageHandler.MessageHandleEvent MHE, MessageHandler.Destinations source,
                        CommandRegistry registry, UUID agentKey, string agentName)
        {
            OCBotMemory ocb = OCBotMemory.Memory;

            if (ocb.BlacklistMailingList.Contains(agentKey)) ocb.BlacklistMailingList.Remove(agentKey);

            ocb.Save();


            MHE(source, client, "OK");
        }
    }
}
