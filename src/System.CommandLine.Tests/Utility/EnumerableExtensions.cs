// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !NET6_0_OR_GREATER

using System.Collections.Generic;

internal static class EnumerableExtensions
{
    public static IEnumerable<T> Prepend<T>(this IEnumerable<T> source, T item)
    {
        yield return item;

        foreach (var x in source)
        {
            yield return x;
        }
    }
}

#endif