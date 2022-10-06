using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SagaUtil.Dto
{
    public class UserDto
    {
        public string UserName { get; set; }
        public string FullName { get; set; }
        public string Password { get; set; }
        public string UserRole { get; set; }
        public string ClientId { get; set; }
    }

    public class RefreshDto
    {
        public string UserName { get; set; }
        public string RefreshTokenValue { get; set; }
        public string ClientID { get; set; }
    }

    public class SetProgressDto 
    {
        public string UserName { get; set; }
        public string BookId { get; set; }
        public string FileId { get; set; }
        public double Offset { get; set; }
    }

    public class GetProgressDto
    {
        public string UserName { get; set; }
        public string BookId { get; set; }
        public string FileId { get; set; }
        public double Offset { get; set; }
    }

    public class SetReadDto
    { 
        public string UserName { get; set; }
        public string BookId { get; set; }
        public bool IsRead { get; set; }

    }

    public class GetReadDto
    {
        public string UserName { get; set; }
        public string BookId { get; set; }
    }

}
