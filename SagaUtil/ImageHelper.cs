using System.Net.Http;

namespace SagaUtil;

public static class ImageHelper
{
    public static byte[] DownloadImage(string fromUrl)
    {
        var httpClient = new HttpClient();
        var _request = httpClient.GetByteArrayAsync(fromUrl);
        _request.Wait();
        var data = _request.Result;
        return data;
    }
}