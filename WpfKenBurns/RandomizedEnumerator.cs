// WpfKenBurns - A simple Ken Burns-style screensaver
// Copyright © 2019-2021 Nicolas Gnyra

// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
// GNU Affero General Public License for more details.

// You should have received a copy of the GNU Affero General Public License
// along with this program.If not, see<https://www.gnu.org/licenses/>.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace WpfKenBurns
{
    internal class RandomizedEnumerator<T> : IEnumerator<T>, ICollection<T> where T : notnull
    {
        private readonly List<T> items;
        private readonly Random random = new();
        private int currentIndex = -1;

        public RandomizedEnumerator(IEnumerable<T> items)
        {
            this.items = items.ToList();
            Shuffle();
        }

        public T Current => currentIndex >= 0 && currentIndex < items.Count ? items[currentIndex] : throw new InvalidOperationException("Collection is empty");

        public int Count => items.Count;

        object IEnumerator.Current => Current;

        public bool IsReadOnly => false;

        public void Dispose() { }

        public bool MoveNext()
        {
            if (items.Count == 0) return false;

            currentIndex++;

            return currentIndex < items.Count;
        }

        public void Reset()
        {
            currentIndex = -1;
            Shuffle();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return this;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this;
        }

        public void Add(T item)
        {
            items.Add(item);
        }

        public void Clear()
        {
            items.Clear();
        }

        public bool Contains(T item)
        {
            return items.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            items.CopyTo(array, arrayIndex);
        }

        public bool Remove(T item)
        {
            return items.Remove(item);
        }

        private void Shuffle()
        {
            if (items.Count <= 1) return;

            for (int i = items.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);

                (items[j], items[i]) = (items[i], items[j]);
            }
        }
    }
}
