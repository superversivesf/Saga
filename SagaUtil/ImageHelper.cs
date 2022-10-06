using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;


namespace SagaUtil
{
    public static class ImageHelper
    {
        public static byte[] DownloadImage(string fromUrl)
        {
            HttpClient httpClient = new HttpClient();
            var _request = httpClient.GetByteArrayAsync(fromUrl);
            _request.Wait();
            byte[] data = _request.Result;
            return data;
        }
    }
}
