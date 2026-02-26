using System.Collections;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using System.IO;
using System;
using Larnix.Entities;
using Larnix.Core.Files;
using Larnix.Core.Utils;
using Larnix.Core.Vectors;
using Larnix.Entities.Structs;
using Larnix.Core.Json;
using Larnix.Blocks.Structs;
using Larnix.Blocks;
using Larnix.Core.DbStructs;

namespace Larnix.Server.Data.SQLite
{
    internal class Database : IDisposable, IDbUserAccess
    {
        private SqliteConnection Connection { get; init; }
        private SqliteTransaction Transaction { get; set; }

        private static bool _initialized = false;
        private static readonly object _initLock = new();

        private bool _disposed = false;

        public Database(string path)
        {
            lock (_initLock)
            {
                if (!_initialized)
                {
                    SQLitePCL.Batteries_V2.Init();
                    _initialized = true;
                }
            }

            string normalizedPath = Path.GetFullPath(path);
            string fullDatabasePath = Path.Combine(normalizedPath, "database.sqlite");

            FileManager.EnsureDirectory(normalizedPath);

            SqliteConnectionStringBuilder csb = new()
            {
                DataSource = fullDatabasePath,
                Pooling = false
            };

            Connection = new SqliteConnection(csb.ConnectionString);
            Connection.Open();

            using (var cmd = CreateCommand())
            {
                cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS players(
                    uid INTEGER PRIMARY KEY,
                    nickname TEXT NOT NULL,
                    password_hash TEXT NOT NULL,
                    challenge_id INTEGER
                );
                CREATE INDEX IF NOT EXISTS idx_players
                    ON players(nickname);

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
                );

                CREATE TABLE IF NOT EXISTS server_tick (
                    id INTEGER PRIMARY KEY,
                    value INTEGER
                );
                ";
                cmd.ExecuteNonQuery();
            }

            EnsureColumnExists("chunks", "nbt", "TEXT NOT NULL DEFAULT ''");
        }

        private void EnsureColumnExists(string table, string column, string definition)
        {
            using var cmd = CreateCommand();
            cmd.CommandText = $"PRAGMA table_info({table});";

            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                    if (reader["name"].ToString() == column)
                        return;
            }

            cmd.CommandText = $"ALTER TABLE {table} ADD COLUMN {column} {definition};";
            cmd.ExecuteNonQuery();
        }

        private SqliteCommand CreateCommand()
        {
            var cmd = Connection.CreateCommand();
            if (Transaction != null)
            {
                cmd.Transaction = Transaction;
            }
            return cmd;
        }

#region User Data Access

        public void SaveUserData(DbUser userData)
        {
            using (var cmd = CreateCommand())
            {
                if (userData.HasUID) // account exists -> change data on uid
                {
                    cmd.CommandText = @"
                        UPDATE players
                        SET nickname = $nickname,
                            password_hash = $password_hash,
                            challenge_id = $challenge_id
                        WHERE uid = $uid;
                       ";
                    cmd.Parameters.AddWithValue("$uid", userData.UID);
                    cmd.Parameters.AddWithValue("$nickname", userData.Username);
                    cmd.Parameters.AddWithValue("$password_hash", userData.PasswordHash);
                    cmd.Parameters.AddWithValue("$challenge_id", userData.ChallengeID);
                }
                else // no account -> create, auto-generate uid
                {
                    cmd.CommandText = @"
                        INSERT INTO players (nickname, password_hash, challenge_id)
                        VALUES ($nickname, $password_hash, $challenge_id);
                        ";
                    cmd.Parameters.AddWithValue("$nickname", userData.Username);
                    cmd.Parameters.AddWithValue("$password_hash", userData.PasswordHash);
                    cmd.Parameters.AddWithValue("$challenge_id", userData.ChallengeID);
                }
                cmd.ExecuteNonQuery();
            }
        }

        public bool TryGetUserData(string username, out DbUser userData)
        {
            using (var cmd = CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM players WHERE nickname = $nickname;";
                cmd.Parameters.AddWithValue("$nickname", username);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        userData = DbUser.FromRecord(
                            uid: (long)reader["uid"],
                            username: (string)reader["nickname"],
                            passwordHash: (string)reader["password_hash"],
                            challengeID: (long)reader["challenge_id"]
                        );
                        return true;
                    }
                }
            }
            userData = default;
            return false;
        }

        public IEnumerable<string> AllUsernames()
        {
            using (var cmd = CreateCommand())
            {
                cmd.CommandText = "SELECT nickname FROM players;";

                using (var reader = cmd.ExecuteReader())
                {
                    List<string> usernames = new();
                    while (reader.Read())
                    {
                        string read = (string)reader["nickname"];
                        if (!string.IsNullOrEmpty(read))
                        {
                            usernames.Add(read);
                        }
                    }
                    return usernames;
                }
            }
        }

#endregion
#region Entity Data Access

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
                        EntityData entityData = new EntityData(
                            id: (EntityID)(long)reader["type"],
                            position: new Vec2((double)reader["pos_x"], (double)reader["pos_y"]),
                            rotation: (float)(double)reader["rotation"],
                            data: Storage.FromString((string)reader["nbt"])
                        );
                        return entityData;
                    }
                }
            }
            return null;
        }

        public Dictionary<ulong, EntityData> GetEntitiesByChunkNoPlayers(Vec2Int chunkCoords)
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
                        returns.Add((ulong)(long)reader["uid"], new EntityData(
                            id: (EntityID)(long)reader["type"],
                            position: new Vec2((double)reader["pos_x"], (double)reader["pos_y"]),
                            rotation: (float)(double)reader["rotation"],
                            data: Storage.FromString((string)reader["nbt"])
                        ));
                    }
                    return returns;
                }
            }
        }

        public void FlushEntities(Dictionary<ulong, EntityData> entities)
        {
            if (Transaction == null)
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

                foreach (var kvp in entities)
                {
                    EntityData entity = kvp.Value;
                    Vec2Int chunkCoords = BlockUtils.CoordsToChunk(entity.Position);

                    paramUid.Value = (long)kvp.Key;
                    paramType.Value = (long)entity.ID;
                    paramChunkX.Value = (long)chunkCoords.x;
                    paramChunkY.Value = (long)chunkCoords.y;
                    paramPosX.Value = entity.Position.x;
                    paramPosY.Value = entity.Position.y;
                    paramRotation.Value = entity.Rotation;
                    paramNbt.Value = entity.Data.ToString();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public bool DeleteEntities(List<ulong> uids)
        {
            if (Transaction == null)
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

#endregion
#region Chunk Data Access

        public void SetChunk(int x, int y, BlockData2[,] chunk)
        {
            using (var cmd = CreateCommand())
            {
                cmd.CommandText = "INSERT OR REPLACE INTO chunks (chunk_x, chunk_y, block_bytes, nbt) VALUES ($chunk_x, $chunk_y, $block_bytes, $nbt);";
                cmd.Parameters.AddWithValue("$chunk_x", x);
                cmd.Parameters.AddWithValue("$chunk_y", y);
                cmd.Parameters.AddWithValue("$block_bytes", chunk.SerializeChunk());
                cmd.Parameters.AddWithValue("$nbt", chunk.ExportData());
                cmd.ExecuteNonQuery();
            }
        }

        public bool TryGetChunk(int x, int y, out BlockData2[,] chunk)
        {
            using (var cmd = CreateCommand())
            {
                cmd.CommandText = "SELECT block_bytes, nbt FROM chunks WHERE chunk_x = $chunk_x AND chunk_y = $chunk_y;";
                cmd.Parameters.AddWithValue("$chunk_x", x);
                cmd.Parameters.AddWithValue("$chunk_y", y);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        chunk = ChunkMethods.DeserializeChunk((byte[])reader["block_bytes"]);
                        chunk.InsertData((string)reader["nbt"]);
                        return true;
                    }
                }
            }
            chunk = default;
            return false;
        }

#endregion
#region Other Data Access

        public long GetSeed(long? suggestion = null)
        {
            bool has_seed;
            long seed;

            using (var cmd = CreateCommand())
            {
                cmd.CommandText = "SELECT COUNT(value) FROM seed;";
                has_seed = (long)cmd.ExecuteScalar() > 0;
            }

            if (has_seed)
            {
                using (var cmd = CreateCommand())
                {
                    cmd.CommandText = "SELECT value FROM seed;";
                    seed = (long)cmd.ExecuteScalar();
                }
            }
            else
            {
                seed = suggestion ?? Common.GetSecureLong();
                
                using (var cmd = CreateCommand())
                {
                    cmd.CommandText = "INSERT INTO seed (value) VALUES ($seed);";
                    cmd.Parameters.AddWithValue("$seed", seed);
                    cmd.ExecuteNonQuery();
                }
            }

            return seed;
        }

        public long GetServerTick()
        {
            using (var cmd = CreateCommand())
            {
                cmd.CommandText = "SELECT value FROM server_tick WHERE id = 1;";
                var result = cmd.ExecuteScalar();
                if (result == null || result == DBNull.Value)
                    return 0L;
                return (long)result;
            }
        }

        public void SetServerTick(long tick)
        {
            using (var cmd = CreateCommand())
            {
                cmd.CommandText = "INSERT OR REPLACE INTO server_tick (id, value) VALUES (1, $value);";
                cmd.Parameters.AddWithValue("$value", tick);
                cmd.ExecuteNonQuery();
            }
        }

#endregion

        public void BeginTransaction()
        {
            if (Transaction != null)
                throw new InvalidOperationException("Transaction already in progress.");

            Transaction = Connection.BeginTransaction();
        }

        public void CommitTransaction()
        {
            if (Transaction == null)
                throw new InvalidOperationException("No active transaction.");

            Transaction.Commit();
            Transaction.Dispose();
            Transaction = null;
        }

        public void RollbackTransaction()
        {
            if (Transaction == null)
                throw new InvalidOperationException("No active transaction.");

            Transaction.Rollback();
            Transaction.Dispose();
            Transaction = null;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;

                if (Transaction != null)
                {
                    RollbackTransaction();
                }

                Connection?.Close();
                Connection?.Dispose();
            }
        }
    }
}
