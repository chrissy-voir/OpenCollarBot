using Bot;
using Bot.CommandSystem;
using OpenMetaverse;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenCollarBot.Estate
{
    class EstateCommands : BaseCommands
    {


        [CommandGroup("auto_restart_sim", 5, 2, "auto_restart_sim [day:mon/tue/wed/thur/fri/sat/sun or every] [timeToRestartAt:0H:0M[pm/am] - Restart the sim the bot is in at the time on the specified day and time", Destinations.DEST_LOCAL | Destinations.DEST_AGENT)]
        public void schedule_auto_restart_sim(UUID client, int level,  string[] additionalArgs,  Destinations source,  UUID agentKey, string agentName)
        {
            MHE(source, client, "Scheduling..");
            OCBotMemory.Memory.AutoRestartSim = true;
            OCBotMemory.Memory.RestartDay = additionalArgs[0].ToLower();
            OCBotMemory.Memory.TimeStringForRestart = additionalArgs[1].ToLower();
            MHE(source, client, "Scheduled");
            OCBotMemory.Memory.Save();
        }

        [CommandGroup("cancel_auto_sim_restart", 5, 0, "cancel_auto_sim_restart - Cancels the automatic restarts of the sim the bot is in", Destinations.DEST_LOCAL | Destinations.DEST_AGENT)]
        public void cancel_auto_restart(UUID client, int level,  string[] additionalArgs,  Destinations source,  UUID agentKey, string agentName)
        {

            OCBotMemory.Memory.AutoRestartSim = false;
            OCBotMemory.Memory.RestartDay = "";
            OCBotMemory.Memory.TimeStringForRestart = "";
            OCBotMemory.Memory.Save();
        }

        

        [CommandGroup("restart_sim", 5, 0, "restart_sim - Automatically restarts the sim", Destinations.DEST_LOCAL | Destinations.DEST_AGENT)]
        public void restart_sim(UUID client, int level,  string[] additionalArgs,  Destinations source,  UUID agentKey, string agentName)
        {
            BotSession.Instance.grid.Estate.RestartRegion();
            MHE(source, client, "Restarting sim");
        }

    }
}
