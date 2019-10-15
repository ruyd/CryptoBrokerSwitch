﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CryptoExchange.Net.Converters
{
    public class ArrayConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return true;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var result = Activator.CreateInstance(objectType);
            var arr = JArray.Load(reader);
            foreach (var property in objectType.GetProperties())
            {
                var attribute =
                    (ArrayPropertyAttribute)property.GetCustomAttribute(typeof(ArrayPropertyAttribute));
                if (attribute == null)
                    continue;

                if (attribute.Index >= arr.Count)
                    continue;

                object value;
                var converterAttribute = (JsonConverterAttribute)property.GetCustomAttribute(typeof(JsonConverterAttribute));
                if (converterAttribute != null)
                    value = arr[attribute.Index].ToObject(property.PropertyType, new JsonSerializer() { Converters = { (JsonConverter)Activator.CreateInstance(converterAttribute.ConverterType) } });
                else
                    value = arr[attribute.Index];

                if (value != null && property.PropertyType.IsInstanceOfType(value))
                    property.SetValue(result, value);
                else
                {
                    if (value is JToken)
                        if (((JToken)value).Type == JTokenType.Null)
                            value = null;

                    property.SetValue(result, value == null ? null : Convert.ChangeType(value, property.PropertyType));
                }
            }
            return result;
        }


        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            JObject jo = new JObject();
            Type type = value.GetType();
            jo.Add("type", type.Name);

            foreach (PropertyInfo prop in type.GetProperties())
            {
                if (prop.CanRead)
                {
                    object propVal = prop.GetValue(value, null);
                    if (propVal != null)
                    {
                        jo.Add(prop.Name, JToken.FromObject(propVal, serializer));
                    }
                }
            }
            jo.WriteTo(writer);
        }

    }

    public class ArrayPropertyAttribute: Attribute
    {
        public int Index { get; }

        public ArrayPropertyAttribute(int index)
        {
            Index = index;
        }
    }
}
