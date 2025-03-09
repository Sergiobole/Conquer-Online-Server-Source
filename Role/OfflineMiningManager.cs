using COServer.Game.MsgServer;
using System;
using System.Threading;

namespace COServer.Role
{
    public static class OfflineMiningManager
    {
        private static readonly TimeSpan MiningDuration = TimeSpan.FromHours(24);
        private static readonly TimeSpan MiningInterval = TimeSpan.FromSeconds(5);

        public static void StartOfflineMining(Client.GameClient client)
        {
            if (client == null || !client.Player.Mining) return;

            client.Player.OfflineMiner = true;
            Thread miningThread = new Thread(() => ProcessOfflineMining(client))
            {
                IsBackground = true
            };
            miningThread.Start();
        }

        private static void ProcessOfflineMining(Client.GameClient client)
        {
            DateTime endTime = DateTime.Now.Add(MiningDuration);
            int visibilityCheckCounter = 0;

            while (client.Player.OfflineMiner && DateTime.Now < endTime)
            {
                try
                {
                    using (var rec = new ServerSockets.RecycledPacket())
                    {
                        var stream = rec.GetStream();
                        Mining.Mine(stream, client);
                        SaveOfflineMinedItems(client);
                        visibilityCheckCounter++;
                        if (visibilityCheckCounter >= 2) // 2 iterações = 10 segundos
                        {
                            client.Player.View.SendView(client.Player.GetArray(stream, false), true);
                            visibilityCheckCounter = 0;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{DateTime.Now}] Erro na mineração offline para {client.Player.Name}: {ex.Message}");
                }

                Thread.Sleep(MiningInterval);
            }

            client.Player.OfflineMiner = false;
            client.Map?.Denquer(client);
        }

        private static void SaveOfflineMinedItems(Client.GameClient client)
        {
            Console.WriteLine($"[{DateTime.Now}] Item minerado offline por {client.Player.Name} salvo.");
        }

        public static void StopOfflineMining(Client.GameClient client)
        {
            if (client != null && client.Player.OfflineMiner)
            {
                client.Player.OfflineMiner = false;
                client.Map?.Denquer(client);
            }
        }

        public static bool IsMiningOffline(uint playerId)
        {
            return Database.Server.GamePoll.TryGetValue(playerId, out var client) && client.Player.OfflineMiner;
        }
    }
}