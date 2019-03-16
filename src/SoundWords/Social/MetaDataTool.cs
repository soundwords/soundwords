using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;

namespace SoundWords.Social
{
    public class MetaDataTool : IMetaDataTool
    {
        public Dictionary<string, string> GetMetaData(object metaData)
        {
            Type type = metaData.GetType();

            var metaDataDictionary = new Dictionary<string, string>();
            foreach (PropertyInfo property in type.GetProperties())
            {
                string propertyName = property.GetCustomAttribute<DataMemberAttribute>().Name;
                var content = (string) property.GetValue(metaData);

                if (content == null)
                {
                    continue;
                }

                metaDataDictionary.Add(propertyName, content);
            }

            return metaDataDictionary;
        }
    }

    public interface IMetaDataTool
    {
        Dictionary<string, string> GetMetaData(object metaData);
    }
}