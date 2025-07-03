// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Fhir.TemplateManagement.Configurations
{
    /// <summary>
    /// Base interface for storage configurations
    /// </summary>
    public interface IStorageConfiguration
    {
        string ContainerName { get; set; }
        string GetContainerUrl();
    }
} 