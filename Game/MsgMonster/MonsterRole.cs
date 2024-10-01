﻿using COServer.Client;
using COServer.Database;
using COServer.Game.MsgFloorItem;
using COServer.Game.MsgNpc;
using COServer.Game.MsgServer;
using COServer.Game.MsgServer.AttackHandler;
using COServer.Game.MsgTournaments;
using System;
using System.Collections.Generic;
using System.Linq;
using static COServer.Game.MsgServer.MsgMessage;
using static COServer.Game.MsgServer.MsgPetInfo;

namespace COServer.Game.MsgMonster
{
    public unsafe class MonsterRole : Role.IMapObj
    {
        public string Name { get; set; }

        public uint DbCount = 0;
        public class ScoreBoard
        {
            public string Name;
            public uint ScoreDmg;
        }
        public Dictionary<uint, ScoreBoard> Scores = new Dictionary<uint, ScoreBoard>();
        public struct DropBag
        {
            public uint Item;
            public int Rate;
        }
        public DateTime RemoveFloor = DateTime.Now;
        public int StampFloorSeconds = 0;

        public static List<uint> SpecialMonsters = new List<uint>()
        {
            20070,
            3130,
            3134,
            20300,
            213883
        };

        public Client.GameClient AttackerScarofEarthl;
        public Database.MagicType.Magic ScarofEarthl;

        public int ExtraDamage { get { return Family.extra_damage; } }
        public int BattlePower { get { return Family.extra_battlelev; } }
        public bool AllowDynamic { get; set; }
        public bool IsTrap() { return false; }
        public uint IndexInScreen { get; set; }

        public Client.GameClient OwnerFloor;
        public Database.MagicType.Magic DBSpell;
        public ushort SpellLevel = 0;
        public DateTime FloorStampTimer = new DateTime();
        public bool IsFloor = false;
        public Game.MsgFloorItem.MsgItemPacket FloorPacket;


        public bool BlackSpot = false;
        public Time32 Stamp_BlackSpot = new Time32();


        public int SizeAdd { get { return Family.AttackRange; } }

        public byte PoisonLevel = 0;

        private Time32 DeadStamp = new Time32();
        private Time32 FadeAway = new Time32();
        public Time32 RespawnStamp = new Time32();
        public Time32 MoveStamp = new Time32();
        public static Time32 LastBossesKilled = new Time32();
        public bool CanRespawn(Role.GameMap map)
        {
            Time32 Now = Time32.Now;
            if (Now > RespawnStamp)
            {
                if (!map.MonsterOnTile(RespawnX, RespawnY))
                {
                    return true;
                }
            }
            return false;

        }

        public void Respawn(bool SendEffect = true)
        {
            using (var rev = new ServerSockets.RecycledPacket())
            {
                var stream = rev.GetStream();

                ClearFlags(false);

                HitPoints = (uint)Family.MaxHealth;
                State = MobStatus.Idle;

                Game.MsgServer.ActionQuery action;

                action = new MsgServer.ActionQuery()
                {
                    ObjId = UID,
                    Type = MsgServer.ActionType.RemoveEntity
                };

                Send(stream.ActionCreate(&action));

                Send(GetArray(stream, false));

                if (SendEffect)
                {
                    action.Type = ActionType.ReviveMonster;
                    Send(stream.ActionCreate(&action));
                }

                if (Family.MaxHealth > ushort.MaxValue)
                {
                    Game.MsgServer.MsgUpdate Upd = new Game.MsgServer.MsgUpdate(stream, UID, 2);
                    stream = Upd.Append(stream, Game.MsgServer.MsgUpdate.DataType.MaxHitpoints, Family.MaxHealth);
                    stream = Upd.Append(stream, Game.MsgServer.MsgUpdate.DataType.Hitpoints, HitPoints);
                    Send(Upd.GetArray(stream));
                }
                /*  if (Family.MaxHealth > 0)
                  {
                      Game.MsgServer.MsgUpdate Upd = new Game.MsgServer.MsgUpdate(stream, UID, 2);
                      stream = Upd.Append(stream, Game.MsgServer.MsgUpdate.DataType.MaxHitpoints, Family.MaxHealth);
                      stream = Upd.Append(stream, Game.MsgServer.MsgUpdate.DataType.Hitpoints, HitPoints);
                      Send(Upd.GetArray(stream));
                  }*/
            }
        }
        public void SendSysMesage(string Messaj, Game.MsgServer.MsgMessage.ChatMode ChatType = Game.MsgServer.MsgMessage.ChatMode.TopLeft
          , Game.MsgServer.MsgMessage.MsgColor color = Game.MsgServer.MsgMessage.MsgColor.white)
        {
            using (var rec = new ServerSockets.RecycledPacket())
            {
                var stream = rec.GetStream();
                Program.SendGlobalPackets.Enqueue(new Game.MsgServer.MsgMessage(Messaj, color, ChatType).GetArray(stream));
            }
        }
        public void SendBossSysMesage(string KillerName, int StudyPoints, Game.MsgServer.MsgMessage.ChatMode ChatType = Game.MsgServer.MsgMessage.ChatMode.Center
          , Game.MsgServer.MsgMessage.MsgColor color = Game.MsgServer.MsgMessage.MsgColor.white)
        {
            SendSysMesage("The " + Name.ToString() + " has been destroyed by " + KillerName.ToString() + "`s !", ChatType, color);
        }

        public void Dead(ServerSockets.Packet stream, Client.GameClient killer, uint aUID, Role.GameMap GameMap, bool CounterKill = false)
        {
            if (Alive)
            {
                if (IsFloor)
                {

                    FloorPacket.DropType = MsgFloorItem.MsgDropID.RemoveEffect;
                    HitPoints = 0;
                    GameMap.SetMonsterOnTile(X, Y, false);
                    return;
                }

                RespawnStamp = Time32.Now.AddSeconds(8 + Family.RespawnTime);

                ClearFlags(false);
                HitPoints = 0;
                AddFlag(MsgServer.MsgUpdate.Flags.Dead, Role.StatusFlagsBigVector32.PermanentFlag, true);
                DeadStamp = Time32.Now;

                var Pet = Database.Server.GamePoll.Values.Where(p => p.Pet?.monster.UID == aUID && p.Pet != null).SingleOrDefault();
                Pet?.Pet.DeAtach(stream);

                InteractQuery action = new InteractQuery()
                {
                    UID = aUID,
                    KilledMonster = true,
                    X = this.X,
                    Y = this.Y,
                    AtkType = MsgAttackPacket.AttackID.Death,
                    OpponentUID = UID
                };
                if (killer != null && killer.Player != null && !CounterKill)
                {
                    killer.MobsKilled++;
                    if (killer.DemonExterminator != null)
                        killer.DemonExterminator.UppdateJar(killer, Family.ID);

                    if (killer.Player.OnXPSkill() == MsgUpdate.Flags.Cyclone || killer.Player.OnXPSkill() == MsgUpdate.Flags.Superman)
                    {
                        killer.Player.XPCount++;
                        killer.Player.KillCounter++;

                        if (killer.Player.OnXPSkill() != MsgServer.MsgUpdate.Flags.Normal)
                        {

                            action.KillCounter = killer.Player.KillCounter;
                            killer.Player.UpdateXpSkill();
                        }

                    }
                    else if (killer.Player.OnXPSkill() == MsgUpdate.Flags.Normal)
                    {
                        killer.Player.XPCount++;
                    }
                }
                Send(stream.InteractionCreate(&action));
                if (RemoveOnDead)
                {
                    AddFlag(MsgUpdate.Flags.FadeAway, 10, false);
                    GMap.View.LeaveMap<Role.IMapObj>(this);
                    if (GMap.IsFlagPresent(X, Y, Role.MapFlagType.Monster))
                        GMap.cells[X, Y] &= ~Role.MapFlagType.Monster;
                }

                if (Map == 3935)
                    return;
                if (killer != null && killer.Player != null)
                {
                    if (ConfirmBoss()) // Verifica se a função ConfirmBoss retorna verdadeiro (ou seja, se o boss é confirmado)
                    {
                        // 70% de chance de fazer o drop de um item com ID 722057
                        if (Role.Core.Rate(70))
                        {
                            // Gera coordenadas aleatórias ao redor da posição (X, Y) do boss
                            ushort xx = (ushort)Program.GetRandom.Next(X - 6, X + 6);
                            ushort yy = (ushort)Program.GetRandom.Next(Y - 6, Y + 6);

                            // Adiciona o item ao chão se a coordenada gerada for válida
                            if (killer.Map.AddGroundItem(ref xx, ref yy))
                            {
                                // Faz o drop de um item com ID 722057 na coordenada (xx, yy)
                                DropItem(stream, killer.Player.UID, killer.Map, 722057, xx, yy, MsgFloorItem.MsgItem.ItemType.Item, 0, false, 0);
                            }
                        }
                        // 30% de chance de fazer o drop de 3 itens com ID 722057
                        else if (Role.Core.Rate(30))
                        {
                            for (int i = 0; i < 3; i++)
                            {
                                // Gera coordenadas aleatórias ao redor da posição (X, Y) do boss
                                ushort xx = (ushort)Program.GetRandom.Next(X - 6, X + 6);
                                ushort yy = (ushort)Program.GetRandom.Next(Y - 6, Y + 6);

                                // Adiciona o item ao chão se a coordenada gerada for válida
                                if (killer.Map.AddGroundItem(ref xx, ref yy))
                                {
                                    // Faz o drop de um item com ID 722057 na coordenada (xx, yy)
                                    DropItem(stream, killer.Player.UID, killer.Map, 722057, xx, yy, MsgFloorItem.MsgItem.ItemType.Item, 0, false, 0);
                                }
                            }
                        }

                        // 70% de chance de fazer o drop do item com ID 730001
                        if (Role.Core.Rate(70))
                        {
                            // Gera coordenadas aleatórias ao redor da posição (X, Y) do boss
                            ushort xx = (ushort)Program.GetRandom.Next(X - 6, X + 6);
                            ushort yy = (ushort)Program.GetRandom.Next(Y - 6, Y + 6);

                            // Adiciona o item ao chão se a coordenada gerada for válida
                            if (killer.Map.AddGroundItem(ref xx, ref yy))
                            {
                                // Faz o drop do item com ID 730001 na coordenada (xx, yy)
                                DropItem(stream, killer.Player.UID, killer.Map, 730001, xx, yy, MsgFloorItem.MsgItem.ItemType.Item, 0, false, 0);
                            }
                        }
                        else // 30% de chance (caso a chance de 70% não se concretize)
                        {
                            // Gera coordenadas aleatórias ao redor da posição (X, Y) do boss
                            ushort xx = (ushort)Program.GetRandom.Next(X - 6, X + 6);
                            ushort yy = (ushort)Program.GetRandom.Next(Y - 6, Y + 6);

                            // Adiciona o item ao chão se a coordenada gerada for válida
                            if (killer.Map.AddGroundItem(ref xx, ref yy))
                            {
                                // Faz o drop do item com ID 730002 na coordenada (xx, yy)
                                DropItem(stream, killer.Player.UID, killer.Map, 730002, xx, yy, MsgFloorItem.MsgItem.ItemType.Item, 0, false, 0);
                            }
                        }

                        return; // Finaliza a execução da função após o processamento dos drops
                    }
                    if (Role.Core.Rate(2, 20000))
                        DropItemID(killer, 1088000, stream, 6);

                    #region EggOrLetter [Quest]
                    uint ItemType = (uint)Program.GetRandom.Next(1, 3); // 1 para Ovo, 2 para Letra
                    uint EggCouler = (uint)Program.GetRandom.Next(1, 4);
                    uint LetraCauler = (uint)Program.GetRandom.Next(1, 8);

                    if (killer.MobsKilled > 2000)
                    {
                        switch (ItemType)
                        {
                            #region Ovos
                            case 1:
                                // Sorteio entre ovos
                                switch (EggCouler)
                                {
                                    #region VertEgg
                                    case 1:
                                        {
                                            if (killer.Player.VipLevel >= 6)
                                            {
                                                killer.Inventory.Add(stream, 729935, 1);
                                            }
                                            else
                                            {
                                                DropItemID(killer, 729935, stream, 6);
                                            }
                                            break;
                                        }
                                    #endregion
                                    #region PurpleEgg
                                    case 2:
                                        {
                                            if (killer.Player.VipLevel >= 6)
                                            {
                                                killer.Inventory.Add(stream, 729936, 1);
                                            }
                                            else
                                            {
                                                DropItemID(killer, 729936, stream, 6);
                                            }
                                            break;
                                        }
                                    #endregion
                                    #region RubyEgg
                                    case 3:
                                        {
                                            if (killer.Player.VipLevel >= 6)
                                            {
                                                killer.Inventory.Add(stream, 729937, 1);
                                            }
                                            else
                                            {
                                                DropItemID(killer, 729937, stream, 6);
                                            }
                                            break;
                                        }
                                        #endregion
                                }
                                break;
                            #endregion
                            #region Letras
                            case 2:
                                // Sorteio entre letras
                                switch (LetraCauler)
                                {
                                    #region Letras1
                                    case 1:
                                        {
                                            if (killer.Player.VipLevel >= 6)
                                            {
                                                killer.Inventory.Add(stream, 7112141, 1);
                                            }
                                            else
                                            {
                                                DropItemID(killer, 7112141, stream, 6);
                                            }
                                            break;
                                        }
                                    #endregion
                                    #region Letra2
                                    case 2:
                                        {
                                            if (killer.Player.VipLevel >= 6)
                                            {
                                                killer.Inventory.Add(stream, 7112151, 1);
                                            }
                                            else
                                            {
                                                DropItemID(killer, 7112151, stream, 6);
                                            }
                                            break;
                                        }
                                    #endregion
                                    #region Letra3
                                    case 3:
                                        {
                                            if (killer.Player.VipLevel >= 6)
                                            {
                                                killer.Inventory.Add(stream, 7112161, 1);
                                            }
                                            else
                                            {
                                                DropItemID(killer, 7112161, stream, 6);
                                            }
                                            break;
                                        }
                                    #endregion
                                    #region Letra4
                                    case 4:
                                        {
                                            if (killer.Player.VipLevel >= 6)
                                            {
                                                killer.Inventory.Add(stream, 7112171, 1);
                                            }
                                            else
                                            {
                                                DropItemID(killer, 7112171, stream, 6);
                                            }
                                            break;
                                        }
                                    #endregion
                                    #region Letra5
                                    case 5:
                                        {
                                            if (killer.Player.VipLevel >= 6)
                                            {
                                                killer.Inventory.Add(stream, 7112181, 1);
                                            }
                                            else
                                            {
                                                DropItemID(killer, 7112181, stream, 6);
                                            }
                                            break;
                                        }
                                    #endregion
                                    #region Letra6
                                    case 6:
                                        {
                                            if (killer.Player.VipLevel >= 6)
                                            {
                                                killer.Inventory.Add(stream, 7112191, 1);
                                            }
                                            else
                                            {
                                                DropItemID(killer, 7112191, stream, 6);
                                            }
                                            break;
                                        }
                                    #endregion
                                    #region Letra7
                                    case 7:
                                        {
                                            if (killer.Player.VipLevel >= 6)
                                            {
                                                killer.Inventory.Add(stream, 7112201, 1);
                                            }
                                            else
                                            {
                                                DropItemID(killer, 7112201, stream, 6);
                                            }
                                            break;
                                        }
                                        #endregion
                                }
                                break;
                                #endregion
                        }

                        // Resetar o contador de mobs após o drop
                        killer.MobsKilled = 0;
                    }
                    #endregion


                    if (killer.Player.VipLevel >= 6)
                    {
                        #region Drop_DragonBall
                        if (killer.DbKilled >= ProjectControl.VipDb_Drop)
                        {
                            killer.DbKilled = 0;
                            if (killer.Inventory.HaveSpace(ProjectControl.Max_DragonBall_Vip))
                            {

                                killer.Inventory.Add(stream, 1088000, ProjectControl.Max_DragonBall_Vip);
                                killer.SendSysMesage("[VIP] You've got " + ProjectControl.Max_DragonBall_Vip + " DragonBalls in your Inventory.", MsgMessage.ChatMode.TopLeft);
                                if (killer.Inventory.Contain(1088000, 10))
                                {
                                    killer.Inventory.Remove(1088000, 10, stream);
                                    killer.Inventory.Add(stream, 720028, 1);
                                    killer.SendSysMesage("[VIP] Auto packed 1xDBScroll in your Inventory.", MsgMessage.ChatMode.TopLeft);
                                }
                                return;
                            }
                        }
                        #endregion
                        #region Drop_Stone
                        if (killer.Drop_Stone >= ProjectControl.Vip_Drop_Stone)
                        {
                            killer.Drop_Stone = 0;
                            if (killer.Inventory.HaveSpace(ProjectControl.Max_Stone_Vip))
                            {
                                if (killer.Inventory.Contain(730001, 3))
                                {
                                    killer.Inventory.Remove(730001, 3, stream);
                                    killer.Inventory.Add(stream, 730002, 1);
                                    killer.SendSysMesage("[Drop System] Auto packed +2Stone in your inventory.", MsgMessage.ChatMode.TopLeft);
                                }
                                else
                                {
                                    killer.Inventory.Add(stream, 730001, ProjectControl.Max_Stone_Vip);
                                    killer.SendSysMesage("[Drop System] You've got " + ProjectControl.Max_Stone_Vip + " +1Stones in your inventory.", MsgMessage.ChatMode.TopLeft);
                                }
                            }
                        }
                        #endregion
                    }
                    else if (killer.Player.VipLevel < 6 || killer.Player.VipLevel == 0)
                    {
                        #region Drop_DragonBall
                        if (killer.DbKilled >= ProjectControl.NormalDb_Drop)
                        {
                            killer.DbKilled = 0;
                            if (killer.Inventory.HaveSpace(ProjectControl.Max_DragonBall))
                            {

                                killer.Inventory.Add(stream, 1088000, ProjectControl.Max_DragonBall);
                                killer.SendSysMesage("[VIP] You've got " + ProjectControl.Max_DragonBall + " DragonBalls in your inventory.", MsgMessage.ChatMode.TopLeft);
                                if (killer.Inventory.Contain(1088000, 10))
                                {
                                    killer.Inventory.Remove(1088000, 10, stream);
                                    killer.Inventory.Add(stream, 720028, 1);
                                    killer.SendSysMesage("[VIP] Auto packed 1 DBScroll in your inventory.", MsgMessage.ChatMode.TopLeft);
                                }
                                return;
                            }
                        }
                        #endregion
                        #region Drop_Meteors
                        if (killer.Drop_Meteors >= ProjectControl.Normal_Drop_Meteors)
                        {
                            killer.Drop_Meteors = 0;
                            if (killer.Inventory.HaveSpace(ProjectControl.Max_Meteors))
                            {
                                if (killer.Inventory.Contain(1088001, 9))
                                {
                                    killer.Inventory.Remove(1088001, 9, stream);
                                    killer.Inventory.Add(stream, 720027, 1);
                                    killer.SendSysMesage("[VIP] Auto packed 1 MeteorScroll in your inventory.", MsgMessage.ChatMode.TopLeft);
                                }
                                else
                                {
                                    killer.Inventory.Add(stream, 1088001, ProjectControl.Max_Meteors);
                                    killer.SendSysMesage("[VIP] You've got " + ProjectControl.Max_Meteors + " Meteors in your inventory.", MsgMessage.ChatMode.TopLeft);
                                }
                            }
                        }
                        #endregion
                        #region Drop_Stone
                        if (killer.Drop_Stone >= ProjectControl.Normal_Drop_Stone)
                        {
                            killer.Drop_Stone = 0;
                            if (killer.Inventory.HaveSpace(ProjectControl.Max_Stone))
                            {
                                if (killer.Inventory.Contain(730001, 3))
                                {
                                    killer.Inventory.Remove(730001, 3, stream);
                                    killer.Inventory.Add(stream, 730002, 1);
                                    killer.SendSysMesage("[VIP] Auto packed +2Stone in your inventory.", MsgMessage.ChatMode.TopLeft);
                                }
                                else
                                {
                                    killer.Inventory.Add(stream, 730001, ProjectControl.Max_Stone);
                                    killer.SendSysMesage("[VIP] You've got " + ProjectControl.Max_Stone + " +1Stones in your inventory.", MsgMessage.ChatMode.TopLeft);
                                }
                            }
                        }
                        #endregion
                    }
                    killer.DbKilled++;
                    killer.Drop_Meteors++;
                    killer.Drop_Stone++;
                    killer.MobsKilled++;
                    killer.TotalMobsKilled++;

                    if (killer.TotalMobsKilled % 100 == 0)
                    {
                        killer.SendSysMesage("You`ve killed " + killer.TotalMobsKilled + " so far, when you reach 100,000 you can visit SoulKeeper in Twin City to claim a prize!", MsgMessage.ChatMode.TopLeft);
                    }
                    if (Map == 5550)
                    {
                        killer.TotalMobsLevel++;
                    }
                    killer.Player.Owner.OnAutoAttack = false;
 
                    if (Family.ID == 3120 && killer.Player.SpawnGuildBeast && Map == 1038)
                    {
                        killer.Player.SendString(stream, MsgStringPacket.StringID.Effect, true, "zf2-e290");
                        MsgSchedules.SendSysMesage($"{killer.Player.Name} has killed the GuildBeast and received a DragonBall.");
                        DropItemID(killer, Database.ItemType.DragonBall, stream);
                        killer.Player.SpawnGuildBeast = false;
                    }
                    if (killer.Player.BlessTime > 0)
                        killer.Player.DbCount += Program.GetRandom.Next(5, 10);
                    else
                        killer.Player.DbCount += Program.GetRandom.Next(1, 10);

                    if (killer.Player.DbCount >= 6500)
                    {
                        killer.Player.SendString(stream, MsgStringPacket.StringID.Effect, true, "downnumber1");
                        killer.Player.DbCount = 0;
                        Program.SendGlobalPackets.Enqueue(new MsgMessage($"Congratulations! {killer.Player.Name} found a DragonBall!", MsgMessage.MsgColor.white, MsgMessage.ChatMode.Center).GetArray(stream));
                        if (killer.Player.VipLevel >= 5)
                        {
                            killer.Inventory.Add(stream, Database.ItemType.DragonBall, 1);
                        }
                        else
                        {
                            DropItemID(killer, Database.ItemType.DragonBall, stream);
                        }
                    }

                    if (killer.Player.VipLevel > 0)
                    {
                        killer.Player.Money += (uint)Program.GetRandom.Next(1, 50);//9. Make gold drop random.
                    }
                    if (killer.Player.BlessTime > 0 && Role.Core.Rate(Global.LUCKY_TIME_EXP_RATE))// 0.50% chance
                    {
                        killer.SendSysMesage("You got lucky, a monster you killed just dropped a ExpBall.", MsgMessage.ChatMode.Action);
                        killer.Player.SendString(stream, MsgStringPacket.StringID.Effect, true, "LuckyGuy");
                        DropItemID(killer, Database.ItemType.ExpBall, stream);
                    }
                    #region SkyPass
                    if (Map == 1040)
                    {
                        if (Family.ID == 3000)
                        {
                            if (Role.Core.RateDouble(10))
                            {
                                DropItemID(killer, 721100, stream);
                            }
                        }
                        if (Family.ID == 3009)
                        {
                            if (Role.Core.RateDouble(10))
                            {
                                DropItemID(killer, 721101, stream);
                            }
                        }
                        if (Family.ID == 3014)
                        {
                            if (Role.Core.RateDouble(15))
                            {
                                DropItemID(killer, 721102, stream);
                            }
                        }
                        if (Family.ID == 3019)
                        {
                            if (Role.Core.RateDouble(15))
                            {
                                DropItemID(killer, 721103, stream);
                            }
                        }
                        if (Family.ID == 3020)
                        {
                            if (Role.Core.RateDouble(15))
                            {
                                DropItemID(killer, 721108, stream);
                            }
                        }
                    }
                    #endregion
                    #region CleanWater
                    if (Map == 1212 && Family.ID == 8500)
                    {
                        if (Role.Core.Rate(40)) // Chance de 40% CleanWater
                        {
                            DropItemID(killer, Database.ItemType.CleanWater, stream);
                            Program.SendGlobalPackets.Enqueue(new MsgMessage($"Congratulations! {killer.Player.Name} found a ClearWater in WaterLord(428,418) !", MsgMessage.MsgColor.white, MsgMessage.ChatMode.Center).GetArray(stream));
                        }
                        else
                        {
                            DropItemID(killer, Database.ItemType.Meteor, stream); // Dropar uma Meteor.
                            Program.SendGlobalPackets.Enqueue(new MsgMessage($"Congratulations! {killer.Player.Name} found a Meteor in WaterLord(428,418) !", MsgMessage.MsgColor.white, MsgMessage.ChatMode.Center).GetArray(stream));
                        }
                    }
                    #endregion
                    #region Bosses 
                    if (Map == 1787)
                    {
                        #region Snow Banshee NemesisTyrant Raikou ThrillingSpook
                        if (Boss > 0 && (Family.ID == 20070 || Family.ID == 20060 || Family.ID == 3822 || Family.ID == 20160 ||
                                         Family.ID == 3820 || Family.ID == 6643 || Family.ID == 20300 || Family.ID == 20055))//Snow Banshee NemesisTyrant ThrillingSpook
                        {
                            for (ushort i = 0; i < 10; i++)
                                DropItemNull(Database.ItemType.Meteor, stream);

                            for (ushort i = 0; i < 5; i++)
                                DropItemNull(Database.ItemType.DragonBall, stream);

                            if (Role.Core.Rate(5))
                            {
                                DropItemNull((uint)(725018 + Program.GetRandom.Next(1, 7)), stream);
                            }
                            if (Role.Core.Rate(3))
                            {
                                uint amount = (uint)Program.GetRandom.Next(1000000);
                                var ItemID = Database.ItemType.MoneyItemID((uint)amount);
                                for (ushort i = 0; i < 10 && i < 20; i++)
                                    DropItemNull(ItemID, stream, MsgItem.ItemType.Money, amount);
                            }
                            return;
                        }
                        #endregion
                    }
                    #endregion
                    #region MeteorDove
                    if (Map == 1210)
                    {
                        if (Family.ID == 8415)
                        {
                            if (Role.Core.Rate(0.001))
                            {
                                DropItemID(killer, Database.ItemType.DragonBall, stream);
                                Program.SendGlobalPackets.Enqueue(new Game.MsgServer.MsgMessage("A DragonBall has dropped from a Meteordove killed by " + killer.Player.Name + ".", Game.MsgServer.MsgMessage.MsgColor.white, Game.MsgServer.MsgMessage.ChatMode.System).GetArray(stream));
                            }
                            else if (Role.Core.Rate(20))
                            {
                                DropItemID(killer, Database.ItemType.Meteor, stream);
                            }
                            else if (Role.Core.Rate(10))
                            {
                                for (int i = 0; i < 2; i++)
                                    DropItemID(killer, Database.ItemType.Meteor, stream);
                            }
                            else if (Role.Core.Rate(1))
                            {
                                for (int i = 0; i < 5; i++)
                                    DropItemID(killer, Database.ItemType.Meteor, stream);
                            }
                            else if (Role.Core.Rate(5))
                            {
                                for (int i = 0; i < 3; i++)
                                    DropItemID(killer, Database.ItemType.Meteor, stream);
                            }

                        }
                    }
                    #endregion
                    if (Map == 1011 && Family.ID == 0007 /*|| Map == 1015 && (Family.ID == 14 || Family.ID == 18)*/)
                    {
                        if (Role.Core.RateDouble(0.23))
                        {
                            DropItemID(killer, 721263, stream);
                        }

                        if (Role.Core.Rate(0.005))
                        {
                            if (killer.Inventory.HaveSpace(1))
                            {
                                killer.Inventory.Add(stream, 721117, 1); //ArmyToken
                            }
                            else DropItemID(killer, 721117, stream);
                        }
                    }
                    // undone
                    #region AncientDevil
                    if (Map == 1082)
                    {
                        #region TrojanGuard
                        if (Family.ID == 9000)
                        {
                            DropItemID(killer, 710017, stream);
                        }

                        #endregion
                        #region WarriorGuard
                        if (Family.ID == 9001)
                        {
                            DropItemID(killer, 710016, stream);
                        }

                        #endregion
                        #region ArcherGuard
                        if (Family.ID == 9002)
                        {
                            DropItemID(killer, 710020, stream);
                        }

                        #endregion
                        #region WaterGuard
                        if (Family.ID == 9004)
                        {
                            DropItemID(killer, 710019, stream);
                        }

                        #endregion
                        #region FireGuard
                        if (Family.ID == 9007)
                        {
                            DropItemID(killer, 710018, stream);
                        }

                        #endregion
                        #region Devil
                        if (Family.ID == 9111)
                        {
                            Game.MsgTournaments.MsgSchedules.SpawnDevil = false;
                            if (Role.Core.Rate(15))
                            {
                                DropItemNull(721330, stream);
                            }
                            if (Role.Core.Rate(15))
                            {
                                // DropItemID(killer, 721331, stream);
                                DropItemNull(721331, stream);

                            }
                            if (Role.Core.Rate(15))
                            {
                                //     DropItemID(killer, 721332, stream);
                                DropItemNull(721332, stream);

                            }
                            for (ushort i = 0; i < 10; i++)
                                DropItemNull(Database.ItemType.Meteor, stream);

                            DropItemNull(Database.ItemType.DragonBall, stream);

                            if (Role.Core.Rate(5))//dances book
                            {
                                //   DropItemID(killer, (uint)(725018 + Program.GetRandom.Next(1, 7)), stream);
                                DropItemNull((uint)(725018 + Program.GetRandom.Next(1, 7)), stream);
                            }
                            if (Role.Core.Rate(3))
                            {
                                uint amount = (uint)Program.GetRandom.Next(1000000);
                                var ItemID = Database.ItemType.MoneyItemID((uint)amount);
                                for (ushort i = 0; i < 10 && i < 20; i++)
                                    DropItemNull(ItemID, stream, MsgItem.ItemType.Money, amount);
                            }

                        }

                        #endregion
                    }
                    #endregion
                    #region SnakeKing
                    if (Map == 1063 && Family.ID == 3102)
                    {
                        switch (Program.GetRandom.Next(0, 5))
                        {
                            case 1:
                                {
                                    if (Role.Core.RateDouble(40))
                                    {
                                        for (byte i = 0; i < 5; i++)
                                            DropItemID(killer, Database.ItemType.Meteor, stream);
                                    }
                                    break;
                                }
                            case 2:
                                {
                                    if (Role.Core.Rate(5))
                                    {
                                        DropItemID(killer, Database.ItemType.DragonBall, stream);
                                        Program.SendGlobalPackets.Enqueue(new Game.MsgServer.MsgMessage("Congratulations! " + killer.Player.Name + " found a DragonBall from killing SnakeKing.", Game.MsgServer.MsgMessage.MsgColor.white, Game.MsgServer.MsgMessage.ChatMode.System).GetArray(stream));
                                    }
                                    break;
                                }
                            case 3:
                                {
                                    DropItemID(killer, Database.ItemType.Stone_1, stream);
                                    break;
                                }
                            case 4:
                                {
                                    if (Role.Core.Rate(5))
                                    {
                                        var gem = Program.GetRandom.Next(0, 8);
                                        var item = (uint)(700000 + gem * 10 + Program.GetRandom.Next(2, 4));
                                        DropItemID(killer, item, stream);
                                    }
                                    break;
                                }
                            case 5:
                                {
                                    DropItemID(killer, Database.ItemType.Meteor, stream);
                                    break;
                                }
                        }
                    }
                    #endregion
                    #region 2rbQuest
                    if (Map == 1700)
                    {// first stage -- drop normal moss / dreams / soul

                        if (Family.ID == 3616 || Family.ID == 3602 || Family.ID == 3628 || Family.ID == 3608 || Family.ID == 3624
                            || Family.ID == 3621 || Family.ID == 3606 || Family.ID == 3631 || Family.ID == 3620 || Family.ID == 3625 || Family.ID == 3601 || Family.ID == 3618
                            || Family.ID == 3626 || Family.ID == 3613 || Family.ID == 3609 || Family.ID == 3605 || Family.ID == 3629 || Family.ID == 3604 || Family.ID == 3600
                            || Family.ID == 3622 || Family.ID == 3617 || Family.ID == 3610 || Family.ID == 3612 || Family.ID == 3614)
                        {
                            if (killer.Player.Quest2rbStage != 1)
                            {
                                if (Role.Core.Rate(0.001))
                                {
                                    DropItemID(killer, Database.ItemType.SoulAroma, stream);
                                }
                                if (Role.Core.Rate(0.002))
                                {
                                    DropItemID(killer, Database.ItemType.DreamGrass, stream);
                                }
                                if (Role.Core.Rate(0.001))
                                {
                                    DropItemID(killer, Database.ItemType.Moss, stream);
                                }
                            }
                        }
                        else
                        {
                            if (killer.Player.Quest2rbS2Point >= 70000 && killer.Player.Quest2rbStage == 1)
                            {
                                if (Family.ID == 3611 || Family.ID == 3619 || Family.ID == 3603 || Family.ID == 3615 || Family.ID == 3627 || Family.ID == 3631 || Family.ID == 3607)
                                {
                                    if (killer.Player.Quest2rbS2Point < 70000)
                                    {
                                        killer.Player.Quest2rbS2Point += 500;
                                        killer.SendSysMesage($"Stage2 : Raze the mountin of Grievanceto the ground +500 / {killer.Player.Quest2rbS2Point}");
                                    }
                                }
                                else
                                {
                                    if (killer.Player.Quest2rbS2Point < 70000)
                                    {
                                        killer.Player.Quest2rbS2Point += 20;
                                        killer.SendSysMesage($"Stage2 : Raze the mountin of Grievanceto the ground +20 / {killer.Player.Quest2rbS2Point}");
                                    }

                                }
                            }
                            else
                            {
                                killer.SendSysMesage("you have finish the stage 2 go to talk to Bruce at (602,643)");
                            }

                        }


                        // stage 2 
                        if (Family.ID == 3632)
                        {
                            if (killer.Player.UID == aUID)
                            {
                                if (Role.Core.Rate(1))
                                {
                                    DropItemID(killer, 722722, stream);
                                }
                            }
                        }
                        else if (Family.ID == 3633)
                        {
                            if (killer.Player.UID == aUID)
                            {
                                if (Role.Core.Rate(1))
                                {
                                    DropItemID(killer, 722726, stream);
                                }
                            }
                        }
                        else if (Family.ID == 3634)
                        {
                            if (killer.Player.UID == aUID)
                            {
                                if (Role.Core.Rate(1))
                                {
                                    DropItemID(killer, 722729, stream);
                                }
                            }
                        }
                        else if (Family.ID == 3635)
                        {// if some one else kill this monser reset all the quest !
                            if (killer.Player.UID == aUID)
                            {
                                if (Role.Core.Rate(1))
                                {
                                    DropItemID(killer, 722731, stream);
                                    killer.Player.Quest2rbStage += 1;
                                    killer.Player.SendString(stream, MsgStringPacket.StringID.Effect, true, new string[1] { "fire1" });
                                }
                            }
                        }


                        //third sage
                        if (killer.Player.Quest2rbStage == 1 && killer.Player.Quest2rbS2Point >= 70000)
                        {
                            if (Family.ID == 3636 && killer.Player.Quest2rbBossesOrderby == 0) // andrew
                            {
                                killer.Player.Quest2rbBossesOrderby += 1;
                                killer.SendSysMesage("Congratulations You'h killed Boss Andrew, now go kill Peter");
                            }
                            else if (Family.ID == 3637 && killer.Player.Quest2rbBossesOrderby == 1) // peter
                            {
                                killer.Player.Quest2rbBossesOrderby += 1;
                                killer.SendSysMesage("Congratulations You'h killed Boss Peter, now go kill Philip");
                            }
                            else if (Family.ID == 3638 && killer.Player.Quest2rbBossesOrderby == 2) // phillip
                            {
                                killer.Player.Quest2rbBossesOrderby += 1;
                                killer.SendSysMesage("Congratulations You'h killed Boss Philip, now go kill Timothy");
                            }
                            else if (Family.ID == 3639 && killer.Player.Quest2rbBossesOrderby == 3) // Timothy
                            {
                                killer.Player.Quest2rbBossesOrderby += 1;
                                killer.SendSysMesage("Congratulations You'h killed Boss Timothy, now go kill Daphne ");
                            }
                            else if (Family.ID == 3640 && killer.Player.Quest2rbBossesOrderby == 4) // Daphne
                            {
                                killer.Player.Quest2rbBossesOrderby += 1;
                                killer.SendSysMesage("Congratulations You'h killed Boss Daphne, now go kill Victoria ");
                            }
                            else if (Family.ID == 3641 && killer.Player.Quest2rbBossesOrderby == 5) // Victoria
                            {
                                killer.Player.Quest2rbBossesOrderby += 1;
                                killer.SendSysMesage("Congratulations You'h killed Boss Victoria, now go kill Wayne ");
                            }
                            else if (Family.ID == 3642 && killer.Player.Quest2rbBossesOrderby == 6) // Wayne
                            {
                                killer.Player.Quest2rbBossesOrderby += 1;
                                killer.SendSysMesage("Congratulations You'h killed Boss Wayne, now go kill Theodore ");
                            }
                            else if (Family.ID == 3643 && killer.Player.Quest2rbBossesOrderby == 7) // Theodore
                            {
                                killer.Player.Quest2rbBossesOrderby += 1;
                                killer.SendSysMesage("Congratulations You'h killed Boss Theodore, now third sage done go talk to Stanley");
                                killer.Player.SendString(stream, MsgStringPacket.StringID.Effect, true, new string[1] { "tj" });

                            }

                            else if (killer.Player.Quest2rbBossesOrderby == 8 && Family.ID == 3611 || Family.ID == 3619 || Family.ID == 3603 || Family.ID == 3615 || Family.ID == 3627 || Family.ID == 3631 || Family.ID == 3607)
                            {
                                DropItemID(killer, 722727, stream);
                                killer.SendSysMesage("Congratulations You'h killed The Lord, and he dropped SquamaBead go to spawn Satan at(326,342)");
                            }
                            else if (killer.Player.Quest2rbBossesOrderby == 8 && Family.ID == 3644)//satan
                            {
                                Database.Server.AddMapMonster(stream, killer.Map, 3645, killer.Player.X, killer.Player.Y, 1, 1, 1);
                            }
                            else if (killer.Player.Quest2rbBossesOrderby == 8 && Family.ID == 3645)//satan
                            {
                                Database.Server.AddMapMonster(stream, killer.Map, 3646, killer.Player.X, killer.Player.Y, 1, 1, 1);
                            }
                            else if (killer.Player.Quest2rbBossesOrderby == 8 && Family.ID == 3646)//Furysatan
                            {
                                if (killer.Player.Quest2rbStage == 1)
                                {
                                    DropItemID(killer, 723701, stream);
                                    Program.SendGlobalPackets.Enqueue(new Game.MsgServer.MsgMessage("Congratulations! " + killer.Player.Name + " he/she finish the 2rb quest .", Game.MsgServer.MsgMessage.MsgColor.yellow, Game.MsgServer.MsgMessage.ChatMode.Center).GetArray(stream));
                                }
                                killer.Player.Quest2rbStage += 1;// 2!
                            }
                        }
                    }
                    #endregion
                    // emerald
                    if (Map == 1000)
                    {
                        if (Family.ID == 0015)
                        {
                            if (Role.Core.Rate(0.005))
                            {
                                DropItemID(killer, 1080001, stream);
                                killer.SendSysMesage("Emerald dropped!", MsgMessage.ChatMode.TopLeft);
                            }
                        }
                    }
                    if (Map == 1001)// health wine
                    {
                        if (Family.ID == 0058)
                        {
                            if (Role.Core.Rate(0.005))
                            {
                                DropItemID(killer, 723030, stream);
                            }
                        }
                    }

                    if (Map == 2021)// soul stone
                    {
                        double i = 0;
                        if (Family.ID == 11 && !Database.AtributesStatus.IsArcher(killer.Player.Class))
                            i += 0.4;
                        if (Role.Core.Rate(0.10 + i))
                        {
                            DropItemID(killer, 723085, stream);
                        }
                    }
                    if (Map == 2022)//dis city map 2
                    {
                        killer.Player.KillersDisCity += 1;
                        killer.SendSysMesage($"Kill more monsters! You have {killer.Player.KillersDisCity} points, you need to hurry for the next gate!");
                    }
                    if (Map == 2024)// dis city map 4
                    {
                        if (Family.ID == 66432)//ultimate pluto
                        {
                            DropItemID(killer, 790001, stream);
                            MsgTournaments.MsgSchedules.DisCity.KillTheUltimatePluto(killer);
                        }
                    }

                    if (Map == 1011 || Map == 1020)
                    {
                        if (Family.ID == 3130 || Family.ID == 3134)//titan/ ganoderma
                        {
                            if (Role.Core.Rate(75))
                            {
                                uint[] DropSpecialItems = new uint[] {
                                    Database.ItemType.Meteor,
                                    Database.ItemType.ExperiencePotion,
                                };
                                uint IDDrop = DropSpecialItems[Program.GetRandom.Next(0, DropSpecialItems.Length)];

                                DropItemID(killer, IDDrop, stream);

                                string drop_name = Database.Server.ItemsBase[IDDrop].Name;
                                SendSysMesage("The " + Name.ToString() + " has been destroyed by " + killer.Player.Name.ToString() + ", and dropped a " + drop_name + " ", Game.MsgServer.MsgMessage.ChatMode.Center, MsgMessage.MsgColor.red);
                            }
                            else if (Role.Core.Rate(0.0001))
                            {
                                DropItemID(killer, Database.ItemType.DragonBall, stream);

                                string drop_name = Database.Server.ItemsBase[Database.ItemType.DragonBall].Name;
                                SendSysMesage("The " + Name.ToString() + " has been destroyed by " + killer.Player.Name.ToString() + ", and dropped a " + drop_name + " ", Game.MsgServer.MsgMessage.ChatMode.Center, MsgMessage.MsgColor.red);

                            }
                        }
                    }
                    else if (Map == 1043 || Map == 1044 || Map == 1045 || Map == 1046 || Map == 1047 || Map == 1048 || Map == 1049)
                    {// moon box
                        if (Family.ID == 6000 || Family.ID == 6001 || Family.ID == 6002 || Family.ID == 6003 || Family.ID == 6004 || Family.ID == 6005)//Moonbox
                        {
                            uint itemid = 0;
                            if (Family.ID == 6000)
                                itemid = 721010;
                            if (Family.ID == 6001)
                                itemid = 721011;
                            if (Family.ID == 6002)
                                itemid = 721012;
                            if (Family.ID == 6003)
                                itemid = 721013;
                            if (Family.ID == 6004)
                                itemid = 721014;
                            if (Family.ID == 6005)
                                itemid = 721015;

                            if (Role.Core.Rate(5))
                            {
                                ushort xx = X;
                                ushort yy = Y;
                                if (killer.Map.AddGroundItem(ref xx, ref yy))
                                {
                                    DropItem(stream, killer.Player.UID, killer.Map, itemid, xx, yy, MsgFloorItem.MsgItem.ItemType.Item, 0, false, 0);
                                }

                            }
                        }
                    }

                    else if (Map == 1351 || Map == 1352 || Map == 1353 || Map == 1354)
                    {
                        if (Family.ID == 3142)
                        {
                            if (Role.Core.Rate(0.056))//SkyToken
                            {
                                DropItemID(killer, 721537, stream);
                            }
                        }
                        if (Family.ID == 3141 || Family.ID == 3143)//lAB1
                        {
                            if (Role.Core.Rate(0.05))
                            {
                                DropItemID(killer, 721533, stream);
                            }
                        }
                        if (Family.ID == 3144)//lab2
                        {
                            if (Role.Core.Rate(0.05))
                            {
                                DropItemID(killer, 721534, stream);
                            }
                        }
                        if (Family.ID == 3145)
                        {
                            if (Role.Core.Rate(0.004))
                            {
                                DropItemID(killer, 721538, stream);
                            }
                        }
                        if (Family.ID == 3147)//lab3 1353
                        {
                            if (Role.Core.Rate(0.5))
                            {
                                DropItemID(killer, 721535, stream);
                            }
                        }
                        if (Family.ID == 3148)
                        {
                            if (Role.Core.Rate(0.004))
                            {
                                DropItemID(killer, 721539, stream);//soul tokken
                            }
                        }
                        if (Family.ID == 3155 || Family.ID == 3156)//lab3 1353
                        {
                            if (Role.Core.Rate(0.002))
                            {
                                DropItemID(killer, 721536, stream);
                            }
                        }

                    }
                    if ((Family.Settings & MonsterSettings.DropItemsOnDeath) == MonsterSettings.DropItemsOnDeath)
                    {
                        if (Family.MaxHealth > 100000 && Family.MaxHealth < 7000000 || Boss == 1)
                        {
                            List<uint> DropIems = Family.ItemGenerator.GenerateBossFamily();
                            foreach (var ids in DropIems)
                            {
                                MsgServer.MsgGameItem DataItem = new MsgServer.MsgGameItem();
                                DataItem.ITEM_ID = ids;
                                Database.ItemType.DBItem DBItem;
                                if (Database.Server.ItemsBase.TryGetValue(ids, out DBItem))
                                {
                                    DataItem.Durability = DataItem.MaximDurability = DBItem.Durability;
                                }
                                DataItem.Color = Role.Flags.Color.Red;
                                ushort xx = X;
                                ushort yy = Y;
                                if (killer.Map.AddGroundItem(ref xx, ref yy))
                                {
                                    MsgFloorItem.MsgItem DropItem = new MsgFloorItem.MsgItem(DataItem, xx, yy, MsgFloorItem.MsgItem.ItemType.Item, 0, DynamicID, Map, killer.Player.UID, true, GMap);

                                    if (killer.Map.EnqueueItem(DropItem))
                                    {
                                        DropItem.SendAll(stream, MsgFloorItem.MsgDropID.Visible);
                                    }
                                }
                            }
                            return;
                        }
                        ushort rand = (ushort)(Program.GetRandom.Next() % 1000);
                        byte count = (byte)(Program.GetRandom.Next(1, 4));
                        if (rand > 10 && rand < 700)
                        {
                            ushort xx = X;
                            ushort yy = Y;
                            for (byte i = 0; i < count; i++)
                            {
                                if (killer.Map.AddGroundItem(ref xx, ref yy))
                                {
                                    uint ItemID = 0;
                                    uint Amount = 0;
                                    if (Map == 1002)
                                    {
                                        Amount = Family.ItemGenerator.GenerateGold(out ItemID, false, true);

                                    }
                                    else
                                    {
                                        if (Map == 1700)
                                            Amount = Family.ItemGenerator.GenerateGold(out ItemID, true);
                                        else
                                            Amount = Family.ItemGenerator.GenerateGold(out ItemID);
                                    }
                                    //DropItem(stream, killer.Player.UID, killer.Map, ItemID, xx, yy, MsgFloorItem.MsgItem.ItemType.Money, Amount, false, 0);
                                }
                            }
                        }
                        if (killer.Player.BlessTime > 0 ? rand > 150 && rand < 700 : rand > 200 && rand < 800)//&& rand < 770)
                        {
                            ushort xx = X;
                            ushort yy = Y;
                            for (byte i = 0; i < count; i++)
                            {

                                Database.ItemType.DBItem DbItem = null;
                                byte ID_Quality;
                                bool ID_Special;
                                uint ID = Family.ItemGenerator.GenerateItemId(Map, out ID_Quality, out ID_Special, out DbItem);
                                if (ID != 0)
                                {
                                    if (killer.Map.AddGroundItem(ref xx, ref yy))
                                    {
                                        if (ID == Database.ItemType.DragonBall)//jason
                                        {

                                            ActionQuery action2 = new ActionQuery()
                                            {
                                                ObjId = killer.Player.UID,
                                                Type = ActionType.DragonBall
                                            };
                                            killer.Send(stream.ActionCreate(&action2));
                                            Program.SendGlobalPackets.Enqueue(new MsgServer.MsgMessage("Congratulations! " + killer.Player.Name + " found a DragonBall! ", "ALLUSERS", "Server", MsgServer.MsgMessage.MsgColor.white, MsgServer.MsgMessage.ChatMode.Center).GetArray(stream));
                                        }
                                        DropItem(stream, killer.Player.UID, killer.Map, ID, xx, yy, MsgFloorItem.MsgItem.ItemType.Item, 0, ID_Special, ID_Quality, killer, DbItem);
                                        if (ID_Special)
                                            break;
                                    }
                                }

                            }
                        }
                        else if (rand > 500 && rand < 600)
                        {

                            ushort xx = X;
                            ushort yy = Y;
                            for (byte i = 0; i < count; i++)
                            {
                                if (killer.Map.AddGroundItem(ref xx, ref yy))
                                {
                                    uint ItemID = Family.DropHPItem;

                                    DropItem(stream, killer.Player.UID, killer.Map, ItemID, xx, yy, MsgFloorItem.MsgItem.ItemType.Item, 0, false, 0);
                                }
                            }
                        }
                        else if (rand > 600 && rand < 700)
                        {
                            ushort xx = X;
                            ushort yy = Y;
                            for (byte i = 0; i < count; i++)
                            {
                                if (killer.Map.AddGroundItem(ref xx, ref yy))
                                {
                                    uint ItemID = Family.DropMPItem;

                                    DropItem(stream, killer.Player.UID, killer.Map, ItemID, xx, yy, MsgFloorItem.MsgItem.ItemType.Item, 0, false, 0);
                                }
                            }
                        }
                    }
                }

            }
        }

        public static string GetItemName(uint ID)
        {
            Database.ItemType.DBItem item;
            if (Server.ItemsBase.TryGetValue(ID, out item))
            {

                return item.Name;
            }
            return "";
        }
        public void DropItemNull(uint Itemid, ServerSockets.Packet stream, MsgItem.ItemType type = MsgItem.ItemType.Item, uint gold = 0)
        {
            MsgServer.MsgGameItem DataItem = new MsgServer.MsgGameItem();
            DataItem.ITEM_ID = Itemid;
            var DBItem = Database.Server.ItemsBase[Itemid];
            DataItem.Durability = DBItem.Durability;
            DataItem.MaximDurability = DBItem.Durability;
            DataItem.Color = Role.Flags.Color.Red;
            ushort xx = X;
            ushort yy = Y;
            if (GMap.AddGroundItem(ref xx, ref yy, 5))
            {
                MsgFloorItem.MsgItem DropItem = new MsgFloorItem.MsgItem(DataItem, (ushort)(xx - Program.GetRandom.Next(5)), (ushort)(yy - Program.GetRandom.Next(5)), type, gold, 0, Map, 0, false, GMap);
                if (GMap.EnqueueItem(DropItem))
                {
                    DropItem.SendAll(stream, MsgFloorItem.MsgDropID.Visible);
                }
            }
        }

        private void DropItem(ServerSockets.Packet stream, uint OwnerItem, Role.GameMap map, uint ItemID, ushort XX, ushort YY, MsgFloorItem.MsgItem.ItemType typ
            , uint amount, bool special, byte ID_Quality, Client.GameClient user = null, Database.ItemType.DBItem DBItem = null)
        {
            MsgServer.MsgGameItem DataItem = new MsgServer.MsgGameItem();

            DataItem.ITEM_ID = ItemID;

            if (DataItem.Durability > 100)
            {
                DataItem.Durability = (ushort)Program.GetRandom.Next(100, DataItem.Durability / 10);
                DataItem.MaximDurability = DataItem.Durability;
            }

            else
            {
                DataItem.Durability = (ushort)Program.GetRandom.Next(1, 10);
                DataItem.MaximDurability = 10;
            }

            DataItem.Color = Role.Flags.Color.Red;
            if (typ == MsgFloorItem.MsgItem.ItemType.Item)
            {
                byte sockets = 0;
                bool lucky = false;
                if (DataItem.IsEquip)
                {
                    if (!special)
                    {

                        lucky = (ID_Quality > 7); // q>unique
                        if (!lucky)
                            lucky = (DataItem.Plus = Family.ItemGenerator.GeneratePurity()) != 0;

                        if (!lucky)
                            lucky = (DataItem.Bless = Family.ItemGenerator.GenerateBless()) != 0;

                        if (!lucky)
                        {
                            if (DataItem.IsWeapon)
                            {
                                sockets = Family.ItemGenerator.GenerateSocketCount(DataItem.ITEM_ID);

                                if (sockets >= 1)
                                    DataItem.SocketOne = Role.Flags.Gem.EmptySocket;
                                else if (sockets == 2)
                                    DataItem.SocketTwo = Role.Flags.Gem.EmptySocket;
                            }
                        }

                    }

                    if (DBItem != null)
                    {
                        DataItem.Durability = (ushort)Program.GetRandom.Next(1, DBItem.Durability / 10 + 10);
                        DataItem.MaximDurability = (ushort)Program.GetRandom.Next(DataItem.Durability, DBItem.Durability);

                    }
                    if (user != null && lucky)
                    {
                        if (user.Player.VipLevel == 6)
                        {
                            bool vaild = false;
                            if (user.Player.LootSocketedItems)
                                if (DataItem.SocketOne != Role.Flags.Gem.NoSocket || DataItem.SocketTwo != Role.Flags.Gem.NoSocket)
                                    vaild = true;
                            if (user.Player.LootBlessedItems)
                                if (DataItem.Bless > 0)
                                    vaild = true;
                            if (user.Player.LootPlusItems)
                                if (DataItem.Plus > 0)
                                    vaild = true;
                            if (user.Player.LootQualityItems)
                                if (ID_Quality > 7)
                                    vaild = true;
                            if (vaild)
                            {
                                if (user.Inventory.HaveSpace(2))
                                {
                                    user.Inventory.Add(stream, DataItem.ITEM_ID, 1, DataItem.Plus, DataItem.Bless, 0, DataItem.SocketOne, DataItem.SocketTwo, false);
                                    user.SendSysMesage($"[Auto Loot VIP] A monster you've killed just dropped a {Database.Server.ItemsBase.GetItemName(DataItem.ITEM_ID)}", MsgMessage.ChatMode.Action);
                                }
                                else
                                {
                                    user.SendSysMesage("[VIP] Please remove some items from inventory!");
                                    user.Player.AddMapEffect(stream, XX, YY, "accession3");
                                }
                                return;
                            }
                        }

                        else if (lucky && Role.Core.Rate(20) && user.Player.VipLevel == 0)//jason
                        {
                            user.SendSysMesage($"A monster you've killed just dropped a {Database.Server.ItemsBase.GetItemName(DataItem.ITEM_ID)}");
                        }
                    }
                }
                else
                {
                    if (DBItem != null)
                        DataItem.Durability = DataItem.MaximDurability = DBItem.Durability;
                }
            }
            if (user != null)
            {
                if (user != null)
                {
                    if (user.Player.VipLevel >= 5)
                    {
                        if (DataItem.ITEM_ID == Database.ItemType.DragonBall && user.Player.LootDragonBall)
                        {
                            if (user.Inventory.HaveSpace(1))
                            {
                                user.Inventory.Update(DataItem, Role.Instance.AddMode.ADD, stream);
                                if (user.Inventory.Contain(Database.ItemType.DragonBall, 10) && user.Player.VipLevel == 6)
                                {
                                    user.Inventory.Remove(Database.ItemType.DragonBall, 10, stream);
                                    user.Inventory.Add(stream, Database.ItemType.DragonBallScroll, 1);
                                }
                            }
                            else
                            {
                                user.SendSysMesage("[VIP] Please remove some items from your inventory!");
                                user.Player.AddMapEffect(stream, XX, YY, "accession3");
                            }
                            return;
                        }
                        else if (DataItem.ITEM_ID == Database.ItemType.Meteor && user.Player.LootMeteorItems)
                        {
                            if (user.Inventory.HaveSpace(1))
                            {
                                user.Inventory.Update(DataItem, Role.Instance.AddMode.ADD, stream);
                                if (user.Inventory.Contain(Database.ItemType.Meteor, 10) && user.Player.VipLevel == 6)
                                {
                                    user.Inventory.Remove(Database.ItemType.Meteor, 10, stream);
                                    user.Inventory.Add(stream, Database.ItemType.MeteorScroll, 1);
                                }
                            }
                            else
                            {
                                user.SendSysMesage("[VIP] Please remove some items from your inventory!");
                                user.Player.AddMapEffect(stream, XX, YY, "accession3");
                            }
                            return;
                        }
                    }
                }
            }
            if (DataItem.ITEM_ID == 730002)
            {
                DataItem.Plus = 2;
            }
            if (DataItem.ITEM_ID == 730003)
            {
                DataItem.Plus = 3;
            }
            MsgFloorItem.MsgItem DropItem = new MsgFloorItem.MsgItem(DataItem, XX, YY, typ, amount, DynamicID, Map, OwnerItem, true, map);
            if (map.EnqueueItem(DropItem))
            {
                DropItem.SendAll(stream, MsgFloorItem.MsgDropID.Visible);
            }
        }
        public void AddFadeAway(int time, Role.GameMap map)
        {
            if (!Alive)
            {
                Time32 timer = new Time32(time);
                if (timer > DeadStamp.AddSeconds(5))
                {
                    if (AddFlag(MsgServer.MsgUpdate.Flags.FadeAway, Role.StatusFlagsBigVector32.PermanentFlag, true))
                    {
                        FadeAway = timer;

                    }
                }
            }
        }
        public unsafe bool RemoveView(int time, Role.GameMap map)
        {
            if (ContainFlag(MsgServer.MsgUpdate.Flags.FadeAway) && State != MobStatus.Respawning)
            {
                Time32 timer = new Time32(time);
                if (timer > FadeAway.AddSeconds(3))
                {
                    using (var rec = new ServerSockets.RecycledPacket())
                    {
                        var stream = rec.GetStream();

                        ActionQuery action;

                        action = new ActionQuery()
                        {
                            ObjId = UID,
                            Type = ActionType.RemoveEntity
                        };

                        Send(stream.ActionCreate(&action));
                    }

                    State = MobStatus.Respawning;

                    map.View.MoveTo<Role.IMapObj>(this, RespawnX, RespawnY);

                    X = RespawnX;
                    Y = RespawnY;
                    Target = null;

                    return true;
                }
            }
            return false;
        }

        public void DropItemID(Client.GameClient killer, uint itemid, ServerSockets.Packet stream, byte range = 3, MsgItem.ItemType type = MsgItem.ItemType.Item, uint gold = 0)
        {
            MsgServer.MsgGameItem DataItem = new MsgServer.MsgGameItem();
            DataItem.ITEM_ID = itemid;
            Database.ItemType.DBItem DBItem;
            if (Database.Server.ItemsBase.TryGetValue(itemid, out DBItem))
            {
                DataItem.Durability = DataItem.MaximDurability = DBItem.Durability;
            }
            DataItem.Color = Role.Flags.Color.Red;
            ushort xx = X;
            ushort yy = Y;
            if (killer.Map.AddGroundItem(ref xx, ref yy, range))
            {
                MsgFloorItem.MsgItem DropItem = new MsgFloorItem.MsgItem(DataItem, xx, yy, type, gold, DynamicID, Map, killer.Player.UID, true, GMap);
                if (itemid == Database.ItemType.DragonBall || itemid == Database.ItemType.Meteor)
                {
                    var DBMap = Database.Server.ServerMaps[Map];
                    if (itemid == Database.ItemType.DragonBall)
                    {
                        ActionQuery action2 = new ActionQuery()
                        {
                            ObjId = killer.Player.UID,
                            Type = ActionType.DragonBall
                        };
                        if (killer.Player.VipLevel == 6)
                        {
                            if (killer.Inventory.HaveSpace(1))
                            {
                                killer.Inventory.Add(stream, Database.ItemType.DragonBall, 1);
                                killer.SendSysMesage("DragonBalls have been auto looted.");
                                if (killer.Inventory.Contain(Database.ItemType.DragonBall, 10))
                                {
                                    killer.Inventory.Remove(Database.ItemType.DragonBall, 10, stream);
                                    killer.Inventory.Add(stream, Database.ItemType.DragonBallScroll, 1);
                                    killer.SendSysMesage("DragonBalls have been auto packed.");
                                }
                                return;
                            }
                            else
                            {
                                killer.SendSysMesage("Inventory is full.");
                                return;
                            }
                        }
                        killer.Send(stream.ActionCreate(&action2));

                    }
                    else
                    {
                        killer.Player.AddMapEffect(stream, xx, yy, "ssch_wlhd_hit");
                    }
                    if (itemid == Database.ItemType.Meteor)
                    {
                        if (killer.Player.VipLevel == 6 && killer.Player.LootMeteorItems)
                        {
                            if (killer.Inventory.HaveSpace(1))
                            {
                                if (killer.Inventory.Contain(Database.ItemType.Meteor, 10))
                                {
                                    killer.Inventory.Remove(Database.ItemType.Meteor, 10, stream);
                                    killer.Inventory.Add(stream, Database.ItemType.MeteorScroll, 1);
                                    killer.SendSysMesage("[VIP] Meteors have been auto packed.");
                                }
                            }
                            else
                            {
                                killer.SendSysMesage("Inventory is full. Unable to loot the Meteor.");
                                return;
                            }
                        }
                    }
                }
                bool drop = true;
                if (DataItem.ITEM_ID == 730002)
                {
                    DataItem.Plus = 2;
                }
                if (DataItem.ITEM_ID == 730003)
                {
                    DataItem.Plus = 3;
                }
                if (killer.Map.EnqueueItem(DropItem) && drop)
                {
                    DropItem.SendAll(stream, MsgFloorItem.MsgDropID.Visible);
                }
            }
        }

        public MonsterFamily Family;
        public MonsterView View;
        public MobStatus State;
        public Role.Player Target = null;
        public Time32 AttackSpeed = new Time32();

        public Role.StatusFlagsBigVector32 BitVector;
        public void AddSpellFlag(Game.MsgServer.MsgUpdate.Flags Flag, int Seconds, bool RemoveOnDead, int Secondstamp = 0)
        {
            if (BitVector.ContainFlag((int)Flag))
                BitVector.TryRemove((int)Flag);
            AddFlag(Flag, Seconds, RemoveOnDead, Secondstamp);
        }
        public bool AddFlag(Game.MsgServer.MsgUpdate.Flags Flag, int Seconds, bool RemoveOnDead, int StampSeconds = 0)
        {
            if (!BitVector.ContainFlag((int)Flag))
            {
                BitVector.TryAdd((int)Flag, Seconds, RemoveOnDead, StampSeconds);
                UpdateFlagOffset();
                return true;
            }
            return false;
        }
        public bool RemoveFlag(Game.MsgServer.MsgUpdate.Flags Flag, Role.GameMap map)
        {
            if (BitVector.ContainFlag((int)Flag))
            {
                BitVector.TryRemove((int)Flag);
                UpdateFlagOffset();
                return true;
            }
            return false;
        }
        public bool UpdateFlag(Game.MsgServer.MsgUpdate.Flags Flag, int Seconds, bool SetNewTimer, int MaxTime)
        {
            return BitVector.UpdateFlag((int)Flag, Seconds, SetNewTimer, MaxTime);
        }
        public void ClearFlags(bool SendScreem = false)
        {
            BitVector.GetClear();
            UpdateFlagOffset(SendScreem);
        }
        public bool ContainFlag(Game.MsgServer.MsgUpdate.Flags Flag)
        {
            return BitVector.ContainFlag((int)Flag);
        }
        private unsafe void UpdateFlagOffset(bool SendScreem = true)
        {
            if (SendScreem)
                SendUpdate(BitVector.bits, Game.MsgServer.MsgUpdate.DataType.StatusFlag);
        }

        public byte OpenBoss = 0;
        public uint Map { get; set; }
        public uint DynamicID { get; set; }

        public int GetMyDistance(ushort X2, ushort Y2)
        {
            return Role.Core.GetDistance(X, Y, X2, Y2);
        }
        public int OldGetDistance(ushort X2, ushort Y2)
        {
            return Role.Core.GetDistance(PX, PY, X2, Y2);
        }
        public bool InView(ushort X2, ushort Y2, byte distance)
        {
            return (!(OldGetDistance(X2, Y2) < distance) && GetMyDistance(X2, Y2) < distance);
        }


        public unsafe void Send(ServerSockets.Packet msg)
        {
            View.SendScreen(msg, GMap);
            SendScores(msg); //for bosses need to recoded
        }
        public void UpdateMonsterView(Role.RoleView Target, ServerSockets.Packet stream)
        {
            foreach (var player in View.Roles(GMap, Role.MapObjectType.Player))
            {
                if (InView(player.X, player.Y, MonsterView.ViewThreshold))
                    player.Send(GetArray(stream, false));
            }
        }
        public bool UpdateMapCoords(ushort New_X, ushort New_Y, Role.GameMap _map)
        {
            if (!_map.IsFlagPresent(New_X, New_Y, Role.MapFlagType.Monster))
            {
                _map.SetMonsterOnTile(X, Y, false);
                _map.SetMonsterOnTile(New_X, New_Y, true);
                _map.View.MoveTo<MonsterRole>(this, New_X, New_Y);
                X = New_X;
                Y = New_Y;
                return true;
            }
            return false;
        }
        public Role.MapObjectType ObjType { get; set; }

        public byte Boss = 0;
        public uint Mesh = 0;
        public uint UID { get; set; }
        public byte Level = 0;
        public uint HitPoints;

        public ushort RespawnX;
        public ushort RespawnY;


        public ushort PX = 0;
        public ushort PY = 0;
        public ushort _xx;
        public ushort _yy;

        public ushort X { get { return _xx; } set { PX = _xx; _xx = value; } }
        public ushort Y { get { return _yy; } set { PY = _yy; _yy = value; } }
        public Role.Flags.ConquerAction Action = Role.Flags.ConquerAction.None;
        public Role.Flags.ConquerAngle Facing = Role.Flags.ConquerAngle.East;
        public string LocationSpawn = "";
        public Role.GameMap GMap;
        public bool RemoveOnDead = false;
        public uint PetFlag = 0;


        public unsafe void SendString(ServerSockets.Packet stream, Game.MsgServer.MsgStringPacket.StringID id, params string[] args)
        {
            Game.MsgServer.MsgStringPacket packet = new Game.MsgServer.MsgStringPacket();
            packet.ID = id;
            packet.UID = UID;

            packet.Strings = args;
            Send(stream.StringPacketCreate(packet));
        }
        public MonsterRole(MonsterFamily Famil, uint _UID, string locationspawn, Role.GameMap _map)
        {
            AllowDynamic = false;
            GMap = _map;
            LocationSpawn = locationspawn;
            ObjType = Role.MapObjectType.Monster;
            Name = Famil.Name;
            Family = Famil;
            UID = _UID;
            Mesh = Famil.Mesh;
            Level = (byte)Famil.Level;
            HitPoints = (uint)Famil.MaxHealth;
            View = new MonsterView(this);
            State = MobStatus.Idle;
            BitVector = new Role.StatusFlagsBigVector32(32 * 1);//5 -- 1
            Boss = Family.Boss;
            Facing = (Role.Flags.ConquerAngle)Program.GetRandom.Next(0, 8);

        }
        public bool Alive { get { return HitPoints > 0; } }
        public unsafe ServerSockets.Packet GetArray(ServerSockets.Packet stream, bool view)
        {
            if (IsFloor && Mesh != 980)
            {
                return stream.ItemPacketCreate(this.FloorPacket);

            }
            stream.InitWriter();
            stream.Write(Mesh);
            stream.Write(UID);//8

            stream.Write(0);
            for (uint x = 0; x < BitVector.bits.Length; x++)
                stream.Write((uint)BitVector.bits[x]);//16 -- 18

            stream.ZeroFill(28);//12 -- 46
            stream.Write((ushort)HitPoints);//48
            stream.Write((ushort)Level);//50
            stream.ZeroFill(2);

            stream.Write(X);
            stream.Write(Y);//60

            //stream.Write((ushort)0);
            stream.Write((byte)Facing);
            stream.Write((byte)Action);//59

            stream.ZeroFill(2);//reborn 68
            //  stream.ZeroFill(1+3);//reborn 68

            stream.Write((byte)Level);//62

            //stream.ZeroFill(89);//86

            //stream.Write((byte)Boss);//175 
            stream.ZeroFill(32);//138
            stream.Write(Name);
            stream.Finalize(Game.GamePackets.SpawnPlayer);
            //    MyConsole.PrintPacketAdvanced(stream.Memory, stream.Size);

            return stream;
        }
        public unsafe void SendUpdate(uint[] Value, Game.MsgServer.MsgUpdate.DataType datatype)
        {
            using (var rec = new ServerSockets.RecycledPacket())
            {
                var stream = rec.GetStream();

                Game.MsgServer.MsgUpdate packet = new Game.MsgServer.MsgUpdate(stream, UID, 1);
                stream = packet.Append(stream, datatype, Value);
                stream = packet.GetArray(stream);
                Send(stream);
            }
        }
        DateTime LastScore;
        internal unsafe void SendScores(ServerSockets.Packet stream)
        {
            if (DateTime.Now < LastScore.AddSeconds(2))
                return;
            LastScore = DateTime.Now;
            if (ConfirmBoss())
            {
                View.SendScreen(new MsgMessage("Top 5 Highest Damage : " + Name + ".", MsgMessage.MsgColor.red, MsgMessage.ChatMode.FirstRightCorner).GetArray(stream), GMap);
                int counter = 1;
                foreach (var player in Scores.OrderByDescending(e => e.Value.ScoreDmg).Take(5))
                {
                    View.SendScreen(new MsgMessage("            " + counter++ + ": " + player.Value.Name + " - " + player.Value.ScoreDmg, MsgMessage.MsgColor.red, MsgMessage.ChatMode.ContinueRightCorner).GetArray(stream), GMap);
                }
            }
        }
        public bool ConfirmBoss()
        {
            if (Family.ID == 20070 ||
                 Family.ID == 20060 ||
                 Family.ID == 20160 ||
                 Family.ID == 3821 ||
                 Family.ID == 3822 ||
                 Family.ID == 6643 ||
                 Family.ID == 20300 ||
                 Family.ID == 20055) return true;
            return false;
        }
        internal unsafe void DistributeBossPoints()
        {
            if (ConfirmBoss())
            {
                var scores = Scores.OrderByDescending(e => e.Value.ScoreDmg).Take(1).FirstOrDefault();
                if (scores.Value == null) return;
                MsgSchedules.SendSysMesage("Player " + scores.Value.Name + " has made the most damage on the " + Name + " and gained 1 Boss Point.", MsgServer.MsgMessage.ChatMode.Center, MsgServer.MsgMessage.MsgColor.white);
                GameClient player;
                if (Server.GamePoll.TryGetValue(scores.Key, out player))
                {
                    //player.Player.BossPoints += 1;
                    player.SendSysMesage("You've received 1 Boss Point!", MsgMessage.ChatMode.TopLeft);
                }
            }
        }
        internal unsafe void UpdateScores(Role.Player player, uint p)
        {
            if (ConfirmBoss())
            {
                if (!Scores.ContainsKey(player.UID))
                    Scores.Add(player.UID, new ScoreBoard() { Name = player.Name, ScoreDmg = p });
                else Scores[player.UID].ScoreDmg += p;
            }
        }
    }
}
