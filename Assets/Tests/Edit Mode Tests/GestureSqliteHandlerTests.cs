
using NOVA.Scripts;
using NUnit.Framework;
using System.IO;
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
        // Keep the handler instance per test 
        handler.ReleaseInstance();
        handler = null;

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
    }

    // generate a test to ensure that only one instance of the class is created

    [Test]
    public void Instance_SingletonInstance_CreatesOnlyOneInstance()
    {
        // Arrange & Act
        var secondInstance = GestureSqliteHandler.Instance(DatabaseName);

        // Assert
        Assert.AreSame(handler, secondInstance, "Multiple instances of GestureSqliteHandler were created.");
    }

    // generate a test that attempts to create a new instance of the class with a different database name

    [Test]
    public void Instance_DifferentDatabaseName_ThrowsException()
    {
        // Arrange
        var newDatabaseName = "DIFFERENT.db";

        // Act & Assert
        Assert.Throws<HandlerExistsException>(() => GestureSqliteHandler.Instance(newDatabaseName),
                  "HandlerExistsException not thrown");
    }

    // generate a test that creates a configuration object and saves it to the database

    [Test]
    public void CreateConfiguration_ValidConfig_SavesToDatabase()
    {
        // Arrange
        var newConfig = new Configuration
        {
            ConfigurationName = "Test Config",
            Gamma = 1,
            ChainTimer = 0.5f,
            LandmarkTolerance = 0.1f,
            ImageExtension = GestureImageExtension.Png
        };

        // Act
        handler.CreateConfiguration(newConfig);
        var retrievedConfig = handler.GetObjectById<Configuration>(1);

        // Assert
        Assert.IsNotNull(retrievedConfig, "Configuration was not saved to the database.");
        Assert.AreEqual(newConfig.ConfigurationName, retrievedConfig.ConfigurationName, "Configuration name does not match.");
        Assert.AreEqual(newConfig.Gamma, retrievedConfig.Gamma, "Gamma does not match.");
        Assert.AreEqual(newConfig.ChainTimer, retrievedConfig.ChainTimer, "ChainTimer does not match.");
        Assert.AreEqual(newConfig.LandmarkTolerance, retrievedConfig.LandmarkTolerance, "LandmarkTolerance does not match.");
        Assert.AreEqual(newConfig.ImageExtension, retrievedConfig.ImageExtension, "ImageExtension does not match.");
    }

    // generate a test that attempts to create a configuration object with the same name as an existing one
    [Test]

    public void CreateConfiguration_ExistingConfig_ThrowsException()
    {
        // Arrange
        var newConfig = new Configuration
        {
            ConfigurationName = "Another Config",
            Gamma = 1,
            ChainTimer = 0.5f,
            LandmarkTolerance = 0.1f,
            ImageExtension = GestureImageExtension.Png
        };

        handler.CreateConfiguration(newConfig);

        // Act & Assert
        Assert.Throws<ConfigurationAlreadyExists>(() => handler.CreateConfiguration(newConfig),
                  "ConfigurationAlreadyExists exception not thrown for existing configuration.");
    }

    // generate a test that attempts to find a configuration object that does not exist in the database (id 2)

    [Test]
    public void GetObjectById_NonExistentId_ThrowsException()
    {
        // Arrange
        var nonExistentId = 2;

        // Act & Assert
        Assert.Throws<ItemNotFoundException>(() => handler.GetObjectById<Configuration>(nonExistentId),
                  "ItemNotFoundException not thrown for non-existent ID.");
    }


    // Utility methods
    private void Cleanup()
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
}
