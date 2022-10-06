using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SagaDb.Models
{
    public class AudioFile
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public string AudioFileId { get; set; }
        public string AudioFileName { get; set; }
        public string AudioFileFolder { get; set; }
        public double Duration { get; set; }
    }


}
