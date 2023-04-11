using System.Collections.Generic;

namespace SagaServer.Dto;

public class SeriesDetailsDto
{
    public string SeriesName { get; set; }
    public string SeriesDetailsHtml { get; set; }
    public string SeriesDetailsText { get; set; }
    public List<AuthorLinkDto> AuthorLinks { get; internal set; }
    public List<BookSeriesLinkDto> BookSeriesLinks { get; internal set; }
}