using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

Host.CreateDefaultBuilder()
.ConfigureServices((_, services) =>
{
    services.AddHttpClient<CrawlerClient>(o =>
    {
        o.BaseAddress = new("http://www.stats.gov.cn");
    });

    services.AddHostedService<Worker>();
})
.Build()
.Run();