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
                                        "uuid VARCHAR NOT NULL, " +
                                        "name VARCHAR(100) NOT NULL, " +
                                        "favourite BOOLEAN DEFAULT false, " +
                                        "sound_ext VARCHAR NOT NULL, " +
                                        "image_ext VARCHAR, " +
                                        "category_id VARCHAR, " +
                                        "FOREIGN KEY(category_id) REFERENCES categories(uuid));";
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

        public static void AddSound(string uuid, string name, string category_id, string ext)
        {
            using (SqliteConnection db = new SqliteConnection("Filename=" + DatabaseName))
            {
                db.Open();
                SqliteCommand insertCommand = new SqliteCommand();
                insertCommand.Connection = db;

                insertCommand.CommandText = "INSERT INTO " + SoundTableName +
                                            " (uuid, name, category_id, sound_ext, image_ext) " +
                                            "VALUES (@Uuid, @Name, @Category_id, @SoundExt, @ImageExt);";

                insertCommand.Parameters.AddWithValue("@Uuid", uuid);
                insertCommand.Parameters.AddWithValue("@Name", name);

                if (String.IsNullOrEmpty(category_id))
                    insertCommand.Parameters.AddWithValue("@Category_id", "");
                else
                    insertCommand.Parameters.AddWithValue("@Category_id", category_id);

                insertCommand.Parameters.AddWithValue("@SoundExt", ext);
                insertCommand.Parameters.AddWithValue("@ImageExt", "");

                try
                {
                    insertCommand.ExecuteReader();
                }
                catch (SqliteException error)
                {
                    Debug.WriteLine("Error in AddSound");
                    Debug.WriteLine(error.Message);
                }
                db.Close();
            }
        }

        public static object GetSound(string uuid)
        {
            using (SqliteConnection db = new SqliteConnection("Filename=" + DatabaseName))
            {
                db.Open();
                string selectCommandText = "SELECT * FROM " + SoundTableName + " WHERE uuid = @Uuid";
                SqliteCommand selectCommand = new SqliteCommand(selectCommandText, db);
                selectCommand.Parameters.AddWithValue("@Uuid", uuid);
                SqliteDataReader query;

                try
                {
                    query = selectCommand.ExecuteReader();
                }
                catch (SqliteException error)
                {
                    Debug.WriteLine("Error in GetSound");
                    Debug.WriteLine(error.Message);
                    return null;
                }

                bool soundExists = false;
                object obj = new object();
                while (query.Read())
                {
                    uuid = query.GetString(1);
                    string name = query.GetString(2);
                    bool favourite = query.GetBoolean(3);
                    string sound_ext = query.GetString(4);
                    string image_ext = query.GetString(5);
                    string category_id = query.GetString(6);

                    obj = new
                    {
                        uuid,
                        name,
                        favourite,
                        sound_ext,
                        image_ext,
                        category_id
                    };
                    soundExists = true;
                }
                if(soundExists)
                    return obj;

                db.Close();
                return null;
            }
        }

        public static List<object> GetAllSounds()
        {
            List<object> entries = new List<object>();
            using (SqliteConnection db = new SqliteConnection("Filename=" + DatabaseName))
            {
                db.Open();
                string selectCommandText = "SELECT * FROM " + SoundTableName + ";";
                
                SqliteCommand selectCommand = new SqliteCommand(selectCommandText, db);
                SqliteDataReader query;

                try
                {
                    query = selectCommand.ExecuteReader();
                }
                catch (SqliteException error)
                {
                    Debug.WriteLine("Error in GetAllSounds");
                    Debug.WriteLine(error.Message);
                    return entries;
                }

                while (query.Read())
                {
                    string uuid = query.GetString(1);
                    string name = query.GetString(2);
                    bool favourite = query.GetBoolean(3);
                    string sound_ext = query.GetString(4);
                    string image_ext = query.GetString(5);
                    string category_id = query.GetString(6);

                    var obj = new
                    {
                        uuid,
                        name,
                        favourite,
                        sound_ext,
                        image_ext,
                        category_id
                    };

                    entries.Add(obj);
                }
                db.Close();
            }
            return entries;
        }

        public static void UpdateSound(string uuid, string name, string category_id, string sound_ext, string image_ext, string favourite)
        {
            using(SqliteConnection db = new SqliteConnection("Filename=" + DatabaseName))
            {
                db.Open();
                string updateCommandText = "UPDATE " + SoundTableName + " SET ";
                SqliteCommand updateCommand = new SqliteCommand();
                
                if (name != null)
                {
                    updateCommandText += "name = @Name, ";
                    updateCommand.Parameters.AddWithValue("@Name", name);
                }
                if(category_id != null)
                {
                    updateCommandText += "category_id = @CategoryId, ";
                    updateCommand.Parameters.AddWithValue("@CategoryId", category_id);
                }
                if(sound_ext != null)
                {
                    updateCommandText += "sound_ext = @SoundExt, ";
                    updateCommand.Parameters.AddWithValue("@SoundExt", sound_ext);
                }
                if(image_ext != null)
                {
                    updateCommandText += "image_ext = @ImageExt, ";
                    updateCommand.Parameters.AddWithValue("@ImageExt", image_ext);
                }
                if(favourite != null)
                {
                    updateCommandText += "favourite = @Favourite, ";
                    updateCommand.Parameters.AddWithValue("@Favourite", favourite.ToLower() == "true");
                }
                updateCommandText = updateCommandText.Remove(updateCommandText.Length - 2); // Remove the last two characters, which are ", "
                updateCommandText += " WHERE uuid = @Uuid;";
                updateCommand.Parameters.AddWithValue("@Uuid", uuid);

                updateCommand.Connection = db;
                updateCommand.CommandText = updateCommandText;
                
                try
                {
                    updateCommand.ExecuteReader();
                }
                catch (SqliteException error)
                {
                    Debug.WriteLine("Error in UpdateSound");
                    Debug.WriteLine(error.Message);
                }
                db.Close();
            }
        }

        public static void DeleteSound(string uuid)
        {
            using (SqliteConnection db = new SqliteConnection("Filename=" + DatabaseName))
            {
                db.Open();
                SqliteCommand insertCommand = new SqliteCommand();
                insertCommand.Connection = db;

                insertCommand.CommandText = "DELETE FROM " + SoundTableName + " WHERE uuid = @Uuid;";
                insertCommand.Parameters.AddWithValue("@Uuid", uuid);

                try
                {
                    insertCommand.ExecuteReader();
                }
                catch (SqliteException error)
                {
                    Debug.WriteLine("Error in DeleteSound");
                    Debug.WriteLine(error.Message);
                }
                db.Close();
            }
        }

        public static void AddCategory(string uuid, string name, string icon)
        {
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

        public static Category GetCategory(string uuid)
        {
            Category category = new Category(uuid, "", "");

            using (SqliteConnection db = new SqliteConnection("Filename=" + DatabaseName))
            {
                db.Open();
                string selectCommandText = "SELECT * FROM " + CategoryTableName + " WHERE uuid = @Uuid;";
                SqliteCommand selectCommand = new SqliteCommand(selectCommandText, db);
                selectCommand.Parameters.AddWithValue("@Uuid", uuid);
                SqliteDataReader query;

                try
                {
                    query = selectCommand.ExecuteReader();
                }
                catch(SqliteException error)
                {
                    Debug.WriteLine("Error in GetCategory");
                    Debug.WriteLine(error.Message);
                    return null;
                }

                bool categoryExists = false;
                while (query.Read())
                {
                    uuid = query.GetString(1);
                    string name = query.GetString(2);
                    string icon = query.GetString(3);

                    category.Name = name;
                    category.Icon = icon;
                    categoryExists = true;
                }
                if (categoryExists)
                    return category;

                db.Close();
                return null;
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
                                            "icon = @Icon " +
                                            "WHERE uuid = @Uuid";

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
