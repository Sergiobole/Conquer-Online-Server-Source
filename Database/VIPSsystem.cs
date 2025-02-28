using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace COServer.Database
{
    public class VIPSystem
    {
        private static readonly string ConnectionString = "Server=localhost;Uid=root;Password=123456789;Database=zq;";
        public class User
        {
            public uint UID;
            public string IP;
            public uint CanClaimFreeVip;
            public override string ToString()
            {
                var writer = new DBActions.WriteLine('/');
                writer.Add(UID).Add(IP).Add(CanClaimFreeVip);
                return writer.Close();
            }
        }

        private static List<User> UsersPoll = new List<User>();

        public static bool HasClaimedFreeVip(string ip)
        {
            using (var conn = new MySqlConnection(ConnectionString))
            {
                conn.Open();
                string query = "SELECT 1 FROM vip_claims WHERE ip = @ip LIMIT 1";
                // Usando o nome totalmente qualificado para evitar conflitos
                using (var cmd = new MySql.Data.MySqlClient.MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ip", ip);
                    using (var reader = cmd.ExecuteReader())
                    {
                        return reader.HasRows;
                    }
                }
            }
        }

        public static void SaveVipClaim(uint playerId, string ip)
        {
            using (var conn = new MySqlConnection(ConnectionString))
            {
                conn.Open();
                string query = "INSERT INTO vip_claims (player_id, ip) VALUES (@playerId, @ip)";
                using (var cmd = new MySql.Data.MySqlClient.MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@playerId", playerId);
                    cmd.Parameters.AddWithValue("@ip", ip);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static bool TryGetObject(uint UID, string IP, out User obj)
        {
            foreach (var _obj in UsersPoll)
            {
                if (_obj.UID == UID || _obj.IP == IP)
                {
                    obj = _obj;
                    return true;
                }
            }
            obj = null;
            return false;
        }
        public static bool CanClaimVIP(Client.GameClient client)
        {
            User _user;
            if (TryGetObject(client.Player.UID, client.Socket.RemoteIp, out _user))
            {
                if (_user.CanClaimFreeVip == 1)
                    return true;
            }
            return false;
        }
        public static void CheckUp(Client.GameClient client)
        {
            // Exemplo de verificação extra, caso necessário
            string clientIP = client.Socket.RemoteIp;
            if (!HasClaimedFreeVip(clientIP) && client.Player.VipLevel == 0)
            {
                client.Player.ExpireVip = DateTime.Now.AddDays(7);
                client.Player.VipLevel = 6;
                SaveVipClaim(client.Player.UID, clientIP);

                using (var rec = new ServerSockets.RecycledPacket())
                {
                    var stream = rec.GetStream();
                    client.Player.SendUpdate(stream, client.Player.VipLevel, Game.MsgServer.MsgUpdate.DataType.VIPLevel);
                    client.Player.UpdateVip(stream);
                }
                client.Player.CanClaimFreeVip = true;
                client.SendSysMesage("You`ve received free VIP 6 (7 days).");

            }
            else
            {
                client.SendSysMesage("You`ve already received the VIP on another account!");
            }
        }
        public static void Save()
        {
            using (Database.DBActions.Write _wr = new Database.DBActions.Write("VIP.txt"))
            {
                foreach (var _obj in UsersPoll)
                    _wr.Add(_obj.ToString());
                _wr.Execute(DBActions.Mode.Open);
            }
        }
        public static void Load()
        {
            using (Database.DBActions.Read r = new Database.DBActions.Read("VIP.txt"))
            {
                if (r.Reader())
                {
                    int count = r.Count;
                    for (uint x = 0; x < count; x++)
                    {
                        Database.DBActions.ReadLine reader = new DBActions.ReadLine(r.ReadString(""), '/');
                        User user = new User();
                        user.UID = reader.Read((uint)0);
                        user.IP = reader.Read("");
                        user.CanClaimFreeVip = reader.Read((uint)0);
                        UsersPoll.Add(user);
                    }
                }
            }

        }

    }
}
