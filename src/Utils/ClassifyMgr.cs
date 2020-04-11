using System.Collections.Generic;
using System.Linq;

namespace Cherry.Net.Utils
{
    public class ClassifyMgr<T> : ClassifyMgr<object, T>
    {
        public ClassifyMgr(params object[] keys) : base(keys)
        {

        }
    }

    public class ClassifyMgr<TKey, T> : Dictionary<TKey, HashSetEx<T>>
    {
        private readonly object _sync = new object();

        /// <summary>
        /// 预定一些分类
        /// </summary>
        /// <param name="keys"></param>
        public ClassifyMgr(params TKey[] keys)
        {
            if (keys == null) return;
            foreach (var key in keys)
            {
                this[key] = new HashSetEx<T>();
            }
        }

        /// <summary>
        /// 向分类添加数据
        /// </summary>
        /// <param name="k"></param>
        /// <param name="t"></param>
        /// <param name="force">true:如果没有 则创建</param>
        public virtual void Add(TKey k, T t, bool force = false)
        {
            if (force) this[k].Add(t);
            else
            {
                lock (_sync)
                {
                    if (base.TryGetValue(k, out var ls))
                    {
                        ls.Add(t);
                    }
                }
            }
        }

        /// <summary>
        /// 向所有分类添加数据
        /// </summary>
        /// <param name="t"></param>
        public virtual void Add(T t)
        {
            lock (_sync)
            {
                foreach (var ls in Values)
                {
                    ls.Add(t);
                }
            }
        }

        /// <summary>
        /// 只添加到一个分类数据
        /// </summary>
        /// <param name="k"></param>
        /// <param name="t"></param>
        /// <param name="force">true:如果没有 则创建</param>
        public virtual void AddSingle(TKey k, T t, bool force = false)
        {
            Add(k, t, force);
            lock (_sync)
            {
                foreach (var pair in this)
                {
                    if (!pair.Key.Equals(k))
                    {
                        pair.Value.Remove(t);
                    }
                }
            }
        }

        /// <summary>
        /// 删除分类数据
        /// </summary>
        /// <param name="k"></param>
        /// <param name="t"></param>
        public virtual void Del(TKey k, T t)
        {
            lock (_sync)
            {
                if (base.TryGetValue(k, out var ls))
                {
                    ls.Remove(t);
                }
            }
        }

        /// <summary>
        /// 删除所有分类数据
        /// </summary>
        /// <param name="t"></param>
        public virtual void Del(T t)
        {
            lock (_sync)
            {
                foreach (var ls in Values)
                {
                    ls.Remove(t);
                }
            }
        }

        /// <summary>
        /// 分类数据数量
        /// </summary>
        /// <param name="k"></param>
        /// <returns></returns> 
        public int Length(TKey k) => this[k].Count; 

        /// <summary>
        /// 所有分类总数
        /// </summary>
        /// <returns></returns>
        public new int Count()
        {
            lock (_sync)
            {
                return Values.Sum(t => t.Count);
            }
        }

        /// <summary>
        /// 清空所有分类
        /// </summary>
        public new void Clear()
        {
            lock (_sync)
            {
                base.Clear();
            }
        }

        /// <summary>
        /// 取得一个分类器
        /// </summary>
        /// <param name="k"></param>
        /// <param name="ls"></param>
        /// <returns></returns>

        public new bool TryGetValue(TKey k, out HashSetEx<T> ls)
        {
            lock (_sync)
            {
                return base.TryGetValue(k, out ls);
            }
        }

        /// <summary>
        /// 如果是获取 不存在则创建一个分类器
        /// </summary>
        /// <param name="k"></param>
        /// <returns></returns>
        public new HashSetEx<T> this[TKey k]
        {
            get
            {
                lock (_sync)
                {
                    if (base.TryGetValue(k, out var ls)) return ls;
                    ls = new HashSetEx<T>();
                    base[k] = ls;
                    return ls;
                }
            }
            set
            {
                lock (_sync)
                {
                    base[k] = value;
                }
            }
        }

        /// <summary>
        /// 数据是否存在 并返回所在分类器集合
        /// </summary> 
        /// <param name="val"></param>
        /// <param name="keys"></param>
        /// <returns></returns>
        public virtual bool Contain(T val, out IList<TKey> keys)
        {
            keys = new List<TKey>();
            lock (_sync)
            {
                foreach (var kv in this)
                {
                    if (kv.Value.Contains(val))
                    {
                        keys.Add(kv.Key);
                    }
                }
            }

            return keys.Count > 0;
        }

        /// <summary>
        /// 数据是否存在 并返回第一个所在分类器
        /// </summary> 
        /// <param name="val"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public virtual bool ContainOne(T val, out TKey key)
        {
            key = default(TKey);
            lock (_sync)
            {
                foreach (var kv in this)
                {
                    if (kv.Value.Contains(val))
                    {
                        key = kv.Key;
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// 返回所有类别
        /// </summary>
        public new ICollection<TKey> Keys()
        {
            lock (_sync)
            {
                return base.Keys.ToList();
            }
        }
    }

}