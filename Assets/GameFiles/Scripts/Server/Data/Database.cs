using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.Data.Sqlite;
using System.IO;
using System;
using Larnix.Socket.Commands;
using Larnix.Files;
using Larnix.Entities;

namespace Larnix.Server.Data
{
    public class Database : IDisposable
    {
        private SqliteConnection connection = null;
        private SqliteTransaction transaction = null;
        private static bool initialized = false;

        public Database(string path, string filename)
        {
            if(!initialized)
            {
                SQLitePCL.Batteries_V2.Init();
                initialized = true;
            }

            FileManager.EnsureDirectory(path);
            connection = new SqliteConnection($"Data Source={Path.Combine(path, filename)};Pooling=False");
            connection.Open();

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS users(
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    username TEXT NOT NULL UNIQUE,
                    password_hash TEXT NOT NULL,
                    password_index INTEGER NOT NULL DEFAULT 1
                );

                CREATE TABLE IF NOT EXISTS entities(
                    uid INTEGER PRIMARY KEY,
                    type INTEGER,
                    chunk_x INTEGER,
                    chunk_Y INTEGER,
                    pos_x REAL,
                    pos_y REAL,
                    rotation REAL,
                    nbt TEXT NOT NULL
                );

                CREATE INDEX IF NOT EXISTS idx_chunk_entities
                    ON entities(chunk_x, chunk_y);

                CREATE TABLE IF NOT EXISTS chunks(
                    chunk_x INTEGER,
                    chunk_y INTEGER,
                    block_bytes BLOB,
                    PRIMARY KEY(chunk_x, chunk_y)
                );";
                cmd.ExecuteNonQuery();
            }
        }

        public byte GetPasswordIndex(string username)
        {
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "SELECT password_index FROM users WHERE username = $username;";
                cmd.Parameters.AddWithValue("$username", username);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        long value = reader.GetInt64(0);
                        if (value < 0 || value > 255)
                            throw new InvalidOperationException("Wrong value inside 'password_index' column.");
                        else
                            return (byte)value;
                    }
                }
            }
            return 0;
        }

        public bool AllowUser(string username, string password, bool can_register = true)
        {
            string password_hash = null;

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "SELECT password_hash FROM users WHERE username = $username;";
                cmd.Parameters.AddWithValue("$username", username);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                        password_hash = reader.GetString(0);
                }
            }

            if (password_hash != null) // user found, check hash
            {
                return Hasher.VerifyPassword(password, password_hash);
            }
            else // user not found
            {
                if(!can_register)
                    return false;

                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "INSERT INTO users (username, password_hash) VALUES ($username, $password_hash);";
                    cmd.Parameters.AddWithValue("$username", username);
                    cmd.Parameters.AddWithValue("$password_hash", Hasher.HashPassword(password));
                    cmd.ExecuteNonQuery();
                }
                return true;
            }
        }

        public A_PasswordChange.ResultType ChangePassword(string username, string oldPassword, string newPassword)
        {
            string password_hash = null;

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "SELECT password_hash FROM users WHERE username = $username;";
                cmd.Parameters.AddWithValue("$username", username);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                        password_hash = reader.GetString(0);
                }
            }

            if (password_hash == null)
                return A_PasswordChange.ResultType.WrongUser;

            if (!Hasher.VerifyPassword(oldPassword, password_hash))
                return A_PasswordChange.ResultType.WrongPassword;

            byte password_index = GetPasswordIndex(username);
            password_index = (byte)(password_index != 255 ? password_index + 1 : 1);

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = @"
                    UPDATE users SET
                    password_hash = $new_password_hash,
                    password_index = $new_password_index
                    WHERE username = $username;";
                cmd.Parameters.AddWithValue("$username", username);
                cmd.Parameters.AddWithValue("$new_password_hash", Hasher.HashPassword(newPassword));
                cmd.Parameters.AddWithValue("$new_password_index", password_index);
                cmd.ExecuteNonQuery();
            }

            return A_PasswordChange.ResultType.Success;
        }

        public bool DeleteUser(string username, string password)
        {
            if (!AllowUser(username, password, false))
                return false;

            using(var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM users WHERE username = $username;";
                cmd.Parameters.AddWithValue("$username", username);
                cmd.ExecuteNonQuery();
            }

            return true;
        }

        public long GetUserID(string username)
        {
            using(var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "SELECT id FROM users WHERE username = $username;";
                cmd.Parameters.AddWithValue("$username", username);
                
                using(var reader = cmd.ExecuteReader())
                {
                    if(reader.Read())
                        return reader.GetInt64(0);
                }
            }
            throw new Exception("User doesn't exist in the database!");
        }

        public EntityData FindEntity(ulong uid)
        {
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM entities WHERE uid = $uid;";
                cmd.Parameters.AddWithValue("$uid", (long)uid);

                using(var reader = cmd.ExecuteReader())
                {
                    if(reader.Read())
                    {
                        EntityData entityData = new EntityData
                        {
                            ID = (EntityData.EntityID)(long)reader["type"],
                            Position = new Vector2((float)(double)reader["pos_x"], (float)(double)reader["pos_y"]),
                            Rotation = (float)(double)reader["rotation"],
                            NBT = reader["nbt"] as string
                        };
                        return entityData;
                    }
                }
            }
            return null;
        }

        public Dictionary<ulong, EntityData> GetEntitiesByChunk(int[] chunkCoords)
        {
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM entities WHERE chunk_x = $chunk_x AND chunk_y = $chunk_y AND type <> " + (long)EntityData.EntityID.Player + ";";
                cmd.Parameters.AddWithValue("$chunk_x", (long)chunkCoords[0]);
                cmd.Parameters.AddWithValue("$chunk_y", (long)chunkCoords[1]);

                using (var reader = cmd.ExecuteReader())
                {
                    Dictionary<ulong, EntityData> returns = new Dictionary<ulong, EntityData>();
                    while (reader.Read())
                    {
                        returns.Add((ulong)(long)reader["uid"], new EntityData
                        {
                            ID = (EntityData.EntityID)(long)reader["type"],
                            Position = new Vector2((float)(double)reader["pos_x"], (float)(double)reader["pos_y"]),
                            Rotation = (float)(double)reader["rotation"],
                            NBT = reader["nbt"] as string
                        });
                    }
                    return returns;
                }
            }
        }

        public void FlushEntities(Dictionary<ulong, EntityData> entities) // USE ONLY INSIDE TRANSACTION!
        {
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = @"
                    INSERT OR REPLACE INTO entities
                    (uid, type, chunk_x, chunk_y, pos_x, pos_y, rotation, nbt) VALUES
                    ($uid, $type, $chunk_x, $chunk_y, $pos_x, $pos_y, $rotation, $nbt);";

                var paramUid = cmd.Parameters.Add("$uid", SqliteType.Integer);
                var paramType = cmd.Parameters.Add("$type", SqliteType.Integer);
                var paramChunkX = cmd.Parameters.Add("$chunk_x", SqliteType.Integer);
                var paramChunkY = cmd.Parameters.Add("$chunk_y", SqliteType.Integer);
                var paramPosX = cmd.Parameters.Add("$pos_x", SqliteType.Real);
                var paramPosY = cmd.Parameters.Add("$pos_y", SqliteType.Real);
                var paramRotation = cmd.Parameters.Add("$rotation", SqliteType.Real);
                var paramNbt = cmd.Parameters.Add("$nbt", SqliteType.Text);

                foreach (var vkp in entities)
                {
                    EntityData entity = vkp.Value;
                    int[] chunkCoords = Common.CoordsToChunk(entity.Position);

                    paramUid.Value = (long)vkp.Key;
                    paramType.Value = (long)entity.ID;
                    paramChunkX.Value = (long)chunkCoords[0];
                    paramChunkY.Value = (long)chunkCoords[1];
                    paramPosX.Value = entity.Position.x;
                    paramPosY.Value = entity.Position.y;
                    paramRotation.Value = entity.Rotation;
                    paramNbt.Value = entity.NBT;
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public bool DeleteEntities(List<ulong> uids)
        {
            if (uids == null || uids.Count == 0)
                return false;

            const int batchSize = 500;
            bool anyDeleted = false;

            for (int offset = 0; offset < uids.Count; offset += batchSize)
            {
                var batch = uids.GetRange(offset, Math.Min(batchSize, uids.Count - offset));
                var parameterNames = new List<string>();

                using (var cmd = connection.CreateCommand())
                {
                    for (int i = 0; i < batch.Count; i++)
                    {
                        string paramName = $"$id{i}";
                        cmd.Parameters.AddWithValue(paramName, (long)batch[i]);
                        parameterNames.Add(paramName);
                    }

                    string sql = $"DELETE FROM entities WHERE uid IN ({string.Join(", ", parameterNames)});";
                    cmd.CommandText = sql;

                    anyDeleted = anyDeleted || cmd.ExecuteNonQuery() > 0;
                }
            }

            return anyDeleted;
        }

        public void SetChunk(int x, int y, byte[] block_bytes)
        {
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "INSERT OR REPLACE INTO chunks (x, y, block_bytes) VALUES ($x, $y, $block_bytes);";
                cmd.Parameters.AddWithValue("$x", x);
                cmd.Parameters.AddWithValue("$y", y);
                cmd.Parameters.AddWithValue("$block_bytes", block_bytes);
                cmd.ExecuteNonQuery();
            }
        }

        public byte[] GetChunk(int x, int y)
        {
            using(var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "SELECT block_bytes FROM chunks WHERE x = $x AND y = $y;";
                cmd.Parameters.AddWithValue("$x", x);
                cmd.Parameters.AddWithValue("$y", y);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                        return (byte[])reader["block_bytes"];
                }
            }
            return null;
        }

        public void BeginTransaction()
        {
            if (transaction != null)
                throw new InvalidOperationException("Transaction already in progress.");

            transaction = connection.BeginTransaction();
        }

        public void CommitTransaction()
        {
            if (transaction == null)
                throw new InvalidOperationException("No active transaction.");

            transaction.Commit();
            transaction.Dispose();
            transaction = null;
        }

        public void RollbackTransaction()
        {
            if (transaction == null)
                throw new InvalidOperationException("No active transaction.");

            transaction.Rollback();
            transaction.Dispose();
            transaction = null;
        }

        public void Dispose()
        {
            if(connection != null)
            {
                connection.Close();
                connection.Dispose();
                connection = null;
            }
            if (transaction != null)
            {
                RollbackTransaction();
            }
        }
    }
}
