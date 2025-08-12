using SQLite;

namespace NOVA.Scripts
{
    [Table("CustomGesture")]
    public class CustomGesture
    {
        [AutoIncrement, PrimaryKey]
        public int CustomGestureId { get; set; }

        // FK
        [NotNull, Unique]
        public int GestureDataId { get; set; }
    }
}
