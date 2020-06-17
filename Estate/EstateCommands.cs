using Bot;
using Bot.CommandSystem;
using OpenMetaverse;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenCollarBot.Estate
{
    class EstateCommands
    {


        [CommandGroup("auto_restart_sim", 5, 2, "auto_restart_sim [day:mon/tue/wed/thur/fri/sat/sun or every] [timeToRestartAt:0H:0M[pm/am] - Restart the sim the bot is in at the time on the specified day and time", MessageHandler.Destinations.DEST_LOCAL | MessageHandler.Destinations.DEST_AGENT)]
        public void schedule_auto_restart_sim(UUID client, int level, GridClient grid, string[] additionalArgs, MessageHandler.MessageHandleEvent MHE, MessageHandler.Destinations source, CommandRegistry registry, UUID agentKey, string agentName)
        {
            MHE(source, client, "Scheduling..");
            OCBotMemory.Memory.AutoRestartSim = true;
            OCBotMemory.Memory.RestartDay = additionalArgs[0].ToLower();
            OCBotMemory.Memory.TimeStringForRestart = additionalArgs[1].ToLower();
            MHE(source, client, "Scheduled");
            OCBotMemory.Memory.Save();
        }

        [CommandGroup("cancel_auto_sim_restart", 5, 0, "cancel_auto_sim_restart - Cancels the automatic restarts of the sim the bot is in", MessageHandler.Destinations.DEST_LOCAL | MessageHandler.Destinations.DEST_AGENT)]
        public void cancel_auto_restart(UUID client, int level, GridClient grid, string[] additionalArgs, MessageHandler.MessageHandleEvent MHE, MessageHandler.Destinations source, CommandRegistry registry, UUID agentKey, string agentName)
        {
            
            OCBotMemory.Memory.AutoRestartSim = false;
            OCBotMemory.Memory.RestartDay = "";
            OCBotMemory.Memory.TimeStringForRestart = "";
            OCBotMemory.Memory.Save();
        }

    }
}
