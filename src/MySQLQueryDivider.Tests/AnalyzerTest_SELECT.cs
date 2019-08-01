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
        private static Regex regex = new Regex(@"\s*SELECT\s+(.*)\s+FROM\s+(?<schema>`?.*`?\.)?(?<table>`?.*`?)", RegexOptions.IgnoreCase);
        private static string[] escapes = new[] { "-- ----", "--", "SET FOREIGN_KEY_CHECKS", "DROP SCHEMA", "CREATE SCHEMA" };

        [Theory]
        [MemberData(nameof(FromStringTest))]
        public void FromStringUnitTest(FromStringData data)
        {
            var option = new AnalyzerOption
            {
                EscapeLines = escapes,
            };
            var tables = Analyzer.FromString(data.InputSql, regex, option);
            for (var i = 0; i < tables.Length; i++)
            {
                tables[i].Query.Should().Be(data.Expected[i].Query);
                tables[i].Title.Should().Be(data.Expected[i].Title);
            }
        }

        [Theory]
        [MemberData(nameof(FromFileTest))]
        public void FromFileUnitTest(FromFileData data)
        {
            var option = new AnalyzerOption
            {
                EscapeLines = escapes,
            };
            var tables = Analyzer.FromFile(data.InputPath, regex, option);
            tables.Length.Should().Be(data.Count);
            for (var i = 0; i < tables.Length; i++)
            {
                tables[i].Query.Should().Be(data.Expected[i].Query);
                tables[i].Title.Should().Be(data.Expected[i].Title);
            }
        }

        [Theory]
        [MemberData(nameof(RemoveSchemaFromFileTest))]
        public void RemoveSchemaNameTest(FromFileData data)
        {
            var option = new AnalyzerOption
            {
                EscapeLines = escapes,
                RemoveSchemaName = true,
            };
            var files = Analyzer.FromFile(data.InputPath, regex, option);
            var tables = files.Select(x => x).ToArray();
            tables.Length.Should().Be(data.Count);
            for (var i = 0; i < tables.Length; i++)
            {
                tables[i].Query.Should().Be(data.Expected[i].Query);
                tables[i].Title.Should().Be(data.Expected[i].Title);
            }
        }
        [Theory]
        [MemberData(nameof(RemoveSchemaWithNoTSchemaQueryFromFileTest))]
        public void RemoveSchemaNameNotEffectToTableOnlyTest(FromFileData data)
        {
            var option = new AnalyzerOption
            {
                EscapeLines = escapes,
                RemoveSchemaName = true,
            };
            var tables = Analyzer.FromFile(data.InputPath, regex, option);
            tables.Length.Should().Be(data.Count);
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

        public class FromFileData
        {
            public string InputPath { get; set; }
            public int Count { get; set; }
            public ParseQuery[] Expected { get; set; }
        }

        public static IEnumerable<object[]> FromFileTest()
        {
            yield return new object[]
            {
                new FromFileData
                {
                    InputPath = "./test_data/select_tables.sql",
                    Count = 3,
                    Expected = new [] {
                        new ParseQuery
                        {
                            Title = "HOGE",
                            Query = @"select * from `HOGE`;",
                        },
                        new ParseQuery
                        {
                            Title = "Foo.Bar",
                            Query = @"select hoge, moge, fuga from `Foo`.`Bar`;",
                        },
                        new ParseQuery
                        {
                            Title = "Foo.Bar",
                            Query = @"select hoge from Foo.Bar;",
                        },
                    },
                }
            };
        }
        public static IEnumerable<object[]> RemoveSchemaFromFileTest()
        {
            yield return new object[]
            {
                new FromFileData
                {
                    InputPath = "./test_data/select_tables.sql",
                    Count = 3,
                    Expected = new [] {
                        new ParseQuery
                        {
                            Title = "HOGE",
                            Query = @"select * from `HOGE`;",
                        },
                        new ParseQuery
                        {
                            Title = "Bar",
                            Query = @"select hoge, moge, fuga from `Bar`;",
                        },
                        new ParseQuery
                        {
                            Title = "Bar",
                            Query = @"select hoge from Bar;",
                        },
                    },
                }
            };
        }
        public static IEnumerable<object[]> RemoveSchemaWithNoTSchemaQueryFromFileTest()
        {
            yield return new object[]
            {
                new FromFileData
                {
                    InputPath = "./test_data/select_tables_complex.sql",
                    Count = 3,
                    Expected = new [] {
                        new ParseQuery
                        {
                            Title = "HOGE",
                            Query = @"select * from HOGE;",
                        },
                        new ParseQuery
                        {
                            Title = "Bar",
                            Query = @"select hoge, moge, fuga from `Bar`;",
                        },
                        new ParseQuery
                        {
                            Title = "Bar",
                            Query = @"select hoge from Bar;",
                        },
                    },
                }
            };
        }
    }
}
