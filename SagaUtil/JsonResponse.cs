using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace SagaImporter.Util
{
    public class JsonResponseStatus
    {
        public const string ok = "ok";
        public const string error = "error";
    }
    public class JsonResponseMessage : HttpResponseMessage
    {
        //transparent wrapper around System.Net.Http.HttpResponse type
    }
    public class JsonResponse<T>
    {
        [JsonProperty(Order = 1)]
        public string Status { get; set; }
        [JsonProperty(Order = 2)]
        public string Message { get; set; }
        [JsonProperty(Order = 3)]
        public T Data { get; set; }

        public JsonResponse()
        {
            this.Status = JsonResponseStatus.ok;
            this.Message = null;
        }

        public JsonResponse(T data)
        {
            this.Status = JsonResponseStatus.ok;
            this.Message = null;
            this.Data = data;
        }

        public JsonResponseMessage ToMessage(HttpStatusCode status = HttpStatusCode.OK, List<Tuple<string, string>> headers = null)
        {
            var _settings = new JsonSerializerSettings() { ReferenceLoopHandling = ReferenceLoopHandling.Ignore, NullValueHandling = NullValueHandling.Include };
            var _response = new JsonResponseMessage();

            if (headers != null)
            {
                foreach (var _header in headers)
                    _response.Headers.Add(_header.Item1, _header.Item2);
            }

            _response.StatusCode = status;
            _response.Content = new StringContent(JsonConvert.SerializeObject(this, _settings), System.Text.Encoding.UTF8, "application/json");
            return _response;
        }
    }
}
