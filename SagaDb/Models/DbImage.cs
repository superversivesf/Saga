using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SagaDb.Models;

public class DbImage
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public string ImageId { get; set; }

    public byte[] ImageData { get; set; }
}