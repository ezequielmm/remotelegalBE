using PrecisionReporters.Platform.Data.Enums;
using System;

namespace PrecisionReporters.Platform.Domain.Attributes
{
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
    public class ResourceIdAttribute : Attribute
    {
        public ResourceIdAttribute(ResourceType resourceType)
        {
            ResourceType = resourceType;
        }

        public ResourceType ResourceType { get; }
    }
}
