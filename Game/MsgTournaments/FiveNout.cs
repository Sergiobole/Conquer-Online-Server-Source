using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static COServer.Game.MsgServer.MsgStringPacket;

namespace COServer.Game.MsgTournaments
{
    public class Fivenout : ITournament
    {

        public ProcesType Process { get; set; }
        public DateTime StartTimer = new DateTime();
        public DateTime InfoTimer = new DateTime();
        public uint Seconds = 10;
        public Role.GameMap Map;
        public uint DinamicMap = 0;
        public KillerSystem KillSystem;
        public TournamentType Type { get; set; }
        public Fivenout(TournamentType _type)
        {
            Type = _type;
            Process = ProcesType.Dead;
        }

        public void Open()
        {
            if (Process == ProcesType.Dead)
            {
                KillSystem = new KillerSystem();
                StartTimer = DateTime.Now;

                MsgSchedules.SendInvitation("Five and Out",431, 251, 1002, 0, 60, MsgServer.MsgStaticMessage.Messages.Fiveout);

                if (Map == null)
                {
                    Map = Database.Server.ServerMaps[700];
                    DinamicMap = Map.GenerateDynamicID();
                }
                InfoTimer = DateTime.Now;
                Seconds = 60;
                Process = ProcesType.Idle;
            }
        }
        public bool Join(Client.GameClient user, ServerSockets.Packet stream)
        {
            if (Process == ProcesType.Idle)
            {
                ushort x = 0;
                ushort y = 0;
                Map.GetRandCoord(ref x, ref y);
                user.Teleport(x, y, Map.ID, DinamicMap);
                user.Player.FiveNOut = 5;

              
                return true;
            }
            return false;
        }
        public void CheckUp()
        {
            if (Process == ProcesType.Idle)
            {
                if (DateTime.Now > StartTimer.AddMinutes(1))
                {
                    MsgSchedules.SendSysMesage("Five and Out has started! Signups are now closed.", MsgServer.MsgMessage.ChatMode.Center, MsgServer.MsgMessage.MsgColor.white);
                    Process = ProcesType.Alive;
                    StartTimer = DateTime.Now;
                }
                else if (DateTime.Now > InfoTimer.AddSeconds(10))
                {
                    Seconds -= 10;
                    MsgSchedules.SendSysMesage("[Five and Out] Fight starts in " + Seconds.ToString() + " seconds.", MsgServer.MsgMessage.ChatMode.Center, MsgServer.MsgMessage.MsgColor.white);
                    InfoTimer = DateTime.Now;
                }
            }

            if (Process == ProcesType.Alive)
            {
                if (DateTime.Now > StartTimer.AddMinutes(15))
                {
                    foreach (var user in MapPlayers())
                    {
                        user.Teleport(428, 378, 1002);
                    }
                    MsgSchedules.SendSysMesage("Five and Out has ended. Players have been teleported back to Twin City.", MsgServer.MsgMessage.ChatMode.Center, MsgServer.MsgMessage.MsgColor.white);
                    Process = ProcesType.Dead;
                }

                if (MapPlayers().Length == 1)
                {
                    var winner = MapPlayers().First();
                    //using (var rec = new ServerSockets.RecycledPacket())
                    //{
                    //    var stream = rec.GetStream();
                    //    Role.Player.Reward(winner, stream, " Five(N)Out Tournaments");
                    //}
                    winner.Player.ConquerPoints += 100;
                    var mymsg = "[EVENT]" + winner.Player.Name + " received 100 CPs and 5 ExpBalls from the Five and Out Tournament!";
                    MsgSchedules.SendSysMesage(mymsg, Game.MsgServer.MsgMessage.ChatMode.System, Game.MsgServer.MsgMessage.MsgColor.white);

                    winner.GainExpBall(3000, true, Role.Flags.ExperienceEffect.angelwing);
                    winner.Teleport(428, 378, 1002, 0);
                    Process = ProcesType.Dead;
                }


                Time32 Timer = Time32.Now;
                foreach (var user in MapPlayers())
                {
                    if (user.Player.Alive == false)
                    {
                        if (user.Player.DeadStamp.AddSeconds(4) < Timer)
                        {
                            user.Teleport(428, 378, 1002);
                            user.GainExpBall(1200, true, Role.Flags.ExperienceEffect.angelwing);
                            var mymsg = "[EVENT]" + user.Player.Name + " has lost in the Five and Out Tournament and received 2 ExpBalls!";
                            MsgSchedules.SendSysMesage(mymsg, Game.MsgServer.MsgMessage.ChatMode.System, Game.MsgServer.MsgMessage.MsgColor.white);

                        }
                    }
                }
            }
        }

        public Client.GameClient[] MapPlayers()
        {
            return Map.Values.Where(p => p.Player.DynamicID == DinamicMap && p.Player.Map == Map.ID).ToArray();
        }

        public bool InTournament(Client.GameClient user)
        {
            if (Map == null) return false;
            return user.Player.Map == Map.ID && user.Player.DynamicID == DinamicMap;
        }
    }
}
