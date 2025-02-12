﻿using System;
using MySql.Data.MySqlClient;
using COServer.Game.Models;

namespace COServer.Game.Data
{
    internal class VoteRepository
    {
        public const string ConnectionString = "Server=localhost;Uid=root;Password=123456789;Database=zq;";

        // Retorna o último voto do jogador
        public static VotesModels GetLastVote(string playerName)
        {
            string query = "SELECT * FROM votesystem WHERE Id = @Id LIMIT 1;";
            using (var connection = new MySqlConnection(ConnectionString))
            {
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", playerName.GetHashCode());

                    connection.Open();
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new VotesModels
                            {
                                Id = reader.GetInt32("Id"),
                                Ip = reader.GetString("Ip"),
                                Timestamp = reader.GetDateTime("Timestamp"),
                                VotePoints = reader.GetInt32("VotePoints")
                            };
                        }
                    }
                }
            }
            return null; // Retorna null se não encontrar um voto
        }

        // Atualiza o voto do jogador (reinicia o timer e soma os pontos)
        public static void UpdateVote(string playerName)
        {
            string query = "UPDATE votesystem SET Timestamp = NOW(), VotePoints = VotePoints + 1 WHERE Id = @Id;";
            using (var connection = new MySqlConnection(ConnectionString))
            {
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", playerName.GetHashCode());

                    connection.Open();
                    command.ExecuteNonQuery();
                }
            }
        }

        // Insere um novo voto no banco
        public static void InsertVoteDb(VotesModels item)
        {
            string query = @"
                INSERT INTO votesystem (Id, Ip, Timestamp, VotePoints) 
                VALUES (@Id, @Ip, @Timestamp, 1)
                ON DUPLICATE KEY UPDATE 
                    VotePoints = VotePoints + 1,
                    Timestamp = VALUES(Timestamp);";

            using (var connection = new MySqlConnection(ConnectionString))
            {
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", item.Id);
                    command.Parameters.AddWithValue("@Ip", item.Ip);
                    command.Parameters.AddWithValue("@Timestamp", item.Timestamp);

                    connection.Open();
                    command.ExecuteNonQuery();
                }
            }
        }


        public static DateTime? GetLastVoteTime(string playerName)
        {
            string query = "SELECT Timestamp FROM votesystem WHERE Id = @Name;";

            using (var connection = new MySqlConnection(ConnectionString))
            {
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Name", playerName);
                    connection.Open();
                    var result = command.ExecuteScalar();
                    return result != null ? (DateTime?)Convert.ToDateTime(result) : null;
                }
            }
        }
    }

}