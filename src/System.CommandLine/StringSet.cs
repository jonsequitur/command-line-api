// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Generic;

namespace System.CommandLine
{
    internal class StringSet : ICollection<string>, IReadOnlyList<string>
    {
        private readonly List<string> _values = new List<string>();

        public IEnumerator<string> GetEnumerator() => _values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void Add(string item)
        {
            for (var i = 0; i < _values.Count; i++)
            {
                var value = _values[i];

                if (value.Equals(item, StringComparison.Ordinal))
                {
                    return;
                }
            }

            _values.Add(item);
        }

        public void Clear() => _values.Clear();

        public bool Contains(string item) => _values.Contains(item);

        public void CopyTo(string[] array, int arrayIndex) => _values.CopyTo(array, arrayIndex);

        public bool Remove(string item) => _values.Remove(item);

        public int Count => _values.Count;

        public bool IsReadOnly => true;

        public string this[int index] => _values[index];
    }
}