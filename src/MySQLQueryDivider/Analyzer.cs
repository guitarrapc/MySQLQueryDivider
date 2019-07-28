using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace MySQLQueryDivider
{
    public struct ParseQuery
    {
        public string Title;
        public string Query;
    }

    public class Analyzer
    {
        /// <summary>
        /// load query from string
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static ParseQuery[] FromString(string query, Regex regex)
        {
            var lines = query.Split(";").Select(x => x + ";").Where(x => x != ";").ToArray();
            var queryPerTables = Parse(lines, null, regex);
            return queryPerTables;
        }

        /// <summary>
        /// load query from direcory
        /// </summary>
        /// <param name="path"></param>
        /// <param name="escapeLines"></param>
        /// <param name="regex"></param>
        /// <returns></returns>
        public static IEnumerable<ParseQuery[]> FromDirectory(string path, string[] escapeLines, Regex regex)
        {
            var files = Directory.EnumerateFiles(path, "*.sql", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                yield return FromFile(file, escapeLines, regex);
            }
        }

        /// <summary>
        /// load query from file
        /// </summary>
        /// <param name="path"></param>
        /// <param name="escapeLines"></param>
        /// <param name="regex"></param>
        /// <returns></returns>
        public static ParseQuery[] FromFile(string path, string[] escapeLines, Regex regex) => FromFile(path, escapeLines, regex, new UTF8Encoding(false));
        /// <summary>
        /// load query from file
        /// </summary>
        /// <param name="path"></param>
        /// <param name="escapeLines"></param>
        /// <param name="regex"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public static ParseQuery[] FromFile(string path, string[] escapeLines, Regex regex, Encoding encoding)
        {
            var lines = File.ReadAllLines(path, encoding);
            var queryPerTables = Parse(lines, escapeLines, regex);
            return queryPerTables;
        }

        public static ParseQuery[] Parse(string[] lines, string[] escapeLines, Regex regex)
        {
            var numLines = escapeLines == null
                ? lines.Select(x => x.RemoveNewLine())
                    .Select(x => x.TrimEnd())
                    .Where(x => !string.IsNullOrEmpty(x))
                    .Select((x, i) => (index: i, content: x))
                    .ToArray()
                : lines.Select(x => x.RemoveNewLine())
                    .Select(x => x.TrimEnd())
                    .Where(x => !string.IsNullOrEmpty(x))
                    .Where(x => !escapeLines.Any(y => x.StartsWith(y, StringComparison.OrdinalIgnoreCase)))
                    .Select((x, i) => (index: i, content: x))
                    .ToArray();
            // query should be end with ;
            var ends = numLines.Where(x => x.content.EndsWith(";")).ToArray();
            // current query's begin index should be previous query's end index + 1.
            // 1st query is outof rule, so just prepend to head.
            var begins = Enumerable.Range(0, ends.Length)
                .SelectMany(x => numLines.Where(y => y.index == ends[x].index + 1))
                .Prepend(numLines.First())
                .ToArray();
            // pick up range
            ParseQuery[] queryPerTables;
            if (begins.Length == 1)
            {
                queryPerTables = new[] {
                    new ParseQuery()
                    {
                        Query = numLines.Select(x => x.content).ToJoinedString("\n"),
                        Title = ExtractTitle(numLines.First().content, regex),
                    },
                };
            }
            else if (begins.Zip(ends, (b, e) => (begin: b.index, end: e.index)).All(x => x.begin == x.end))
            {
                queryPerTables = numLines.Select(x => new ParseQuery()
                {
                    Query = x.content,
                    Title = ExtractTitle(x.content, regex),
                })
                .ToArray();
            }
            else
            {
                queryPerTables = Enumerable.Range(0, begins.Length - 1)
                    .Select(x => numLines
                        .Skip(begins[x].index) // CREATE TABLE ....
                        .Take(begins[x + 1].index - begins[x].index)) // .... ;
                    .Select(x => new ParseQuery()
                    {
                        Query = x.Select(y => y.content).ToJoinedString("\n"),
                        Title = ExtractTitle(x.FirstOrDefault().content, regex),
                    })
                    .ToArray();
            }
            return queryPerTables;
        }

        /// <summary>
        /// Extract title from query with RegEx pattern
        /// </summary>
        /// <remarks>
        /// pattern1:
        ///   create table new_t  (like t1);
        /// pattern2:
        ///   create table `another some table $$` like `some table $$`;
        /// pattern3: 
        ///   CREATE TABLE IF NOT EXISTS `schema`.`table` (
        ///    `Id` INT NOT NULL,
        ///    PRIMARY KEY(`Id`));
        /// </remarks>
        /// <param name="query"></param>
        /// <param name="regex"></param>
        /// <returns></returns>
        public static string ExtractTitle(string query, Regex regex)
        {
            if (string.IsNullOrEmpty(query)) return null;
            var match = regex.Match(query);
            var result = "default_table";
            if (!match.Success)
            {
                return result;
            }

            // obtain schema and table name via Regex
            var collection = match.Groups;
            var pair = Enumerable.Range(1, collection.Count)
                .Select(y => (name: regex.GroupNameFromNumber(y), value: collection[y].Value))
                .ToArray();
            var table = pair.Where(y => y.name == "table").Where(y => y.value != null).Select(y => y.value).FirstOrDefault();
            var schema = pair.Where(y => y.name == "schema").Where(y => y.value != null).Select(y => y.value).FirstOrDefault();
            result = string.IsNullOrEmpty(schema)
                ? table
                : $"{schema}.{table}";

            // remove garbages
            var parenthesis = result.IndexOf("(");
            result = parenthesis != -1
                ? result?.Substring(0, parenthesis)
                : result;
            result = result.TrimEnd().TrimStart();
            result = result.Replace("`", "");
            var dallar = result.IndexOf("$");
            result = dallar != -1
                    ? result.Substring(0, dallar)
                    : result;
            var semicolun = result.IndexOf(";");
            result = semicolun != -1
                    ? result.Substring(0, semicolun)
                    : result;
            result = result.Replace(" ", "_");
            result = result.Replace("\r\n", "\n");
            return result;
        }
    }

    public static class StringExtensions
    {
        public static string ToJoinedString(this IEnumerable<string> values, string separator = "")
            => string.Join(separator, values);
        public static string RemoveNewLine(this string value)
            => value?.Replace("\r\n", "")?.Replace("\n", "");
    }
}
