/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;

namespace CardGameDef
{
    [PublicAPI]
    public class CardSearchFilters
    {
        public const string Delimiter = " ";
        public const string KeywordFormat = Delimiter + "{0}{1}";
        public const string KeywordId = "id:";
        public const string KeywordSet = "set:";
        public const string KeywordIs = "is:";
        public const string KeywordNot = "not:";
        public const string KeywordString = ":";
        public const string KeywordIntMin = ">=";
        public const string KeywordIntMax = "<=";
        public const string KeywordEnum = "=";
        public const string Quote = "\"";

        public string Id { get; set; } = "";

        public string Name { get; set; } = "";

        public string SetCode { get; set; } = "";

        public Dictionary<string, string> StringProperties { get; } = new Dictionary<string, string>();

        public Dictionary<string, int> IntMinProperties { get; } = new Dictionary<string, int>();

        public Dictionary<string, int> IntMaxProperties { get; } = new Dictionary<string, int>();

        public Dictionary<string, bool> BoolProperties { get; } = new Dictionary<string, bool>();

        public Dictionary<string, int> EnumProperties { get; } = new Dictionary<string, int>();

        public void Clear()
        {
            Id = string.Empty;
            Name = string.Empty;
            SetCode = string.Empty;
            StringProperties.Clear();
            IntMinProperties.Clear();
            IntMaxProperties.Clear();
            BoolProperties.Clear();
            EnumProperties.Clear();
        }

        public void Parse(string input)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            Clear();

            // If some search criteria has a space in it, then that criteria should be enclosed in quotes
            // Using this assumption, we find all quoted areas and replace the space(s) temporarily so that we treat it as 1 word
            // This may not be the best solution (definitely not the cleanest), but it should get the job done
            var processedInput = new StringBuilder();
            var unprocessedInput = input;
            while (unprocessedInput.Contains(Quote))
            {
                var leftQuoteIndex =
                    unprocessedInput.IndexOf(Quote,
                        StringComparison.Ordinal); // Guaranteed to be found because we checked with Contains()
                // If the left quote is the last character, then we obviously won't find a right quote
                var rightQuoteIndex = leftQuoteIndex == unprocessedInput.Length - Quote.Length
                    ? -1
                    : unprocessedInput.IndexOf(Quote, leftQuoteIndex + Quote.Length, StringComparison.Ordinal);
                var beforeQuote = unprocessedInput.Substring(0, leftQuoteIndex);
                var quotation = string.Empty;
                if (rightQuoteIndex != -1) // If there's no right quote, then we don't have a quotation
                {
                    var startIndex = leftQuoteIndex + Quote.Length;
                    quotation = unprocessedInput.Substring(startIndex, rightQuoteIndex - startIndex)
                        .Replace(Delimiter, Quote);
                }

                processedInput.Append(beforeQuote + quotation);
                // If there's no quotation, we'll finish by taking everything after the 1 quote
                // Otherwise, we'll keep checking the rest of the input for quotations
                unprocessedInput = rightQuoteIndex == -1
                    ? unprocessedInput.Substring(leftQuoteIndex + Quote.Length)
                    : unprocessedInput.Substring(rightQuoteIndex + Quote.Length);
            }

            var nameBuilder = new StringBuilder(Name);
            processedInput.Append(unprocessedInput);
            foreach (var word in processedInput.ToString()
                         .Split(new[] {Delimiter}, StringSplitOptions.RemoveEmptyEntries))
            {
                var token = word.Replace(Quote, Delimiter); // Restore spaces that we temporarily replaced
                if (token.StartsWith(KeywordId))
                    Id = token.Substring(KeywordId.Length);
                else if (token.StartsWith(KeywordSet))
                    SetCode = token.Substring(KeywordSet.Length);
                else if (token.StartsWith(KeywordIs))
                    BoolProperties.Add(token.Substring(KeywordIs.Length), true);
                else if (token.StartsWith(KeywordNot))
                    BoolProperties.Add(token.Substring(KeywordNot.Length), false);
                else if (token.Contains(KeywordString))
                    StringProperties.Add(token.Substring(0, token.IndexOf(KeywordString, StringComparison.Ordinal)),
                        token.Substring(token.IndexOf(KeywordString, StringComparison.Ordinal) + KeywordString.Length));
                else if (token.Contains(KeywordIntMin) &&
                         int.TryParse(
                             token.Substring(token.IndexOf(KeywordIntMin, StringComparison.Ordinal) +
                                             KeywordIntMin.Length),
                             out var minValue))
                    IntMinProperties.Add(token.Substring(0, token.IndexOf(KeywordIntMin, StringComparison.Ordinal)),
                        minValue);
                else if (token.Contains(KeywordIntMax) &&
                         int.TryParse(
                             token.Substring(token.IndexOf(KeywordIntMax, StringComparison.Ordinal) +
                                             KeywordIntMax.Length),
                             out var maxValue))
                    IntMaxProperties.Add(token.Substring(0, token.IndexOf(KeywordIntMax, StringComparison.Ordinal)),
                        maxValue);
                else if (token.Contains(KeywordEnum) &&
                         int.TryParse(
                             token.Substring(token.IndexOf(KeywordEnum, StringComparison.Ordinal) + KeywordEnum.Length),
                             out var enumValue))
                    EnumProperties.Add(token.Substring(0, token.IndexOf(KeywordEnum, StringComparison.Ordinal)),
                        enumValue);
                else
                    nameBuilder.Append((nameBuilder.Length == 0 ? string.Empty : Delimiter) + token);
            }

            Name = nameBuilder.ToString();
        }

        public override string ToString()
        {
            var filters = new StringBuilder(Name);

            if (!string.IsNullOrEmpty(Id))
            {
                var filterValue = Id.Contains(Delimiter) ? Quote + Id + Quote : Id;
                filters.AppendFormat(KeywordFormat, KeywordId, filterValue);
            }

            if (!string.IsNullOrEmpty(SetCode))
            {
                var filterValue = SetCode.Contains(Delimiter) ? Quote + SetCode + Quote : SetCode;
                filters.AppendFormat(KeywordFormat, KeywordSet, filterValue);
            }

            foreach (var filter in StringProperties)
            {
                var filterValue = filter.Value.Contains(Delimiter) ? Quote + filter.Value + Quote : filter.Value;
                filters.AppendFormat(KeywordFormat, filter.Key + KeywordString, filterValue);
            }

            foreach (var filter in BoolProperties)
                filters.AppendFormat(KeywordFormat, filter.Value ? KeywordIs : KeywordNot, filter.Key);
            foreach (var filter in IntMinProperties)
                filters.AppendFormat(KeywordFormat, filter.Key + KeywordIntMin, filter.Value);
            foreach (var filter in IntMaxProperties)
                filters.AppendFormat(KeywordFormat, filter.Key + KeywordIntMax, filter.Value);
            foreach (var filter in EnumProperties)
                filters.AppendFormat(KeywordFormat, filter.Key + KeywordEnum, filter.Value);

            return filters.ToString();
        }
    }
}
