using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace NOVA.Scripts
{
    public class GestureSqliteHandler : SqliteHandler
    {
        // Utility constants
        private const string GesturesDatabaseName = "Gestures.db";
        private const string GestureAssetsDirName = "GestureAssets";

        private static GestureSqliteHandler instance; // Singleton instance

        private GestureSqliteHandler(string databaseName)
            : base(databaseName)
        {
            string gestureAssetsDir = Path.Combine(Application.streamingAssetsPath, GestureAssetsDirName);
            Initialize(gestureAssetsDir);
        }

        // Overriden Initialize method to set up the database
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
                conn.CreateTable<RecognitionLog>();
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
                // 6. RecognitionLog
                // 7. Landmark
                // 8. LandmarkDistance
                // 9. GestureImage

                // Populate database with default data

                var defaultConfig = new Configuration
                {
                    ConfigurationId = 1, // Assuming this is the first config
                    Name = "Default",
                    ChainTimer = 2.0f,
                    Gamma = 50,
                    ImageExtension = GestureImageExtension.Jpeg,
                    LandmarkTolerance = 0.1f,
                    Active = true
                };

                AddItemByName(defaultConfig, defaultConfig.Name);

                var predefinedCategory = new GestureCategory
                {
                    CategoryId = 1, // Assuming this is the first category
                    Name = "Predefined"
                };

                AddItemByName(predefinedCategory, predefinedCategory.Name);

                Debug.Log($"Database {dbPath} created successfully with initial tables.");
                CloseConnection();
            }
        }

        // Easy-access method to check if a gesture exists by name
        public bool GestureExists(string gestureName)
        {
            conn = GetSqliteConnection();

            var query = "SELECT COUNT(*) FROM GestureData WHERE Name = ?";
            var exists = conn.ExecuteScalar<int>(query, gestureName) > 0;

            CloseConnection();

            return exists;
        }

        // Method to retrieve all the information about a gesture by its name
        public GestureInfo GetGestureInfo(string gestureName)
        {
            var gestureData = GetObjectByName<GestureData>(gestureName);

            // Use the GestureData to retrieve the associated Gesture object (PredefinedGesture or CustomGesture based on flag)

            conn = GetSqliteConnection();

            int gestureId;
            string query;
            if (gestureData.IsPredefined)
            {
                query = "SELECT PredefinedGestureId FROM PredefinedGesture WHERE GestureDataId = ?";
            }
            else
            {
                query = "SELECT CustomGestureId FROM CustomGesture WHERE GestureDataId = ?";
            }

            // Query is the same for both cases, but we need to ensure we get the correct ID based on the gesture type
            gestureId = conn.ExecuteScalar<int>(query, gestureData.GestureDataId);

            var category = conn.Get<GestureCategory>(gestureData.GestureCategoryId);
            var image = conn.Get<GestureImage>(gestureData.GestureImageName);

            // We have to retrieve landmarks and distances based on the gestureId and whether it is predefined or not,
            // since they are stored in separate tables for predefined and custom gestures
            var landmarks = conn.Table<Landmark>().Where(l => l.GestureId == gestureId && l.IsPredefined == gestureData.IsPredefined).ToList();
            var distances = conn.Table<LandmarkDistance>().Where(ld => ld.GestureId == gestureId && ld.IsPredefined == gestureData.IsPredefined).ToList();

            CloseConnection();

            return new GestureInfo
            {
                GestureId = gestureId,
                GestureName = gestureData.Name,
                Category = category,
                Image = image,
                Data = gestureData,
                Landmarks = landmarks,
                Distances = distances
            };
        }

        // Method to create a gesture based on sample data that is then mapped
        // and linked on the database
        public void AddGesture(QueryableGestureInfo qgi)
        {
            // The idea is that when we create a gesture, it will provide the ID that we can use to link the other tables.
            // This assumes that there won't be any conflicts with existing names or IDs.

            conn = GetSqliteConnection();

            // Create the GestureData obj
            var gestureData = new GestureData
            {
                Name = qgi.GestureName,
                GestureImageName = qgi.ImageName,
                GestureCategoryId = conn.Table<GestureCategory>().FirstOrDefault(c => c.Name == qgi.CategoryName).CategoryId,
                IsPredefined = qgi.IsPredefined
            };

            conn.Insert(gestureData);

            int gestureDataId = gestureData.GestureDataId; // Get the ID of the newly inserted GestureData
            int gestureId;

            // Create the Gesture obj (Predefined or Custom based on IsPredefined)
            if (qgi.IsPredefined)
            {
                var predefinedGesture = new PredefinedGesture
                {
                    GestureDataId = gestureDataId
                };
                conn.Insert(predefinedGesture);
                gestureId = predefinedGesture.PredefinedGestureId;
            }
            else
            {
                var customGesture = new CustomGesture
                {
                    GestureDataId = gestureDataId
                };
                conn.Insert(customGesture);
                gestureId = customGesture.CustomGestureId;
            }

            // Get the current configuration that is Active
            var config = conn.Table<Configuration>().FirstOrDefault(c => c.Active);

            // Create the GestureImage object
            var gestureImage = new GestureImage
            {
                Name = qgi.ImageName,
                GestureId = gestureId,
                FileExtension = config.ImageExtension,
                IsPredefined = qgi.IsPredefined
            };

            conn.Insert(gestureImage);

            // Insert landmarks
            foreach (var landmark in qgi.Landmarks)
            {
                landmark.GestureId = gestureId;
                landmark.IsPredefined = qgi.IsPredefined;
                conn.Insert(landmark);
            }

            // Insert distances
            foreach (var distance in qgi.Distances)
            {
                distance.GestureId = gestureId;
                distance.IsPredefined = qgi.IsPredefined;
                conn.Insert(distance);
            }

            CloseConnection();
        }

        // Handle singleton instance creation and management
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

        // This is only to be called when the instance is no longer needed
        public void ReleaseInstance()
        {
            if (instance != null)
            {
                instance.CloseConnection();
                instance = null;
            }
        }
    }

    // This is to simplify the retrieval of gesture information
    public struct GestureInfo
    {
        public int GestureId;
        public string GestureName;
        public GestureCategory Category;
        public GestureImage Image;
        public GestureData Data;
        public List<Landmark> Landmarks;
        public List<LandmarkDistance> Distances;
        public readonly bool IsPredefined => Data.IsPredefined;
    }

    // This is to simplify the creation of gestures from the UI or other sources
    public struct QueryableGestureInfo
    {
        public string GestureName;
        public string CategoryName;
        public string ImageName;
        public List<Landmark> Landmarks;
        public List<LandmarkDistance> Distances;
        public bool IsPredefined;
    }
}
