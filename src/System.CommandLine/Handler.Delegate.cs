// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine.Binding;
using System.CommandLine.Invocation;

namespace System.CommandLine;

public static partial class Handler
{
    // FIX: (Handler) 
    public static void SetHandler(
        this Command command,
        Delegate handle,
        params IValueDescriptor[] symbols) =>
        command.Handler = new AnonymousCommandHandler(
            context => { });
}