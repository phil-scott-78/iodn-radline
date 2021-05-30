using RadLine;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Encodings.Web;

namespace IssuesOfDotNet
{
    public sealed class MyAutoCompletionService
    {
        private readonly HttpClient _client;

        public MyAutoCompletionService(HttpClient client)
        {
            _client = client ?? throw new System.ArgumentNullException(nameof(client));
        }

        public AutoCompleteResults? GetCompletions(LineBuffer buffer)
        {
            var encoded = UrlEncoder.Default.Encode(buffer.Content);
            var pos = buffer.CursorPosition - 1;
            var url = @$"https://issuesof.net/api/completion?q={encoded}&pos={pos}";
            var result = _client.GetFromJsonAsync<AutoCompleteResults>(url).Result;

            if (result?.List == null)
            {
                return null;
            }

            return result;
        }
    }

    public sealed class AutoCompleteResult
    {
        public List<string> List { get; set; }
        public int From { get; set; }
        public int To { get; set; }
    }
}
