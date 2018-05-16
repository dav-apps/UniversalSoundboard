using SQLite;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UniversalSoundboard.Models;
using UniversalSoundBoard.Models;
using Windows.Storage;
using static UniversalSoundBoard.Models.SyncObject;

namespace UniversalSoundBoard.DataAccess
{
    class DatabaseOperations
    {
        public const string DatabaseName = "universalsoundboard.db";
        private const string CategoryTableName = "Category";
        private const string SoundTableName = "Sound";
        private const string PlayingSoundTableName = "PlayingSound";
        private const string SyncCategoryTableName = "SyncCategory";
        private const string SyncSoundTableName = "SyncSound";
        private const string SyncPlayingSoundTableName = "SyncPlayingSound";
        private const int currentDatabaseVersion = 0;
        private static string databasePath = Path.Combine(ApplicationData.Current.LocalFolder.Path, DatabaseName);

        #region Initialization
        public static void InitializeDatabase()
        {
            var db = new SQLiteConnection(databasePath);

            // Create Category table
            CreateCategoryTable(db);

            // Create Sound table
            CreateSoundTable(db);

            // Create PlayingSound table
            CreatePlayingSoundTable(db);

            // Create the tables for synchronisation
            //CreateSyncTables(db);

            // Check if the database is on the newest version
            // Upgrade the database schema and version if necessary
            // https://stackoverflow.com/questions/989558/best-practices-for-in-app-database-migration-for-sqlite
            int databaseVersion = GetUserVersion(db);
            if (databaseVersion != currentDatabaseVersion)
            {
                if (databaseVersion == 0)
                {
                    // Upgrade to version 1
                }
            }
        }

        private static int GetUserVersion(SQLiteConnection db)
        {
            // Get the user_version
            string userVersionCommandText = "PRAGMA user_version;";
            int userVersion = 0;

            try
            {
                var result = db.Query<int>(userVersionCommandText);

                foreach(int version in result)
                {
                    userVersion = version;
                }
            }
            catch (SQLiteException e)
            {
                Debug.WriteLine("Error in getting the user_version");
                Debug.WriteLine(e.Message);
            }

            return userVersion;
        }

        private static void CreateCategoryTable(SQLiteConnection db)
        {
            string categoryTableCommandText = "CREATE TABLE IF NOT EXISTS " + CategoryTableName +
                                        " (id INTEGER PRIMARY KEY, " +
                                        "uuid VARCHAR NOT NULL, " +
                                        "name VARCHAR(100) NOT NULL, " +
                                        "icon VARCHAR);";
            
            try
            {
                db.Query<object>(categoryTableCommandText);
            }
            catch (SQLiteException e)
            {
                Debug.WriteLine("Error in create category table");
                Debug.WriteLine(e.Message);
            }
        }

        private static void CreateSoundTable(SQLiteConnection db)
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
            
            try
            {
                db.Query<object>(soundTableCommandText);
            }
            catch (SQLiteException e)
            {
                Debug.WriteLine("Error in create sound table");
                Debug.WriteLine(e.Message);
            }
        }

        private static void CreatePlayingSoundTable(SQLiteConnection db)
        {
            string playingSoundTableCommandText = "CREATE TABLE IF NOT EXISTS " + PlayingSoundTableName +
                                                    " (id VARCHAR PRIMARY KEY, " +
                                                    "uuid VARCHAR NOT NULL, " +
                                                    "sound_ids TEXT NOT NULL, " +
                                                    "current INTEGER DEFAULT 0, " +
                                                    "repetitions INTEGER DEFAULT 0, " +
                                                    "randomly BOOLEAN DEFAULT false," +
                                                    "volume REAL DEFAULT 1);";
            
            try
            {
                db.Query<object>(playingSoundTableCommandText);
            }
            catch (SQLiteException e)
            {
                Debug.WriteLine("Error in create playingSound table");
                Debug.WriteLine(e.Message);
            }
        }

        private static void CreateSyncTables(SQLiteConnection db)
        {
            CreateSyncCategoryTable(db);
            CreateSyncSoundTable(db);
            CreateSyncPlayingSoundTable(db);
        }

        private static void CreateSyncCategoryTable(SQLiteConnection db)
        {
            string syncCategoryTableCommandText = "CREATE TABLE IF NOT EXISTS " + SyncCategoryTableName +
                                                    " (id INTEGER PRIMARY KEY, " +
                                                    " uuid VARCHAR NOT NULL, " +
                                                    " operation INTEGER NOT NULL);";

            try
            {
                db.Query<object>(syncCategoryTableCommandText);
            }
            catch (SQLiteException e)
            {
                Debug.WriteLine("Error in create SyncCategory table");
                Debug.WriteLine(e.Message);
            }
        }

        private static void CreateSyncSoundTable(SQLiteConnection db)
        {
            string syncSoundTableCommandText = "CREATE TABLE IF NOT EXISTS " + SyncSoundTableName +
                                                    " (id INTEGER PRIMARY KEY, " +
                                                    " uuid VARCHAR NOT NULL, " +
                                                    " operation INTEGER NOT NULL);";

            try
            {
                db.Query<object>(syncSoundTableCommandText);
            }
            catch (SQLiteException e)
            {
                Debug.WriteLine("Error in create SyncSound table");
                Debug.WriteLine(e.Message);
            }
        }

        private static void CreateSyncPlayingSoundTable(SQLiteConnection db)
        {
            string syncPlayingSoundTableCommandText = "CREATE TABLE IF NOT EXISTS " + SyncPlayingSoundTableName +
                                                    " (id INTEGER PRIMARY KEY, " +
                                                    " uuid VARCHAR NOT NULL, " +
                                                    " operation INTEGER NOT NULL);";

            try
            {
                db.Query<object>(syncPlayingSoundTableCommandText);
            }
            catch (SQLiteException e)
            {
                Debug.WriteLine("Error in create SyncPlayingSound table");
                Debug.WriteLine(e.Message);
            }
        }
        # endregion

        public static void AddSound(string uuid, string name, string category_id, string ext)
        {
            var db = new SQLiteConnection(databasePath);

            try
            {
                db.Insert(new OldSoundDatabaseModel
                {
                    uuid = uuid,
                    name = name,
                    category_id = category_id,
                    sound_ext = ext
                });
            }
            catch (SQLiteException error)
            {
                Debug.WriteLine("Error in AddSound");
                Debug.WriteLine(error.Message);
            }
        }

        public static OldSoundDatabaseModel GetSound(string uuid)
        {
            var db = new SQLiteConnection(databasePath);
            OldSoundDatabaseModel sound = null;

            string selectCommandText = "SELECT * FROM " + SoundTableName + " WHERE uuid = ?;";

            try
            {
                var soundList = db.Query<OldSoundDatabaseModel>(selectCommandText, uuid);
                if (soundList.Count > 0)
                {
                    sound = soundList[0];
                    return sound;
                }
                else
                {
                    return null;
                }
            }
            catch (SQLiteException error)
            {
                Debug.WriteLine("Error in GetSound");
                Debug.WriteLine(error.Message);
                return null;
            }
        }

        public static List<OldSoundDatabaseModel> GetAllSounds()
        {
            List<OldSoundDatabaseModel> entries = new List<OldSoundDatabaseModel>();

            var db = new SQLiteConnection(databasePath);
            string selectCommandText = "SELECT * FROM " + SoundTableName + ";";

            try
            {
                foreach(OldSoundDatabaseModel sound in db.Query<OldSoundDatabaseModel>(selectCommandText))
                    entries.Add(sound);
            }
            catch (SQLiteException error)
            {
                Debug.WriteLine("Error in GetAllSounds");
                Debug.WriteLine(error.Message);
                return entries;
            }
            
            return entries;
        }

        public static void UpdateSound(string uuid, string name, string category_id, string sound_ext, string image_ext, string favourite)
        {
            var db = new SQLiteConnection(databasePath);

            string updateCommandText = "UPDATE " + SoundTableName + " SET ";
            List<string> parametersList = new List<string>();

            if (!String.IsNullOrEmpty(name))
            {
                updateCommandText += "name = ?, ";
                parametersList.Add(name);
            }
            if (!String.IsNullOrEmpty(category_id))
            {
                updateCommandText += "category_id = ?, ";
                parametersList.Add(category_id);
            }
            if (!String.IsNullOrEmpty(sound_ext))
            {
                updateCommandText += "sound_ext = ?, ";
                parametersList.Add(sound_ext);
            }
            if (!String.IsNullOrEmpty(image_ext))
            {
                updateCommandText += "image_ext = ?, ";
                parametersList.Add(image_ext);
            }
            if (!String.IsNullOrEmpty(favourite))
            {
                updateCommandText += "favourite = ?, ";
                parametersList.Add((favourite.ToLower() == "true").ToString());
            }
            updateCommandText = updateCommandText.Remove(updateCommandText.Length - 2); // Remove the last two characters, which are ", "
            updateCommandText += " WHERE uuid = ?;";
            parametersList.Add(uuid);

            try
            {
                db.Query<OldSoundDatabaseModel>(updateCommandText, parametersList.ToArray());
            }
            catch (SQLiteException error)
            {
                Debug.WriteLine("Error in UpdateSound");
                Debug.WriteLine(error.Message);
            }
        }

        public static void DeleteSound(string uuid)
        {
            var db = new SQLiteConnection(databasePath);

            string insertCommandText = "DELETE FROM " + SoundTableName + " WHERE uuid = ?;";

            try
            {
                db.Query<OldSoundDatabaseModel>(insertCommandText, uuid);
            }
            catch (SQLiteException error)
            {
                Debug.WriteLine("Error in DeleteSound");
                Debug.WriteLine(error.Message);
            }
        }

        public static void AddCategory(string uuid, string name, string icon)
        {
            var db = new SQLiteConnection(databasePath);

            string insertCommandText = "INSERT INTO " + CategoryTableName +
                                        " (uuid, name, icon) " +
                                        "VALUES (?, ?, ?);";

            try
            {
                db.Query<OldCategoryDatabaseModel>(insertCommandText, uuid, name, icon);
            }
            catch (SQLiteException error)
            {
                Debug.WriteLine("Error in AddCategory");
                Debug.WriteLine(error.Message);
            }
        }

        public static Category GetCategory(string uuid)
        {
            var db = new SQLiteConnection(databasePath);
            OldCategoryDatabaseModel category = null;
            string selectCommandText = "SELECT * FROM " + CategoryTableName + " WHERE uuid = ?;";

            try
            {
                var categoryList = db.Query<OldCategoryDatabaseModel>(selectCommandText, uuid);
                if (categoryList.Count > 0)
                    category = categoryList[0];
            }
            catch (SQLiteException error)
            {
                Debug.WriteLine("Error in GetCategory");
                Debug.WriteLine(error.Message);
                return null;
            }

            if (category != null)
            {
                Category c = new Category
                {
                    Uuid = category.uuid,
                    Name = category.name,
                    Icon = category.icon
                };

                return c;
            }

            return null;
        }

        public static List<Category> GetCategories()
        {
            List<Category> entries = new List<Category>();
            var db = new SQLiteConnection(databasePath);

            string selectCommandText = "SELECT * FROM " + CategoryTableName + ";";
            
            try
            {
                foreach(var category in db.Query<OldCategoryDatabaseModel>(selectCommandText))
                {
                    entries.Add(new Category
                    {
                        Uuid = category.uuid,
                        Name = category.name,
                        Icon = category.icon
                    });
                }
            }
            catch (SQLiteException error)
            {
                Debug.WriteLine("Error in GetCategories");
                Debug.WriteLine(error.Message);
                return entries;
            }

            return entries;
        }

        public static void UpdateCategory(string uuid, string name, string icon)
        {
            var db = new SQLiteConnection(databasePath);

            string updateCommandText = "UPDATE " + CategoryTableName +
                                            " SET name = ?, " +
                                            "icon = ? " +
                                            "WHERE uuid = ?";

            try
            {
                db.Query<OldCategoryDatabaseModel>(updateCommandText, name, icon, uuid);
            }
            catch (SQLiteException error)
            {
                Debug.WriteLine("Error in UpdateCategory");
                Debug.WriteLine(error.Message);
            }
        }

        public static void DeleteCategory(string uuid)
        {
            var db = new SQLiteConnection(databasePath);

            string commandText = "DELETE FROM " + CategoryTableName + " WHERE uuid = ?;";

            try
            {
                db.Query<OldCategoryDatabaseModel>(commandText, uuid);
            }
            catch (SQLiteException error)
            {
                Debug.WriteLine("Error in DeleteCategory");
                Debug.WriteLine(error.Message);
            }
        }

        public static void AddPlayingSound(string uuid, List<string> soundIds, int current, int repetitions, bool randomly, double volume)
        {
            var db = new SQLiteConnection(databasePath);
            string soundIdsString = ConvertIdListToString(soundIds);
            string insertCommandText = "INSERT INTO " + PlayingSoundTableName +
                                        " (uuid, sound_ids, current, repetitions, randomly, volume) " +
                                        "VALUES (?, ?, ?, ?, ?, ?);";

            try
            {
                db.Query<OldPlayingSoundDatabaseModel>(insertCommandText, uuid, soundIdsString, current, repetitions, randomly, volume);
            }
            catch (SQLiteException error)
            {
                Debug.WriteLine("Error in AddPlayingSound");
                Debug.WriteLine(error.Message);
            }
        }

        public static List<OldPlayingSoundDatabaseModel> GetAllPlayingSounds()
        {
            List<OldPlayingSoundDatabaseModel> entries = new List<OldPlayingSoundDatabaseModel>();
            var db = new SQLiteConnection(databasePath);

            string selectCommandText = "SELECT * FROM " + PlayingSoundTableName + ";";

            try
            {
                foreach(var playingSound in db.Query<OldPlayingSoundDatabaseModel>(selectCommandText))
                    entries.Add(playingSound);
            }
            catch (SQLiteException e)
            {
                Debug.WriteLine("Error in GetAllPlayingSounds");
                Debug.WriteLine(e.Message);
                return entries;
            }
            
            return entries;
        }

        public static OldPlayingSoundDatabaseModel GetPlayingSound(string uuid)
        {
            var db = new SQLiteConnection(databasePath);
            OldPlayingSoundDatabaseModel playingSound = null;
            string selectCommandText = "SELECT * FROM " + PlayingSoundTableName + " WHERE uuid = ?;";

            try
            {
                var playingSoundList = db.Query<OldPlayingSoundDatabaseModel>(selectCommandText, uuid);
                if (playingSoundList.Count > 0)
                    playingSound = playingSoundList[0];

                return playingSound;
            }
            catch (SQLiteException e)
            {
                Debug.WriteLine("Error in GetPlayingSound");
                Debug.WriteLine(e.Message);
                return null;
            }
        }

        public static void UpdatePlayingSound(string uuid, List<string> soundIds, string current, string repetitions, string randomly, string volume)
        {
            var db = new SQLiteConnection(databasePath);

            string updateCommandText = "UPDATE " + PlayingSoundTableName + " SET ";
            List<string> parametersList = new List<string>();

            if (soundIds != null)
            {
                string soundIdsString = ConvertIdListToString(soundIds);
                updateCommandText += "sound_ids = ?, ";
                parametersList.Add(soundIdsString);
            }
            if (!String.IsNullOrEmpty(current))
            {
                updateCommandText += "current = ?, ";
                parametersList.Add(current);
            }
            if (!String.IsNullOrEmpty(repetitions))
            {
                updateCommandText += "repetitions = ?, ";
                parametersList.Add(repetitions);
            }
            if (!String.IsNullOrEmpty(randomly))
            {
                updateCommandText += "randomly = ?, ";
                parametersList.Add(randomly);
            }
            if (!String.IsNullOrEmpty(volume))
            {
                volume = volume.Replace(',', '.');
                updateCommandText += "volume = ?, ";
                parametersList.Add(volume);
            }

            updateCommandText = updateCommandText.Remove(updateCommandText.Length - 2);
            updateCommandText += " WHERE uuid = ?;";
            parametersList.Add(uuid);

            try
            {
                db.Query<OldPlayingSoundDatabaseModel>(updateCommandText, parametersList.ToArray());
            }
            catch (SQLiteException error)
            {
                Debug.WriteLine("Error in UpdatePlayingSound");
                Debug.WriteLine(error.Message);
            }
        }

        public static void DeletePlayingSound(string uuid)
        {
            var db = new SQLiteConnection(databasePath);
            string deleteCommandText = "DELETE FROM " + PlayingSoundTableName + " WHERE uuid = ?;";

            try
            {
                db.Query<OldPlayingSoundDatabaseModel>(deleteCommandText, uuid);
            }
            catch (SQLiteException error)
            {
                Debug.WriteLine("Error in DeletePlayingSound");
                Debug.WriteLine(error.Message);
            }
        }

        /*
        public static void AddSyncObject(SyncTable table, Guid uuid, SyncOperation operation)
        {
            SqliteCommand insertCommand = new SqliteCommand();
            insertCommand.Connection = db;

            insertCommand.CommandText = "INSERT INTO " + GetNameOfSyncTable(table) +
                                        " (uuid, operation) " +
                                        "VALUES (@Uuid, @Operation);";

            insertCommand.Parameters.AddWithValue("@Uuid", uuid);
            insertCommand.Parameters.AddWithValue("@Operation", GetValueOfSyncOperation(operation));

            try
            {
                insertCommand.ExecuteReader();
            }
            catch (SqliteException error)
            {
                Debug.WriteLine("Error in AddSyncObject");
                Debug.WriteLine(error.Message);
            }
        }

        public static List<SyncObject> GetAllSyncObjects(SyncTable table)
        {
            string selectCommandText = "SELECT * FROM " + GetNameOfSyncTable(table) + ";";
            SqliteCommand selectCommand = new SqliteCommand(selectCommandText, db);
            SqliteDataReader query;

            try
            {
                query = selectCommand.ExecuteReader();
            }
            catch (SqliteException e)
            {
                Debug.WriteLine("Error in GetAllSyncObjects");
                Debug.WriteLine(e.Message);
                return null;
            }

            List<SyncObject> syncObjects = new List<SyncObject>();
            while (query.Read())
            {

                SyncObject syncObject = new SyncObject(query.GetInt32(0),
                                                        query.GetGuid(1),
                                                        GetSyncOperationOfValue(query.GetInt32(2)));
                syncObjects.Add(syncObject);
            }

            List<SyncObject> syncObjects = new List<SyncObject>();
            return syncObjects;
        }

        public static void DeleteSyncObject(SyncTable table, int id)
        {
            SqliteCommand insertCommand = new SqliteCommand();
            insertCommand.Connection = db;

            insertCommand.CommandText = "DELETE FROM " + GetNameOfSyncTable(table) + " WHERE id = @Id;";
            insertCommand.Parameters.AddWithValue("@Id", id);

            try
            {
                insertCommand.ExecuteReader();
            }
            catch (SqliteException error)
            {
                Debug.WriteLine("Error in DeleteSyncObject");
                Debug.WriteLine(error.Message);
            }
        }
        */


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

        private static string GetNameOfSyncTable(SyncTable table)
        {
            string tableName = "";
            switch (table)
            {
                case SyncTable.SyncCategory:
                    tableName = SyncCategoryTableName;
                    break;
                case SyncTable.SyncSound:
                    tableName = SyncSoundTableName;
                    break;
                case SyncTable.SyncPlayingSound:
                    tableName = SyncPlayingSoundTableName;
                    break;
            }

            return tableName;
        }

        private static int GetValueOfSyncOperation(SyncOperation operation)
        {
            int operationValue = 0;
            switch (operation)
            {
                case SyncOperation.Create:
                    operationValue = 0;
                    break;
                case SyncOperation.Update:
                    operationValue = 1;
                    break;
                case SyncOperation.Delete:
                    operationValue = 2;
                    break;
            }

            return operationValue;
        }

        private static SyncOperation GetSyncOperationOfValue(int operation)
        {
            SyncOperation syncOperation = SyncOperation.Create;
            switch (operation)
            {
                case 0:
                    syncOperation = SyncOperation.Create;
                    break;
                case 1:
                    syncOperation = SyncOperation.Update;
                    break;
                case 2:
                    syncOperation = SyncOperation.Delete;
                    break;
            }

            return syncOperation;
        }
    }
}
