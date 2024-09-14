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
                int Nr = R.Next(1, 5); // Sorteio de local, 4 opções

                // Sorteia os diferentes locais para o TeratoDragon
                if (Nr == 1)
                {
                    SpawnHandler(1002, 564, 792, 20060, "TeratoDragon",
                        "will appear at " + now.Hour + ":00! Get ready to fight! You only have 5 minutes!",
                        " has spawned in " + Database.Server.MapName[1002] + " (564,792)!",
                        MsgServer.MsgStaticMessage.Messages.TeratoDragon);

                    MsgSchedules.SendInvitation("TeratoDragon", 564, 792, 1002, 0, 60, Game.MsgServer.MsgStaticMessage.Messages.TeratoDragon);
                }
                else if (Nr == 2)
                {
                    SpawnHandler(1015, 199, 193, 20060, "TeratoDragon",
                        "will appear at " + now.Hour + ":00! Get ready to fight! You only have 5 minutes!",
                        " has spawned in " + Database.Server.MapName[1015] + " (199,193)!",
                        MsgServer.MsgStaticMessage.Messages.TeratoDragon);

                    MsgSchedules.SendInvitation("TeratoDragon", 199, 193, 1015, 0, 60, Game.MsgServer.MsgStaticMessage.Messages.TeratoDragon);
                }
                else if (Nr == 3)
                {
                    SpawnHandler(1012, 118, 110, 20060, "TeratoDragon",
                        "will appear at " + now.Hour + ":00! Get ready to fight! You only have 5 minutes!",
                        " has spawned in " + Database.Server.MapName[1012] + " (118,110)!",
                        MsgServer.MsgStaticMessage.Messages.TeratoDragon);

                    MsgSchedules.SendInvitation("TeratoDragon", 118, 110, 1012, 0, 60, Game.MsgServer.MsgStaticMessage.Messages.TeratoDragon);
                }
                else if (Nr == 4)
                {
                    SpawnHandler(1105, 93, 54, 20060, "TeratoDragon",
                        "will appear at " + now.Hour + ":00! Get ready to fight! You only have 5 minutes!",
                        " has spawned in " + Database.Server.MapName[1105] + " (93,54)!",
                        MsgServer.MsgStaticMessage.Messages.TeratoDragon);

                    MsgSchedules.SendInvitation("TeratoDragon", 93, 54, 1105, 0, 60, Game.MsgServer.MsgStaticMessage.Messages.TeratoDragon);
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
