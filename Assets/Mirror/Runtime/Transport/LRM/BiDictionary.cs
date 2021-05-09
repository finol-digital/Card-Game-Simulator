//
// Source: https://stackoverflow.com/questions/255341/getting-multiple-keys-of-specified-value-of-a-generic-dictionary#255630
//
using System;
using System.Collections.Generic;

namespace LightReflectiveMirror
{
    class BiDictionary<TFirst, TSecond>
    {
        IDictionary<TFirst, TSecond> firstToSecond = new Dictionary<TFirst, TSecond>();
        IDictionary<TSecond, TFirst> secondToFirst = new Dictionary<TSecond, TFirst>();

        public void Add(TFirst first, TSecond second)
        {
            if (firstToSecond.ContainsKey(first) ||
                secondToFirst.ContainsKey(second))
            {
                throw new ArgumentException("Duplicate first or second");
            }
            firstToSecond.Add(first, second);
            secondToFirst.Add(second, first);
        }

        public bool TryGetByFirst(TFirst first, out TSecond second)
        {
            return firstToSecond.TryGetValue(first, out second);
        }

        public void Remove(TFirst first)
        {
            secondToFirst.Remove(firstToSecond[first]);
            firstToSecond.Remove(first);
        }

        public ICollection<TFirst> GetAllKeys()
        {
            return secondToFirst.Values;
        }

        public bool TryGetBySecond(TSecond second, out TFirst first)
        {
            return secondToFirst.TryGetValue(second, out first);
        }

        public TSecond GetByFirst(TFirst first)
        {
            return firstToSecond[first];
        }

        public TFirst GetBySecond(TSecond second)
        {
            return secondToFirst[second];
        }
    }
}