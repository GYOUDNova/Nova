using SQLite;

namespace NOVA.Scripts
{
    [Table("CustomGesture")]
    public class CustomGesture
    {
        [NotNull, AutoIncrement, Unique]
        public int CustomGestureId { get; set; }

        // FK
        [NotNull, Unique]
        public int GestureDataId { get; set; }
    }
}
