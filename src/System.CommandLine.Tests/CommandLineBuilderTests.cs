// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine.Builder;
using System.CommandLine.Help;
using System.CommandLine.IO;
using System.CommandLine.Parsing;
using FluentAssertions;
using Xunit;

namespace System.CommandLine.Tests;

public class CommandLineBuilderTests
{
    [Fact]
    public void issue_1691()
    {
        //This throws exception with: "One or more errors occurred. (An item with the same key has already been added. Key: --version)'"
        var customHelpMessage = "**Adding extra help here**";

        var command = new RootCommand();

        var parser = new CommandLineBuilder(command)
                     .UseDefaults()
                     .UseHelp(ctx =>
                     {
                         ctx.HelpBuilder.CustomizeLayout(_ => HelpBuilder.Default
                                                                         .GetLayout()
                                                                         .Prepend(
                                                                             _ => _.Output.WriteLine(customHelpMessage)
                                                                         ));
                     }).Build();

        var console = new TestConsole();

        // rootCommand.Invoke("-h", console);
        //
        // console.Out.ToString().Should().Contain(customHelpMessage);

        //This runs, but the help output is not modified as expected
        var command2 = new RootCommand();
        var parser2 = new CommandLineBuilder(command2)
                      .UseHelp(ctx =>
                      {
                          ctx.HelpBuilder.CustomizeLayout(_ => HelpBuilder.Default
                                                                          .GetLayout()
                                                                          .Prepend(
                                                                              _ => _.Output.WriteLine(customHelpMessage)
                                                                          ));
                      }).Build();

        var console2 = new TestConsole();

        parser2.Invoke("-h", console2);

        console2.Out.ToString().Should().Contain(customHelpMessage);

        var console3 = new TestConsole();

        command2.Invoke("-h", console3);

        console3.Out.ToString().Should().Contain(customHelpMessage);



        // TODO (issue_1691) write test
        throw new NotImplementedException();
    }
}