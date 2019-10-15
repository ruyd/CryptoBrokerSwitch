using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml.Serialization;

namespace Piggy 
{
    public static partial class TypeEx
    {

        public static decimal? ToNearestHalfDecimal(this decimal? d)
        {
            return Convert.ToDecimal(Math.Round((double)(d * 2)) / 2);
        }

        public static decimal? ToHalf(this decimal? d, int? decimals = null)
        {
            return decimals.HasValue ? Math.Round((decimal)d / 2, decimals.Value) : Math.Round((decimal)d / 2);
        }

        public static decimal? ToSatoshi8(this decimal? d)
        {
            return d * 0.00000001m;
        }

        public static T Deserialize<T>(this string jsonData) where T : class
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(jsonData);
        }

        public static string Serialize<T>(this T obj) where T : class
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(obj);
        }

        public static string SerializeXML<T>(this T source)
        {
            var x = new XmlSerializer(source.GetType());
            string data = string.Empty;
            using (StringWriter writer = new StringWriter())
            {
                x.Serialize(writer, source);
                data = writer.ToString();
            }
            return data;
        }

        public static T DeserializeXML<T>(this string xmlContent)
        {
            var serializer = new System.Xml.Serialization.XmlSerializer(typeof(T));
            object obj;
            using (TextReader reader = new StringReader(xmlContent))
            {
                obj = (T)serializer.Deserialize(reader);
            }
            return (T)obj;
        }
        public static string Clean(this string s, int? max = null)
        {
            if (s == null)
                return null;
            s = s.Trim();
            if (max.HasValue && s.Length > max)
                s = s.Substring(0, max.Value);
            return s.Trim();
        }

        public static string ToInitials(this string str)
        {
            return Regex.Replace(str, @"^(?'b'\w)\w*,\s*(?'a'\w)\w*$|^(?'a'\w)\w*\s*(?'b'\w)\w*$", "${a}${b}", RegexOptions.Singleline);
        }

        public static string ToCamelCase(this string s)
        {
            if (s == null)
                return null;
            s = s.Trim().ToLower();
            s = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(s);
            return s;
        }

        public static string StripNonAlpha(this string str, char? exclude = null)
        {
            if (String.IsNullOrWhiteSpace(str))
                return str;
            else
            {
                var s = str.Trim();
                char[] arr = s.Where(c => (char.IsLetterOrDigit(c) || char.IsWhiteSpace(c) || exclude == c)).ToArray();
                s = new string(arr);
                return s;
            }
        }
        public static string ToPhoneFormat(this string str, char? exclude = null)
        {
            if (String.IsNullOrWhiteSpace(str))
                return str;
            else
            {
                var clean = str.StripNonNumeric();

                return string.Format("({0}) {1}-{2}",
                    clean.Substring(0, 3),
                    clean.Substring(3, 3),
                    clean.Substring(6));
            }
        }
        public static string StripNonNumeric(this string str, char? exclude = null)
        {
            if (String.IsNullOrWhiteSpace(str))
                return str;
            else
            {
                var s = str.Trim();
                char[] arr = s.Where(c => (char.IsDigit(c) || exclude == c)).ToArray();
                s = new string(arr);
                return s;
            }
        }
     
        public static string toString(this DateTime? date, string format = "d")
        {
            if (!date.HasValue)
                return null;

            return date.Value.ToString(format);
        }
        public static string toString(this decimal? num, string format = "n1")
        {
            if (!num.HasValue)
                return null;

            return num.Value.ToString(format);
        }
        public static string toString(this int? num, string format = "n1")
        {
            if (!num.HasValue)
                return null;

            return num.Value.ToString(format);
        }
        public static string toMonthName(this DateTime? date)
        {
            if (!date.HasValue)
                return null;
            //CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(8)
            return date.Value.ToString("MMM");
        }

        public static string AddWhere(this string clause, string append)
        {
            if (clause != null && clause.Contains("WHERE"))
                clause += "AND ";
            else
                clause = "WHERE ";

            clause += append;

            return clause;
        }

        public static DateTime UnixTimeToDateTime(this long UnixTime)
        {
            DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0);

            return epoch.AddSeconds(UnixTime).ToLocalTime();
        }
        public static DateTime JsonTimeToDateTime(this string JsonTime)
        {
            DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            var unixTime = Convert.ToInt64(JsonTime);
            return epoch.AddSeconds(unixTime).ToLocalTime();
        }

        public static DateTime? ToDateTimeN(this string textTime)
        {
            DateTime result;
            if (DateTime.TryParse(textTime, out result))
                return result;
            return null;
        }
        public static DateTimeOffset? Tz(this DateTime? date)
        {
            if (!date.HasValue) return null;
            return new DateTimeOffset(date.Value);
        }

        public static Int32? ToIntN(this string s)
        {
            Int32 result;
            if (Int32.TryParse(s, out result))
                return result;
            return null;
        }

        public static int ToInt(this decimal i)
        {
            return Convert.ToInt32(i);
        }

    }
}

