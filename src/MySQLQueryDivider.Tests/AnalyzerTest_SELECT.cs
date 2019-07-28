using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace MySQLQueryDivider.Tests
{
    public class AnalyzerTest_SELECT
    {
        private static Regex regex = new Regex(@"\s*SELECT\s*(.*)\s*FROM\s*(?<schema>`?.+`?)\.?(?<table>`?.*`?)", RegexOptions.IgnoreCase);
        private static string[] escapes = new[] { "-- ----", "--", "SET FOREIGN_KEY_CHECKS", "DROP SCHEMA", "CREATE SCHEMA" };

        [Theory]
        [MemberData(nameof(FromStringTest))]
        public void FromStringUnitTest(FromStringData data)
        {
            var tables = Analyzer.FromString(data.InputSql, regex);
            for (var i = 0; i < tables.Length; i++)
            {
                tables[i].Query.Should().Be(data.Expected[i].Query);
                tables[i].Title.Should().Be(data.Expected[i].Title);
            }
        }

        public class FromStringData
        {
            public string InputSql { get; set; }
            public ParseQuery[] Expected { get; set; }
        }
        public static IEnumerable<object[]> FromStringTest()
        {
            yield return new object[]
            {
                new FromStringData
                {
                    InputSql = "SELECT * FROM HOGE;",
                    Expected = new [] {
                        new ParseQuery
                        {
                            Title = "HOGE",
                            Query = "SELECT * FROM HOGE;",
                        },
                    },
                 }
            };
            yield return new object[]
            {
                new FromStringData
                {
                    InputSql = @"SELECT * FROM HOGE;SELECT fuga, piyo FROM TYGER;",
                    Expected = new [] {
                        new ParseQuery
                        {
                            Title = "HOGE",
                            Query = "SELECT * FROM HOGE;",
                        },
                        new ParseQuery
                        {
                            Title = "TYGER",
                            Query = "SELECT fuga, piyo FROM TYGER;",
                        },
                    }
                 }
            };
            yield return new object[]
            {
                new FromStringData
                {
                    InputSql = @"SELECT * FROM HOGE;SELECT fuga, piyo FROM TYGER;",
                    Expected = new [] {
                        new ParseQuery
                        {
                            Title = "HOGE",
                            Query = "SELECT * FROM HOGE;",
                        },
                        new ParseQuery
                        {
                            Title = "TYGER",
                            Query = "SELECT fuga, piyo FROM TYGER;",
                        },
                    }
                 }
            };
        }
    }
}
