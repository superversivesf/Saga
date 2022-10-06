using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SagaServer.Dto
{
    public class BookDto
    {
        public string Title { get; set; }
        public string BookId { get; set; }
        public List<string> Authors { get; set; }
        public string CoverImageId { get; set; }
        public string ShortDesc { get; set; }
    }
}
