using SQLite;

namespace NOVA.Scripts
{
    [Table("Entity")]
    public class Entity
    {
        [AutoIncrement, PrimaryKey]
        [Column("id")]
        public int Id { get; set; }

        [NotNull, MaxLength(64)]
        [Column("first_name")]
        public string FirstName { get; set; }

        [NotNull, MaxLength(64)]
        [Column("last_name")]
        public string LastName { get; set; }

        [NotNull, MaxLength(64), Unique]
        [Column("email")]
        public string Email { get; set; }
    }

    [Table("RelationshipEntity")]
    public class RelationshipEntity
    {
        [AutoIncrement, PrimaryKey]
        [Column("id")]
        public int Id { get; set; }

        [Indexed]
        [Column("entity_id")]
        public int EntityId { get; set; }

        [Ignore]
        public Entity Entity { get; set; }
    }
}
