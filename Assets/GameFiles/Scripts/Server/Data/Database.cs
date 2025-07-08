using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.Data.Sqlite;
using Larnix.Socket.Data;
using System.IO;
using System;
using Larnix.Socket.Commands;

namespace Larnix.Server.Data
{
    public class Database : IDisposable
    {
        private SqliteConnection connection = null;
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

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS users(
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    username TEXT NOT NULL UNIQUE,
                    password_hash TEXT NOT NULL
                )";
            cmd.ExecuteNonQuery();
        }

        public bool HasUser(string username)
        {
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "SELECT COUNT(*) FROM users WHERE username = $username;";
                cmd.Parameters.AddWithValue("$username", username);
                return (long)cmd.ExecuteScalar() >= 1;
            }
        }

        public bool AllowUser(string username, string password)
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
            else // user not found, create new one
            {
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

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "UPDATE users SET password_hash = $new_password_hash WHERE username = $username;";
                cmd.Parameters.AddWithValue("$username", username);
                cmd.Parameters.AddWithValue("$new_password_hash", Hasher.HashPassword(newPassword));
                cmd.ExecuteNonQuery();
            }

            return A_PasswordChange.ResultType.Success;
        }

        public void Dispose()
        {
            if(connection != null) {
                connection.Close();
                connection.Dispose();
            }
        }
    }
}
