using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace NOVA.Scripts
{
    public class GestureSqliteHandler : SqliteHandler
    {
        // Utility constants
        private const string GesturesDatabaseName = "Gestures.db";

        private static GestureSqliteHandler instance; // Singleton instance

        private GestureSqliteHandler(string databaseName)
            : base(databaseName)
        {
            string gestureAssetsDir = Path.Combine(Application.streamingAssetsPath, HelperConstants.GestureAssetsDirName);
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

                lock (lockObject)
                {
                    using var conn = GetSqliteConnection();

                    conn.CreateTable<Configuration>();
                    conn.CreateTable<GestureCategory>();
                    conn.CreateTable<GestureData>();
                    conn.CreateTable<PredefinedGesture>();
                    conn.CreateTable<CustomGesture>();
                    conn.CreateTable<RecognitionLog>();
                    conn.CreateTable<Landmark>();
                    conn.CreateTable<LandmarkDistance>();
                    conn.CreateTable<LandmarkDirection>(); //Adding directions
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

                    conn.Insert(defaultConfig); // Direct insert for thread safety

                    var predefinedCategory = new GestureCategory
                    {
                        CategoryId = 1, // Assuming this is the first category
                        Name = "Predefined"
                    };

                    conn.Insert(predefinedCategory); // Direct insert for thread safety

                    Debug.Log($"Database {dbPath} created successfully with initial tables.");
                }
            }
        }

        // Easy-access method to check if a gesture exists by name
        public bool GestureExists(string gestureName)
        {
            lock (lockObject)
            {
                using var conn = GetSqliteConnection();

                var query = "SELECT COUNT(*) FROM GestureData WHERE Name = ?";
                var exists = conn.ExecuteScalar<int>(query, gestureName) > 0;


                return exists;
            }
        }

        public bool HasPredefinedGestures()
        {
            lock (lockObject)
            {
                using var conn = GetSqliteConnection();

                // Check if there are any predefined gestures in the database
                return conn.Table<PredefinedGesture>().Count() > 0;
            }
        }

        // Method to retrieve all gestures from the database, limited to only UI information
        public List<GestureInfo> GetAllUIGestures()
        {
            lock (lockObject)
            {
                using var conn = GetSqliteConnection();

                // Retrieve all GestureData objects
                var gestureDataList = conn.Table<GestureData>().ToList();
                var gestureInfos = new List<GestureInfo>();

                foreach (var gestureData in gestureDataList)
                {
                    var category = conn.Get<GestureCategory>(gestureData.GestureCategoryId);
                    var image = conn.Get<GestureImage>(gestureData.GestureImageName);

                    /*
                     * The only information we need for the UI is:
                     * - Gesture Name
                     * - Category
                     * - Image (location to fetch the image from the assets)
                     * - GestureData to retrieve other properties (like IsPredefined)
                     *
                    */

                    gestureInfos.Add(new GestureInfo
                    {
                        GestureName = gestureData.Name,
                        Category = category,
                        Image = image,
                        Data = gestureData
                    });
                }

                return gestureInfos;
            }
        }

        // Method to retrieve all distances for a gesture by its name
        public List<float> GetDistancesByName(string gestureName)
        {
            lock (lockObject)
            {
                return GetGestureInfo(gestureName).Distances.ConvertAll(ld => ld.Distance);
            }
        }

        // Mehtod to retrieve all directions for a gesture by its name
        public List<string> GetDirectionsByName(string gestureName)
        {
            lock (lockObject)
            {
                return GetGestureInfo(gestureName).Directions.ConvertAll(id => id.Direction);
            }
        }

        // Method to retrieve all gestures from the database, limited to only UI information
        public List<GestureInfo> GetUIGesturesByCategory(string categoryName)
        {
            lock (lockObject)
            {
                using var conn = GetSqliteConnection();

                // Retrieve all GestureData objects
                var gestureDataList = conn.Table<GestureData>().ToList();
                var gestureInfos = new List<GestureInfo>();

                foreach (var gestureData in gestureDataList)
                {
                    var category = conn.Get<GestureCategory>(gestureData.GestureCategoryId);

                    if (category.Name == categoryName)
                    {
                        var image = conn.Get<GestureImage>(gestureData.GestureImageName);

                        /*
                         * The only information we need for the UI is:
                         * - Gesture Name
                         * - Category
                         * - Image (location to fetch the image from the assets)
                         * - GestureData to retrieve other properties (like IsPredefined)
                         *
                        */

                        gestureInfos.Add(new GestureInfo
                        {
                            GestureName = gestureData.Name,
                            Category = category,
                            Image = image,
                            Data = gestureData
                        });
                    }
                }
                return gestureInfos;
            }
        }

        // Method to retrieve all the information about a gesture by its name
        public GestureInfo GetGestureInfo(string gestureName)
        {
            var gestureData = GetObjectByName<GestureData>(gestureName);

            // Use the GestureData to retrieve the associated Gesture object (PredefinedGesture or CustomGesture based on flag)

            lock (lockObject)
            {
                using var conn = GetSqliteConnection();

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
                var directions = conn.Table<LandmarkDirection>().Where(id => id.GestureId == gestureId && id.IsPredefined == gestureData.IsPredefined).ToList();

                return new GestureInfo
                {
                    GestureId = gestureId,
                    GestureName = gestureData.Name,
                    Category = category,
                    Image = image,
                    Data = gestureData,
                    Landmarks = landmarks,
                    Distances = distances,
                    Directions = directions
                };
            }
        }

        // Method to create a gesture based on sample data that is then mapped
        // and linked on the database
        public void AddGesture(QueryableGestureInfo qgi)
        {
            // The idea is that when we create a gesture, it will provide the ID that we can use to link the other tables.
            // This assumes that there won't be any conflicts with existing names or IDs,
            // as in, gesture name checks should be done before calling this method.

            lock (lockObject)
            {
                using var conn = GetSqliteConnection();

                // Create the category if it doesn't exist
                var category = conn.Table<GestureCategory>().FirstOrDefault(c => c.Name == qgi.CategoryName);
                if (category == null)
                {
                    category = new GestureCategory
                    {
                        Name = qgi.CategoryName
                    };

                    conn.Insert(category);
                }

                var gestureData = new GestureData
                {
                    Name = qgi.GestureName,
                    GestureImageName = qgi.ImageName,
                    GestureCategoryId = category.CategoryId,
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

                // Insert directions
                foreach (var direction in qgi.Direction)
                {
                    direction.GestureId = gestureId;
                    direction.IsPredefined = qgi.IsPredefined;
                    conn.Insert(direction);
                }
            }
        }

        public void DeleteConfiguration(string itemName)
        {
            lock (lockObject)
            {
                using var conn = GetSqliteConnection();
                var config = conn.Table<Configuration>().FirstOrDefault(c => c.Name == itemName);

                if (config is null)
                {
                    throw new ItemNotFoundException($"No configuration with name: {itemName} exists");
                }

                conn.Delete(config);
            }
        }

        // Public method to delete a gesture by its name
        public void DeleteGesture(string gestureName)
        {
            lock (lockObject)
            {
                using var conn = GetSqliteConnection();

                // Retrieve the GestureData object by name
                var gestureData = conn.Table<GestureData>().FirstOrDefault(g => g.Name == gestureName);

                if (gestureData == null)
                {
                    throw new ItemNotFoundException($"No gesture with name: {gestureName} exists");
                }

                int gestureID;

                // Retrieve the entire gesture object (Predefined or Custom) based on the IsPredefined flag, keep the ID for linking

                if (gestureData.IsPredefined)
                {
                    var predefinedGesture = conn.Table<PredefinedGesture>().FirstOrDefault(pg => pg.GestureDataId == gestureData.GestureDataId);

                    if (predefinedGesture == null)
                    {
                        throw new ItemNotFoundException($"No predefined gesture with name: {gestureName} exists");
                    }

                    gestureID = predefinedGesture.PredefinedGestureId;
                    conn.Delete(predefinedGesture);
                }
                else
                {
                    var customGesture = conn.Table<CustomGesture>().FirstOrDefault(cg => cg.GestureDataId == gestureData.GestureDataId);

                    if (customGesture == null)
                    {
                        throw new ItemNotFoundException($"No custom gesture with name: {gestureName} exists");
                    }

                    gestureID = customGesture.CustomGestureId;
                    conn.Delete(customGesture);
                }

                // Delete the image file before deleting the record
                var gestureImage = conn.Table<GestureImage>().FirstOrDefault(gi => gi.GestureId == gestureID);
                FileHandler.DeleteImageFromResources(gestureImage.Name, gestureImage.FileExtension);

                // Delete everything associated with the gesture
                conn.Delete<GestureData>(gestureData.GestureDataId);
                conn.Delete<GestureImage>(gestureData.GestureImageName);
                conn.Table<Landmark>().Delete(l => l.GestureId == gestureID && l.IsPredefined == gestureData.IsPredefined);
                conn.Table<LandmarkDistance>().Delete(ld => ld.GestureId == gestureID && ld.IsPredefined == gestureData.IsPredefined);
                conn.Table<LandmarkDirection>().Delete(id => id.GestureId == gestureID && id.IsPredefined == gestureData.IsPredefined);
            }
        }

        public Configuration GetActiveConfiguration()
        {
            lock (lockObject)
            {
                using var conn = GetSqliteConnection();

                // Retrieve the active configuration
                var config = conn.Table<Configuration>().FirstOrDefault(c => c.Active);

                if (config == null)
                {
                    throw new ItemNotFoundException("No active configuration found in the database.");
                }

                return config;
            }
        }

        public void SetActivePropertyToFalse(Configuration currActiveConfig)
        {
            lock (lockObject)
            {
                using var conn = GetSqliteConnection();
                currActiveConfig.Active = false;
                conn.Update(currActiveConfig);
            }
        }

        public void SetActivePropertyToTrue(Configuration toBeActiveConfig)
        {
            lock (lockObject)
            {
                using var conn = GetSqliteConnection();
                toBeActiveConfig.Active = true;
                conn.Update(toBeActiveConfig);
            }
        }

        // Handle singleton instance creation and management
        public static GestureSqliteHandler Instance(string databaseName = GesturesDatabaseName)
        {
            if (instance == null)
            {
                lock (lockObject)
                {
                    // This performs a second null-check
                    instance ??= new GestureSqliteHandler(databaseName);
                }
            }
            else if (instance.databaseName != databaseName)
            {
                throw new HandlerExistsException("Cannot change the database name after the instance has been created.");
            }

            return instance;
        }

        // This is only to be called when the instance is no longer needed
        public static void ReleaseInstance()
        {
            lock (lockObject)
            {
                if (instance != null)
                {
                    instance = null;
                }
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
        public List<LandmarkDirection> Directions;
        public readonly bool IsPredefined => Data.IsPredefined;

        public readonly bool Equals(QueryableGestureInfo qgi)
        {
            return GestureName == qgi.GestureName &&
                   Category.Name == qgi.CategoryName &&
                   Image.Name == qgi.ImageName &&
                   IsPredefined == qgi.IsPredefined &&
                   Landmarks.Count == qgi.Landmarks.Count &&
                   Distances.Count == qgi.Distances.Count &&
                   Directions.Count == qgi.Direction.Count;
        }
    }

    // This is to simplify the creation of gestures from the UI or other sources
    public struct QueryableGestureInfo
    {
        public string GestureName;
        public string CategoryName;
        public string ImageName;
        public List<Landmark> Landmarks;
        public List<LandmarkDistance> Distances;
        //public List<LandmarkAngle> Angles;
        public List<LandmarkDirection> Direction;
        public bool IsPredefined;
    }
}
