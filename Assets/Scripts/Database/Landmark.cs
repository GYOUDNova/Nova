using SQLite;

namespace NOVA.Scripts
{
    [Table("Landmark")]
    public class Landmark
    {
        [AutoIncrement, PrimaryKey]
        public int LandmarkId { get; set; }

        [NotNull]
        public int GestureId { get; set; }

        [NotNull, Unique]
        public int LandmarkIndex { get; set; }

        [NotNull]
        public float X { get; set; }

        [NotNull]
        public float Y { get; set; }

        [NotNull]
        public float Z { get; set; }

        [NotNull]
        public bool IsPredefined { get; set; }
    }
}
