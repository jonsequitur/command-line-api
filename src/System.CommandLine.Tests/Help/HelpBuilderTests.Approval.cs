// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System.IO;
using ApprovalTests;

namespace System.CommandLine.Tests.Help
{
    public partial class HelpBuilderTests
    {
        [Fact]
        public void Help_describes_default_values_for_complex_root_command_scenario()
        {
            var command = new RootCommand(description: "Test description")
            {
                new Argument<string>("arg-no-description-no-default"),
                new Argument<string>("arg-no-description-default",
                    argResult => "arg-no-description-default-value",
                    isDefault: true),
                new Argument<string>("arg-no-default")
                {
                    Description = "arg-no-default-description",
                },
                new Argument<string>("arg", () => "arg-one-value")
                {
                    Description = "arg-description"
                },
                new Argument<FileAccess>("arg-enum-default", () => FileAccess.Read)
                {
                    Description = "arg-enum-default-description",
                },
                new Option(aliases: new string[] {"--option-no-arg", "-na"}) {
                    Description = "option-no-arg-description",
                    IsRequired = true
                },
                new Option<string>(
                    aliases: new string[] {"--option-no-description-default-arg", "-ondda"}, 
                    parseArgument: _ => "option-no-description-default-arg-value",
                    isDefault: true
                ),
                new Option<string>(aliases: new string[] {"--option-no-default-arg", "-onda"}) {
                    Description = "option-no-default-description",
                    ArgumentHelpName = "option-arg-no-default-arg",
                    IsRequired = true
                },
                new Option<string>(aliases: new string[] {"--option-default-arg", "-oda"}, () => "option-arg-value") 
                {
                    Description = "option-default-arg-description",
                    ArgumentHelpName = "option-arg",
                },
                new Option<FileAccess>(aliases: new string[] {"--option-enum-arg", "-oea"}, () => FileAccess.Read) 
                {
                    Description = "option-description",
                    ArgumentHelpName = "option-arg",
                },
                new Option<FileAccess>(aliases: new string[] {"--option-required-enum-arg", "-orea"}, () => FileAccess.Read) 
                {
                    Description = "option-description",
                    ArgumentHelpName = "option-arg",
                    IsRequired = true
                },
                new Option<string>(aliases: new string[] {"--option-multi-line-description", "-omld"}) {
                    Description = "option with\r\nmulti-line\ndescription"
                }
            };
            command.Name = "command";

            GetHelpBuilder(LargeMaxWidth).Write(command);
            Approvals.Verify(_console.Out.ToString());
        }
    }
}
