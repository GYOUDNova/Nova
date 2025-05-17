using SQLite;

namespace NOVA.Scripts
{
    [Table("GestureData")]
    public class GestureData
    {
        [NotNull, PrimaryKey, AutoIncrement]
        public int GestureDataId { get; set; }

        [NotNull, MaxLength(256), Unique]
        public string GestureName { get; set; }

        // FK
        [NotNull, MaxLength(30), Unique]
        public string GestureImageName { get; set; }

        // FK
        [NotNull, Unique]
        public int GestureCategoryId { get; set; }

        [NotNull]
        public bool IsPredefined { get; set; }
    }
}
