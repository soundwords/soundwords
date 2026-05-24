using System.Reflection;
using System.Runtime.Serialization;

namespace SoundWords.Social;

public class MetaDataTool : IMetaDataTool
{
    public Metadata GetMetaData(object metaData)
    {
        Type type = metaData.GetType();

        Dictionary<string, string> metadataDictionary = new();
        foreach (PropertyInfo property in type.GetProperties())
        {
            DataMemberAttribute? dataMember = property.GetCustomAttribute<DataMemberAttribute>();
            if (dataMember?.Name == null)
            {
                continue;
            }

            string? content = (string?)property.GetValue(metaData);
            if (content == null)
            {
                continue;
            }

            metadataDictionary[dataMember.Name] = content;
        }

        return new Metadata { Data = metadataDictionary };
    }
}
