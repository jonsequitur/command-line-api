// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using Xunit;

namespace System.CommandLine.Tests
{
    public class GlobalOptionTests
    {
        [Fact]
        public void Global_options_appear_in_options_list()
        {
            var root = new Command("parent");

            var option = new Option<int>("--global");

            root.AddGlobalOption(option);

            var child = new Command("child");

            root.AddCommand(child);

            root.Options.Should().Contain(option);
        }

        [Fact] // https://github.com/dotnet/command-line-api/issues/1540
        public void When_a_required_global_option_is_omitted_it_results_in_an_error()
        {
            var command = new Command("child");
            var rootCommand = new RootCommand { command };
            command.SetHandler(() => { });
            var requiredOption = new Option<bool>("--i-must-be-set")
            {
                IsRequired = true
            };
            rootCommand.AddGlobalOption(requiredOption);

            var result = rootCommand.Parse("child");

            result.Errors
                  .Should()
                  .ContainSingle()
                  .Which.Message.Should().Be("Option '--i-must-be-set' is required.");
        }

        [Fact] 
        public void When_a_required_global_option_has_multiple_aliases_the_error_message_uses_longest()
        {
            var rootCommand = new RootCommand();
            var requiredOption = new Option<bool>(new[] { "-i", "--i-must-be-set" })
            {
                IsRequired = true
            };
            rootCommand.AddGlobalOption(requiredOption);

            var result = rootCommand.Parse("");

            result.Errors
                  .Should()
                  .ContainSingle()
                  .Which.Message.Should().Be("Option '--i-must-be-set' is required.");
        }

        [Fact]
        public void When_a_required_global_option_is_present_on_child_of_command_it_was_added_to_it_does_not_result_in_an_error()
        {
            var command = new Command("child");
            var rootCommand = new RootCommand { command };
            command.SetHandler(() => { });
            rootCommand.AddGlobalOption(new Option<bool>("--i-must-be-set")
            {
                IsRequired = true
            });

            var result = rootCommand.Parse("child --i-must-be-set");

            result.Errors.Should().BeEmpty();
        }

        [Fact]
        public void Subcommands_added_after_a_global_option_is_added_to_parent_will_recognize_the_global_option()
        {
            var root = new Command("parent");

            var option = new Option<int>("--global");

            root.AddGlobalOption(option);

            var child = new Command("child");

            root.AddCommand(child);

            root.Parse("child --global 123").GetValueForOption(option).Should().Be(123);

            child.Parse("--global 123").GetValueForOption(option).Should().Be(123);
        }

        [Fact]
        public void Subcommands_with_global_option_should_propagate_option_to_children()
        {
            var root = new Command("parent");
            
            var firstChild = new Command("first");
            
            root.AddCommand(firstChild);
            
            var option = new Option<int>("--global");
            
            firstChild.AddGlobalOption(option);
            
            var secondChild = new Command("second");
            
            firstChild.AddCommand(secondChild);
            
            root.Parse("first second --global 123").GetValueForOption(option).Should().Be(123);
            
            firstChild.Parse("second --global 123").GetValueForOption(option).Should().Be(123);
            
            secondChild.Parse("--global 123").GetValueForOption(option).Should().Be(123);
        }

        [Fact]
        public void Global_options_on_child_commands_take_precedence_over_global_options_on_parent_commands()
        {
            var child = new Command("child");

            var parent = new RootCommand
            {
                child
            };

            var parentOption = new Option<string>("-x");
            parent.AddGlobalOption(parentOption);
            var childOption = new Option<string>("-x");
            child.AddGlobalOption(childOption);

            parent.Invoking(p => new CommandLineConfiguration(p).ThrowIfInvalid()).Should().NotThrow();

            var resultWhenPassingParentOption = parent.Parse("-x hello-parent");

            resultWhenPassingParentOption.GetValueForOption(parentOption).Should().Be("hello-parent");
            resultWhenPassingParentOption.GetValueForOption(childOption).Should().BeNull();

            var resultWhenPassingChildOption = parent.Parse("child -x hello-child");

            resultWhenPassingChildOption.GetValueForOption(childOption).Should().Be("hello-child");
            resultWhenPassingChildOption.GetValueForOption(parentOption).Should().BeNull();

            var resultWhenPassingBoth = parent.Parse("-x hello-parent child -x hello-child");

            resultWhenPassingBoth.GetValueForOption(childOption).Should().Be("hello-child");
            resultWhenPassingBoth.GetValueForOption(parentOption).Should().Be("hello-parent");
        }

        [Fact]
        public void Specific_aliases_on_options_on_child_commands_can_override_global_options_sharing_only_that_alias()
        {
            var child = new Command("child");

            var parent = new RootCommand
            {
                child
            };

            var globalOption = new Option<string>("--dupe");
            parent.AddGlobalOption(globalOption);
            var childOption = new Option<string>(new[] { "--dupe", "--different" });
            child.AddOption(childOption);

            parent.Parse("--dupe hello-parent").GetValueForOption(globalOption).Should().Be("hello-parent");
            parent.Parse("child --dupe hello-child").GetValueForOption(childOption).Should().Be("hello-child");
            parent.Parse("child --different hello-child").GetValueForOption(childOption).Should().Be("hello-child");
        }

        [Fact]
        public void When_global_option_is_declared_on_the_same_command_as_local_option_then_______()
        {
            // FIX: is this a valid scenario?

            var command = new Command("the-command")
            {
                new Option<string>("--same")
            };

            command
                .Invoking(c => c.AddGlobalOption(new Option<int>("--same")))
                .Should()
                .NotThrow<ArgumentException>();

            command
                .Invoking(c => new CommandLineConfiguration(c).ThrowIfInvalid()).Should()
                .NotThrow();
        }
    }
}