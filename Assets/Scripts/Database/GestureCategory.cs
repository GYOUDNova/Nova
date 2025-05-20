using SQLite;

namespace Nova.Scripts
{
    [Table("GestureCategory")]
    public class GestureCategory
    {
        [AutoIncrement, PrimaryKey]
        public int CategoryId { get; set; }

        [NotNull, MaxLength(64), Unique]
        public string CategoryName { get; set; }
    }
}
