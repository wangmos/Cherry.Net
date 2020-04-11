using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Cherry.Net.Extensions
{
    /// <summary>
    /// dynamic 不能解析到object的拓展函数
    /// </summary>
    public static class ObjectEx
    { 
        /// <summary>
        /// 获取成员  
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        /// <param name="memberName"></param>
        /// <returns></returns>
        public static T InvokeValue<T>(this object t, string memberName)
        {
            return (T)t.InvokeValue(memberName);
        }

        /// <summary>
        /// 获取成员  
        /// </summary> 
        /// <param name="t"></param>
        /// <param name="memberName"></param>
        /// <returns></returns>
        public static object InvokeValue(this object t, string memberName)
        {
            return t.GetType()
                .InvokeMember(memberName, //反射的属性
                                          //搜索的方式
                    BindingFlags.NonPublic | BindingFlags.Public
                                           | BindingFlags.GetField | BindingFlags.GetProperty
                                           | BindingFlags.Instance,
                    null,
                    //目标
                    t,
                    //参数
                    null);
        }

        /// <summary>
        /// 设置成员
        /// </summary>
        /// <param name="t"></param>
        /// <param name="memberName"></param>
        /// <param name="args"></param>
        public static void InvokeValue(this object t, string memberName, params object[] args)
        {
            t.GetType()
                .InvokeMember(memberName, //反射的属性
                                          //搜索的方式
                    BindingFlags.NonPublic | BindingFlags.Public
                    | BindingFlags.SetField | BindingFlags.SetProperty
                    | BindingFlags.Instance,
                    null,
                    //目标
                    t,
                    //参数
                    args);
        }

        /// <summary>
        /// 委托函数
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        /// <param name="methodName"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static T InvokeMethod<T>(this object t, string methodName, params object[] args)
        {
            return (T)t.GetType()
                .InvokeMember(methodName, //反射的属性
                                          //搜索的方式
                    BindingFlags.NonPublic | BindingFlags.Public
                    | BindingFlags.InvokeMethod
                    | BindingFlags.Instance,
                    null,
                    //目标
                    t,
                    //参数
                    args);
        }

        /// <summary>
        /// 委托函数
        /// </summary>
        /// <param name="t"></param>
        /// <param name="methodName"></param>
        /// <param name="args"></param>
        public static void InvokeMethod(this object t, string methodName, params object[] args)
        {
            t.GetType()
                .InvokeMember(methodName, //反射的属性
                                          //搜索的方式
                    BindingFlags.NonPublic | BindingFlags.Public
                    | BindingFlags.InvokeMethod
                    | BindingFlags.Instance,
                    null,
                    //目标
                    t,
                    //参数
                    args);
        }

        /// <summary>
        /// 浅拷贝
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        public static T Copy<T>(this T t) where T : class, new() => t.Copy(new T());

        /// <summary>
        /// 浅拷贝
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        /// <param name="cpyT"></param>
        public static T Copy<T>(this T t, T cpyT) where T : class
        {
            var typ = typeof(T);

            foreach (var info in typ.GetProperties(BindingFlags.Public | BindingFlags.NonPublic
                                                                       | BindingFlags.Instance))
            {
                cpyT.InvokeValue(info.Name, t.InvokeValue(info.Name));
            }

            foreach (var info in typ.GetFields(BindingFlags.Public | BindingFlags.NonPublic
                                                                   | BindingFlags.Instance))
            {
                cpyT.InvokeValue(info.Name, t.InvokeValue(info.Name));
            }

            return cpyT;
        }

        /// <summary>
        /// 文本填充对象容器  不知道数据类型的
        /// </summary>
        /// <param name="o"></param>
        /// <param name="val"></param>
        /// <param name="sep"></param>
        public static void FillCollectionStr(this ICollection o, string val, string sep)
        { 
            o.InvokeMethod("Clear");

            if (string.IsNullOrEmpty(val)) return;

            var type = o.GetType().GetGenericType();

            foreach (var str in val.Split(new[] { sep }, StringSplitOptions.None))
            {
                o.InvokeMethod("Add", type.FromSimpleString(str, sep, null));
            }

        }


        /// <summary>
        /// 文本填充对象容器
        /// </summary>
        /// <param name="o"></param>
        /// <param name="val"></param>
        /// <param name="sep"></param>
        public static void FillCollectionStr<T>(this List<T> o, string val, string sep)
        {
            o.Clear();

            if (string.IsNullOrEmpty(val)) return;

            var type = typeof(T);

            foreach (var str in val.Split(new[] { sep }, StringSplitOptions.None))
            {
                o.Add((T)type.FromSimpleString(str, sep, null)); 
            }

        }

        /// <summary>
        /// 文本填充对象容器
        /// </summary>
        /// <param name="o"></param>
        /// <param name="val"></param>
        /// <param name="sep"></param>
        public static void FillCollectionStr<T>(this HashSet<T> o, string val, string sep)
        {
            o.Clear();

            if (string.IsNullOrEmpty(val)) return;

            var type = typeof(T);

            foreach (var str in val.Split(new[] { sep }, StringSplitOptions.None))
            {
                o.Add((T)type.FromSimpleString(str, sep, null));
            }

        }

        /// <summary>
        /// 文本填充字典 不知道数据类型的
        /// </summary>
        /// <param name="o"></param>
        /// <param name="val"></param>
        /// <param name="sep"></param>
        public static void FillDictionaryStr(this IDictionary o, string val, string sep)
        { 
            o.InvokeMethod("Clear");

            if (string.IsNullOrEmpty(val)) return; 

            var ls = val.Split(new[] { sep }, StringSplitOptions.None);

            var keyType = o.GetType().GetGenericType();
            var valType = o.GetType().GetGenericType(1);

            for (var i = 0; i < ls.Length; i += 2)
            {
                var key = ls[i];
                var valStr = ls[i + 1];

                var keyVal = keyType.FromSimpleString(key, sep, null);
                var valVal = valType.FromSimpleString(valStr, sep, null);
                o.InvokeMethod("Insert", keyVal, valVal, false);
            }
        }

        /// <summary>
        /// 文本填充字典
        /// </summary>
        /// <param name="o"></param>
        /// <param name="val"></param>
        /// <param name="sep"></param>
        public static void FillDictionaryStr<TKey,TVal>(this Dictionary<TKey, TVal> o, string val, string sep)
        {
            o.Clear();

            if (string.IsNullOrEmpty(val)) return;
             
            var ls = val.Split(new[] { sep }, StringSplitOptions.None);
            var keyType = typeof(TKey);
            var valType = typeof(TVal);
            for (var i = 0; i < ls.Length; i += 2)
            {
                var key = ls[i];
                var valStr = ls[i + 1];

                var keyVal = (TKey)keyType.FromSimpleString(key, sep, null);
                var valVal = (TVal)valType.FromSimpleString(valStr, sep, null);

                o.Add(keyVal,valVal);
            }
        }

        /// <summary>
        /// 对象容器拼接文本  
        /// </summary>
        /// <param name="src"></param>
        /// <param name="sep"></param>
        /// <returns></returns>
        public static string JoinCollectionStr(this ICollection src, string sep)
        {
            var sb = new StringBuilder();
            foreach (var str in src)
            {
                sb.Append($"{str.ToSimpleString(sep)}{sep}");
            }
            if (sb.Length > 0)
            {
                sb.Remove(sb.Length - sep.Length, sep.Length);
            }
            return sb.ToString();
        }


        /// <summary>
        /// 字典拼接文本
        /// </summary>
        /// <param name="src"></param>
        /// <param name="sep"></param>
        /// <returns></returns>
        public static string JoinDictionaryStr(this IDictionary src, string sep)
        {
            var sb = new StringBuilder();
            foreach (DictionaryEntry pair in src)
            {
                sb.Append($"{(pair.Key).ToSimpleString(sep)}{sep}{(pair.Value).ToSimpleString(sep)}{sep}");
            }
            if (sb.Length > 0)
            {
                sb.Remove(sb.Length - sep.Length, sep.Length);
            }

            return sb.ToString();
        } 


        public static T FromSimpleString<T>(this T o, string val, string sep)
        {
            return (T)o.GetType().FromSimpleString(val, sep, o);
        } 

        /// <summary>
        /// 各种类型 返回简单字符串表示
        /// </summary>
        /// <param name="src"></param>
        /// <param name="sep"></param>
        /// <returns></returns>
        public static string ToSimpleString<T>(this T src, string sep)
        {
            switch (src)
            {
                case decimal _:
                case double _:
                case float _:
                    return $"{src:F}"; //0.00
                case DateTime dt:
                    return $"{(dt.ToUniversalTime() - DateTime.Parse("1970-01-01")).TotalSeconds}"; //秒
                case TimeSpan ts:
                    return $"{ts.TotalSeconds}"; //秒
                case Enum _:
                    return $"{Convert.ToInt32(src)}"; //数字
                case IDictionary dicSrc:
                    return dicSrc.JoinDictionaryStr(sep);
                default:
                    return src is ICollection colSrc ? colSrc.JoinCollectionStr(sep) : src.ToString();
            }
        } 
    }
}