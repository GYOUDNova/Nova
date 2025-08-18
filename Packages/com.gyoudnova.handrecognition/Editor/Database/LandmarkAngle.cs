using SQLite;

namespace NOVA.Scripts
{
    [Table("LandmarkAngle")]
    public class LandmarkAngle
    {
        [AutoIncrement, PrimaryKey]
        public int LandmarkAngleId { get; set; }

        // FK: Gesture
        [NotNull]
        public int GestureId { get; set; }

        // FK: Landmark 1
        [NotNull]
        public int LandmarkId { get; set; }

        // FK: Landmark 2
        [NotNull]
        public int OtherLandmarkId { get; set; }

        [NotNull]
        public float LandmarkAngles { get; set; }
    }
}
