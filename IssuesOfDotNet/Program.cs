using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;
using RadLine;
using Spectre.Console;

namespace IssuesOfDotNet
{
    class Program
    {
        private static readonly HttpClient HttpClient = new();

        static async Task Main()
        {
            var provider = new MyServiceProvider(HttpClient);

            do
            {
                var editor = new LineEditor(provider: provider)
                {
                    Highlighter = new WordHighlighter()
                        .AddWord("is", new Style(Color.Blue))
                        .AddWord(":", new Style(Color.White))
                        .AddWord("(", new Style(Color.Green))
                        .AddWord(")", new Style(Color.Green))
                };

                // Register new key bindings
                editor.KeyBindings.Add(ConsoleKey.Tab, () => new MyAutoCompletionCommand(AutoComplete.Next));
                editor.KeyBindings.Add(ConsoleKey.Tab, ConsoleModifiers.Shift, () => new MyAutoCompletionCommand(AutoComplete.Previous));

                var result = await editor.ReadLine(CancellationToken.None);
                if (string.IsNullOrWhiteSpace(result)) return;

                var issues = (await GetIssues(result)).ToList();
                if (issues.Any() == false)
                {
                    AnsiConsole.MarkupLine("[red]no results[/]");
                }

                var table = new Table()
                    .NoBorder()
                    .HideHeaders()
                    .AddColumns("Info", "Comments");

                var openedStyle = new Style(Color.Green);
                var closedStyle = new Style(Color.Red);
                var mergedStyle = new Style(Color.Purple);

                foreach (var issue in issues.Take(10))
                {
                    var detailsText = new Markup(
                        $"[blue]{issue.Title.EscapeMarkup()}[/]{Environment.NewLine}" +
                        $"{issue.Org.EscapeMarkup()}/{issue.Repo.EscapeMarkup()}#{issue.Number}");

                    var statusText = new Text($"{issue.Type} ({issue.State})", issue.State switch
                    {
                        "merged" => mergedStyle,
                        "open" => openedStyle,
                        "closed" => closedStyle,
                        _ => Style.Plain
                    });

                    table.AddRow(statusText, detailsText);
                }

                AnsiConsole.Render(table);
            } while (true);
        }


        private static async Task<IEnumerable<IssueResults>> GetIssues(string query)
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                NewLine = "\n"
            };

            var url = $"https://issuesof.net/download/?q={query}";
            var csvResponse = await HttpClient.GetStringAsync(url);
            using var reader = new StringReader(csvResponse);
            using var csv = new CsvReader(reader, config);
            return csv.GetRecords<IssueResults>().ToList();
        }

        private class QueryCompletion : ITextCompletion
        {
            public IEnumerable<string>? GetCompletions(string prefix, string word, string suffix)
            {
                var encoded = UrlEncoder.Default.Encode(prefix + word);
                var pos = prefix.Length + word.Length;
                var url = @$"https://issuesof.net/api/completion?q={encoded}&pos={pos}";
                var result = HttpClient.GetFromJsonAsync<AutoCompleteResults>(url).Result;

                if (result?.List == null) yield break;

                string prefixWord = word.Contains(":", StringComparison.InvariantCulture)
                    ? word[..(word.IndexOf(":", StringComparison.InvariantCulture) + 1)]
                    : word;

                foreach (var autoCompleteResult in result.List)
                {
                    yield return autoCompleteResult.StartsWith(word, StringComparison.InvariantCultureIgnoreCase)
                        ? autoCompleteResult
                        : prefixWord + autoCompleteResult;
                }
            }
        }
    }

    public record AutoCompleteResults (List<string> List, int From, int To);

    public record IssueResults(string Org, string Repo, string Type, string State, int Number, string Title);
}