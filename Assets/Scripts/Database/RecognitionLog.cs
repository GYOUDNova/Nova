using System;
using SQLite;

namespace NOVA.Scripts
{
    [Table("RecognitionLog")]
    public class RecognitionLog
    {
        [NotNull, AutoIncrement, PrimaryKey]
        public int LogId { get; set; }

        [NotNull]
        public int GestureId { get; set; }

        [NotNull]
        public DateTime RecognitionTime { get; set; }

        [NotNull]
        public bool IsPredefined { get; set; }
    }
}
