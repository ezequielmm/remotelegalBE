﻿namespace PrecisionReporters.Platform.Domain.AppConfigurations.Sections
{
    public class CorsConfiguration
    {
        public string Origins { get; set; }
        public string[] Methods { get; set; }

        public string[] GetOrigingsAsArray()
        {
            return Origins.Split(",");
        }
    }
}
