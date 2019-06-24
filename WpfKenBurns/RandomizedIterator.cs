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
