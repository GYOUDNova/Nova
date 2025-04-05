using SQLite;
using UnityEditor;
using UnityEngine;

namespace NOVA.Scripts
{
    [InitializeOnLoad]
    public class SqliteHandler
    {
        private SQLiteConnection connection;

        static SqliteHandler()
        {
            // This static constructor is called when the class is loaded
            var handler = new SqliteHandler();
        }

        public SqliteHandler()
        {
            Debug.Log("Initializing SQlite database and applying migrations...");

            // Create required tables if they don't exist
            connection = new SQLiteConnection("test.db");

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

                Debug.Log("Inserted entities into database");

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

                // Query the entities FROM the relationship table
                var relationshipEntities = connection.Table<RelationshipEntity>().ToList();

                foreach (var relationshipEntity in relationshipEntities)
                {
                    var relatedEntity = connection.Get<Entity>(relationshipEntity.EntityId);
                    Debug.Log($"Relationship Entity ID: {relationshipEntity.Id}, Related Entity: {relatedEntity.FirstName} {relatedEntity.LastName}");
                }
            }
            else
            {
                Debug.Log("Database and tables already exist...skipping");
            }

            // Close the connection
            connection.Close();
            Debug.Log("Database connection closed.");
        }
    }
}
