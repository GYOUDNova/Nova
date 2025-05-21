using SQLite;

namespace NOVA.Scripts
{

    [Table("PredefinedGesture")]
    public class PredefinedGesture
    {
        [AutoIncrement, PrimaryKey]
        public int PredefinedGestureId { get; set; }

        // FK
        [NotNull, Unique]
        public int GestureDataId { get; set; }
    }
}
