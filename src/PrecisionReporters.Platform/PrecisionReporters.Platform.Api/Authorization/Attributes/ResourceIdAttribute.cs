using PrecisionReporters.Platform.Data.Enums;
using System;

namespace PrecisionReporters.Platform.Api.Authorization.Attributes
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public class ResourceIdAttribute : Attribute
    {
        public ResourceIdAttribute(ResourceType resourceType)
        {
            ResourceType = resourceType;
        }

        public ResourceType ResourceType { get; }
    }
}
