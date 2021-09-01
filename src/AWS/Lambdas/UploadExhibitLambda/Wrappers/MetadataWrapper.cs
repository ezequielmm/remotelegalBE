using Amazon.S3.Model;
using UploadExhibitLambda.Wrappers.Interface;

namespace UploadExhibitLambda.Wrappers
{
    public class MetadataWrapper : IMetadataWrapper
    {
        public string GetMetadataByKey(GetObjectMetadataResponse objectMetadata, string metadataKey)
        {
            return objectMetadata.Metadata[metadataKey] ?? "";
        }
    }
}