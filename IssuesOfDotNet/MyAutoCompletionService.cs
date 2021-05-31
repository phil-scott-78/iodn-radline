using RadLine;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Encodings.Web;

namespace IssuesOfDotNet
{
    public sealed class MyAutoCompletionService
    {
        private record AutoCompleteResults (List<string> List, int From, int To);

        private readonly HttpClient _client;

        public MyAutoCompletionService(HttpClient client)
        {
            _client = client ?? throw new System.ArgumentNullException(nameof(client));
        }

        public IEnumerable<string>? GetCompletions(LineBuffer buffer)
        {
            var encoded = UrlEncoder.Default.Encode(buffer.Content);
            var pos = buffer.CursorPosition;
            if (!buffer.Content.EndsWith(":"))
                pos--;
            var url = @$"https://issuesof.net/api/completion?q={encoded}&pos={pos}";
            var result = _client.GetFromJsonAsync<AutoCompleteResults>(url).Result;

            return result?.List;
        }
    }
}
