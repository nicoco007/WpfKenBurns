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
    internal class RandomizedEnumerator<T> : IEnumerator<T>, IReadOnlyCollection<T> where T : notnull
    {
        private readonly T[] items;
        private int currentIndex = -1;

        public RandomizedEnumerator(IEnumerable<T> items)
        {
            this.items = items.ToArray();
            Shuffle();
        }

        public T Current => currentIndex >= 0 && currentIndex < items.Length ? items[currentIndex] : throw new InvalidOperationException("Collection is empty");

        public int Count => items.Length;

        object IEnumerator.Current => Current;

        public void Dispose() { }

        public bool MoveNext()
        {
            if (items.Length == 0) return false;

            currentIndex++;

            return currentIndex < items.Length;
        }

        public void Reset()
        {
            currentIndex = -1;
            Shuffle();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return (IEnumerator<T>)items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return items.GetEnumerator();
        }

        private void Shuffle()
        {
            if (items.Length <= 1) return;

            Random random = new();

            for (int i = items.Length - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);

                T tmp = items[i];
                items[i] = items[j];
                items[j] = tmp;
            }
        }
    }
}
