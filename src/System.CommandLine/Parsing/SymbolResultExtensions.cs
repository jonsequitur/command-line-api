// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace System.CommandLine.Parsing
{
    internal static class SymbolResultExtensions
    {
        internal static IEnumerable<SymbolResult> AllSymbolResults(this SymbolResult symbolResult)
        {
            yield return symbolResult;

            foreach (var item in symbolResult
                                 .Children
                                 .FlattenBreadthFirst(o => o.Children))
            {
                yield return item;
            }
        }

        internal static Token Token(this SymbolResult symbolResult)
        {
            return symbolResult switch
            {
                CommandResult commandResult => commandResult.Token,
                OptionResult optionResult => optionResult.Token ??
                                             CreateImplicitToken(optionResult.Option),
                _ => throw new ArgumentOutOfRangeException(nameof(symbolResult))
            };

            Token CreateImplicitToken(IOption option)
            {
                var optionName = option.Name;

                string defaultAlias = null;

                for (var i = 0; i < option.Aliases.Count; i++)
                {
                    var alias = option.Aliases[i];

                    if (alias.RemovePrefix().Equals(optionName, StringComparison.Ordinal))
                    {
                        defaultAlias = alias;
                    }
                }

                return new ImplicitToken(defaultAlias, TokenType.Option);
            }
        }
    }
}