using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using COServer.Game.Data;
using COServer.Game.Models;
using System.Timers;

namespace COServer.Game.MsgServer
{
    public static class VoteSystem
    {
        private static Dictionary<string, List<string>> PlayerVotes = new Dictionary<string, List<string>>();
        private static Dictionary<string, Timer> PlayerTimers = new Dictionary<string, Timer>();

        // Método para adicionar voto
        public static void AddVote(string playerName, string ip)
        {
            Console.WriteLine($"Tentando adicionar voto: {playerName} com IP: {ip}"); // Debug

            lock (PlayerVotes)
            {
                // Verifica se o jogador já votou nas últimas 12 horas
                var lastVote = VoteRepository.GetLastVote(playerName);

                if (lastVote != null && lastVote.Timestamp.AddHours(12) > DateTime.Now)
                {
                    Console.WriteLine($"[ERRO] {playerName} já votou nas últimas 12 horas.");
                    return;
                }

                // Adiciona a mensagem de voto para o Discord
                if (!PlayerVotes.ContainsKey(playerName))
                {
                    PlayerVotes[playerName] = new List<string>();
                }
                PlayerVotes[playerName].Add($"🗳️ {playerName} votou com o IP: {ip}");

                // Se já existe um registro, só atualiza a contagem
                if (lastVote != null)
                {
                    VoteRepository.UpdateVote(playerName);
                }
                else
                {
                    // Se for a primeira vez votando, insere no banco
                    VoteRepository.InsertVoteDb(new VotesModels
                    {
                        Id = playerName.GetHashCode(),
                        Ip = ip,
                        Timestamp = DateTime.Now,
                        VotePoints = 1
                    });
                }

                // Inicia o timer para enviar para o Discord após 1 minuto
                if (!PlayerTimers.ContainsKey(playerName))
                {
                    Timer timer = new Timer(60000); // Timer de 1 minuto
                    timer.Elapsed += (sender, e) => SendToDiscord(playerName);
                    timer.AutoReset = false;
                    timer.Start();
                    PlayerTimers[playerName] = timer;
                }
            }
        }


        // Envia a notificação para o Discord
        private static void SendToDiscord(string playerName)
        {
            lock (PlayerVotes)
            {
                if (PlayerVotes.ContainsKey(playerName) && PlayerVotes[playerName].Count > 0)
                {
                    string message = string.Join("\n", PlayerVotes[playerName]);
                    Console.WriteLine($"Enviando para Discord: {message}"); // Log para verificar a mensagem
                    Program.DiscordAPIwinners.Enqueue($"```{message}```");

                    // Limpa a lista de votos e remove o timer para esse jogador
                    PlayerVotes.Remove(playerName);
                    PlayerTimers.Remove(playerName);
                }
            }
        }
    }
}
    


