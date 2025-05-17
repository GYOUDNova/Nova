using SQLite;

namespace NOVA.Scripts
{
    [Table("GestureImage")]
    public class GestureImage
    {
        [NotNull, PrimaryKey, MaxLength(30)]
        public string Name { get; set; }

        // FK --> GestureID (each gesture has an image)
        [NotNull]
        public int GestureId { get; set; }

        [NotNull]
        public GestureImageExtension FileExtension { get; set; }

        [NotNull]
        public bool IsPredefined { get; set; }
    }
}
