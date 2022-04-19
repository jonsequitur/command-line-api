// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine.Parsing;
using FluentAssertions;
using Xunit;

namespace System.CommandLine.Tests
{
    public class UseParseErrorReportingTests
    {
        [Fact] // https://github.com/dotnet/command-line-api/issues/817
        public void Parse_error_reporting_reports_error_when_help_is_used_and_required_subcommand_is_missing()
        {
            var root = new RootCommand
            {
                new Command("inner")
            };

            var parser = new CommandLineBuilder(root)
                         .UseParseErrorReporting()
                         .UseHelp()
                         .Build();

            var parseResult = parser.Parse("");

            parseResult.Errors.Should().NotBeEmpty();

            var result = parser.Invoke("");

            result.Should().Be(1);
        }

        [Fact]
        public void Parse_error_uses_custom_error_result_code()
        {
            var root = new RootCommand
            {
                new Command("inner")
            };

            var parser = new CommandLineBuilder(root)
                         .UseParseErrorReporting(errorExitCode: 42)
                         .Build();

            int result = parser.Invoke("");

            result.Should().Be(42);
        }

        [Fact]
        public void Type_conversion_exceptions_do_not_show_detailed_exception_information_for_types_without_conversion_support()
        {
            // FIX: (Type_conversion_exceptions_do_not_show_detailed_exception_information_for_types_without_conversion_support) 
            var argument = new Argument<CantParseThis>();
            var root = new RootCommand
            {
                argument
            };
            root.SetHandler((CantParseThis o) =>
            {
                Console.WriteLine(o.ToString());
            }, argument);

            var parser = new CommandLineBuilder(root)
                         .UseParseErrorReporting()
                         .Build();

            TestConsole console = new();
            parser.Invoke("can't-parse-this", console);
            
            console.Out.ToString().Should().NotContain("Exception");
            console.Error.ToString().Should().NotContain("Exception");
        }

        public record struct CantParseThis
        {
            public int Value;
        }
    }
}