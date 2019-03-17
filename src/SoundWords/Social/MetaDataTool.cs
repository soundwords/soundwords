using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;

namespace SoundWords.Social
{
    public class MetaDataTool : IMetaDataTool
    {
        public Metadata GetMetaData(object metaData)
        {
            Type type = metaData.GetType();

            var metadataDictionary = new Dictionary<string, string>();
            foreach (PropertyInfo property in type.GetProperties())
            {
                string propertyName = property.GetCustomAttribute<DataMemberAttribute>().Name;
                var content = (string) property.GetValue(metaData);

                if (content == null)
                {
                    continue;
                }

                metadataDictionary.Add(propertyName, content);
            }

            return new Metadata {Data = metadataDictionary};
        }
    }
}