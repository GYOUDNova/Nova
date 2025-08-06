using SQLite;

namespace NOVA.Scripts
{
    [Table("LandmarkDistance")]
    public class LandmarkDistance
    {
        [AutoIncrement, PrimaryKey]
        public int LandmarkDistanceId { get; set; }

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
        public float Distance { get; set; }
    }
}
