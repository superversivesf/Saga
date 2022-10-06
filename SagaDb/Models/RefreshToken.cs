
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SagaDb.Models
{
    public class RefreshToken
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string UserId { get; set; }
        public string RefreshTokenValue { get; set; }
        public DateTime ExpiryDate { get; set; }
        public string ClientID { get; set; }
    }

}
