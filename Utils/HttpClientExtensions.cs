using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace SPCode.Utils;

public static class HttpClientExtensions
{
    public static async Task DownloadFile(this HttpClient client, string url, string local)
    {
        await using var fileStream = File.OpenWrite(local);
        var response = await client.GetAsync(url);
        await response.Content.CopyToAsync(fileStream);
    }
}