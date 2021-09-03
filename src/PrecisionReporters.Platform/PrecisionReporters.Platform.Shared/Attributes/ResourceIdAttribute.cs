using PrecisionReporters.Platform.Shared.Enums;
using System;

namespace PrecisionReporters.Platform.Shared.Attributes
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
