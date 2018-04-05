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
        private const string PlayingSoundTableName = "PlayingSound";
        private const int currentDatabaseVersion = 0;

        #region Initialization
        public static void InitializeDatabase()
        {
            SQLitePCL.Batteries.Init();
            using (SqliteConnection db = new SqliteConnection("Filename=" + DatabaseName))
            {
                db.Open();

                // Create Category table
                CreateCategoryTable(db);

                // Create Sound table
                CreateSoundTable(db);

                // Create PlayingSound table
                CreatePlayingSoundTable(db);


                // Check if the database is on the newest version
                // Upgrade the database schema and version if necessary
                // https://stackoverflow.com/questions/989558/best-practices-for-in-app-database-migration-for-sqlite
                int databaseVersion = GetUserVersion(db);
                if(databaseVersion != currentDatabaseVersion)
                {
                    if(databaseVersion == 0)
                    {
                        // Upgrade to version 1
                    }
                }

                db.Close();
            }
        }

        private static void CreateCategoryTable(SqliteConnection db)
        {
            string categoryTableCommandText = "CREATE TABLE IF NOT EXISTS " + CategoryTableName +
                                        " (id INTEGER PRIMARY KEY, " +
                                        "uuid VARCHAR NOT NULL, " +
                                        "name VARCHAR(100) NOT NULL, " +
                                        "icon VARCHAR);";
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
        }

        private static void CreateSoundTable(SqliteConnection db)
        {
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
        }

        private static void CreatePlayingSoundTable(SqliteConnection db)
        {
            string playingSoundTableCommandText = "CREATE TABLE IF NOT EXISTS " + PlayingSoundTableName +
                                                    " (id VARCHAR PRIMARY KEY, " +
                                                    "uuid VARCHAR NOT NULL, " +
                                                    "sound_ids TEXT NOT NULL, " +
                                                    "current INTEGER DEFAULT 0, " +
                                                    "repetitions INTEGER DEFAULT 0, " +
                                                    "randomly BOOLEAN DEFAULT false);";
            SqliteCommand playingSoundTableCommand = new SqliteCommand(playingSoundTableCommandText, db);
            try
            {
                playingSoundTableCommand.ExecuteReader();
            }
            catch (SqliteException e)
            {
                Debug.WriteLine("Error in create playingSound table");
                Debug.WriteLine(e.Message);
            }
        }

        private static int GetUserVersion(SqliteConnection db)
        {
            // Get the user_version
            string userVersionCommandText = "PRAGMA user_version;";
            SqliteCommand userVersionCommand = new SqliteCommand(userVersionCommandText, db);
            SqliteDataReader query;
            int userVersion = 0;

            try
            {
                query = userVersionCommand.ExecuteReader();

                while (query.Read())
                {
                    userVersion = query.GetInt32(0);
                }
            }
            catch (SqliteException e)
            {
                Debug.WriteLine("Error in getting the user_version");
                Debug.WriteLine(e.Message);
            }

            return userVersion;
        }
        # endregion

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
                string selectCommandText = "SELECT * FROM " + SoundTableName + " WHERE uuid = @Uuid;";
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
                
                if (!String.IsNullOrEmpty(name))
                {
                    updateCommandText += "name = @Name, ";
                    updateCommand.Parameters.AddWithValue("@Name", name);
                }
                if(!String.IsNullOrEmpty(category_id))
                {
                    updateCommandText += "category_id = @CategoryId, ";
                    updateCommand.Parameters.AddWithValue("@CategoryId", category_id);
                }
                if(!String.IsNullOrEmpty(sound_ext))
                {
                    updateCommandText += "sound_ext = @SoundExt, ";
                    updateCommand.Parameters.AddWithValue("@SoundExt", sound_ext);
                }
                if(!String.IsNullOrEmpty(image_ext))
                {
                    updateCommandText += "image_ext = @ImageExt, ";
                    updateCommand.Parameters.AddWithValue("@ImageExt", image_ext);
                }
                if(!String.IsNullOrEmpty(favourite))
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

        public static void AddPlayingSound(string uuid, List<string> soundIds, int current, int repetitions, bool randomly)
        {
            string soundIdsString = ConvertIdListToString(soundIds);

            using (SqliteConnection db = new SqliteConnection("Filename=" + DatabaseName))
            {
                db.Open();
                SqliteCommand insertCommand = new SqliteCommand();
                insertCommand.Connection = db;

                insertCommand.CommandText = "INSERT INTO " + PlayingSoundTableName +
                                            " (uuid, sound_ids, current, repetitions, randomly) " +
                                            "VALUES (@Uuid, @SoundIds, @Current, @Repetitions, @Randomly);";

                insertCommand.Parameters.AddWithValue("@Uuid", uuid);
                insertCommand.Parameters.AddWithValue("@SoundIds", soundIdsString);
                insertCommand.Parameters.AddWithValue("@Current", current);
                insertCommand.Parameters.AddWithValue("@Repetitions", repetitions);
                insertCommand.Parameters.AddWithValue("@Randomly", randomly);

                try
                {
                    insertCommand.ExecuteReader();
                }
                catch (SqliteException error)
                {
                    Debug.WriteLine("Error in AddPlayingSound");
                    Debug.WriteLine(error.Message);
                }
                db.Close();
            }
        }

        public static List<object> GetAllPlayingSounds()
        {
            List<object> entries = new List<object>();
            using (SqliteConnection db = new SqliteConnection("Filename=" + DatabaseName))
            {
                db.Open();
                string selectCommandText = "SELECT * FROM " + PlayingSoundTableName + ";";

                SqliteCommand selectCommand = new SqliteCommand(selectCommandText, db);
                SqliteDataReader query;

                try
                {
                    query = selectCommand.ExecuteReader();
                }
                catch (SqliteException e)
                {
                    Debug.WriteLine("Error in GetAllPlayingSounds");
                    Debug.WriteLine(e.Message);
                    return entries;
                }

                while (query.Read())
                {
                    string uuid = query.GetString(1);
                    string soundIds = query.GetString(2);
                    int current = query.GetInt32(3);
                    int repetitions = query.GetInt32(4);
                    bool randomly = query.GetBoolean(5);

                    var obj = new
                    {
                        uuid,
                        soundIds,
                        current,
                        repetitions,
                        randomly
                    };

                    entries.Add(obj);
                }
                db.Close();
            }
            return entries;
        }

        public static object GetPlayingSound(string uuid)
        {
            using (SqliteConnection db = new SqliteConnection("Filename=" + DatabaseName))
            {
                db.Open();
                string selectCommandText = "SELECT * FROM " + PlayingSoundTableName + " WHERE uuid = @Uuid;";
                SqliteCommand selectCommand = new SqliteCommand(selectCommandText, db);
                selectCommand.Parameters.AddWithValue("@Uuid", uuid);
                SqliteDataReader query;

                try
                {
                    query = selectCommand.ExecuteReader();
                }
                catch (SqliteException e)
                {
                    Debug.WriteLine("Error in GetPlayingSound");
                    Debug.WriteLine(e.Message);
                    return null;
                }

                bool soundExists = false;
                object obj = new object();
                while (query.Read())
                {
                    uuid = query.GetString(1);
                    string soundIds = query.GetString(2);
                    int current = query.GetInt32(3);
                    int repetitions = query.GetInt32(4);
                    bool randomly = query.GetBoolean(5);

                    obj = new
                    {
                        uuid,
                        soundIds,
                        current,
                        repetitions,
                        randomly
                    };
                    soundExists = true;
                }

                if (soundExists)
                    return obj;

                db.Close();
                return null;
            }
        }

        public static void UpdatePlayingSound(string uuid, List<string> soundIds, string current, string repetitions, string randomly)
        {
            using (SqliteConnection db = new SqliteConnection("Filename=" + DatabaseName))
            {
                db.Open();
                string updateCommandText = "UPDATE " + PlayingSoundTableName + " SET ";
                SqliteCommand updateCommand = new SqliteCommand();
                
                if(soundIds != null)
                {
                    string soundIdsString = ConvertIdListToString(soundIds);
                    updateCommandText += "sound_ids = @SoundIds, ";
                    updateCommand.Parameters.AddWithValue("@SoundIds", soundIdsString);
                }
                if(!String.IsNullOrEmpty(current))
                {
                    updateCommandText += "current = @Current, ";
                    updateCommand.Parameters.AddWithValue("@Current", current);
                }
                if (!String.IsNullOrEmpty(repetitions))
                {
                    updateCommandText += "repetitions = @Repetitions, ";
                    updateCommand.Parameters.AddWithValue("@Repetitions", repetitions);
                }
                if (!String.IsNullOrEmpty(randomly))
                {
                    updateCommandText += "randomly = @Randomly, ";
                    updateCommand.Parameters.AddWithValue("@Randomly", randomly);
                }
                updateCommandText = updateCommandText.Remove(updateCommandText.Length - 2);
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
                    Debug.WriteLine("Error in UpdatePlayingSound");
                    Debug.WriteLine(error.Message);
                }
                db.Close();
            }
        }

        public static void DeletePlayingSound(string uuid)
        {
            using (SqliteConnection db = new SqliteConnection("Filename=" + DatabaseName))
            {
                db.Open();
                SqliteCommand insertCommand = new SqliteCommand();
                insertCommand.Connection = db;

                insertCommand.CommandText = "DELETE FROM " + PlayingSoundTableName + " WHERE uuid = @Uuid;";
                insertCommand.Parameters.AddWithValue("@Uuid", uuid);

                try
                {
                    insertCommand.ExecuteReader();
                }
                catch (SqliteException error)
                {
                    Debug.WriteLine("Error in DeletePlayingSound");
                    Debug.WriteLine(error.Message);
                }
                db.Close();
            }
        }


        // Other methods
        private static string ConvertIdListToString(List<string> ids)
        {
            string idsString = "";
            foreach (string id in ids)
            {
                idsString += id + ",";
            }
            // Remove the last character, which is a ,
            idsString = idsString.Remove(idsString.Length - 1);

            return idsString;
        }
    }
}
