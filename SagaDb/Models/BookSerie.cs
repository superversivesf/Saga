using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SagaDb.Models
{
    public class BookSerie
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string SeriesId { get; set; }
        public string BookId { get; set; }
        public string SeriesVolume { get; set; }
    }


}
