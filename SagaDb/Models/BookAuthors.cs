using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SagaDb.Models
{
    public class BookAuthors
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string BookId { get; set; }
        public string AuthorId { get; set; }
        public AuthorType AuthorType { get; set; }
    }


}
