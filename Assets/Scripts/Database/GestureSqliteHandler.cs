using NOVA.Scripts;
using SQLite;
using UnityEditor;
using UnityEngine;

namespace Nova.Scripts
{
    [InitializeOnLoad]
    public class GestureSqliteHandler
    {
        private const string DatabaseName = "Gestures.db";
        private const string GestureAssetsName = "GestureAssets";

        static GestureSqliteHandler()
        {
            // Make sure a directory exists for the database (StreamingAssets/GestureAssets/Gestures.db)
            string gestureAssetsDir = System.IO.Path.Combine(Application.streamingAssetsPath, GestureAssetsName);

            if (!System.IO.Directory.Exists(gestureAssetsDir))
            {
                Debug.Log("GestureAssets directory does not exist under StreamingAssets...attempting to create");

                System.IO.Directory.CreateDirectory(gestureAssetsDir);
            }

            string dbPath = System.IO.Path.Combine(gestureAssetsDir, DatabaseName);

            if (!System.IO.File.Exists(dbPath))
            {
                Debug.Log("Database 'Gestures.db' does not exist under GestureAssets...attempting to create");

                // Create the database file if it doesn't exist
                using var connection = new SQLiteConnection(dbPath);

                connection.CreateTable<Configuration>();
                connection.CreateTable<GestureCategory>();
                connection.CreateTable<GestureData>();
                connection.CreateTable<PredefinedGesture>();
                connection.CreateTable<CustomGesture>();
                connection.CreateTable<Landmark>();
                connection.CreateTable<LandmarkDistance>();
                connection.CreateTable<GestureImage>();

                // Tables need to be populated in a specific order in order to
                // avoid missing foreign key references:
                // 1. Configuration (not related to any other table)
                // 2. GestureCategory
                // 3. GestureData
                // 4. PredefinedGesture
                // 5. CustomGesture
                // 6. PredefinedGesture
                // 7. Landmark
                // 8. LandmarkDistance
                // 9. GestureImage
            }
        }

        public void CreateConfiguration(Configuration newConfig)
        {
            using var connection = GetSqliteConnection();

            var tableInfo = connection.GetTableInfo(nameof(Configuration));
            if (tableInfo == null)
            {
                throw new TableNotFoundException($"Table '{nameof(Configuration)}' not found in the database.");
            }

            // Check if the configuration already exists
            var existingConfig = connection.Table<Configuration>().FirstOrDefault(c => c.ConfigurationName == newConfig.ConfigurationName);
            if (existingConfig != null)
            {
                throw new ConfigurationAlreadyExists($"Configuration with name '{newConfig.ConfigurationName}' already exists.");
            }

            // Insert the new configuration into the database
            connection.Insert(newConfig);
        }

        private static SQLiteConnection GetSqliteConnection()
        {
            string gestureAssetsDir = System.IO.Path.Combine(Application.streamingAssetsPath, GestureAssetsName);
            string dbPath = System.IO.Path.Combine(gestureAssetsDir, DatabaseName);
            return new SQLiteConnection(dbPath);
        }
    }
}
