using System.Collections.Generic;
using System.Linq;

namespace Cherry.Net.Utils
{
    /// <summary>
    /// 对象池 过滤重复
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class UniquePool<T> where T : class, new()
    { 
        protected readonly HashSet<T> Pool;

        public UniquePool(int capacity = 0)
        {
            Pool = new HashSet<T>(capacity);
            while (capacity-- > 0)
            {
                Pool.Add(new T()); //new T()  default(T) 引用类型返回null  值类型返回0  结构体返回默认值结构体
            }
        }

        public int Count => Pool.Count;


        public virtual void Push(T t)
        {
            if (t == null) return;
            Pool.Add(t);
        }


        public virtual T Pop()
        {
            T t;
            if (Pool.Count <= 0)
            {
                t = new T();
            }
            else
            {
                t = Pool.First();
                Pool.Remove(t);
            }
            return t;
        } 
    }


    public class ConcurrentUniquePool<T> : UniquePool<T> where T : class, new()
    {
        public ConcurrentUniquePool(int capacity = 0) : base(capacity)
        {

        }

        public override void Push(T t)
        {
            lock (Pool)
            {
                base.Push(t);
            }
        }

        public override T Pop()
        {
            lock (Pool)
            {
                return base.Pop();
            }
        }

    }
}