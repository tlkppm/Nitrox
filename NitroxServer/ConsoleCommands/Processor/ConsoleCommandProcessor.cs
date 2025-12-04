using System;
using System.Collections.Generic;
using System.Linq;
using NitroxModel.DataStructures.GameLogic;
using NitroxModel.DataStructures.Util;
using NitroxModel.Logger;
using NitroxModel.Serialization;
using NitroxServer.ConsoleCommands.Abstract;
using NitroxServer.Exceptions;

namespace NitroxServer.ConsoleCommands.Processor
{
    public class ConsoleCommandProcessor
    {
        private readonly Dictionary<string, Command> commands = new();
        private readonly char[] splitChar = { ' ' };

        public ConsoleCommandProcessor(IEnumerable<Command> cmds)
        {
            foreach (Command cmd in cmds)
            {
                if (commands.ContainsKey(cmd.Name))
                {
                    throw new DuplicateRegistrationException($"Command {cmd.Name} is registered multiple times.");
                }

                commands[cmd.Name] = cmd;

                foreach (string alias in cmd.Aliases)
                {
                    if (commands.ContainsKey(alias))
                    {
                        throw new DuplicateRegistrationException($"Command {alias} is registered multiple times.");
                    }

                    commands[alias] = cmd;
                }
            }
        }

        public void ProcessCommand(string msg, Optional<Player> sender, Perms permissions)
        {
            if (string.IsNullOrWhiteSpace(msg))
            {
                return;
            }
            
            // 命令拦截逻辑
            if (sender.HasValue && ShouldInterceptCommand(msg))
            {
                LogInterceptedCommand(sender.Value, msg);
            }
            
            Span<string> parts = msg.Split(splitChar, StringSplitOptions.RemoveEmptyEntries);
            if (!commands.TryGetValue(parts[0], out Command command))
            {
                Command.SendMessage(sender, $"Command not found: {parts[0]}");
                return;
            }
            if (!sender.HasValue && command.Flags.HasFlag(PermsFlag.NO_CONSOLE))
            {
                Log.Error("This command cannot be used by CONSOLE");
                return;
            }

            if (command.CanExecute(permissions))
            {
                command.TryExecute(sender, parts[1..]);
            }
            else
            {
                Command.SendMessage(sender, "You do not have the required permissions for this command !");
            }
        }
        
        private bool ShouldInterceptCommand(string commandText)
        {
            try
            {
                SubnauticaServerConfig config = SubnauticaServerConfig.Load(".");
                if (!config.CommandInterceptionEnabled)
                {
                    return false;
                }
                
                if (string.IsNullOrWhiteSpace(config.InterceptedCommands))
                {
                    return true; // 拦截所有命令
                }
                
                string[] commandsToIntercept = config.InterceptedCommands.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(cmd => cmd.Trim().ToLower()).ToArray();
                
                string commandName = commandText.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0].ToLower();
                return commandsToIntercept.Contains(commandName);
            }
            catch (Exception ex)
            {
                Log.Error($"Error checking command interception: {ex.Message}");
                return false;
            }
        }
        
        private void LogInterceptedCommand(Player player, string commandText)
        {
            string logMessage = $"[COMMAND INTERCEPTED] Player: {player.Name} (ID: {player.Id}) executed command: {commandText} at {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
            Log.Info(logMessage);
            
            // 可以在这里添加更多的日志记录逻辑，比如写入到特定的文件或发送到外部系统
            try
            {
                string logFile = "intercepted_commands.log";
                System.IO.File.AppendAllText(logFile, logMessage + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to write intercepted command to log file: {ex.Message}");
            }
        }
    }
}
