// -----------------------------------------------------------------------
// <copyright file="SdkVersionUtils.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation. 
// All rights reserved.  2013
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.ApplicationInsights.Implementation
{
    using System;
    using System.Linq;
    using System.Reflection;

    internal class SdkVersionUtils
    {
        internal static string GetSdkVersion(string versionPrefix)
        {
            string versionStr = typeof(SdkVersionUtils).GetTypeInfo().Assembly.GetCustomAttributes()
                    .OfType<AssemblyFileVersionAttribute>()
                    .First()
                    .Version;

            Version version = new Version(versionStr);
            return (versionPrefix ?? string.Empty) + version.ToString(3) + "-" + version.Revision;
        }
    }
}
