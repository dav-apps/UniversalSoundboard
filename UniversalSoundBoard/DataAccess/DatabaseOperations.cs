using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using UniversalSoundBoard.Models;

namespace UniversalSoundBoard.DataAccess
{
    class DatabaseOperations
    {
        private const string DatabaseName = "universalsoundboard.db";
        private const string CategoryTableName = "Category";
        private const string SoundTableName = "Sound";

        public static void InitializeDatabase()
        {
            SQLitePCL.Batteries.Init();
            using (SqliteConnection db = new SqliteConnection("Filename=" + DatabaseName))
            {
                db.Open();

                // Create Category table
                string categoryTableCommandText = "CREATE TABLE IF NOT EXISTS " + CategoryTableName +
                                        " (id INTEGER PRIMARY KEY, " +
                                        "uuid VARCHAR NOT NULL, " +
                                        "name VARCHAR(100) NOT NULL, " +
                                        "icon VARCHAR)";
                SqliteCommand categoryTableCommand = new SqliteCommand(categoryTableCommandText, db);
                try
                {
                    categoryTableCommand.ExecuteReader();
                }
                catch (SqliteException e)
                {
                    Debug.WriteLine("Error in create category table");
                    Debug.WriteLine(e.Message);
                }

                // Create Sound table
                string soundTableCommandText = "CREATE TABLE IF NOT EXISTS " + SoundTableName +
                                        " (id VARCHAR PRIMARY KEY, " +
                                        "name VARCHAR(100) NOT NULL, " +
                                        "category_id INTEGER NOT NULL, " +
                                        "FOREIGN KEY(category_id) REFERENCES categories(id));";
                SqliteCommand soundTableCommand = new SqliteCommand(soundTableCommandText, db);
                try
                {
                    soundTableCommand.ExecuteReader();
                }
                catch (SqliteException e)
                {
                    Debug.WriteLine("Error in create sound table");
                    Debug.WriteLine(e.Message);
                }
                db.Close();
            }
        }

        public static void AddCategory(string name, string icon)
        {
            string uuid = Guid.NewGuid().ToString();

            using (SqliteConnection db = new SqliteConnection("Filename=" + DatabaseName))
            {
                db.Open();
                SqliteCommand insertCommand = new SqliteCommand();
                insertCommand.Connection = db;

                insertCommand.CommandText = "INSERT INTO " + CategoryTableName +
                                            " (uuid, name, icon) " +
                                            "VALUES (@Uuid, @Name, @Icon);";

                insertCommand.Parameters.AddWithValue("@Uuid", uuid);
                insertCommand.Parameters.AddWithValue("@Name", name);
                insertCommand.Parameters.AddWithValue("@Icon", icon);

                try
                {
                    insertCommand.ExecuteReader();
                }
                catch (SqliteException error)
                {
                    Debug.WriteLine("Error in AddCategory");
                    Debug.WriteLine(error.Message);
                }
                db.Close();
            }
        }

        public static List<Category> GetCategories()
        {
            List<Category> entries = new List<Category>();
            using (SqliteConnection db = new SqliteConnection("Filename=" + DatabaseName))
            {
                db.Open();
                string selectCommandText = "SELECT * FROM " + CategoryTableName + ";";

                SqliteCommand selectCommand = new SqliteCommand(selectCommandText, db);
                SqliteDataReader query;
                try
                {
                    query = selectCommand.ExecuteReader();
                }
                catch (SqliteException error)
                {
                    Debug.WriteLine("Error in GetCategories");
                    Debug.WriteLine(error.Message);
                    return entries;
                }

                while (query.Read())
                {
                    string uuid = query.GetString(1);
                    string name = query.GetString(2);
                    string icon = query.GetString(3);

                    entries.Add(new Category(uuid, name, icon));
                }
                db.Close();
            }
            return entries;
        }

        public static void UpdateCategory(string uuid, string name, string icon)
        {
            using (SqliteConnection db = new SqliteConnection("Filename=" + DatabaseName))
            {
                db.Open();
                string updateCommandText = "UPDATE " + CategoryTableName +
                                            " SET name = @Name, " +
                                            " icon = @Icon " +
                                            " WHERE uuid = @Uuid";

                SqliteCommand updateCommand = new SqliteCommand(updateCommandText, db);
                updateCommand.Parameters.AddWithValue("@Name", name);
                updateCommand.Parameters.AddWithValue("@Icon", icon);
                updateCommand.Parameters.AddWithValue("@Uuid", uuid);
                SqliteDataReader query;

                try
                {
                    query = updateCommand.ExecuteReader();
                }
                catch (SqliteException error)
                {
                    Debug.WriteLine("Error in UpdateCategory");
                    Debug.WriteLine(error.Message);
                }
                db.Close();
            }
        }

        public static void DeleteCategory(string uuid)
        {
            using (SqliteConnection db = new SqliteConnection("Filename=" + DatabaseName))
            {
                db.Open();
                SqliteCommand insertCommand = new SqliteCommand();
                insertCommand.Connection = db;

                insertCommand.CommandText = "DELETE FROM " + CategoryTableName + " WHERE uuid = @Uuid;";
                insertCommand.Parameters.AddWithValue("@Uuid", uuid);

                try
                {
                    insertCommand.ExecuteReader();
                }
                catch (SqliteException error)
                {
                    Debug.WriteLine("Error in DeleteCategory");
                    Debug.WriteLine(error.Message);
                }
                db.Close();
            }
        }
    }
}
