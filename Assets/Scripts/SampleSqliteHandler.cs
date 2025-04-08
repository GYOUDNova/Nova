using System.Collections.Generic;
using NUnit.Framework;
using SQLite;
using UnityEditor;
using UnityEngine;

namespace NOVA.Scripts
{
    /// <summary>
    /// This is a sample class that demonstrates how to use the SQLite.net library
    /// in Unity. It creates a sample SQLite database and applies migrations to it.
    /// </summary>

    public class SampleSqliteHandler
    {
        private const string DatabaseName = "test.db";
        private SQLiteConnection connection;

        public SampleSqliteHandler()
        {
            Debug.Log("Initializing SQlite database and applying migrations...");

            // Create required tables if they don't exist
            connection = GetConnection();

            var entityTableList = connection.GetTableInfo("Entity");

            if (entityTableList.Count == 0)
            {
                connection.CreateTable<Entity>();
                connection.CreateTable<RelationshipEntity>();

                // Populate the database with some test data
                Entity entity = new Entity()
                {
                    FirstName = "John",
                    LastName = "Doe",
                    Email = "email@example.com"
                };

                Entity entity2 = new Entity()
                {
                    FirstName = "Jane",
                    LastName = "Doe",
                    Email = "email2@example.com"
                };

                // Insert the entities into the database
                connection.Insert(entity);
                connection.Insert(entity2);

                // Create two relationship entities
                RelationshipEntity relationshipEntity1 = new RelationshipEntity()
                {
                    EntityId = entity.Id
                };

                RelationshipEntity relationshipEntity2 = new RelationshipEntity()
                {
                    EntityId = entity2.Id
                };

                // Insert the relationship entities into the database
                connection.InsertAll(new[] { relationshipEntity1, relationshipEntity2 });
            }
            else
            {
                Debug.Log("Database and tables already exist...skipping initialization");
            }

            // Close the connection
            CloseConnection();
        }

        public List<Entity> GetEntities()
        {
            connection = GetConnection();

            // Query the entities from the database
            var entities = connection.Table<Entity>().ToList();

            // Close the connection
            CloseConnection();
            return entities;
        }

        public List<RelationshipEntity> GetRelationshipEntities()
        {
            connection = GetConnection();

            // Query the relationship entities from the database
            var relationshipEntities = connection.Table<RelationshipEntity>().ToList();

            // Close the connection
            CloseConnection();
            return relationshipEntities;
        }

        public Dictionary<RelationshipEntity, Entity> GetRelationshipMapping()
        {
            connection = GetConnection();

            // Query the relationship entities from the database
            var relationshipEntities = connection.Table<RelationshipEntity>().ToList();

            // Create a dictionary to store the mapping
            Dictionary<RelationshipEntity, Entity> mapping = new Dictionary<RelationshipEntity, Entity>();
            foreach (var relationshipEntity in relationshipEntities)
            {
                var relatedEntity = connection.Get<Entity>(relationshipEntity.EntityId);
                mapping.Add(relationshipEntity, relatedEntity);
            }

            // Close the connection
            CloseConnection();
            return mapping;
        }

        public void CloseConnection()
        {
            // Close the SQLite connection

            if (connection != null)
            {
                connection.Close();
                connection.Dispose();
                connection = null;

                Debug.Log("DB Connection closed successfully");
            }
        }

        private SQLiteConnection GetConnection()
        {
            // Create a new SQLite connection
            var dbPath = System.IO.Path.Combine(Application.dataPath, DatabaseName);
            return new SQLiteConnection(dbPath);
        }
    }
}
