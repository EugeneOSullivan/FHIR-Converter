// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Fhir.TemplateManagement.Configurations
{
    public class TemplateHostingConfiguration
    {
        public static readonly string TemplateHosting = "TemplateHosting";

        /// <summary>
        /// Legacy Azure Storage configuration (deprecated, use CloudStorageConfiguration instead)
        /// </summary>
        public StorageAccountConfiguration StorageAccountConfiguration { get; set; }

        /// <summary>
        /// Cloud-agnostic storage configuration supporting multiple providers
        /// </summary>
        public CloudStorageConfiguration CloudStorageConfiguration { get; set; }
    }
}
