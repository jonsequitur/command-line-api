﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using Xunit;

namespace System.CommandLine.Tests
{
    public abstract class SymbolTests
    {
        [Fact]
        public void When_Name_is_explicitly_set_then_adding_aliases_does_not_change_it()
        {
            var symbol = CreateSymbol("original");

            symbol.Name = "changed";

            symbol.Name.Should().Be("changed");
        }

        [Fact]
        public void When_Name_is_changed_then_old_name_is_not_among_aliases()
        {
            var symbol = CreateSymbol("original");

            symbol.Name = "changed";

            symbol.HasAlias("original").Should().BeFalse();
            symbol.RawAliases.Should().NotContain("original");
            symbol.Aliases.Should().NotContain("original");
        }

        protected abstract Symbol CreateSymbol(string name);
    }
}