using Amazon.S3.Model;

namespace UploadExhibitLambda.Wrappers.Interface
{
    public interface IMetadataWrapper
    {
        string GetMetadataByKey(GetObjectMetadataResponse objectMetadata, string metadataKey);
    }
}