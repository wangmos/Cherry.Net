using System;
using System.Diagnostics;
using Newtonsoft.Json;

namespace Cherry.Net.Utils
{
    public static class Json
    {
        public static string Encode(object obj)
        {
            return JsonConvert.SerializeObject(obj);
        }

        /// <summary>
        ///     访问属性或字段时 最好指定类型
        /// </summary>
        /// <param name="jsonStr"></param>
        /// <param name="jsonObj"></param>
        /// <returns></returns>
        public static bool Decode(string jsonStr, out dynamic jsonObj)
        {
            jsonObj = Decode(jsonStr); 
            return jsonObj != null;
        }

        public static dynamic Decode(string jsonStr)
        {
            try
            {
                return JsonConvert.DeserializeObject(jsonStr);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }

            return null;
        }
    }
}