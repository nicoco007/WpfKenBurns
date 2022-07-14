// <copyright file="RandomizedEnumerator.cs" company="PlaceholderCompany">
// WpfKenBurns - A simple Ken Burns-style screensaver
// Copyright © 2019-2022 Nicolas Gnyra
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
// GNU Affero General Public License for more details.
//
// You should have received a copy of the GNU Affero General Public License
// along with this program.If not, see&lt;https://www.gnu.org/licenses/&gt;.
// </copyright>

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace WpfKenBurns
{
    internal class RandomizedEnumerator<T> : IEnumerator<T>, ICollection<T>
        where T : notnull
    {
        private readonly List<T> items;
        private readonly Random random = new();
        private int currentIndex = -1;

        public RandomizedEnumerator(IEnumerable<T> items)
        {
            this.items = items.ToList();
            this.Shuffle();
        }

        public T Current => this.currentIndex >= 0 && this.currentIndex < this.items.Count ? this.items[this.currentIndex] : throw new InvalidOperationException("Collection is empty");

        public int Count => this.items.Count;

        object IEnumerator.Current => this.Current;

        public bool IsReadOnly => false;

        public void Dispose()
        {
        }

        public bool MoveNext()
        {
            if (this.items.Count == 0)
            {
                return false;
            }

            this.currentIndex++;

            return this.currentIndex < this.items.Count;
        }

        public void Reset()
        {
            this.currentIndex = -1;
            this.Shuffle();
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
            this.items.Add(item);
        }

        public void Clear()
        {
            this.items.Clear();
        }

        public bool Contains(T item)
        {
            return this.items.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            this.items.CopyTo(array, arrayIndex);
        }

        public bool Remove(T item)
        {
            return this.items.Remove(item);
        }

        private void Shuffle()
        {
            if (this.items.Count <= 1)
            {
                return;
            }

            for (int i = this.items.Count - 1; i > 0; i--)
            {
                int j = this.random.Next(i + 1);

                (this.items[j], this.items[i]) = (this.items[i], this.items[j]);
            }
        }
    }
}
