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
        public string ConfigurationName { get; set; }

        [NotNull]
        public int Gamma { get; set; }

        [NotNull]
        public float ChainTimer { get; set; }

        [NotNull]
        public float LandmarkTolerance { get; set; }

        [NotNull]
        public GestureImageExtension ImageExtension { get; set; }
    }
}
