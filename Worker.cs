using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

internal class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly CrawlerClient _crawlerClient;
    private readonly IHostApplicationLifetime _lifetime;

    public Worker(ILogger<Worker> logger, CrawlerClient crawlerClient, IHostApplicationLifetime lifetime)
    {
        _logger = logger;
        _crawlerClient = crawlerClient;
        _lifetime = lifetime;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var nodes = await LoadNodes("tjsj/tjbz/tjyqhdmhcxhfdm/2021/index.html");

        using var sr = new StreamWriter("nodes.json");

        await sr.WriteAsync(JsonConvert.SerializeObject(nodes, new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        }));

        _lifetime.StopApplication();

        async Task<List<AreaDataNode>> LoadNodes(string? uri, string? name = null)
        {
            await Task.Delay(200);

            if (uri is null)
                return new();

            var html = await _crawlerClient.FetchHtmlAsync(uri);

            var nodes = _crawlerClient.HtmlToNode(html);

            return nodes
#if DEBUG
            .Take(2)
#endif
            .Select(x =>
            {
                var routes = uri.Split("/");
                var childrenURI = x.ChildrenURI is null ? null :
                                  $"{string.Join('/', routes[0..(routes.Length - 1)])}/{x.ChildrenURI}";

                var fullName = name is null ? x.Name : $"{name}/{x.Name}";

                _logger.LogInformation("{_},{_}", fullName, x.Code);

                return new AreaDataNode
                {
                    Level = x.Level,
                    Code = x.Code,
                    Name = x.Name,
                    URI = new($"{_crawlerClient.BaseAddress}{uri}"),
                    ChildrenURI = x.ChildrenURI is null ? null : $"{_crawlerClient.BaseAddress}{childrenURI}",
                    FullName = fullName,
                    Children = LoadNodes(childrenURI, fullName).Result
                };
            })
            .ToList();
        }
    }
}