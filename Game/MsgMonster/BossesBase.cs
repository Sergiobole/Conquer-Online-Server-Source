using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using COServer.Game.MsgTournaments;

namespace COServer.Game.MsgMonster
{
    public class BossesBase
    {
        private static DateTime lastSpawnTime = DateTime.MinValue;

        public static void BossesTimer()
        {
            DateTime now = DateTime.Now;

            // Verifica se o spawn deve ocorrer a cada 1 hora
            if ((now - lastSpawnTime).TotalHours >= 1)
            {
                Random R = new Random();
                int Nr = R.Next(1, 6); // Sorteio de local, agora com 6 opções

                // Sorteia os diferentes locais para o TeratoDragon
                if (Nr == 1)
                {
                    SpawnHandler(1002, 564, 792, 20060, "TeratoDragon",
                        "will appear at " + now.Hour + ":00! Get ready to fight! You only have 5 minutes!",
                        " has spawned in " + Database.Server.MapName[1002] + " (564,792)!",
                        MsgServer.MsgStaticMessage.Messages.TeratoDragon);
                }
                else if (Nr == 2)
                {
                    SpawnHandler(1000, 500, 704, 20060, "TeratoDragon",
                        "will appear at " + now.Hour + ":00! Get ready to fight! You only have 5 minutes!",
                        " has spawned in " + Database.Server.MapName[1000] + " (500,704)!",
                        MsgServer.MsgStaticMessage.Messages.TeratoDragon);
                }
                else if (Nr == 3)
                {
                    SpawnHandler(1020, 568, 584, 20060, "TeratoDragon",
                        "will appear at " + now.Hour + ":00! Get ready to fight! You only have 5 minutes!",
                        " has spawned in " + Database.Server.MapName[1012] + " (568,564)!",
                        MsgServer.MsgStaticMessage.Messages.TeratoDragon);
                }
                else if (Nr == 4)
                {
                    SpawnHandler(1015, 799, 575, 20060, "TeratoDragon",
                        "will appear at " + now.Hour + ":00! Get ready to fight! You only have 5 minutes!",
                        " has spawned in " + Database.Server.MapName[1105] + " (799,575)!",
                        MsgServer.MsgStaticMessage.Messages.TeratoDragon);
                }
                else if (Nr == 5)
                {
                    // Novo respawn do Dragon
                    MsgMonster.BossesBase.SpawnHandler(1787, 48, 38, 20070, "Dragon",
                        "will appear at " + DateTime.Now.Hour + ":30! Get ready to fight! You only have 5 minutes left!",
                        " has spawned in Dragon Island!");
                }

                // Spawna o Titan e Ganoderma junto com o TeratoDragon ou Dragon
                if (Nr <= 4) // Verifica se o TeratoDragon foi sorteado
                {
                    SpawnHandler(1020, 417, 625, 3134, "Titan",
                        "A Titan has spawned in " + Database.Server.MapName[1020] + " (417, 625)! Get ready to fight!",
                        " has spawned in " + Database.Server.MapName[1020] + " (417, 625)!");

                    SpawnHandler(1011, 655, 799, 3130, "Ganoderma",
                        "A Ganoderma has spawned in " + Database.Server.MapName[1011] + " (655, 799)! Get ready to fight!",
                        " has spawned in " + Database.Server.MapName[1011] + " (655, 799)!");
                }
                else if (Nr == 5) // Verifica se o Dragon foi sorteado
                {
                    // Spawna o Titan e Ganoderma junto com o Dragon
                    SpawnHandler(1020, 417, 625, 3134, "Titan", 
                        "A Titan has spawned in " + Database.Server.MapName[1020] + " (417, 625)! Get ready to fight!",
                        " has spawned in " + Database.Server.MapName[1020] + " (417, 625)!");

                    SpawnHandler(1011, 655, 799, 3130, "Ganoderma",
                        "A Ganoderma has spawned in " + Database.Server.MapName[1011] + " (655, 799)! Get ready to fight!",
                        " has spawned in " + Database.Server.MapName[1011] + " (655, 799)!");
                }

                // Atualiza o tempo do último spawn
                lastSpawnTime = now;
            }
        }

        public static void SpawnHandler(uint MapID, ushort X, ushort Y, uint MobID, string MonsterName, string PrepareMsg, string Msg, MsgServer.MsgStaticMessage.Messages idmsg = MsgServer.MsgStaticMessage.Messages.None)
        {
            var Map = Database.Server.ServerMaps[MapID];
            if (!Map.ContainMobID(MobID))
            {
                using (var rec = new ServerSockets.RecycledPacket())
                {
                    var stream = rec.GetStream();
                    Program.SendGlobalPackets.Enqueue(new MsgServer.MsgMessage(MonsterName + Msg, "ALLUSERS", "Server", MsgServer.MsgMessage.MsgColor.white, MsgServer.MsgMessage.ChatMode.Center).GetArray(stream));
                    Database.Server.AddMapMonster(stream, Map, MobID, X, Y, 1, 1, 1);
                }
            }
            else
            {
                using (var rec = new ServerSockets.RecycledPacket())
                {
                    var stream = rec.GetStream();
                    Program.SendGlobalPackets.Enqueue(new MsgServer.MsgMessage(MonsterName + Msg, "ALLUSERS", "Server", MsgServer.MsgMessage.MsgColor.white, MsgServer.MsgMessage.ChatMode.Center).GetArray(stream));
                }
            }
        }
    }
}
