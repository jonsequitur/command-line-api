// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine.Parsing;
using System.CommandLine.Tests.Utility;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace System.CommandLine.Tests
{
    public class SplitCommandLineTests
    {
        private readonly ITestOutputHelper _output;

        public SplitCommandLineTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void It_splits_strings_based_on_whitespace()
        {
            var commandLine = "one two\tthree   four ";

            CommandLineStringSplitter.Instance
                                     .Split(commandLine)
                                     .Should()
                                     .BeEquivalentSequenceTo("one", "two", "three", "four");
        }

        [Fact]
        public void It_does_not_break_up_double_quote_delimited_values()
        {
            var commandLine = @"rm -r ""c:\temp files\""";

            CommandLineStringSplitter
                .Instance
                .Split(commandLine)
                .Should()
                .BeEquivalentSequenceTo("rm", "-r", @"c:\temp files\");
        }

        [Theory]
        [InlineData("-", '=')]
        [InlineData("-", ':')]
        [InlineData("--", '=')]
        [InlineData("--", ':')]
        [InlineData("/", '=')]
        [InlineData("/", ':')]
        public void It_does_not_split_double_quote_delimited_values_when_a_non_whitespace_argument_delimiter_is_used(
            string prefix,
            char delimiter)
        {
            var optionAndArgument = $@"{prefix}the-option{delimiter}""c:\temp files\""";

            var commandLine = $"the-command {optionAndArgument}";

            CommandLineStringSplitter
                .Instance
                .Split(commandLine)
                .Should()
                .BeEquivalentSequenceTo("the-command", optionAndArgument.Replace("\"", ""));
        }

        [Fact]
        public void It_handles_multiple_options_with_quoted_arguments()
        {
            var source = Directory.GetCurrentDirectory();
            var destination = Path.Combine(Directory.GetCurrentDirectory(), ".trash");

            var commandLine = $"move --from \"{source}\" --to \"{destination}\"";

            var tokenized = CommandLineStringSplitter.Instance.Split(commandLine);

            _output.WriteLine(commandLine);

            foreach (var token in tokenized)
            {
                _output.WriteLine("         " + token);
            }

            tokenized.Should()
                     .BeEquivalentSequenceTo("move", "--from", source, "--to", destination);
        }

        // FIX: (SplitCommandLineTests) https://docs.microsoft.com/en-us/previous-versions/17w5ykft(v=vs.85)

        [Theory]
        [InlineData("\"abc\" d e", new[]{"abc","d", "e"})]
        [InlineData("a\\\\\\b d\"e f\"g h", new[]{ "a\\\\\\b", "de fg", "h"})]
        [InlineData(@"a\\\""b c d", new[]{ @"a\\\""b", "c", "d"})]
        [InlineData(@"a\\\\""b c"" d e", new[]{ @"a\\\\b c", "d", "e"})]
        public void Actual_behavior_of_CommandLineToArgvW(string input, string[] expectedOutput)
        {
            var output = CommandLineToArgs(input);
            
            output.Should().BeEquivalentTo(expectedOutput, c => c.WithStrictOrdering());
        }
        
        [DllImport("shell32.dll", SetLastError = true)]
        private static extern IntPtr CommandLineToArgvW([MarshalAs(UnmanagedType.LPWStr)] string lpCmdLine, out int pNumArgs);

        public static string[] CommandLineToArgs(string commandLine)
        {
            int argc;
            var argv = CommandLineToArgvW(commandLine, out argc);
            if (argv == IntPtr.Zero)
            {
                throw new Win32Exception();
            }
            
            try
            {
                var args = new string[argc];
                for (var i = 0; i < args.Length; i++)
                {
                    var p = Marshal.ReadIntPtr(argv, i * IntPtr.Size);
                    args[i] = Marshal.PtrToStringUni(p);
                }

                return args;
            }
            finally
            {
                Marshal.FreeHGlobal(argv);
            }
        }

        [Fact]
        public void issue_1740()
        {
            var raw = "\"dotnet publish \\\"xxx.csproj\\\" -c Release -o \\\"./bin/latest/\\\" -r linux-x64 --self-contained false\"";
            var raw2 = @"""dotnet publish \""xxx.csproj\"" -c Release -o \""./bin/latest/\"" -r linux-x64 --self-contained false""";
            var raw3 = "\"dotnet publish \"xxx.csproj\" -c Release -o \"./bin/latest/\" -r linux-x64 --self-contained false\"";

            var array = CommandLineStringSplitter.Instance.Split(raw3).ToArray();

            foreach (var arg in array)
            {
                Console.WriteLine(arg);
            }

            array.Should().BeEquivalentTo(
                new[] { "dotnet", "publish", "\"xxx.csproj\"", "-c", "Release", "-o", "\"./bin/latest\"", "-r", "linux-x64", "--self-contained", "false" });

            // TODO (testname) write test
            throw new NotImplementedException();
        }

        [Theory] // test cases take from: https://stackoverflow.com/questions/298830/split-string-containing-command-line-parameters-into-string-in-c-sharp
        [InlineData("One", new[] { "One" })]
        [InlineData("One ", new[] { "One" })]
        [InlineData(" One", new[] { "One" })]
        [InlineData(" One ", new[] { "One" })]
        [InlineData("One Two", new[] { "One", "Two" })]
        [InlineData("One  Two", new[] { "One", "Two" })]
        [InlineData("One   Two", new[] { "One", "Two" })]
        [InlineData("\"One Two\"", new[] { "One Two" })]
        [InlineData("One \"Two Three\"", new[] { "One", "Two Three" })]
        [InlineData("One \"Two Three\" Four", new[] { "One", "Two Three", "Four" })]
        [InlineData("One=\"Two Three\" Four", new[] { "One=Two Three", "Four" })]
        [InlineData("One\"Two Three\" Four", new[] { "OneTwo Three", "Four" })]
        [InlineData("One\"Two Three   Four", new[] { "OneTwo Three   Four" })]
        [InlineData("One\" \"Two", new[] { "One Two" })]
        [InlineData("\"One\"  \"Two\"", new[] { "One", "Two" })]
        [InlineData("One\\\"  Two", new[] { "One\"", "Two" })]
        [InlineData("\\\"One\\\"  Two", new[] { "\"One\"", "Two" })]
        [InlineData("One\"", new[] { "One" })]
        [InlineData("\"One", new[] { "One" })]
        [InlineData("One \"\"", new[] { "One", "" })]
        [InlineData("One \"", new[] { "One", "" })]
        [InlineData("1 A=\"B C\"=D 2", new[] { "1", "A=B C=D", "2" })]
        [InlineData("1 A=\"B \\\" C\"=D 2", new[] { "1", "A=B \" C=D", "2" })]
        [InlineData("1 \\A 2", new[] { "1", "\\A", "2" })]
        [InlineData("1 \\\" 2", new[] { "1", "\"", "2" })]
        [InlineData("1 \\\\\" 2", new[] { "1", "\\\"", "2" })]
        [InlineData("\"", new[] { "" })]
        [InlineData("\\\"", new[] { "\"" })]
        [InlineData("'A B'", new[] { "'A", "B'" })]
        [InlineData("^", new[] { "^" })]
        [InlineData("^A", new[] { "A" })]
        [InlineData("^^", new[] { "^" })]
        [InlineData("\\^^", new[] { "\\^" })]
        [InlineData("^\\\\", new[] { "\\\\" })]
        [InlineData("^\"A B\"", new[] { "A B" })]
        [InlineData("/src:\"C:\\tmp\\Some Folder\\Sub Folder\" /users:\"abcdefg@hijkl.com\" tasks:\"SomeTask,Some Other Task\" -someParam foo",
                    new[]
                    {
                        "/src:C:\\tmp\\Some Folder\\Sub Folder",
                        "/users:abcdefg@hijkl.com",
                        "tasks:SomeTask,Some Other Task",
                        "-someParam",
                        "foo"
                    })]
        [InlineData("", new string[] { })]
        [InlineData("a", new[] { "a" })]
        [InlineData(" abc ", new[] { "abc" })]
        [InlineData("a b ", new[] { "a", "b" })]
        [InlineData("a b \"c d\"", new[] { "a", "b", "c d" })]
        [InlineData("this is a test ", new[] { "this", "is", "a", "test" })]
        [InlineData("this \"is a\" test", new[] { "this", "is a", "test" })]
        [InlineData("\"C:\\Program Files\"", new[] { "C:\\Program Files" })]
        [InlineData("\"He whispered to her \\\"I love you\\\".\"", new[] { "He whispered to her \"I love you\"." })]
        public void Matches_Main_args_string_split_behavior(string input, string[] expected)
        {
            var actualManaged = CommandLineStringSplitter.Instance.Split2(input).ToArray();
            var actualNative = CommandLineToArgs(input);

            actualManaged.Should().BeEquivalentTo(expected, config => config.WithStrictOrdering());
         //   actualNative.Should().BeEquivalentTo(expected, config => config.WithStrictOrdering());
        }
    }
}