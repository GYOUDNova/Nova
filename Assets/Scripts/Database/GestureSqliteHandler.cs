using System.IO;
using UnityEngine;

namespace NOVA.Scripts
{
    public class GestureSqliteHandler : SqliteHandler
    {
        private const string GesturesDatabaseName = "Gestures.db";
        private const string GestureAssetsDirName = "GestureAssets";

        private static GestureSqliteHandler instance;

        private GestureSqliteHandler(string databaseName)
            : base(databaseName)
        {
            string gestureAssetsDir = Path.Combine(Application.streamingAssetsPath, GestureAssetsDirName);
            Initialize(gestureAssetsDir);
        }

        protected override void Initialize(string directory)
        {
            // Base class handles pre-processing
            base.Initialize(directory);

            dbPath = Path.Combine(directory, databaseName);

            if (!File.Exists(dbPath))
            {
                // Create the database file if it doesn't exist
                Debug.Log($"Database {dbPath} does not exist...applying database migrations");

                conn = GetSqliteConnection();

                conn.CreateTable<Configuration>();
                conn.CreateTable<GestureCategory>();
                conn.CreateTable<GestureData>();
                conn.CreateTable<PredefinedGesture>();
                conn.CreateTable<CustomGesture>();
                conn.CreateTable<Landmark>();
                conn.CreateTable<LandmarkDistance>();
                conn.CreateTable<GestureImage>();

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

                CloseConnection();
            }
        }

        public static GestureSqliteHandler Instance(string databaseName = GesturesDatabaseName)
        {
            if (instance == null)
            {
                instance = new GestureSqliteHandler(databaseName);
            }
            else if (instance.databaseName != databaseName)
            {
                throw new HandlerExistsException("Cannot change the database name after the instance has been created.");
            }

            return instance;
        }

        public void CreateConfiguration(Configuration newConfig)
        {
            conn = GetSqliteConnection();

            var tableInfo = conn.GetTableInfo(nameof(Configuration));
            if (tableInfo == null)
            {
                throw new TableNotFoundException($"Table '{nameof(Configuration)}' not found in the database.");
            }

            // Check if the configuration already exists
            var existingConfig = conn.Table<Configuration>().FirstOrDefault(c => c.ConfigurationName == newConfig.ConfigurationName);
            if (existingConfig != null)
            {
                throw new ConfigurationAlreadyExists($"Configuration with name '{newConfig.ConfigurationName}' already exists.");
            }

            // Insert the new configuration into the database
            conn.Insert(newConfig);
            CloseConnection();
        }

        public void ReleaseInstance()
        {
            if (instance != null)
            {
                instance.CloseConnection();
                instance = null;
            }
        }
    }
}
