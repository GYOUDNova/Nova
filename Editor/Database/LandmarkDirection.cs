using SQLite;

namespace NOVA.Scripts
{
    [Table("LandmarkDirection")]
    public class LandmarkDirection
    {
        [AutoIncrement, PrimaryKey]
        public int LandmarkDirectionId { get; set; }

        // FK: Gesture
        [NotNull]
        public int GestureId { get; set; }

        // Identification purposes
        [NotNull]
        public bool IsPredefined { get; set; }

        // FK: Landmark 1
        [NotNull]
        public int LandmarkId { get; set; }

        // FK: Landmark 2
        [NotNull]
        public int OtherLandmarkId { get; set; }

        [NotNull]
        public string Direction { get; set; }
    }
}
