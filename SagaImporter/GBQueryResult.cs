using System.Collections.Generic;

namespace SagaImporter;

public class GBQueryResult
{
    public List<string> Authors { get; }
    public string Title { get; }
    public string SeriesTitle { get; set; }
    public string SeriesCount { get; set; }
    public string Link { get; }
    
    public GBQueryResult(string title, List<string> authors, string link)
    {
        this.Link = link;
        this.Authors = authors;
        this.Title = title;
    }
}