using System;
using AngleSharp.Html.Parser;
using Flurl.Http;

namespace AreaData;
public class CrawlerClient
{
    private readonly IFlurlClient _client;
    private readonly HtmlParser _htmlParser;
    private readonly Uri _baseAddress;

    public CrawlerClient(HttpClient client)
    {
        _baseAddress = client.BaseAddress!;
        _client = new FlurlClient(client);
        _htmlParser = new();
    }

    public Uri BaseAddress => _baseAddress;

    public Task<string> FetchHtmlAsync(string route)
    {
        return _client
        .Request(route)
        .WithHeader("Accept", @"text/html,application/xhtml+xml,application/xml;")
        .WithHeader("Accept-Encoding", "utf-8")
        .GetStringAsync();
    }

    public List<AreaDataNode> HtmlToNode(string html)
    {
        var document = _htmlParser.ParseDocument(html);
        //省
        var provinceDoms = document.QuerySelectorAll(".provincetable .provincetr a");
        //市
        var cityDoms = document.QuerySelectorAll(".citytable .citytr");
        //县 
        var countyDoms = document.QuerySelectorAll(".countytable .countytr");
        //街道
        var townDoms = document.QuerySelectorAll(".towntable .towntr");
        //村
        var villageDoms = document.QuerySelectorAll(".villagetable .villagetr");

        return Array.Empty<AreaDataNode>()
                .Concat(provinceDoms.Select(dom =>
                {
                    var childrenURI = dom.GetAttribute("href");
                    return new AreaDataNode
                    {
                        Level = 1,
                        Name = dom.TextContent,
                        Code = childrenURI!.Replace(".html", "").PadRight(12, '0'),
                        ChildrenURI = childrenURI
                    };
                }))
                .Concat(cityDoms.Select(dom =>
                {
                    var links = dom.QuerySelectorAll("a");
                    return new AreaDataNode
                    {
                        Level = 2,
                        Name = links[1].TextContent,
                        Code = links[0].TextContent,
                        ChildrenURI = links[0].GetAttribute("href")
                    };
                }))
                .Concat(countyDoms.Select(dom =>
                {
                    var links = dom.QuerySelectorAll("a");
                    return new AreaDataNode
                    {
                        Level = 3,
                        Name = links[1].TextContent,
                        Code = links[0].TextContent,
                        ChildrenURI = links[0].GetAttribute("href")
                    };
                }))
                .Concat(townDoms.Select(dom =>
                {
                    var links = dom.QuerySelectorAll("a");
                    return new AreaDataNode
                    {
                        Level = 4,
                        Name = links[1].TextContent,
                        Code = links[0].TextContent,
                        ChildrenURI = links[0].GetAttribute("href")
                    };
                }))
                .Concat(villageDoms.Select(dom =>
                {
                    var links = dom.QuerySelectorAll("td");
                    return new AreaDataNode
                    {
                        Level = 5,
                        Name = links[2].TextContent,
                        Code = links[0].TextContent,
                        ChildrenURI = null
                    };
                }))
                .ToList();
    }
}

public class AreaDataNode
{
    public int Level { get; set; }
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string FullName { get; set; } = default!;
    public string? ChildrenURI { get; set; }
    public Uri URI { get; set; } = default!;
    public List<AreaDataNode> Children { get; set; } = new();
}