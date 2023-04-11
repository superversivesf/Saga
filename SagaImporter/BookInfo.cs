using System.Collections.Generic;
using SagaDb.Models;

namespace SagaImporter;

internal class BookInfo
{
    public BookInfo()
    {
        Authors = new List<string>();
        Series = new List<SeriesInfo>();
    }

    public string GoodreadsLink { get; set; }
    public string Title { get; set; }
    public List<string> Authors { get; set; }
    public List<SeriesInfo> Series { get; set; }
    public bool GoodreadsFetchTried { get; set; }

    public string GoodreadsDescription { get; set; }
}