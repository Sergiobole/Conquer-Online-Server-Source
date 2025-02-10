using System;
using System.Collections.Generic;
using System.Timers;
using COServer.Game.Data;  // Importando o repositório que vai salvar no banco
using COServer.Game.Models;  // Importando o modelo do MarketItem

namespace COServer.Game.MsgServer
{
    public static class VendorDiscordNotifier
    {
        private static Dictionary<string, List<string>> PlayerItems = new Dictionary<string, List<string>>();
        private static Dictionary<string, Timer> PlayerTimers = new Dictionary<string, Timer>();

        // Método que é chamado para adicionar um item ao mercado
        public static void AddItem(string playerName, string itemName, uint amount, byte plus)
        {
            lock (PlayerItems)
            {
                // Se o jogador ainda não tiver itens, cria uma lista
                if (!PlayerItems.ContainsKey(playerName))
                {
                    PlayerItems[playerName] = new List<string>();
                }

                // Formatação do texto para o Discord (incluindo o "plus" se necessário)
                string plusText = plus > 0 ? $" +{plus}" : "";
                PlayerItems[playerName].Add($"🛒 {playerName} colocou à venda: {itemName}{plusText} por CPS💎{amount}");

                // Salva o item no banco de dados
                MarketRepository.InsertMarketItem(new MarketItem
                {
                    PlayerName = playerName,
                    ItemName = itemName,
                    Price = amount,
                    Timestamp = DateTime.Now  // Data e hora de quando o item foi adicionado
                });

                // Se não existir um timer para o jogador, cria um novo
                if (!PlayerTimers.ContainsKey(playerName))
                {
                    Timer timer = new Timer(60000); // Timer de 1 minuto
                    timer.Elapsed += (sender, e) => SendToDiscord(playerName);
                    timer.AutoReset = false;  // O timer vai executar apenas uma vez
                    timer.Start();
                    PlayerTimers[playerName] = timer;
                }
            }
        }

        // Método que envia os itens para o Discord após 1 minuto
        private static void SendToDiscord(string playerName)
        {
            lock (PlayerItems)
            {
                if (PlayerItems.ContainsKey(playerName) && PlayerItems[playerName].Count > 0)
                {
                    // Cria a mensagem que será enviada para o Discord
                    string message = string.Join("\n", PlayerItems[playerName]);
                    Program.DiscordAPIwinners.Enqueue($"```{message}```");

                    // Limpa a lista de itens e remove o timer para esse jogador
                    PlayerItems.Remove(playerName);
                    PlayerTimers.Remove(playerName);
                }
            }
        }
    }
}
