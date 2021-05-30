﻿using RadLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IssuesOfDotNet
{
    public sealed class MyAutoCompletionCommand : LineEditorCommand
    {
        private const string QUERY_STATE = "AUTOCOMPLETE_QUERY";
        private const string RESULT_STATE = "AUTOCOMPLETE_RESULTS";
        private const string INDEX_STATE = "AUTOCOMPELTE_INDEX";

        private readonly AutoComplete _kind;

        public MyAutoCompletionCommand(AutoComplete kind)
        {
            _kind = kind;
        }

        public override void Execute(LineEditorContext context)
        {
            var service = context.GetService(typeof(MyAutoCompletionService)) as MyAutoCompletionService;
            if (service == null)
            {
                throw new InvalidOperationException("AutoCompletion service has not been registered.");
            }

            // Find the word boundary
            var prefix = GetPrefixAtWordBoundary(context);
            var wordEnd = GetSuffixPosition(context);

            var previousQuery = context.GetState<string?>(QUERY_STATE, () => null);
            var query = context.Buffer.Content.Substring(0, wordEnd);
            if (previousQuery != null)
            {
                query = context.Buffer.Content.Substring(0, Math.Min(wordEnd, previousQuery.Length));
            }

            var index = context.GetState(INDEX_STATE, () => 0);
            if (previousQuery == null || !query.Equals(previousQuery, StringComparison.OrdinalIgnoreCase))
            {
                // Fetch completions
                var completions = service.GetCompletions(context.Buffer);
                if (completions?.List == null)
                {
                    return;
                }

                context.SetState(INDEX_STATE, 0);
                context.SetState(QUERY_STATE, context.Buffer.Content.Substring(0, wordEnd));
                context.SetState(RESULT_STATE, completions.List);
            }

            // Get the results
            var result = context.GetState(RESULT_STATE, () => new List<string>());
            if (result.Count == 0)
            {
                return;
            }

            // Clamp the results
            if (index > result.Count - 1)
            {
                index = 0;
            }
            if (index < 0)
            {
                index = result.Count - 1;
            }

            // Insert the autocomplete word
            context.Buffer.Clear(prefix.Length, wordEnd - prefix.Length);
            context.Buffer.Move(prefix.Length);
            context.Buffer.Insert(result[index]);
            context.Buffer.Move(prefix.Length + result[index].Length);

            // Increase/Decrease index
            context.SetState(INDEX_STATE, _kind == AutoComplete.Next ? ++index : --index);
        }

        private static string GetPrefixAtWordBoundary(LineEditorContext context)
        {
            for (int pos = context.Buffer.CursorPosition - 1; pos > 0; pos--)
            {
                var current = context.Buffer.Content[pos];
                if (current == ':' || char.IsWhiteSpace(current))
                {
                    return context.Buffer.Content.Substring(0, pos + 1);
                }
            }

            return string.Empty;
        }

        private static int GetSuffixPosition(LineEditorContext context)
        {
            var start = context.Buffer.CursorPosition - 1;
            var length = context.Buffer.Length;
            for (int pos = start; pos < length; pos++)
            {
                var current = context.Buffer.Content[pos];
                if (char.IsWhiteSpace(current))
                {
                    return pos;
                }
            }

            return length;
        }
    }
}
