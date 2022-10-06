using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SagaServer.Dto
{
    public class BookDetailsDto
    {
        public string Title { get; set; }
        public string BookId { get; set; }
        public List<AuthorLinkDto> Authors { get; set; }
        public List<GenreLinkDto> Genres { get; set; }
        public List<SeriesLinkDto> Series { get; set; }
        public List<FilesDto> Files { get; set; }
        public string CoverImageId { get; set; }
        public string DescriptionHtml { get; set; }
        public string DescriptionText { get; set; }

    }
}
