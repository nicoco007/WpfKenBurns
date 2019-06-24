// WpfKenBurns - A simple Ken Burns-style screensaver
// Copyright © 2019 Nicolas Gnyra

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
    class RandomizedIterator<T> : IEnumerator<T>
    {
        private T[] items;
        private int currentIndex = -1;

        public RandomizedIterator(IEnumerable<T> items)
        {
            this.items = items.ToArray();
            Shuffle();
        }

        public T Current => currentIndex >= 0 && currentIndex < items.Length ? items[currentIndex] : default;

        object IEnumerator.Current => Current;

        public void Dispose() { }

        public bool MoveNext()
        {
            if (items.Length == 0 || currentIndex >= items.Length) return false;

            currentIndex++;

            return true;
        }

        public void Reset()
        {
            currentIndex = -1;
            Shuffle();
        }

        private void Shuffle()
        {
            if (items.Length == 0) return;

            Random random = new Random();

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
