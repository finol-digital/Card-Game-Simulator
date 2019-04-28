/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CardGameDef
{
    public class CardSearchFilters
    {
        public const string Delimiter = " ";
        public const string KeywordFormat = Delimiter + "{0}{1}";
        public const string Keyword_Id = "id:";
        public const string Keyword_Set = "set:";
        public const string Keyword_Is = "is:";
        public const string Keyword_Not = "not:";
        public const string Keyword_String = ":";
        public const string Keyword_IntMin = ">=";
        public const string Keyword_IntMax = "<=";
        public const string Keyword_Enum = "=";
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
            StringBuilder processedInput = new StringBuilder();
            string unprocessedInput = input;
            while (unprocessedInput.Contains(Quote))
            {
                int leftQuoteIndex = unprocessedInput.IndexOf(Quote); // Guaranteed to be found because we checked with Contains()
                // If the left quote is the last character, then we obviously won't find a right quote
                int rightQuoteIndex = leftQuoteIndex == unprocessedInput.Length - Quote.Length
                    ? -1 : unprocessedInput.IndexOf(Quote, leftQuoteIndex + Quote.Length);
                string beforeQuote = unprocessedInput.Substring(0, leftQuoteIndex);
                string quotation = string.Empty;
                if (rightQuoteIndex != -1) // If there's no right quote, then we don't have a quotation
                {
                    int startIndex = leftQuoteIndex + Quote.Length;
                    quotation = unprocessedInput.Substring(startIndex, rightQuoteIndex - startIndex).Replace(Delimiter, Quote);
                }
                processedInput.Append(beforeQuote + quotation);
                if (rightQuoteIndex == -1) // If there's no quotation, we'll finish by taking everything after the 1 quote
                    unprocessedInput = unprocessedInput.Substring(leftQuoteIndex + Quote.Length);
                else // Otherwise, we'll keep checking the rest of the input for quotations
                    unprocessedInput = unprocessedInput.Substring(rightQuoteIndex + Quote.Length);
            }
            processedInput.Append(unprocessedInput);
            foreach (string word in processedInput.ToString().Split(new[] { Delimiter }, StringSplitOptions.RemoveEmptyEntries))
            {
                string token = word.Replace(Quote, Delimiter); // Restore spaces that we temporarily replaced
                if (token.StartsWith(Keyword_Id))
                    Id = token.Substring(Keyword_Id.Length);
                else if (token.StartsWith(Keyword_Set))
                    SetCode = token.Substring(Keyword_Set.Length);
                else if (token.StartsWith(Keyword_Is))
                    BoolProperties.Add(token.Substring(Keyword_Is.Length), true);
                else if (token.StartsWith(Keyword_Not))
                    BoolProperties.Add(token.Substring(Keyword_Not.Length), false);
                else if (token.Contains(Keyword_String))
                    StringProperties.Add(token.Substring(0, token.IndexOf(Keyword_String)), token.Substring(token.IndexOf(Keyword_String) + Keyword_String.Length));
                else if (token.Contains(Keyword_IntMin) && int.TryParse(token.Substring(token.IndexOf(Keyword_IntMin) + Keyword_IntMin.Length), out int minValue))
                    IntMinProperties.Add(token.Substring(0, token.IndexOf(Keyword_IntMin)), minValue);
                else if (token.Contains(Keyword_IntMax) && int.TryParse(token.Substring(token.IndexOf(Keyword_IntMax) + Keyword_IntMax.Length), out int maxValue))
                    IntMaxProperties.Add(token.Substring(0, token.IndexOf(Keyword_IntMax)), maxValue);
                else if (token.Contains(Keyword_Enum) && int.TryParse(token.Substring(token.IndexOf(Keyword_Enum) + Keyword_Enum.Length), out int enumValue))
                    EnumProperties.Add(token.Substring(0, token.IndexOf(Keyword_Enum)), enumValue);
                else
                    Name += (string.IsNullOrEmpty(Name) ? string.Empty : Delimiter) + token;
            }
        }

        public override string ToString()
        {
            StringBuilder filters = new StringBuilder(Name);

            if (!string.IsNullOrEmpty(Id))
            {
                string filterValue = Id.Contains(Delimiter) ? Quote + Id + Quote : Id;
                filters.AppendFormat(KeywordFormat, Keyword_Id, filterValue);
            }
            if (!string.IsNullOrEmpty(SetCode))
            {
                string filterValue = SetCode.Contains(Delimiter) ? Quote + SetCode + Quote : SetCode;
                filters.AppendFormat(KeywordFormat, Keyword_Set, filterValue);
            }
            foreach (var filter in StringProperties)
            {
                string filterValue = filter.Value.Contains(Delimiter) ? Quote + filter.Value + Quote : filter.Value;
                filters.AppendFormat(KeywordFormat, filter.Key + Keyword_String, filterValue);
            }
            foreach (var filter in BoolProperties)
                filters.AppendFormat(KeywordFormat, filter.Value ? Keyword_Is : Keyword_Not, filter.Key);
            foreach (var filter in IntMinProperties)
                filters.AppendFormat(KeywordFormat, filter.Key + Keyword_IntMin, filter.Value);
            foreach (var filter in IntMaxProperties)
                filters.AppendFormat(KeywordFormat, filter.Key + Keyword_IntMax, filter.Value);
            foreach (var filter in EnumProperties)
                filters.AppendFormat(KeywordFormat, filter.Key + Keyword_Enum, filter.Value);

            return filters.ToString();
        }
    }
}
