using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.Data.Sqlite;
using System.IO;
using System;
using Larnix.Socket.Commands;
using Larnix.Files;
using Larnix.Entities;
using Larnix.Server.Terrain;
using Larnix.Blocks;
using System.Security.Cryptography;

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

            using (var cmd = CreateCommand())
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
                );

                CREATE TABLE IF NOT EXISTS seed (
                    value INTEGER
                );";
                cmd.ExecuteNonQuery();
            }
        }

        public bool UserExists(string username)
        {
            return GetPasswordIndex(username) != 0;
        }

        public void AddUser(string username, string password_hash, byte password_index = 1)
        {
            using(var cmd = CreateCommand())
            {
                cmd.CommandText = @"
                    INSERT INTO users
                    (username, password_hash, password_index) VALUES
                    ($username, $password_hash, $password_index);";
                cmd.Parameters.AddWithValue("$username", username);
                cmd.Parameters.AddWithValue("$password_hash", password_hash);
                cmd.Parameters.AddWithValue("$password_index", password_index);
                cmd.ExecuteNonQuery();
            }
        }

        public void DeleteUser(string username)
        {
            using(var cmd = CreateCommand())
            {
                cmd.CommandText = "DELETE FROM users WHERE username = $username;";
                cmd.Parameters.AddWithValue("$username", username);
                cmd.ExecuteNonQuery();
            }
        }

        public void ChangePassword(string username, string new_password_hash)
        {
            using (var cmd = CreateCommand())
            {
                cmd.CommandText = @"
                    UPDATE users
                    SET password_hash = $new_password_hash
                    WHERE username = $username;";

                cmd.Parameters.AddWithValue("$new_password_hash", new_password_hash);
                cmd.Parameters.AddWithValue("$username", username);
                cmd.ExecuteNonQuery();
            }
        }

        public string GetPasswordHash(string username)
        {
            using (var cmd = CreateCommand())
            {
                cmd.CommandText = "SELECT password_hash FROM users WHERE username = $username;";
                cmd.Parameters.AddWithValue("$username", username);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                        return (string)reader["password_hash"];
                }
            }
            return null;
        }

        public long GetPasswordIndex(string username) // 0 -> user doesn't exist
        {
            using (var cmd = CreateCommand())
            {
                cmd.CommandText = "SELECT password_index FROM users WHERE username = $username;";
                cmd.Parameters.AddWithValue("$username", username);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        long password_index = reader.GetInt64(0);
                        return password_index;
                    }
                }
            }
            return 0;
        }

        public void IncrementPasswordIndex(string username)
        {
            using (var cmd = CreateCommand())
            {
                cmd.CommandText = "UPDATE users SET password_index = password_index + 1 WHERE username = $username;";
                cmd.Parameters.AddWithValue("$username", username);
                cmd.ExecuteNonQuery();
            }
        }

        public long GetUserID(string username)
        {
            using (var cmd = CreateCommand())
            {
                cmd.CommandText = "SELECT id FROM users WHERE username = $username;";
                cmd.Parameters.AddWithValue("$username", username);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                        return reader.GetInt64(0);
                }
            }
            throw new Exception("User doesn't exist in the database!");
        }

        public long GetMinUID()
        {
            using (var cmd = CreateCommand())
            {
                cmd.CommandText = "SELECT IFNULL(MIN(uid), 0) FROM entities;";
                long min_uid = (long)cmd.ExecuteScalar();
                return min_uid > 0 ? 0 : min_uid;
            }
        }

        public EntityData FindEntity(ulong uid)
        {
            using (var cmd = CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM entities WHERE uid = $uid;";
                cmd.Parameters.AddWithValue("$uid", (long)uid);

                using(var reader = cmd.ExecuteReader())
                {
                    if(reader.Read())
                    {
                        EntityData entityData = new EntityData
                        {
                            ID = (EntityID)(long)reader["type"],
                            Position = new Vector2((float)(double)reader["pos_x"], (float)(double)reader["pos_y"]),
                            Rotation = (float)(double)reader["rotation"],
                            NBT = (string)reader["nbt"]
                        };
                        return entityData;
                    }
                }
            }
            return null;
        }

        public Dictionary<ulong, EntityData> GetEntitiesByChunkNoPlayers(Vector2Int chunkCoords)
        {
            using (var cmd = CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM entities WHERE chunk_x = $chunk_x AND chunk_y = $chunk_y AND type <> " + (long)EntityID.Player + ";";
                cmd.Parameters.AddWithValue("$chunk_x", (long)chunkCoords.x);
                cmd.Parameters.AddWithValue("$chunk_y", (long)chunkCoords.y);

                using (var reader = cmd.ExecuteReader())
                {
                    Dictionary<ulong, EntityData> returns = new Dictionary<ulong, EntityData>();
                    while (reader.Read())
                    {
                        returns.Add((ulong)(long)reader["uid"], new EntityData
                        {
                            ID = (EntityID)(long)reader["type"],
                            Position = new Vector2((float)(double)reader["pos_x"], (float)(double)reader["pos_y"]),
                            Rotation = (float)(double)reader["rotation"],
                            NBT = reader["nbt"] as string
                        });
                    }
                    return returns;
                }
            }
        }

        public void FlushEntities(Dictionary<ulong, EntityData> entities)
        {
            if (transaction == null)
                throw new InvalidOperationException("Active transaction is needed for this method!");

            using (var cmd = CreateCommand())
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
                    Vector2Int chunkCoords = ChunkMethods.CoordsToChunk(entity.Position);

                    paramUid.Value = (long)vkp.Key;
                    paramType.Value = (long)entity.ID;
                    paramChunkX.Value = (long)chunkCoords.x;
                    paramChunkY.Value = (long)chunkCoords.y;
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
            if (transaction == null)
                throw new InvalidOperationException("Active transaction is needed for this method!");

            if (uids == null || uids.Count == 0)
                return false;

            const int batchSize = 500;
            bool anyDeleted = false;

            for (int offset = 0; offset < uids.Count; offset += batchSize)
            {
                var batch = uids.GetRange(offset, Math.Min(batchSize, uids.Count - offset));
                var parameterNames = new List<string>();

                using (var cmd = CreateCommand())
                {
                    for (int i = 0; i < batch.Count; i++)
                    {
                        string paramName = $"$id{i}";
                        cmd.Parameters.AddWithValue(paramName, (long)batch[i]);
                        parameterNames.Add(paramName);
                    }

                    string sql = $"DELETE FROM entities WHERE uid IN ({string.Join(", ", parameterNames)});";
                    cmd.CommandText = sql;

                    bool deleted = cmd.ExecuteNonQuery() > 0;
                    anyDeleted = anyDeleted || deleted;
                }
            }

            return anyDeleted;
        }

        public long GetSeed(long? suggestion = null)
        {
            bool has_seed;
            long seed;

            using (var cmd = CreateCommand())
            {
                cmd.CommandText = "SELECT COUNT(value) FROM seed;";
                has_seed = (long)cmd.ExecuteScalar() > 0;
            }

            if(has_seed)
            {
                using (var cmd = CreateCommand())
                {
                    cmd.CommandText = "SELECT value FROM seed;";
                    seed = (long)cmd.ExecuteScalar();
                }
            }
            else
            {
                if (suggestion == null)
                {
                    byte[] bytes = new byte[8];
                    RandomNumberGenerator.Fill(bytes);
                    seed = BitConverter.ToInt64(bytes, 0);
                }
                else
                {
                    seed = (long)suggestion;
                }

                using (var cmd = CreateCommand())
                {
                    cmd.CommandText = "INSERT INTO seed (value) VALUES ($seed);";
                    cmd.Parameters.AddWithValue("$seed", seed);
                    cmd.ExecuteNonQuery();
                }
            }

            return seed;
        }

        public void SetChunk(int x, int y, byte[] block_bytes)
        {
            using (var cmd = CreateCommand())
            {
                cmd.CommandText = "INSERT OR REPLACE INTO chunks (chunk_x, chunk_y, block_bytes) VALUES ($chunk_x, $chunk_y, $block_bytes);";
                cmd.Parameters.AddWithValue("$chunk_x", x);
                cmd.Parameters.AddWithValue("$chunk_y", y);
                cmd.Parameters.AddWithValue("$block_bytes", block_bytes);
                cmd.ExecuteNonQuery();
            }
        }

        public bool TryGetChunk(int x, int y, out byte[] block_bytes)
        {
            using(var cmd = CreateCommand())
            {
                cmd.CommandText = "SELECT block_bytes FROM chunks WHERE chunk_x = $chunk_x AND chunk_y = $chunk_y;";
                cmd.Parameters.AddWithValue("$chunk_x", x);
                cmd.Parameters.AddWithValue("$chunk_y", y);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        block_bytes = (byte[])reader["block_bytes"];
                        return true;
                    }
                }
            }
            block_bytes = null;
            return false;
        }

        private SqliteCommand CreateCommand()
        {
            var cmd = connection.CreateCommand();
            if(transaction != null)
                cmd.Transaction = transaction;
            return cmd;
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
