using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SagaDb.Models
{
    public class Author
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public string AuthorId { get; set; }
        public string AuthorName { get; set; }
        public string AuthorDescription { get; set; }
        public bool GoodReadsAuthor { get; set; }
        public AuthorType AuthorType { get; set; }
        public string GoodReadsAuthorLink { get; set; }
        public string AuthorImageLink { get; set; }
        public string AuthorWebsite { get; set; }
        public string Born { get; set; }
        public string Died { get; set; }
        public string Genre { get; set; }
        public string Influences { get; set; }
        public string Twitter { get; set; }
    }
}
