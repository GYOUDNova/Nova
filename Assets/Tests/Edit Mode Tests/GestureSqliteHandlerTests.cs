
using NOVA.Scripts;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEngine;

[TestFixture]
public class GestureSqliteHandlerTests
{
    private const string DatabaseName = "TESTING.db";
    private const string GestureAssetsDirName = "GestureAssets";

    private string gestureAssetsPath;
    private string databasePath;
    private string metaPath;

    private GestureSqliteHandler handler;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        // Gather test data
        gestureAssetsPath = Path.Combine(Application.streamingAssetsPath, GestureAssetsDirName);
        databasePath = Path.Combine(gestureAssetsPath, DatabaseName);
        metaPath = $"{databasePath}.meta";
    }

    [SetUp]
    public void Setup()
    {
        // Cleanup any existing test data (if any)
        Cleanup();

        // Initiate the handler
        handler = GestureSqliteHandler.Instance(DatabaseName);
    }

    [TearDown]
    public void TearDown()
    {
        // Remove existing data
        Cleanup();
    }

    [Test]
    public void Constructor_DatabaseName_PathsExist()
    {
        // Arrange & Act in Setup

        // Assert

        Assert.IsTrue(Directory.Exists(gestureAssetsPath), "GestureAssets directory does not exist under StreamingAssets.");
        Assert.IsTrue(File.Exists(databasePath), $"Database {DatabaseName} does not exist under GestureAssets.");
    }

    [Test]
    public void Constructor_DatabaseName_TablesExist()
    {
        // Arrange & Act in Setup

        // Assert

        Assert.IsTrue(handler.HasTable("Configuration"), "Configuration table does not exist.");
        Assert.IsTrue(handler.HasTable("GestureCategory"), "GestureCategory table does not exist.");
        Assert.IsTrue(handler.HasTable("GestureData"), "GestureData table does not exist.");
        Assert.IsTrue(handler.HasTable("PredefinedGesture"), "PredefinedGesture table does not exist.");
        Assert.IsTrue(handler.HasTable("CustomGesture"), "CustomGesture table does not exist.");
        Assert.IsTrue(handler.HasTable("Landmark"), "Landmark table does not exist.");
        Assert.IsTrue(handler.HasTable("LandmarkDistance"), "LandmarkDistance table does not exist.");
        Assert.IsTrue(handler.HasTable("GestureImage"), "GestureImage table does not exist.");
        Assert.IsTrue(handler.HasTable("RecognitionLog"), "RecognitionLog table does not exist");
        Assert.IsFalse(handler.HasTable("NonExistentTable"), "NonExistentTable should not exist in the database.");
    }

    [Test]
    public void Instance_SingletonInstance_CreatesOnlyOneInstance()
    {
        // Arrange & Act
        var secondInstance = GestureSqliteHandler.Instance(DatabaseName);

        // Assert
        Assert.AreSame(handler, secondInstance, "Multiple instances of GestureSqliteHandler were created.");
    }

    [Test]
    public void Instance_DifferentDatabaseName_ThrowsException()
    {
        // Arrange
        var newDatabaseName = "DIFFERENT.db";

        // Act & Assert
        Assert.Throws<HandlerExistsException>(() => GestureSqliteHandler.Instance(newDatabaseName),
                  "HandlerExistsException not thrown");
    }

    [Test]
    public void AddItemByName_ValidConfig_SavesToDatabase()
    {
        // Arrange
        var newConfig = new Configuration
        {
            Name = "Test Config",
            Gamma = 1,
            ChainTimer = 0.5f,
            LandmarkTolerance = 0.1f,
            ImageExtension = GestureImageExtension.Png
        };

        // Act
        handler.AddItemByName(newConfig, newConfig.Name);
        var retrievedConfig = handler.GetObjectByName<Configuration>(newConfig.Name);

        // Assert
        Assert.IsNotNull(retrievedConfig, "Configuration was not saved to the database.");
        Assert.AreEqual(newConfig.Name, retrievedConfig.Name, "Configuration name does not match.");
        Assert.AreEqual(newConfig.Gamma, retrievedConfig.Gamma, "Gamma does not match.");
        Assert.AreEqual(newConfig.ChainTimer, retrievedConfig.ChainTimer, "ChainTimer does not match.");
        Assert.AreEqual(newConfig.LandmarkTolerance, retrievedConfig.LandmarkTolerance, "LandmarkTolerance does not match.");
        Assert.AreEqual(newConfig.ImageExtension, retrievedConfig.ImageExtension, "ImageExtension does not match.");
    }

    [Test]
    public void AddItemByName_ExistingConfig_ThrowsException()
    {
        // Arrange
        var newConfig = new Configuration
        {
            Name = "Another Config",
            Gamma = 1,
            ChainTimer = 0.5f,
            LandmarkTolerance = 0.1f,
            ImageExtension = GestureImageExtension.Png
        };

        handler.AddItemByName(newConfig, newConfig.Name);

        // Act & Assert
        Assert.Throws<ItemAlreadyExistsException>(() => handler.AddItemByName(newConfig, newConfig.Name),
                  "ItemAlreadyExistsException exception not thrown for existing configuration.");
    }

    [Test]
    public void GetObjectById_NonExistentId_ThrowsException()
    {
        // Arrange
        var nonExistentId = 2;

        // Act & Assert
        Assert.Throws<ItemNotFoundException>(() => handler.GetObjectById<Configuration>(nonExistentId),
                  "ItemNotFoundException not thrown for non-existent ID.");
    }

    [Test]
    public void GetObjectByName_ValidName_ReturnsConfiguration()
    {
        // Arrange
        var newConfig = new Configuration
        {
            Name = "Config By Name",
            Gamma = 1,
            ChainTimer = 0.5f,
            LandmarkTolerance = 0.1f,
            ImageExtension = GestureImageExtension.Png
        };

        handler.AddItemByName(newConfig, newConfig.Name);

        // Act
        var retrievedConfig = handler.GetObjectByName<Configuration>(newConfig.Name);

        // Assert
        Assert.IsNotNull(retrievedConfig, "Configuration was not retrieved by name.");
        Assert.AreEqual(newConfig.Name, retrievedConfig.Name, "Configuration name does not match.");
    }

    [Test]
    public void GetObjectByName_NonExistentName_ThrowsException()
    {
        // Arrange
        var nonExistentName = "NonExistent Config";
        // Act & Assert
        Assert.Throws<ItemNotFoundException>(() => handler.GetObjectByName<Configuration>(nonExistentName),
                  "ItemNotFoundException not thrown for non-existent name.");
    }

    [Test]
    public void GetObjectByName_NonExistentProperty_ThrowsException()
    {
        // Arrange
        var nonExistentName = "NonExistent";

        // Act & Assert
        Assert.Throws<PropertyNotFoundException>(() => handler.GetObjectByName<Landmark>(nonExistentName),
                  "PropertyNotFoundException not thrown for non-existent property.");
    }

    [Test]
    public void AddItem_ValidGesture_SavesToDatabase()
    {
        // Arrange (generic Predefined category exists)
        var gestureData = new GestureData
        {
            Name = "Test Gesture",
            GestureImageName = "test_image.png",
            GestureCategoryId = 1, // Assuming the category ID is 1
            IsPredefined = true
        };

        handler.AddItemByName(gestureData, gestureData.Name);

        // Act
        var predefinedGesture = new PredefinedGesture
        {
            GestureDataId = gestureData.GestureDataId,
        };

        handler.AddItem(predefinedGesture);

        var retrievedGesture = handler.GetObjectById<PredefinedGesture>(1);
        var retrievedGestureData = handler.GetObjectByName<GestureData>(gestureData.Name);

        // Assert
        Assert.IsTrue(handler.GestureExists(gestureData.Name));
        Assert.IsNotNull(retrievedGesture, "PredefinedGesture was not saved to the database.");
        Assert.IsNotNull(retrievedGestureData, "GestureData was not saved to the database.");
        Assert.AreEqual(gestureData.Name, retrievedGestureData.Name, "GestureData name does not match.");
    }

    [Test]
    public void GetGestureInfo_ValidGesture_ReturnsGestureInfo()
    {
        // Arrange
        var gestureData = new GestureData
        {
            Name = "Test Gesture",
            GestureImageName = "test_image.png",
            GestureCategoryId = 1, // Assuming the category ID is 1
            IsPredefined = true
        };
        handler.AddItemByName(gestureData, gestureData.Name);

        var predefinedGesture = new PredefinedGesture
        {
            GestureDataId = gestureData.GestureDataId,
        };
        handler.AddItem(predefinedGesture);

        // Create a sample image for the gesture
        var gestureImage = new GestureImage
        {
            Name = gestureData.GestureImageName,
            FileExtension = GestureImageExtension.Png,
            GestureId = predefinedGesture.PredefinedGestureId,
            IsPredefined = gestureData.IsPredefined
        };

        handler.AddItemByName(gestureImage, gestureImage.Name);

        // Create two sample landmarks
        var landmark1 = new Landmark
        {
            LandmarkIndex = 1,
            X = 0.1f,
            Y = 0.2f,
            Z = 0.3f,
            GestureId = predefinedGesture.PredefinedGestureId,
            IsPredefined = gestureData.IsPredefined
        };

        var landmark2 = new Landmark
        {
            LandmarkIndex = 2,
            X = 0.4f,
            Y = 0.5f,
            Z = 0.6f,
            GestureId = predefinedGesture.PredefinedGestureId,
            IsPredefined = gestureData.IsPredefined
        };

        handler.AddItem(landmark1);
        handler.AddItem(landmark2);

        // Create a sample distance between landmarks
        var distance = new LandmarkDistance
        {
            GestureId = predefinedGesture.PredefinedGestureId,
            IsPredefined = gestureData.IsPredefined,
            Distance = 0.5f,
            LandmarkId = landmark1.LandmarkId,
            OtherLandmarkId = landmark2.LandmarkId
        };

        handler.AddItem(distance);

        // Act
        var gestureInfo = handler.GetGestureInfo(gestureData.Name);

        // Assert
        Assert.IsNotNull(gestureInfo, "Gesture info was not retrieved.");
        Assert.AreEqual(gestureData.Name, gestureInfo.Data.Name, "GestureData name does not match.");
        Assert.AreEqual(predefinedGesture.PredefinedGestureId, gestureInfo.GestureId, "PredefinedGesture ID does not match.");
        Assert.AreEqual(gestureInfo.Category.CategoryId, gestureData.GestureCategoryId);
        Assert.AreEqual(gestureData.IsPredefined, gestureInfo.IsPredefined, "IsPredefined flag does not match.");
        Assert.AreEqual(predefinedGesture.PredefinedGestureId, gestureInfo.Image.GestureId, "GestureImage ID does not match.");
        Assert.IsTrue(gestureInfo.Landmarks.Count == 2, "No landmarks were retrieved for the gesture.");
        Assert.IsTrue(gestureInfo.Distances.Count == 1, "No distances were retrieved for the gesture.");
    }

    [Test]
    public void GetGestureInfo_NonExistentGesture_ThrowsException()
    {
        // Arrange
        var nonExistentGestureName = "NonExistent Gesture";

        // Act & Assert
        Assert.Throws<ItemNotFoundException>(() => handler.GetGestureInfo(nonExistentGestureName),
                  "ItemNotFoundException not thrown for non-existent gesture.");
    }

    [Test]
    public void AddGesture_ValidInfo_SavesToDatabase()
    {
        // Arrange
        var queryableInfo = new QueryableGestureInfo
        {
            GestureName = "Queryable Gesture",
            CategoryName = "New Sample",
            ImageName = "queryable_image",
            IsPredefined = true,
            Landmarks = new List<Landmark>
            {
                new Landmark { LandmarkIndex = 1, X = 0.1f, Y = 0.2f, Z = 0.3f },
                new Landmark { LandmarkIndex = 2, X = 0.4f, Y = 0.5f, Z = 0.6f }
            },
            Distances = new List<LandmarkDistance>
            {
                new LandmarkDistance { Distance = 0.5f, LandmarkId = 1, OtherLandmarkId = 2 }
            }
        };

        // Act
        handler.AddGesture(queryableInfo);
        var gestureInfo = handler.GetGestureInfo(queryableInfo.GestureName);

        //Assert
        Assert.IsNotNull(gestureInfo, "Gesture info was not retrieved.");
        Assert.AreEqual(queryableInfo.GestureName, gestureInfo.Data.Name, "GestureData name does not match.");
        Assert.AreEqual(queryableInfo.CategoryName, gestureInfo.Category.Name, "GestureCategory name does not match.");
        Assert.AreEqual(queryableInfo.ImageName, gestureInfo.Image.Name, "GestureImage name does not match.");
        Assert.IsTrue(gestureInfo.Landmarks.Count == 2, "No landmarks were retrieved for the gesture.");
        Assert.IsTrue(gestureInfo.Distances.Count == 1, "No distances were retrieved for the gesture.");
    }

    [Test]
    public void GetObjects_Landmarks_ReturnsAllLandmarks()
    {
        // Arrange
        var landmarks = new List<Landmark>
        {
            new Landmark { LandmarkIndex = 1, X = 0.1f, Y = 0.2f, Z = 0.3f, GestureId = 1, IsPredefined = true },
            new Landmark { LandmarkIndex = 2, X = 0.4f, Y = 0.5f, Z = 0.6f, GestureId = 1, IsPredefined = true }
        };

        // Act
        foreach (var landmark in landmarks)
        {
            handler.AddItem(landmark);
        }

        var retrievedLandmarks = handler.GetObjects<Landmark>();

        // Assert
        Assert.IsNotNull(retrievedLandmarks, "No landmarks were retrieved from the database.");
        Assert.AreEqual(2, retrievedLandmarks.Count, "Incorrect number of landmarks retrieved from the database.");
    }


    [Test]
    public void MultiThreadedDatabaseOperations_NoIssues()
    {
        // Arrange
        var threadCount = 10;
        var threads = new List<Thread>();
        var successCount = 0;
        var failedThreads = new List<int>(); // To track which threads failed

        for (int i = 0; i < threadCount; i++)
        {
            int threadId = Thread.CurrentThread.ManagedThreadId + i; // Generate unique ID for gesture name
            threads.Add(new Thread(() =>
            {
                try
                {
                    QueryableGestureInfo qgi = new QueryableGestureInfo()
                    {
                        GestureName = $"Multi{threadId}", // Use unique ID to prevent ItemAlreadyExists
                        CategoryName = "Predefined",
                        ImageName = $"multi_{threadId}",
                        IsPredefined = true,
                        Landmarks = new List<Landmark>
                        {
                            new Landmark { LandmarkIndex = 1, X = 0.1f, Y = 0.2f, Z = 0.3f },
                            new Landmark { LandmarkIndex = 2, X = 0.4f, Y = 0.5f, Z = 0.6f }
                        },
                        Distances = new List<LandmarkDistance>
                        {
                            new LandmarkDistance { Distance = 0.5f, LandmarkId = 1, OtherLandmarkId = 2 }
                        }
                    };

                    handler.AddGesture(qgi);
                    Interlocked.Increment(ref successCount); // Thread-safe increment
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"{ex.Message}");
                    lock (failedThreads) // Protect access to shared list
                    {
                        failedThreads.Add(threadId);
                    }
                }
            }));
        }

        // Act
        foreach (var thread in threads)
        {
            thread.Start();
        }
        foreach (var thread in threads)
        {
            thread.Join(); // Wait for all threads to complete
        }

        Debug.Log($"Successfully added {successCount} gestures out of {threadCount} expected to the database.");

        // Assert that all operations succeeded
        Assert.AreEqual(threadCount, successCount, $"Expected all {threadCount} gestures to be added, but {threadCount - successCount} failed. Failed threads: {string.Join(", ", failedThreads)}");
    }

    [Test]
    public void GetAllUIGestures_ReturnsAllGestures()
    {
        // Arrange
        var gestureData1 = new GestureData
        {
            Name = "Gesture 1",
            GestureImageName = "gesture1.png",
            GestureCategoryId = 1,
            IsPredefined = true
        };
        handler.AddItemByName(gestureData1, gestureData1.Name);
        var gestureData2 = new GestureData
        {
            Name = "Gesture 2",
            GestureImageName = "gesture2.png",
            GestureCategoryId = 1,
            IsPredefined = true
        };
        handler.AddItemByName(gestureData2, gestureData2.Name);

        // Create the images
        var gestureImage1 = new GestureImage
        {
            Name = gestureData1.GestureImageName,
            FileExtension = GestureImageExtension.Png,
            GestureId = 1, // Assuming the ID of the first gesture
            IsPredefined = gestureData1.IsPredefined
        };

        var gestureImage2 = new GestureImage
        {
            Name = gestureData2.GestureImageName,
            FileExtension = GestureImageExtension.Png,
            GestureId = 2, // Assuming the ID of the second gesture
            IsPredefined = gestureData2.IsPredefined
        };

        handler.AddItemByName(gestureImage1, gestureImage1.Name);
        handler.AddItemByName(gestureImage2, gestureImage2.Name);

        // Act
        var allGestures = handler.GetAllUIGestures();

        // Assert
        Assert.IsNotNull(allGestures, "No gestures were retrieved from the database.");
        Assert.IsTrue(allGestures.Count >= 2, "Expected at least two gestures to be retrieved.");
    }

    [Test]
    public void DeleteGesture_ValidGesture_DeletesAllRelatedData()
    {
        // Arrange
        var gestureData = new GestureData
        {
            Name = "Delete Test Gesture",
            GestureImageName = "delete_test_image",
            GestureCategoryId = 1, // Assuming the category ID is 1
            IsPredefined = true
        };

        handler.AddItemByName(gestureData, gestureData.Name);

        var predefinedGesture = new PredefinedGesture
        {
            GestureDataId = gestureData.GestureDataId,
        };

        handler.AddItem(predefinedGesture);

        var gestureImage = new GestureImage
        {
            Name = gestureData.GestureImageName,
            FileExtension = GestureImageExtension.Png,
            GestureId = predefinedGesture.PredefinedGestureId,
            IsPredefined = gestureData.IsPredefined
        };

        handler.AddItemByName(gestureImage, gestureImage.Name);

        var landmark1 = new Landmark
        {
            LandmarkIndex = 1,
            X = 0.1f,
            Y = 0.2f,
            Z = 0.3f,
            GestureId = predefinedGesture.PredefinedGestureId,
            IsPredefined = gestureData.IsPredefined
        };

        handler.AddItem(landmark1);
        var landmark2 = new Landmark
        {
            LandmarkIndex = 2,
            X = 0.4f,
            Y = 0.5f,
            Z = 0.6f,
            GestureId = predefinedGesture.PredefinedGestureId,
            IsPredefined = gestureData.IsPredefined
        };

        handler.AddItem(landmark2);

        var distance = new LandmarkDistance
        {
            GestureId = predefinedGesture.PredefinedGestureId,
            IsPredefined = gestureData.IsPredefined,
            Distance = 0.5f,
            LandmarkId = landmark1.LandmarkId,
            OtherLandmarkId = landmark2.LandmarkId
        };

        handler.AddItem(distance);

        // Act
        handler.DeleteGesture(gestureData.Name);

        // Assert that all related data is deleted
        Assert.Throws<ItemNotFoundException>(() => handler.GetObjectByName<GestureData>(gestureData.Name), "GestureData was not deleted.");
        Assert.Throws<ItemNotFoundException>(() => handler.GetObjectById<PredefinedGesture>(predefinedGesture.PredefinedGestureId), "PredefinedGesture was not deleted.");
        Assert.Throws<ItemNotFoundException>(() => handler.GetObjectByName<GestureImage>(gestureImage.Name), "GestureImage was not deleted.");
        Assert.Throws<ItemNotFoundException>(() => handler.GetObjectById<Landmark>(landmark1.LandmarkId), "Landmark 1 was not deleted.");
        Assert.Throws<ItemNotFoundException>(() => handler.GetObjectById<Landmark>(landmark2.LandmarkId), "Landmark 2 was not deleted.");
        Assert.Throws<ItemNotFoundException>(() => handler.GetObjectById<LandmarkDistance>(distance.LandmarkDistanceId), "LandmarkDistance was not deleted.");
    }

    // Utility methods

    private void Cleanup()
    {
        if (handler != null)
        {
            handler = null;
        }

        if (Directory.Exists(gestureAssetsPath))
        {
            if (File.Exists(databasePath))
            {
                File.Delete(databasePath);
            }
            if (File.Exists(metaPath))
            {
                File.Delete(metaPath);
            }
        }

        // Release handler instance (if it exists)
        GestureSqliteHandler.ReleaseInstance();
    }
}
