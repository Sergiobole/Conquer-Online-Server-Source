using System;
using System.Linq;

namespace COServer.Game.MsgTournaments
{
    public class MsgPkWar
    {
        public const int RewardConquerPoints = 860,
            FinishMinutes = 15;
        public const uint MapID = 1508;
        private ProcesType Mode;
        private DateTime StartTimer = new DateTime();
        public DateTime ScoreStamp = new DateTime();
        public Role.GameMap Map;
        public uint WinnerUID = 0;
        public int Duration = 0;
        public MsgPkWar()
        {
            Mode = ProcesType.Dead;
        }
        public void Open()
        {
            if (Mode == ProcesType.Dead)
            {
                Mode = ProcesType.Idle;
                Map = Database.Server.ServerMaps[MapID];
                MsgSchedules.SendInvitation("PKDeathMatch Pk War", 421, 362, 1002, 0, 60, MsgServer.MsgStaticMessage.Messages.WeeklyPKWar);
                StartTimer = DateTime.Now;
                Duration = 15 * 60;
            }
        }
        public bool AllowJoin()
        {
            return Mode == ProcesType.Idle;
        }
        public void CheckUp()
        {
            if (Mode == ProcesType.Idle)
            {
                #region Score
                if (DateTime.Now > ScoreStamp)
                {
                    using (var rec = new ServerSockets.RecycledPacket())
                    {
                        var stream = rec.GetStream();
                        var array = MapPlayers().OrderByDescending(p => p.Player.PkWarScore).ToArray();
                        Game.MsgServer.MsgMessage msg = new MsgServer.MsgMessage("# Weakly Pk Score # ", MsgServer.MsgMessage.MsgColor.yellow, MsgServer.MsgMessage.ChatMode.FirstRightCorner);
                        SendMapPacket(msg.GetArray(stream));

                        int x = 0;
                        foreach (var obj in array)
                        {
                            if (x == 4)
                                break;
                            Game.MsgServer.MsgMessage amsg = new MsgServer.MsgMessage("No " + (x + 1).ToString() + ". " + obj.Player.Name + " (" + obj.Player.PkWarScore.ToString() + ")", MsgServer.MsgMessage.MsgColor.yellow, MsgServer.MsgMessage.ChatMode.ContinueRightCorner);
                            SendMapPacket(amsg.GetArray(stream));
                            x++;
                        }
                        foreach (var user in MapPlayers())
                        {
                            msg = new MsgServer.MsgMessage("Your Score : " + user.Player.PkWarScore.ToString() + "", MsgServer.MsgMessage.MsgColor.yellow, MsgServer.MsgMessage.ChatMode.ContinueRightCorner);
                            user.Player.View.SendView(stream, true);
                            user.Send(msg.GetArray(stream));
                        }
                        msg = new MsgServer.MsgMessage(RemainingTime() + "", MsgServer.MsgMessage.MsgColor.yellow, MsgServer.MsgMessage.ChatMode.ContinueRightCorner);
                        SendMapPacket(msg.GetArray(stream));
                    }
                    ScoreStamp = DateTime.Now.AddSeconds(1);
                }
                #endregion
                #region Revive
                Time32 Timer = Time32.Now;
                foreach (var user in MapPlayers())
                {
                    if (user.Player.Alive == false && Mode != ProcesType.Dead)
                    {
                        if (user.Player.DeadStamp.AddSeconds(15) < Timer)
                        {
                            ushort x = 0;
                            ushort y = 0;
                            Map.GetRandCoord(ref x, ref y);
                            user.Teleport(x, y, Map.ID);
                        }
                    }
                }
                #endregion
                #region End
                if (Duration <= 0)
                {
                    Mode = ProcesType.Dead;
                    //foreach (var user in MapPlayers())
                    //{
                    //    user.Teleport(428, 378, 1002);
                    //}
                }
                #endregion
            }
        }
        public string RemainingTime()
        {
            TimeSpan T = TimeSpan.FromSeconds(Duration);
            if (Duration > 0)
                --Duration;
            string message = $"Time left {T.ToString(@"mm\:ss")}";
            return message;
        }
        public bool InTournament(Client.GameClient user)
        {
            if (Map == null)
                return false;
            return user.Player.Map == Map.ID;
        }
        public void Revive(Time32 Timer, Client.GameClient user)
        {
            if (user.Player.Alive == false && Mode != ProcesType.Dead)
            {
                if (InTournament(user))
                {
                    if (user.Player.DeadStamp.AddSeconds(4) < Timer)
                    {
                        ushort x = 0;
                        ushort y = 0;
                        Map.GetRandCoord(ref x, ref y);
                        user.Teleport(x, y, Map.ID);
                    }
                }
            }
        }
        public Client.GameClient[] MapPlayers()
        {
            return Map.Values.Where(p => InTournament(p)).ToArray();
        }
        public void SendMapPacket(ServerSockets.Packet stream)
        {
            foreach (var user in MapPlayers())
                user.Send(stream);
        }
        public bool IsFinished() { return Mode == ProcesType.Dead; }
        public bool TheLastPlayer()
        {
            return Database.Server.GamePoll.Values.Where(p => p.Player.Map == 1508 && p.Player.Alive).Count() == 1;
        }
        public void GiveReward(Client.GameClient client, ServerSockets.Packet stream)
        {
            WinnerUID = client.Player.UID;
            client.SendSysMesage("You've received " + RewardConquerPoints.ToString() + " CPs. ", MsgServer.MsgMessage.ChatMode.System, MsgServer.MsgMessage.MsgColor.white);
            MsgSchedules.SendSysMesage("" + client.Player.Name + " won PKDeathMatch PK War , he/she received " + RewardConquerPoints.ToString() + " CPs!", MsgServer.MsgMessage.ChatMode.TopLeftSystem, MsgServer.MsgMessage.MsgColor.white);
            string reward = "[EVENT]" + client.Player.Name + " has received " + RewardConquerPoints + " from PKDeathMatch.";
            //Program.DiscordAPI.Enqueue("``{reward}``");
            Database.ServerDatabase.LoginQueue.Enqueue(reward);
            client.Player.ConquerPoints += RewardConquerPoints;
            AddTop(client);
            client.Teleport(430, 269, 1002, 0);
        }
        public void AddTop(Client.GameClient client)
        {
            if (WinnerUID == client.Player.UID)
                client.Player.AddFlag(MsgServer.MsgUpdate.Flags.WeeklyPKChampion, Role.StatusFlagsBigVector32.PermanentFlag, false);
        }
    }
}
