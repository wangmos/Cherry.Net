using System;
using System.Collections;
using System.ComponentModel;
using System.Reflection;

namespace Cherry.Net.Extensions
{
    public static class TypeEx
    {
        /// <summary>
        /// 委托静态成员
        /// </summary> 
        /// <param name="t"></param>
        /// <param name="memberName"></param>
        /// <returns></returns>
        public static object InvokeValue(this Type t, string memberName)
        {
            return t.InvokeMember(memberName, //反射的属性
                                              //搜索的方式
                BindingFlags.NonPublic | BindingFlags.Public
                                       | BindingFlags.GetField | BindingFlags.GetProperty
                                       | BindingFlags.Static,
                null,
                //目标
                null,
                //参数
                null);
        }

        /// <summary>
        /// 委托静态成员
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        /// <param name="memberName"></param>
        /// <returns></returns>
        public static T InvokeValue<T>(this Type t, string memberName)
        {
            return (T)t.InvokeValue(memberName);
        }
        /// <summary>
        /// 设置静态成员
        /// </summary>
        /// <param name="t"></param>
        /// <param name="memberName"></param>
        /// <param name="args"></param>
        public static void InvokeValue(this Type t, string memberName, params object[] args)
        {
            t.InvokeMember(memberName, //反射的属性
                                       //搜索的方式
                    BindingFlags.NonPublic | BindingFlags.Public
                    | BindingFlags.SetField | BindingFlags.SetProperty
                    | BindingFlags.Static,
                    null,
                    //目标
                    null,
                    //参数
                    args);
        }
        /// <summary>
        /// 委托静态函数
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        /// <param name="methodName"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static T InvokeMethod<T>(this Type t, string methodName, params object[] args)
        {
            return (T)t.InvokeMember(methodName, //反射的属性
                                                 //搜索的方式
                    BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.InvokeMethod | BindingFlags.Static,
                    null,
                    //目标
                    null,
                    //参数
                    args);
        }
        /// <summary>
        /// 委托静态函数
        /// </summary>
        /// <param name="t"></param>
        /// <param name="methodName"></param>
        /// <param name="args"></param>
        public static void InvokeMethod(this Type t, string methodName, params object[] args)
        {
            t.InvokeMember(methodName, //反射的属性
                                       //搜索的方式
                    BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.InvokeMethod | BindingFlags.Static,
                    null,
                    //目标
                    null,
                    //参数
                    args);
        }

        /// <summary>
        /// 取得成员值
        /// </summary>
        /// <param name="src"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public static object GetValueEx(this MemberInfo src, object t)
        {
            return (src as FieldInfo)?.GetValue(t) ?? (src as PropertyInfo)?.GetValue(t, null);
        }
        /// <summary>
        /// 设置成员值
        /// </summary>
        /// <param name="src"></param>
        /// <param name="t"></param>
        /// <param name="val"></param>
        public static void SetValueEx(this MemberInfo src, object t, object val)
        {
            var info = src as FieldInfo;
            if (info != null) info.SetValue(t, val);
            else (src as PropertyInfo)?.SetValue(t, val, null);
        }


        /// <summary>
        /// 取得成员类型
        /// </summary>
        /// <param name="src"></param>
        /// <returns></returns>
        public static Type GetValueType(this MemberInfo src)
            => (src as FieldInfo)?.FieldType ?? (src as PropertyInfo)?.PropertyType;


        /// <summary>
        /// 取得自定义属性
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="src"></param> 
        /// <returns></returns>
        public static T GetAttr<T>(this MemberInfo src) where T : Attribute
        {
            var ls = src.GetCustomAttributes(typeof(T), true);
            return ls.Length > 0 ? ls[0] as T : null;
        }

        /// <summary>
        /// 取得描述属性
        /// </summary>
        /// <param name="eval"></param>
        /// <returns></returns>
        public static string GetDescription(this Enum eval)
        {
            var info = eval.GetType().GetField(eval.ToString());
            var att = info.GetAttr<DescriptionAttribute>();
            return att?.Description;
        }

        /// <summary>
        /// 取得描述属性
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string GetDescription(this MemberInfo type)
        {
            return type.GetAttr<DescriptionAttribute>()?.Description ?? "";
        }

        /// <summary>
        /// 是否List集合类型
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsList(this Type type)
            => (type.GetInterface("IList") ?? type.GetInterface("IList`1")) != null;

        /// <summary>
        /// 是否集合类型
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsCollection(this Type type)
            => (type.GetInterface("ICollection") ?? type.GetInterface("ICollection`1")) != null;

        /// <summary>
        /// 是否字典
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsDictionary(this Type type)
            => (type.GetInterface("IDictionary") ?? type.GetInterface("IDictionary`2")) != null;


        /// <summary>
        /// 取得泛型成员类型
        /// </summary>
        /// <param name="type"></param>
        /// <param name="index"></param>
        /// <returns></returns> 
        public static Type GetGenericType(this Type type, int index = 0)
        {
            var ls = type.GetGenericArguments();
            return ls.Length > index ? ls[index] : null;
        }

        /// <summary>
        /// Activator.CreateInstance(type,obj...) 就是对他的封装  默认参数不能省略哦 一定要填
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="typ"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static T CreateInstance<T>(this Type typ, params object[] args) where T : class
        {
            return typ.GetConstructor(Array.ConvertAll(args, o => o.GetType()))?.Invoke(args) as T;
        }

        #region FromSimpleString

        /// <summary>
        ///  简单字符串到各种内建类型
        /// </summary>
        /// <param name="type"></param>
        /// <param name="val"></param>
        /// <param name="sep"></param>
        /// <param name="o"></param>
        /// <returns></returns>
        public static object FromSimpleString(this Type type, string val, string sep, object o)
        { 
            if (type.IsEnum)
            {
                return Enum.Parse(type, val);//数字到枚举 
            }
            if (type == typeof(DateTime))
            {
                return (double.TryParse(val, out var i)
                    ? DateTime.Parse("1970-01-01").AddSeconds(i).ToLocalTime() : DateTime.MinValue);
            }
            if (type == typeof(TimeSpan))
            {
                return (int.TryParse(val, out var i)
                    ? TimeSpan.FromSeconds(i) : TimeSpan.Zero);
            }
            if (type.IsDictionary())
            { 
                ((IDictionary)o).FillDictionaryStr(val, sep);
                return o;
            }

            if (type.IsCollection())
            { 
                ((ICollection)o).FillCollectionStr(val, sep);
                return o;
            }

            return Convert.ChangeType(val, type);

        }
         
        #endregion
    }
}