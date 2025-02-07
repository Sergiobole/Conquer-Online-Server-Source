using System;
using System.Collections.Generic;
using System.Timers;

namespace COServer.Game.MsgServer
{
    public static class VendorDiscordNotifier
    {
        private static Dictionary<string, List<string>> PlayerItems = new Dictionary<string, List<string>>();
        private static Dictionary<string, Timer> PlayerTimers = new Dictionary<string, Timer>();

        public static void AddItem(string playerName, string itemName, uint amount, byte plus)
        {
            lock (PlayerItems)
            {
                if (!PlayerItems.ContainsKey(playerName))
                {
                    PlayerItems[playerName] = new List<string>();
                }

                string plusText = plus > 0 ? $" +{plus}" : "";
                PlayerItems[playerName].Add($"🛒 {playerName} colocou à venda: {itemName}{plusText} por {amount}");

                // Se não existir um timer para o jogador, cria um
                if (!PlayerTimers.ContainsKey(playerName))
                {
                    Timer timer = new Timer(60000); // 1 minuto
                    timer.Elapsed += (sender, e) => SendToDiscord(playerName);
                    timer.AutoReset = false; // Só executa uma vez
                    timer.Start();
                    PlayerTimers[playerName] = timer;
                }
            }
        }

        private static void SendToDiscord(string playerName)
        {
            lock (PlayerItems)
            {
                if (PlayerItems.ContainsKey(playerName) && PlayerItems[playerName].Count > 0)
                {
                    string message = string.Join("\n", PlayerItems[playerName]);
                    Program.DiscordAPIwinners.Enqueue($"```{message}```");

                    // Limpa a lista e remove o timer
                    PlayerItems.Remove(playerName);
                    PlayerTimers.Remove(playerName);
                }
            }
        }
    }
}