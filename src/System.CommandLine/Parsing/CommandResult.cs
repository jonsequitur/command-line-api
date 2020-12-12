﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine.Binding;
using System.Linq;

namespace System.CommandLine.Parsing
{
    public class CommandResult : SymbolResult
    {
        private ArgumentConversionResultSet? _results;

        internal CommandResult(
            ICommand command,
            Token token,
            CommandResult? parent = null) :
            base(command ?? throw new ArgumentNullException(nameof(command)),
                 parent)
        {
            Command = command;

            Token = token ?? throw new ArgumentNullException(nameof(token));
        }

        public ICommand Command { get; }

        // FIX: (CommandResult) remove indexer
        public OptionResult? this[string alias] => Children.GetByAlias(alias) as OptionResult;

        public Token Token { get; }

        internal ArgumentConversionResultSet ArgumentConversionResults
        {
            get
            {
                if (_results is null)
                {
                    var results = Children
                                  .OfType<ArgumentResult>()
                                  .Select(r => r.Convert(r.Argument));

                    _results = new ArgumentConversionResultSet();

                    foreach (var result in results)
                    {
                        _results.Add(result);
                    }
                }

                return _results;
            }
        }
    }
}
