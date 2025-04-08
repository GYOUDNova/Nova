using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;
using NOVA.Scripts;
using NUnit.Framework;

/// <summary>
/// This class contains tests for the SampleSqliteHandler class
/// </summary>

[TestFixture]
public class SampleSqliteHandlerTests
{
    /// <summary>
    /// The name of the SQLite database file
    /// </summary>
    private const string DatabaseName = "test.db";

    private SampleSqliteHandler handler;

    [OneTimeSetUp]
    public void SetUp()
    {
        // Delete the database file if it exists
        var dbPath = System.IO.Path.Combine(Application.dataPath, DatabaseName);

        if (System.IO.File.Exists(dbPath))
        {
            System.IO.File.Delete(dbPath);
        }

        // Create a new instance of SampleSqliteHandler
        handler = new SampleSqliteHandler();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        // Clean up
        handler.CloseConnection();
        handler = null;

        // Delete the database files if it exists
        var dbPath = System.IO.Path.Combine(Application.dataPath, DatabaseName);
        var metaPath = $"{dbPath}.meta";

        if (System.IO.File.Exists(dbPath))
        {
            System.IO.File.Delete(dbPath);
            System.IO.File.Delete(metaPath);
        }
    }

    [UnityTest]
    public IEnumerator Constructor_DatabaseCreation_DatabaseExists()
    {
        // Arrange
        var dbPath = System.IO.Path.Combine(Application.dataPath, DatabaseName);

        // Act (this happens in the constructor)
        yield return null;

        // Assert
        Assert.IsTrue(System.IO.File.Exists(dbPath), "Database file should exist after constructor is called.");
    }

    [UnityTest]
    public IEnumerator GetEntities_ExistingDatabase_EntityPropertiesAreCorrect()
    {
        // Arrange
        var dbPath = System.IO.Path.Combine(Application.dataPath, DatabaseName);

        // Act
        var entities = handler.GetEntities();
        yield return null;

        // Assert
        Assert.IsNotNull(entities, "Entities list should not be null.");
        Assert.IsTrue(entities.Count > 0, "Entities list should contain elements.");

        foreach (var entity in entities)
        {
            Assert.IsFalse(string.IsNullOrEmpty(entity.FirstName), "FirstName should not be null or empty.");
            Assert.IsFalse(string.IsNullOrEmpty(entity.LastName), "LastName should not be null or empty.");
            Assert.IsFalse(string.IsNullOrEmpty(entity.Email), "Email should not be null or empty.");
        }
    }

    [UnityTest]
    public IEnumerator GetRelationshipEntities_ExistingDatabase_RelationshipEntityPropertiesAreCorrect()
    {
        // Arrange
        var dbPath = System.IO.Path.Combine(Application.dataPath, DatabaseName);

        // Act
        var relationshipEntities = handler.GetRelationshipEntities();
        yield return null;

        // Assert
        Assert.IsNotNull(relationshipEntities, "Relationship entities list should not be null.");
        Assert.IsTrue(relationshipEntities.Count > 0, "Relationship entities list should contain elements.");

        foreach (var relationshipEntity in relationshipEntities)
        {
            Assert.IsTrue(relationshipEntity.EntityId > 0, "EntityId should be greater than 0.");
            Assert.IsNull(relationshipEntity.Entity, "Entity relation should be null.");
        }
    }

    [UnityTest]
    public IEnumerator GetRelationshipMapping_ExistingDatabase_RelationshipMappingIsCorrect()
    {
        // Arrange
        var dbPath = System.IO.Path.Combine(Application.dataPath, DatabaseName);

        // Act
        var mapping = handler.GetRelationshipMapping();
        yield return null;

        // Assert
        Assert.IsNotNull(mapping, "Mapping should not be null.");
        Assert.IsTrue(mapping.Count > 0, "Mapping should contain elements.");

        foreach (var kvp in mapping)
        {
            Assert.IsNotNull(kvp.Key, "Key (RelationshipEntity) should not be null.");
            Assert.IsNotNull(kvp.Value, "Value (Entity) should not be null.");
            Assert.IsTrue(kvp.Key.EntityId > 0, "EntityId should be greater than 0.");
            Assert.IsFalse(string.IsNullOrEmpty(kvp.Value.FirstName), "FirstName should not be null or empty.");
            Assert.IsFalse(string.IsNullOrEmpty(kvp.Value.LastName), "LastName should not be null or empty.");
            Assert.IsFalse(string.IsNullOrEmpty(kvp.Value.Email), "Email should not be null or empty.");
        }
    }
}
