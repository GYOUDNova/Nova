using SQLite;

namespace NOVA.Scripts
{
    [Table("Configuration")]
    public class Configuration
    {
        [Column("ConfigurationId")]
        [AutoIncrement, PrimaryKey]
        public int ConfigurationId { get; set; }

        [NotNull, Unique, MaxLength(20)]
        public string Name { get; set; }

        [NotNull]
        public int Gamma { get; set; }

        [NotNull]
        public float ChainTimer { get; set; }

        [NotNull]
        public float LandmarkTolerance { get; set; }

        [NotNull]
        public GestureImageExtension ImageExtension { get; set; }

        [NotNull]
        public bool Active { get; set; }
    }
}
