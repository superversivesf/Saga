using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SagaDb.Models
{
    public class Genre
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public string GenreId { get; set; }
        public string GenreName { get; set; }
    }


}
