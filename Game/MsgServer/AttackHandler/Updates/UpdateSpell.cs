using COServer.Game.MsgServer.AttackHandler.Calculate;
using System;
using System.Collections.Generic;
using static COServer.Game.MsgServer.AttackHandler.Calculate.Base;

namespace COServer.Game.MsgServer.AttackHandler.Updates
{
    public class UpdateSpell
    {
        public unsafe static void CheckUpdate(ServerSockets.Packet stream, Client.GameClient client, InteractQuery Attack, uint Damage, Dictionary<ushort, Database.MagicType.Magic> DBSpells)
        {
            if (Damage == 0)
                return;
            if (Attack.SpellID == 30000)
                return;
            if (DBSpells != null)
            {
                MsgSpell ClientSpell;
                if (client.MySpells.ClientSpells.TryGetValue(Attack.SpellID, out ClientSpell))
                {
                    ushort firstlevel = ClientSpell.Level;
                    if (ClientSpell.Level < DBSpells.Count - 1)
                    {//coped from EO SOURCE
                     // Damage = 10;
                        if (client.GemValues(Role.Flags.Gem.NormalMoonGem) > 0)
                        {
                            Damage += Damage * client.GemValues(Role.Flags.Gem.NormalMoonGem) / 100;
                        }
                        switch (ClientSpell.ID)
                        {
                            //case (ushort)Role.Flags.SpellID.Thunder:
                            // case (ushort)Role.Flags.SpellID.Fire:
                            // case (ushort)Role.Flags.SpellID.FastBlader:
                            // case (ushort)Role.Flags.SpellID.ScrenSword:
                            // case (ushort)Role.Flags.SpellID.Hercules:
                            // case (ushort)Role.Flags.SpellID.Snow:
                            // case (ushort)Role.Flags.SpellID.ViperFang:
                            // case (ushort)Role.Flags.SpellID.ScatterFire:
                            // case (ushort)Role.Flags.SpellID.Rage:
                            //     Damage = 100;//200
                            //     break;
                            case (ushort)Role.Flags.SpellID.Tornado:
                                Damage *= 100;
                                break;
                            case (ushort)Role.Flags.SpellID.Meditation:
                                // case (ushort)Role.Flags.SpellID.FireCircle:
                                // case (ushort)Role.Flags.SpellID.FireofHell:
                                Damage += 1000;
                                break;
                            case (ushort)Role.Flags.SpellID.Stigma:
                            case (ushort)Role.Flags.SpellID.DivineHare:
                            case (ushort)Role.Flags.SpellID.Penetration:
                            case (ushort)Role.Flags.SpellID.Phoenix:
                            case (ushort)Role.Flags.SpellID.Intensify:
                            //case (ushort)Role.Flags.SpellID.RapidFire:
                            case (ushort)Role.Flags.SpellID.Golem:
                            case (ushort)Role.Flags.SpellID.NightDevil:
                            case (ushort)Role.Flags.SpellID.WaterElf:
                            case (ushort)Role.Flags.SpellID.SummonGuard:
                                Damage = 1;
                                break;

                            default:
                                Damage /= 3;
                                break;
                        }

                        if (client.Player.Level >= DBSpells[ClientSpell.Level].NeedLevel)
                        {
                            ClientSpell.Experience += (int)(Damage * Program.ServerConfig.ExpRateSpell);
                            if (ClientSpell.Experience > DBSpells[ClientSpell.Level].Experience)
                            {
                                ClientSpell.PreviousLevel = (byte)ClientSpell.Level;
                                ClientSpell.Level++;
                                ClientSpell.Experience = 0;
                            }
                            if (ClientSpell.PreviousLevel != 0 && ClientSpell.PreviousLevel >= ClientSpell.Level)
                            {
                                ClientSpell.Level = ClientSpell.PreviousLevel;
                            }
                            try
                            {
                                if (ClientSpell.Level > firstlevel)
                                    client.SendSysMesage("You have just leveled your skill " + DBSpells[0].Name + ".", MsgMessage.ChatMode.System);
                            }
                            catch (Exception e) { Console.WriteLine(e.ToString()); }
                            client.Send(stream.SpellCreate(ClientSpell));
                        }

                    }
                }
            }
            else if (Attack.AtkType == MsgAttackPacket.AttackID.Physical || Attack.AtkType == MsgAttackPacket.AttackID.Archer || Attack.AtkType == MsgAttackPacket.AttackID.Magic)
            {
                uint ProfRightWeapon = client.Equipment.RightWeapon / 1000;
                uint PorfLeftWeapon = client.Equipment.LeftWeapon / 1000;
                if (ProfRightWeapon != 0)
                    client.MyProfs.CheckUpdate(ProfRightWeapon, Damage, stream);

                if (PorfLeftWeapon != 0)
                    client.MyProfs.CheckUpdate(PorfLeftWeapon, Damage / 2, stream);


                //if (Database.ItemType.IsArrow(client.Equipment.LeftWeapon))
                //    client.MyProfs.CheckUpdate(PorfLeftWeapon, Damage, stream);

            }
        }
    }
}
