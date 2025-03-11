using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using COServer.Game.MsgServer;
using COServer.Game.MsgTournaments;

namespace COServer.Game.MsgMonster
{
    public class BossesBase
    {
        internal static void SendInvitation(string Name, ushort X, ushort Y, ushort map, ushort DinamicID, int Seconds, Game.MsgServer.MsgStaticMessage.Messages messaj = Game.MsgServer.MsgStaticMessage.Messages.None)
        {
            string Message = " " + Name + " is about to begin! Will you join it?";
            using (var rec = new ServerSockets.RecycledPacket())
            {
                var stream = rec.GetStream();
                Program.SendGlobalPackets.Enqueue(new MsgMessage($"{Name} has started!", MsgMessage.MsgColor.yellow, MsgMessage.ChatMode.TopLeftSystem).GetArray(rec.GetStream()));

                var packet = new Game.MsgServer.MsgMessage(Message, MsgServer.MsgMessage.MsgColor.yellow, MsgServer.MsgMessage.ChatMode.Center).GetArray(stream);
                foreach (var client in Database.Server.GamePoll.Values)
                {
                    client.Send(packet);
                    client.Player.MessageBox(Message, new Action<Client.GameClient>(user => user.Teleport(X, Y, map, DinamicID)), null, Seconds, messaj);
                }
            }

            // Envia uma mensagem para o Discord informando que o evento começou
            Program.DiscordAPIevents.Enqueue($"``{Name} has started!``");
            Program.DiscordAPI.Enqueue($"`` Server Online ``");
        }



        public static DateTime lastCleanwaterSpawnTime = DateTime.Now; // Armazena o último spawn do Cleanwater
        public static DateTime lastGanodermaTitanSpawnTime = DateTime.Now; // Armazena o último spawn de Ganoderma e Titan
        public static DateTime lastSpawnTime = DateTime.Now; // Armazena o último spawn dos outros bosses

        public static void BossesTimer()
        {
            DateTime now = DateTime.Now;

            // Verifica se o spawn dos bosses regulares (TeratoDragon e Dragon) deve ocorrer a cada 1 hora
            if ((now - lastSpawnTime).TotalHours >= 1)
            {
                Random R = new Random();
                int Nr = R.Next(1, 6); // Sorteio de local, agora com 6 opções

                if (Nr == 1)
                {
                    SpawnHandler(1002, 564, 792, 20060, "TeratoDragon",
                        "will appear at " + now.Hour + ":00! Get ready to fight! You only have 5 minutes!",
                        " has spawned in " + Database.Server.MapName[1002] + " (564,792)!",
                        MsgServer.MsgStaticMessage.Messages.TeratoDragon);


                    SendInvitation("TeratoDragon", 564, 792, 1002, 0, 60, MsgServer.MsgStaticMessage.Messages.TeratoDragon);
                    Program.DiscordAPIevents.Enqueue("``TeratoDragon Spawned in TwinCity(564, 792)!``");

                }
                else if (Nr == 2)
                {
                    SpawnHandler(1000, 500, 704, 20060, "TeratoDragon",
                        "will appear at " + now.Hour + ":00! Get ready to fight! You only have 5 minutes!",
                        " has spawned in " + Database.Server.MapName[1000] + " (500,704)!",
                        MsgServer.MsgStaticMessage.Messages.TeratoDragon);

                    SendInvitation("TeratoDragon", 500, 704, 1000, 0, 60, MsgServer.MsgStaticMessage.Messages.TeratoDragon);
                    Program.DiscordAPIevents.Enqueue("``TeratoDragon Spawned in DesertIsland(500, 704)!``");
                }
                else if (Nr == 3)
                {
                    SpawnHandler(1020, 568, 584, 20060, "TeratoDragon",
                        "will appear at " + now.Hour + ":00! Get ready to fight! You only have 5 minutes!",
                        " has spawned in " + Database.Server.MapName[1012] + " (568,584)!",
                        MsgServer.MsgStaticMessage.Messages.TeratoDragon);

                    SendInvitation("TeratoDragon", 568, 584, 1020, 0, 60, MsgServer.MsgStaticMessage.Messages.TeratoDragon);
                    Program.DiscordAPIevents.Enqueue("``TeratoDragon Spawned in Ape Island(568, 584)!``");
                }
                else if (Nr == 4)
                {
                    SpawnHandler(1015, 799, 575, 20060, "TeratoDragon",
                        "will appear at " + now.Hour + ":00! Get ready to fight! You only have 5 minutes!",
                        " has spawned in " + Database.Server.MapName[1105] + " (799,575)!",
                        MsgServer.MsgStaticMessage.Messages.TeratoDragon);

                    SendInvitation("TeratoDragon", 799, 575, 1015, 0, 60, MsgServer.MsgStaticMessage.Messages.TeratoDragon);
                    Program.DiscordAPIevents.Enqueue("``TeratoDragon Spawned in Bird Island(799, 575)!``");
                }
                else if (Nr == 5)
                {
                    MsgMonster.BossesBase.SpawnHandler(1787, 48, 38, 20070, "Dragon",
                        "will appear at " + DateTime.Now.Hour + ":30! Get ready to fight! You only have 5 minutes left!",
                        " has spawned in Dragon Island!");

                    SendInvitation("DragonIsland", 48, 38, 1787, 0, 60, MsgServer.MsgStaticMessage.Messages.TeratoDragon);
                    Program.DiscordAPIevents.Enqueue("``Dragon Spawned in DragonIsland(48, 38)!``");
                }

                // Atualiza o tempo do último spawn
                lastSpawnTime = now;
            }

            // Verifica se o Ganoderma e o Titan devem spawnar a cada 30 minutos
            if ((now.Minute == 15 && now.Second == 0) && (now - lastGanodermaTitanSpawnTime).TotalMinutes >= 60)
            {
                SpawnHandler(1011, 655, 799, 3130, "Ganoderma",
                    "A Ganoderma has spawned in " + Database.Server.MapName[1011] + " (655, 799)! Get ready to fight!",
                    " has spawned in " + Database.Server.MapName[1011] + " (655, 799)!");

                Program.DiscordAPIevents.Enqueue("``Ganoderma Spawned (655, 799)!``");

                lastGanodermaTitanSpawnTime = now;  // Atualiza o tempo do último spawn de Ganoderma e Titan
            }

            if ((now.Minute == 21 && now.Second == 0) && (now - lastGanodermaTitanSpawnTime).TotalMinutes >= 60)
            {
                SpawnHandler(1020, 417, 625, 3134, "Titan",
                    "A Titan has spawned in " + Database.Server.MapName[1020] + " (417, 625)! Get ready to fight!",
                    " has spawned in " + Database.Server.MapName[1020] + " (417, 625)!");

                Program.DiscordAPIevents.Enqueue("``Titan Spawned (417, 625)!``");

                lastGanodermaTitanSpawnTime = now;  // Atualiza o tempo do último spawn de Ganoderma e Titan
            }

            // Verifica se o Cleanwater deve spawnar a cada 1 hora
            if ((now - lastCleanwaterSpawnTime).TotalMinutes >= 30)
            {
                SpawnHandler(1212, 428, 418, 8500, "Cleanwater",
                    "Cleanwater has spawned in " + Database.Server.MapName[1212] + " (428, 418)! Get ready to fight!",
                    " has spawned in " + Database.Server.MapName[1212] + " (428, 418)!");
                Program.DiscordAPIevents.Enqueue("``Cleanwater Spawned in map 1212 (428, 418)!``");

                lastCleanwaterSpawnTime = now;
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
