using System;
using System.Collections.Generic;
using System.Linq;

namespace Cherry.Net.Utils
{
    /// <inheritdoc />
    /// <summary>
    /// 线程安全的hashset
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class HashSetEx<T> : HashSet<T>
    {
        private readonly Random _rnd = new Random();

        private readonly object _sync = new object();
          
        public HashSetEx()
        {    
        }

        public HashSetEx(int capacity) : base(capacity)
        { 
        } 
        public new bool Add(T t)
        {
            lock (_sync)
                return base.Add(t);
        }

        public void Add(IEnumerable<T> ls)
        {
            lock (_sync)
                foreach (var t in ls)
                {
                    base.Add(t);
                }
        }

        public new bool Remove(T t)
        {
            lock (_sync)
                return base.Remove(t);
        }

        public void Remove(IEnumerable<T> ls)
        {
            lock (_sync)
                foreach (var t in ls)
                {
                    base.Remove(t);
                }
        }

        public void AddOrRemove(T t, bool add)
        {
            lock (_sync)
            {
                if (add) base.Add(t);
                else base.Remove(t);
            }
        }

        public bool Random(out T t, bool del = false)
        {
            t = default(T);

            lock (_sync)
            {
                if (Count == 0) return false;
                t = ElementAt(_rnd.Next(Count));
                if (del) base.Remove(t);
            }

            return true;
        }


        public T First(bool remove = false)
        {
            lock (_sync)
            {
                var info = Enumerable.First(this);
                if (remove) base.Remove(info);
                return info;
            }
        }

        public T ElementAt(int index, bool remove = false)
        {
            lock (_sync)
            {
                var info = Enumerable.ElementAt(this, index);
                if (remove) base.Remove(info);
                return info; 
            }
        }

        public T ElementAtOrDefault(int index, bool remove = false)
        {
            lock (_sync)
            {
                var info = Enumerable.ElementAtOrDefault(this, index);
                if (remove && info != null) base.Remove(info);
                return info;
            }
        }

        public IList<T> ToList(bool clear = false)
        {
            lock (_sync)
            {
                var ls = Enumerable.ToList(this);
                if (clear)
                {
                    base.Clear();
                }

                return ls;
            }
        }

        public Dictionary<TKey, T> ToDictionary<TKey>(Func<T, TKey> keyFunc, bool clear = false)
        {
            lock (_sync)
            {
                var ls = this.ToDictionary<T, TKey>(keyFunc);
                if (clear)
                {
                    base.Clear();
                }

                return ls;
            }
        }

        public new bool Contains(T t)
        {
            lock (_sync)
            {
                return base.Contains(t);
            }
        }

        public new void Clear()
        {
            lock (_sync)
            {
                base.Clear();
            }
        }
    }
}