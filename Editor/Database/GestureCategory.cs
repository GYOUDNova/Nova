using SQLite;

namespace NOVA.Scripts
{
    [Table("GestureCategory")]
    public class GestureCategory
    {
        public const string PredefinedCategoryName = "Predefined";

        [AutoIncrement, PrimaryKey]
        public int CategoryId { get; set; }

        [NotNull, MaxLength(64), Unique]
        public string Name { get; set; }
    }
}
