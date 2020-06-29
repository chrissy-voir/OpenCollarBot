using System;
using System.Collections.Generic;
using System.Text;
using Bot.CommandSystem;
using Bot;
using OpenMetaverse;
using System.Linq;
using Octokit.Internal;
using System.Threading;

namespace OpenCollarBot.GroupCommands
{
    class Members : BaseCommands
    {
        UUID REQUEST_ID;
        int YEARS;
        ManualResetEvent MRE = new ManualResetEvent(false);
        [CommandGroup("simulate_eject", 5, 2, "simulate_eject [years:int] [groupID]", Destinations.DEST_AGENT | Destinations.DEST_DISCORD | Destinations.DEST_LOCAL)]
        public void simulate_eject(UUID client, int level, string[] additionalArgs,
                                Destinations source,
                                UUID agentKey, string agentName)
        {
            MHE(source, client, "Stand By...");
            YEARS = Convert.ToInt32(additionalArgs[0]);
            BotSession.Instance.grid.Groups.GroupProfile += Groups_GroupProfile;
            BotSession.Instance.grid.Groups.RequestGroupProfile(UUID.Parse(additionalArgs[1]));


        }

        private void Groups_GroupProfile(object sender, GroupProfileEventArgs e)
        {
            BotSession.Instance.grid.Groups.GroupProfile -= Groups_GroupProfile;
            MHE(Destinations.DEST_LOCAL, UUID.Zero, "Total Members in group: " + e.Group.GroupMembershipCount.ToString());
            MHE(Destinations.DEST_LOCAL, UUID.Zero, "Requesting member list...");
            OCBSession.Instance.MemberLookupRE.Reset();
            OCBSession.Instance.GroupMembers.Clear();
            OCBSession.Instance.MemberLookupRequest = BotSession.Instance.grid.Groups.RequestGroupMembers(e.Group.ID);

            while (true)
            {
                if (MRE.WaitOne(TimeSpan.FromMinutes(1)))
                {
                    foreach(KeyValuePair<UUID, GroupMember> kvp in OCBSession.Instance.GroupMembers)
                    {

                        // continue
                        MHE(Destinations.DEST_LOCAL, UUID.Zero, $"secondlife:///app/agent/{kvp.Value.ID.ToString()}/about - OnlineStatus: {kvp.Value.OnlineStatus}");

                    }
                }
                else
                {
                    if(OCBSession.Instance.GroupMembers.Count == e.Group.GroupMembershipCount)
                    {

                        foreach (KeyValuePair<UUID, GroupMember> kvp in OCBSession.Instance.GroupMembers)
                        {

                            // continue
                            MHE(Destinations.DEST_LOCAL, UUID.Zero, $"secondlife:///app/agent/{kvp.Value.ID.ToString()}/about - OnlineStatus: {kvp.Value.OnlineStatus}");

                        }
                        OCBSession.Instance.GroupMembers.Clear();
                        OCBSession.Instance.MemberLookupRequest = UUID.Zero;
                        MHE(Destinations.DEST_LOCAL, UUID.Zero, "Request finished");
                        break;
                    }
                    else
                    {
                        foreach (KeyValuePair<UUID, GroupMember> kvp in OCBSession.Instance.GroupMembers)
                        {

                            // continue
                            MHE(Destinations.DEST_LOCAL, UUID.Zero, $"secondlife:///app/agent/{kvp.Value.ID.ToString()}/about - OnlineStatus: {kvp.Value.OnlineStatus}");

                        }
                        MHE(Destinations.DEST_LOCAL, UUID.Zero, $"Still processing... Group Member info retrieved from SecondLife: {OCBSession.Instance.GroupMembers.Count} / {e.Group.GroupMembershipCount}");

                        OCBSession.Instance.MemberLookupRequest = BotSession.Instance.grid.Groups.RequestGroupMembers(e.Group.ID);
                    }
                }
            }
        }

    }
}
