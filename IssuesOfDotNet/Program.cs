using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
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

            var keywords = new[]
            {
                "area", "area-under", "area-node", "area-owner", "area-lead",
                "type", "is", "in", "user", "org", "repo", "state", "author", "assignee",
                "mentions", "team", "commenter", "involves", "linked", "label", "milestone",
                "project", "status", "SHA", "head", "base", "language", "comments", "interactions",
                "reactions", "draft", "review", "reviewed-by", "review-requested", "team-review-requested",
                "created", "updated", "closed", "merged", "archived", "no"
            };

            var parenStyle = new Style(Color.Green);
            var keywordStyle = new Style(Color.Purple);

            var wordHighlighter = new WordHighlighter()
                .AddWord("(", parenStyle)
                .AddWord(")", parenStyle);

            foreach (var keyword in keywords)
            {
                wordHighlighter.AddWord(keyword, keywordStyle);
            }

            var editor = new LineEditor(provider: provider)
            {
                Highlighter = wordHighlighter
            };

            // Register new key bindings
            static MyAutoCompletionCommand NextAutoComplete() => new(AutoComplete.Next);
            static MyAutoCompletionCommand PreviousAutoComplete() => new(AutoComplete.Previous);

            editor.KeyBindings.Add(ConsoleKey.Tab, NextAutoComplete);
            editor.KeyBindings.Add(ConsoleKey.Spacebar, ConsoleModifiers.Control, NextAutoComplete);
            editor.KeyBindings.Add(ConsoleKey.Tab, ConsoleModifiers.Shift, PreviousAutoComplete);

            do
            {
                var result = await editor.ReadLine(CancellationToken.None);
                if (string.IsNullOrWhiteSpace(result)) return;

                if (result.Equals("clear()"))
                {
                    AnsiConsole.Clear();
                    continue;
                }

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
    }

    public record IssueResults(string Org, string Repo, string Type, string State, int Number, string Title);
}