using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SagaDb.Models
{
    public class Series
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public string SeriesId { get; set; }
        public string SeriesName { get; set; }
        public string SeriesDescription { get; set; }
        public string SeriesLink { get; set; }
    }


}
