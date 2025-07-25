﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DotLiquid;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.Health.Fhir.Liquid.Converter;
using Microsoft.Health.Fhir.Liquid.Converter.Exceptions;
using Microsoft.Health.Fhir.Liquid.Converter.Models;
using Microsoft.Health.Fhir.Liquid.Converter.Processors;
using Microsoft.Health.Fhir.TemplateManagement.ArtifactProviders;
using Microsoft.Health.Fhir.TemplateManagement.Client;
using Microsoft.Health.Fhir.TemplateManagement.Configurations;
using Microsoft.Health.Fhir.TemplateManagement.Exceptions;
using Microsoft.Health.Fhir.TemplateManagement.Factory;
using Microsoft.Health.Fhir.TemplateManagement.Models;
using Microsoft.Health.Fhir.TemplateManagement.Utilities;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.Health.Fhir.TemplateManagement.FunctionalTests
{
    public class TemplateCollectionFunctionalTests : IAsyncLifetime
    {
        private readonly string token;
        private readonly TemplateCollectionConfiguration _config = new TemplateCollectionConfiguration();
        private readonly IMemoryCache cache = new MemoryCache(new MemoryCacheOptions());
        private readonly string baseLayerTemplatePath = "TestData/TarGzFiles/baseLayer.tar.gz";
        private readonly string userLayerTemplatePath = "TestData/TarGzFiles/userV2.tar.gz";
        private readonly string invalidTarGzPath = "TestData/TarGzFiles/invalid1.tar.gz";
        private readonly string invalidTemplatePath = "TestData/TarGzFiles/invalidTemplates.tar.gz";
        private readonly string _defaultHl7v2TemplateImageReference = "microsofthealth/hl7v2templates:default";
        private readonly string _defaultCcdaTemplateImageReference = "microsofthealth/ccdatemplates:default";
        private readonly string _defaultJsonTemplateImageReference = "microsofthealth/jsontemplates:default";
        private readonly string _defaultStu3ToR4TemplateImageReference = "microsofthealth/stu3tor4templates:default";
        private readonly string testOneLayerImageReference;
        private readonly string testMultiLayerImageReference;
        private readonly string testOneLayerOCIImageReference;
        private readonly string testMultiLayerOCIImageReference;
        private readonly string testInvalidImageReference;
        private readonly string testInvalidTemplateImageReference;
        private string testOneLayerImageDigest;
        private string testMultiLayerImageDigest;
        private readonly ContainerRegistry _containerRegistry = new ContainerRegistry();
        private readonly ContainerRegistryInfo _containerRegistryInfo;
        private static readonly string _templateDirectory = Path.Join("..", "..", "data", "Templates");
        private static readonly string _sampleDataDirectory = Path.Join("..", "..", "data", "SampleData");
        private static readonly string _testTarGzPath = Path.Join("TestData", "TarGzFiles");
        private readonly string _baseLayerTemplatePath = Path.Join(_testTarGzPath, "layerbase.tar.gz");
        private readonly string _userLayerTemplatePath = Path.Join(_testTarGzPath, "layer2.tar.gz");
        private static readonly ProcessorSettings _processorSettings = new ProcessorSettings();
        private bool _isOrasValid = true;
        private readonly string _orasErrorMessage = "Oras tool invalid.";
        private const string _orasCacheEnvironmentVariableName = "ORAS_CACHE";
        private const string _defaultOrasCacheEnvironmentVariable = ".oras/cache";

        public TemplateCollectionFunctionalTests()
        {
            _containerRegistryInfo = _containerRegistry.GetTestContainerRegistryInfo();
            if (_containerRegistryInfo == null)
            {
                return;
            }

            testOneLayerImageReference = _containerRegistryInfo.ContainerRegistryServer + "/templatetest:onelayer";
            testMultiLayerImageReference = _containerRegistryInfo.ContainerRegistryServer + "/templatetest:multilayers";
            testInvalidImageReference = _containerRegistryInfo.ContainerRegistryServer + "/templatetest:invalidlayers";
            testInvalidTemplateImageReference = _containerRegistryInfo.ContainerRegistryServer + "/templatetest:invalidtemplateslayers";
            testOneLayerOCIImageReference = _containerRegistryInfo.ContainerRegistryServer + "/templatetest:ocionelayer";
            testMultiLayerOCIImageReference = _containerRegistryInfo.ContainerRegistryServer + "/templatetest:ocimultilayer";
            token = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_containerRegistryInfo.ContainerRegistryUsername}:{_containerRegistryInfo.ContainerRegistryPassword}"));

            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(_orasCacheEnvironmentVariableName)))
            {
                Environment.SetEnvironmentVariable(_orasCacheEnvironmentVariableName, _defaultOrasCacheEnvironmentVariable);
            }
        }

        public async Task InitializeAsync()
        {
            if (_containerRegistryInfo == null)
            {
                return;
            }

            await InitOneLayerImageAsync();
            await InitMultiLayerImageAsync();
            await InitInvalidTarGzImageAsync();
            await InitInvalidTemplateImageAsync();

            await OrasLogin();
            await PushOneLayerOCIImageAsync();
            await PushMultiLayersOCIImageAsync();
        }

        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }

        public static IEnumerable<object[]> GetValidImageInfoWithTag()
        {
            yield return new object[] { new List<int> { 838 }, "templatetest", "onelayer" };
            yield return new object[] { new List<int> { 767, 838 }, "templatetest", "multilayers" };
        }

        public static IEnumerable<object[]> GetValidOCIImageInfoWithTag()
        {
            yield return new object[] { new List<int> { 838 }, "templatetest", "ocionelayer" };
            yield return new object[] { new List<int> { 834, 838 }, "templatetest", "ocimultilayer" };
        }

        public static IEnumerable<object[]> GetHl7v2DataAndEntryTemplate()
        {
            var data = new List<object[]>
            {
                new object[] { @"ADT01-23.hl7", @"ADT_A01" },
                new object[] { @"IZ_1_1.1_Admin_Child_Max_Message.hl7", @"VXU_V04" },
                new object[] { @"LAB-ORU-1.hl7", @"ORU_R01" },
                new object[] { @"MDHHS-OML-O21-1.hl7", @"OML_O21" },
            };
            return data.Select(item => new object[]
            {
                Path.Join(_sampleDataDirectory, "Hl7v2", Convert.ToString(item[0])),
                Convert.ToString(item[1]),
            });
        }

        public static IEnumerable<object[]> GetHl7v2DataAndTemplateSources()
        {
            var data = new List<object[]>
            {
                new object[] { @"ADT01-23.hl7", @"ADT_A01" },
                new object[] { @"IZ_1_1.1_Admin_Child_Max_Message.hl7", @"VXU_V04" },
                new object[] { @"LAB-ORU-1.hl7", @"ORU_R01" },
                new object[] { @"MDHHS-OML-O21-1.hl7", @"OML_O21" },
            };
            return data.Select(item => new object[]
            {
                Path.Join(_sampleDataDirectory, "Hl7v2", Convert.ToString(item[0])),
                Path.Join(_templateDirectory, "Hl7v2"),
                Convert.ToString(item[1]),
            });
        }

        public static IEnumerable<object[]> GetCcdaDataAndTemplateSources()
        {
            var data = new List<object[]>
            {
                new object[] { @"170.314B2_Amb_CCD.ccda", @"CCD" },
                new object[] { @"C-CDA_R2-1_CCD.xml.ccda", @"CCD" },
                new object[] { @"CCD.ccda", @"CCD" },
                new object[] { @"CCD-Parent-Document-Replace-C-CDAR2.1.ccda", @"CCD" },
            };
            return data.Select(item => new object[]
            {
                Path.Join(_sampleDataDirectory, "Ccda", Convert.ToString(item[0])),
                Path.Join(_templateDirectory, "Ccda"),
                Convert.ToString(item[1]),
            });
        }

        public static IEnumerable<object[]> GetJsonDataAndTemplateSources()
        {
            var data = new List<object[]>
            {
                new object[] { @"ExamplePatient.json", @"ExamplePatient" },
                new object[] { @"Stu3ChargeItem.json", @"Stu3ChargeItem" },
            };
            return data.Select(item => new object[]
            {
                Path.Join(_sampleDataDirectory, "Json", Convert.ToString(item[0])),
                Path.Join(_templateDirectory, "Json"),
                Convert.ToString(item[1]),
            });
        }

        public static IEnumerable<object[]> GetFhirStu3DataAndTemplateSources()
        {
            var data = new List<string>
            {
                @"CapabilityStatement",
                @"CodeSystem",
                @"Observation",
                @"OperationDefinition",
                @"OperationOutcome",
                @"Parameters",
                @"Patient",
                @"StructureDefinition",
                @"ValueSet",
            };
            return data.Select(item => new[]
            {
                Path.Join(_sampleDataDirectory, "Stu3", $"{item}.json"),
                Path.Join(_templateDirectory, "Stu3ToR4"),
                item,
            });
        }

        public static IEnumerable<object[]> GetNotExistImageInfo()
        {
            yield return new object[] { "templatetest", "notexist" };
            yield return new object[] { "notexist", "multilayers" };
        }

        public static IEnumerable<object[]> GetInvalidImageReference()
        {
            yield return new object[] { "testacr.azurecr.io@v1" };
            yield return new object[] { "testacr.azurecr.io:templateset:v1" };
            yield return new object[] { "testacr.azurecr.io_v1" };
            yield return new object[] { "testacr.azurecr.io:v1" };
            yield return new object[] { "testacr.azurecr.io/" };
            yield return new object[] { "testacr.azurecr.io/name:" };
            yield return new object[] { "testacr.azurecr.io/name@" };
        }

        public static IEnumerable<object[]> GetDefaultTemplatesInfo()
        {
            yield return new object[] { "microsofthealth/fhirconverter:default", "Hl7v2" };
            yield return new object[] { "microsofthealth/hl7v2templates:default", "Hl7v2" };
            yield return new object[] { "microsofthealth/ccdatemplates:default", "Ccda" };
            yield return new object[] { "microsofthealth/jsontemplates:default", "Json" };
            yield return new object[] { "microsofthealth/stu3tor4templates:default", "Stu3ToR4" };
        }

        [Fact]
        public async Task GiveImageReference_WhenGetTemplateCollection_IfImageTooLarge_ExceptionWillBeThrownAsync()
        {
            if (_containerRegistryInfo == null)
            {
                return;
            }

            var config = new TemplateCollectionConfiguration() { TemplateCollectionSizeLimitMegabytes = 0 };
            string imageReference = testOneLayerImageReference;
            TemplateCollectionProviderFactory factory = new TemplateCollectionProviderFactory(cache, Options.Create(config));
            var templateCollectionProvider = factory.CreateTemplateCollectionProvider(imageReference, token);
            await Assert.ThrowsAsync<ImageTooLargeException>(async () => await templateCollectionProvider.GetTemplateCollectionAsync());
        }

        [Fact]
        public async Task GiveImageReference_WhenGetTemplateCollection_IfTemplateParsedFailed_ExceptionWillBeThrownAsync()
        {
            if (_containerRegistryInfo == null)
            {
                return;
            }

            string imageReference = testInvalidTemplateImageReference;
            TemplateCollectionProviderFactory factory = new TemplateCollectionProviderFactory(cache, Options.Create(_config));
            var templateCollectionProvider = factory.CreateTemplateCollectionProvider(imageReference, token);
            await Assert.ThrowsAsync<TemplateParseException>(async () => await templateCollectionProvider.GetTemplateCollectionAsync());
        }

        [Fact]
        public async Task GiveImageReference_WhenGetTemplateCollection_IfImageDecompressedFailed_ExceptionWillBeThrownAsync()
        {
            if (_containerRegistryInfo == null)
            {
                return;
            }

            string imageReference = testInvalidImageReference;
            TemplateCollectionProviderFactory factory = new TemplateCollectionProviderFactory(cache, Options.Create(_config));
            var templateCollectionProvider = factory.CreateTemplateCollectionProvider(imageReference, token);
            await Assert.ThrowsAsync<ArtifactArchiveException>(async () => await templateCollectionProvider.GetTemplateCollectionAsync());
        }

        [Theory]
        [MemberData(nameof(GetInvalidImageReference))]
        public void GiveInvalidImageReference_WhenGetTemplateCollection_ExceptionWillBeThrownAsync(string imageReference)
        {
            if (_containerRegistryInfo == null)
            {
                return;
            }

            TemplateCollectionProviderFactory factory = new TemplateCollectionProviderFactory(cache, Options.Create(_config));
            Assert.Throws<ImageReferenceException>(() => factory.CreateTemplateCollectionProvider(imageReference, token));
        }

        [Theory]
        [MemberData(nameof(GetNotExistImageInfo))]
        public async Task GiveImageReference_WhenGetTemplateCollection_IfImageNotFound_ExceptionWillBeThrownAsync(string imageName, string tag)
        {
            if (_containerRegistryInfo == null)
            {
                return;
            }

            string imageReference = string.Format("{0}/{1}:{2}", _containerRegistryInfo.ContainerRegistryServer, imageName, tag);
            TemplateCollectionProviderFactory factory = new TemplateCollectionProviderFactory(cache, Options.Create(_config));
            var templateCollectionProvider = factory.CreateTemplateCollectionProvider(imageReference, token);
            await Assert.ThrowsAsync<ImageNotFoundException>(async () => await templateCollectionProvider.GetTemplateCollectionAsync());
        }

        [Theory]
        [MemberData(nameof(GetValidImageInfoWithTag))]
        public async Task GiveImageReference_WhenGetTemplateCollection_IfTokenInvalid_ExceptionWillBeThrownAsync(List<int> expectedTemplatesCounts, string imageName, string tag)
        {
            if (_containerRegistryInfo == null)
            {
                return;
            }

            var fakeToken = "fakeToken";
            string imageReference = string.Format("{0}/{1}:{2}", _containerRegistryInfo.ContainerRegistryServer, imageName, tag);
            TemplateCollectionProviderFactory factory = new TemplateCollectionProviderFactory(cache, Options.Create(_config));
            var templateCollectionProvider = factory.CreateTemplateCollectionProvider(imageReference, fakeToken);
            await Assert.ThrowsAsync<ContainerRegistryAuthenticationException>(async () => await templateCollectionProvider.GetTemplateCollectionAsync());
            var emptyToken = string.Empty;
            Assert.Throws<ContainerRegistryAuthenticationException>(() => factory.CreateTemplateCollectionProvider(imageReference, emptyToken));
            Assert.NotNull(expectedTemplatesCounts);
        }

        [Theory]
        [MemberData(nameof(GetValidImageInfoWithTag))]
        public async Task GiveImageReference_WhenGetTemplateCollection_ACorrectTemplateCollectionWillBeReturnedAsync(List<int> expectedTemplatesCounts, string imageName, string tag)
        {
            if (_containerRegistryInfo == null)
            {
                return;
            }

            string imageReference = string.Format("{0}/{1}:{2}", _containerRegistryInfo.ContainerRegistryServer, imageName, tag);
            TemplateCollectionProviderFactory factory = new TemplateCollectionProviderFactory(cache, Options.Create(_config));
            var templateCollectionProvider = factory.CreateTemplateCollectionProvider(imageReference, token);
            var templateCollection = await templateCollectionProvider.GetTemplateCollectionAsync();
            Assert.Equal(expectedTemplatesCounts.Count(), templateCollection.Count());
            for (var i = 0; i < expectedTemplatesCounts.Count(); i++)
            {
                Assert.Equal(expectedTemplatesCounts[i], templateCollection[i].Count());
            }
        }

        [Theory]
        [MemberData(nameof(GetValidOCIImageInfoWithTag))]
        public async Task GiveOCIImageReference_WhenGetTemplateCollection_ACorrectTemplateCollectionWillBeReturnedAsync(List<int> expectedTemplatesCounts, string imageName, string tag)
        {
            if (_containerRegistryInfo == null)
            {
                return;
            }

            Assert.True(_isOrasValid, _orasErrorMessage);

            string imageReference = string.Format("{0}/{1}:{2}", _containerRegistryInfo.ContainerRegistryServer, imageName, tag);
            TemplateCollectionProviderFactory factory = new TemplateCollectionProviderFactory(cache, Options.Create(_config));
            var templateCollectionProvider = factory.CreateTemplateCollectionProvider(imageReference, token);
            var templateCollection = await templateCollectionProvider.GetTemplateCollectionAsync();
            Assert.Equal(expectedTemplatesCounts.Count(), templateCollection.Count());
            for (var i = 0; i < expectedTemplatesCounts.Count(); i++)
            {
                Assert.Equal(expectedTemplatesCounts[i], templateCollection[i].Count());
            }
        }

        [Fact]
        public async Task GiveOCIImageReferenceWithDigest_WhenGetTemplateCollection_ACorrectTemplateCollectionWillBeReturnedAsync()
        {
            if (_containerRegistryInfo == null)
            {
                return;
            }

            Assert.True(_isOrasValid, _orasErrorMessage);

            string imageReference = string.Format("{0}/{1}@{2}", _containerRegistryInfo.ContainerRegistryServer, "templatetest", testOneLayerImageDigest);
            TemplateCollectionProviderFactory factory = new TemplateCollectionProviderFactory(cache, Options.Create(_config));
            var templateCollectionProvider = factory.CreateTemplateCollectionProvider(imageReference, token);
            var templateCollection = await templateCollectionProvider.GetTemplateCollectionAsync();
            Assert.Single(templateCollection);

            imageReference = string.Format("{0}/{1}@{2}", _containerRegistryInfo.ContainerRegistryServer, "templatetest", testMultiLayerImageDigest);
            templateCollectionProvider = factory.CreateTemplateCollectionProvider(imageReference, token);
            templateCollection = await templateCollectionProvider.GetTemplateCollectionAsync();
            Assert.Equal(2, templateCollection.Count());
        }

        [Theory]
        [MemberData(nameof(GetHl7v2DataAndEntryTemplate))]
        public async Task GetTemplateCollectionFromAcr_WhenGivenHl7v2DataForConverting__ExpectedFhirResourceShouldBeReturnedAsync(string hl7v2Data, string entryTemplate)
        {
            if (_containerRegistryInfo == null)
            {
                return;
            }

            TemplateCollectionProviderFactory factory = new TemplateCollectionProviderFactory(cache, Options.Create(_config));
            var templateCollectionProvider = factory.CreateTemplateCollectionProvider(testOneLayerImageReference, token);
            var templateCollection = await templateCollectionProvider.GetTemplateCollectionAsync();
            TestByTemplate(hl7v2Data, entryTemplate, templateCollection);
        }

        [Theory]
        [MemberData(nameof(GetHl7v2DataAndEntryTemplate))]
        public async Task GetTemplateCollectionFromAcr_WhenGivenHl7v2DataForConverting_IfTemplateNotExist_ExceptionWillBeThrownAsync(string hl7v2Data, string entryTemplate)
        {
            if (_containerRegistryInfo == null)
            {
                return;
            }

            TemplateCollectionProviderFactory factory = new TemplateCollectionProviderFactory(cache, Options.Create(_config));
            var templateCollectionProvider = factory.CreateTemplateCollectionProvider(testMultiLayerImageReference, token);
            await Assert.ThrowsAsync<RenderException>(async () => TestByTemplate(hl7v2Data, entryTemplate, await templateCollectionProvider.GetTemplateCollectionAsync()));
        }

        [Theory]
        [MemberData(nameof(GetDefaultTemplatesInfo))]
        public async Task GiveDefaultImageReference_WhenGetTemplateCollectionWithEmptyToken_DefaultTemplatesWillBeReturnedAsync(string imageReference, string expectedTemplatesFolder)
        {
            TemplateCollectionProviderFactory factory = new TemplateCollectionProviderFactory(cache, Options.Create(_config));
            var templateCollectionProvider = factory.CreateTemplateCollectionProvider(imageReference, string.Empty);
            var templateCollection = await templateCollectionProvider.GetTemplateCollectionAsync();
            Assert.Single(templateCollection);

            // metadata.json will not be returned as template.
            // Json/Schema/meta-schema.json will not be returned as template.
            var excludeFiles = new HashSet<string>()
            {
                Path.Join(_templateDirectory, expectedTemplatesFolder, "metadata.json"),
                Path.Join(_templateDirectory, expectedTemplatesFolder, "Schema", "meta-schema.json"),
            };
            var expectedTemplateFiles = Directory.GetFiles(Path.Join(_templateDirectory, expectedTemplatesFolder), "*", SearchOption.AllDirectories)
                .Where(file => !excludeFiles.Contains(file)).ToList();
            Assert.Equal(expectedTemplateFiles.Count, templateCollection.First().Count());
        }

        // Conversion results of DefaultTemplates.tar.gz, default template folder and default template collection provider should be the same.
        [Theory]
        [MemberData(nameof(GetHl7v2DataAndTemplateSources))]
        public async Task GivenHl7v2SameInputData_WithDifferentTemplateSource_WhenConvert_ResultShouldBeIdentical(string inputFile, string defaultTemplateDirectory, string rootTemplate)
        {
            var folderTemplateProvider = new TemplateProvider(defaultTemplateDirectory, DataType.Hl7v2);

            var templateProviderFactory = new TemplateCollectionProviderFactory(new MemoryCache(new MemoryCacheOptions()), Options.Create(new TemplateCollectionConfiguration()));
            var templateProvider = templateProviderFactory.CreateTemplateCollectionProvider(_defaultHl7v2TemplateImageReference, string.Empty);
            var imageTemplateProvider = new TemplateProvider(await templateProvider.GetTemplateCollectionAsync(CancellationToken.None));

            var defaultTemplateCollectionProvider = GetDefaultTemplateCollectionProvider();
            var defaultTemplateProvider = new TemplateProvider(await defaultTemplateCollectionProvider.GetTemplateCollectionAsync(CancellationToken.None), isDefaultTemplateProvider: true);

            var hl7v2Processor = new Hl7v2Processor(_processorSettings, FhirConverterLogging.CreateLogger<Hl7v2Processor>());
            var inputContent = File.ReadAllText(inputFile);

            var imageResult = hl7v2Processor.Convert(inputContent, rootTemplate, imageTemplateProvider);
            var folderResult = hl7v2Processor.Convert(inputContent, rootTemplate, folderTemplateProvider);
            var defaultProviderResult = hl7v2Processor.Convert(inputContent, rootTemplate, defaultTemplateProvider);

            var regex = new Regex(@"<div .*>.*</div>");
            imageResult = regex.Replace(imageResult, string.Empty);
            folderResult = regex.Replace(folderResult, string.Empty);
            defaultProviderResult = regex.Replace(defaultProviderResult, string.Empty);

            Assert.Equal(imageResult, folderResult);
            Assert.Equal(folderResult, defaultProviderResult);
        }

        [Theory]
        [MemberData(nameof(GetCcdaDataAndTemplateSources))]
        public async Task GivenCcdaSameInputData_WithDifferentTemplateSource_WhenConvert_ResultShouldBeIdentical(string inputFile, string defaultTemplateDirectory, string rootTemplate)
        {
            var folderTemplateProvider = new TemplateProvider(defaultTemplateDirectory, DataType.Ccda);

            var templateProviderFactory = new TemplateCollectionProviderFactory(new MemoryCache(new MemoryCacheOptions()), Options.Create(new TemplateCollectionConfiguration()));
            var templateProvider = templateProviderFactory.CreateTemplateCollectionProvider(_defaultCcdaTemplateImageReference, string.Empty);
            var imageTemplateProvider = new TemplateProvider(await templateProvider.GetTemplateCollectionAsync(CancellationToken.None));

            var defaultTemplateCollectionProvider = GetDefaultTemplateCollectionProvider();
            var defaultTemplateProvider = new TemplateProvider(await defaultTemplateCollectionProvider.GetTemplateCollectionAsync(CancellationToken.None), isDefaultTemplateProvider: true);

            var ccdaProcessor = new CcdaProcessor(_processorSettings, FhirConverterLogging.CreateLogger<CcdaProcessor>());
            var inputContent = File.ReadAllText(inputFile);

            var imageResult = ccdaProcessor.Convert(inputContent, rootTemplate, imageTemplateProvider);
            var folderResult = ccdaProcessor.Convert(inputContent, rootTemplate, folderTemplateProvider);
            var defaultProviderResult = ccdaProcessor.Convert(inputContent, rootTemplate, defaultTemplateProvider);

            var imageResultObject = JObject.Parse(imageResult);
            var folderResultObject = JObject.Parse(folderResult);
            var defaultProviderResultObject = JObject.Parse(defaultProviderResult);

            // Remove DocumentReference, where date is different every time conversion is run and gzip result is OS dependent
            imageResultObject["entry"]?.Last()?.Remove();
            folderResultObject["entry"]?.Last()?.Remove();
            defaultProviderResultObject["entry"]?.Last()?.Remove();

            Assert.True(JToken.DeepEquals(imageResultObject, folderResultObject));
            Assert.True(JToken.DeepEquals(folderResultObject, defaultProviderResultObject));
        }

        [Theory]
        [MemberData(nameof(GetJsonDataAndTemplateSources))]
        public async Task GivenJsonSameInputData_WithDifferentTemplateSource_WhenConvert_ResultShouldBeIdentical(string inputFile, string defaultTemplateDirectory, string rootTemplate)
        {
            var folderTemplateProvider = new TemplateProvider(defaultTemplateDirectory, DataType.Json);

            var templateProviderFactory = new TemplateCollectionProviderFactory(new MemoryCache(new MemoryCacheOptions()), Options.Create(new TemplateCollectionConfiguration()));
            var templateProvider = templateProviderFactory.CreateTemplateCollectionProvider(_defaultJsonTemplateImageReference, string.Empty);
            var imageTemplateProvider = new TemplateProvider(await templateProvider.GetTemplateCollectionAsync(CancellationToken.None));

            var defaultTemplateCollectionProvider = GetDefaultTemplateCollectionProvider();
            var defaultTemplateProvider = new TemplateProvider(await defaultTemplateCollectionProvider.GetTemplateCollectionAsync(CancellationToken.None), isDefaultTemplateProvider: true);

            var jsonProcessor = new JsonProcessor(_processorSettings, FhirConverterLogging.CreateLogger<JsonProcessor>());
            var inputContent = File.ReadAllText(inputFile);

            var imageResult = jsonProcessor.Convert(inputContent, rootTemplate, imageTemplateProvider);
            var folderResult = jsonProcessor.Convert(inputContent, rootTemplate, folderTemplateProvider);
            var defaultProviderResult = jsonProcessor.Convert(inputContent, rootTemplate, defaultTemplateProvider);

            var imageResultObject = JObject.Parse(imageResult);
            var folderResultObject = JObject.Parse(folderResult);
            var defaultProviderResultObject = JObject.Parse(defaultProviderResult);

            Assert.True(JToken.DeepEquals(imageResultObject, folderResultObject));
            Assert.True(JToken.DeepEquals(folderResultObject, defaultProviderResultObject));
        }

        [Theory]
        [MemberData(nameof(GetFhirStu3DataAndTemplateSources))]
        public async Task GivenFhirStu3SameInputData_WithDifferentTemplateSource_WhenConvert_ResultShouldBeIdentical(string inputFile, string defaultTemplateDirectory, string rootTemplate)
        {
            var folderTemplateProvider = new TemplateProvider(defaultTemplateDirectory, DataType.Fhir);

            var templateProviderFactory = new TemplateCollectionProviderFactory(new MemoryCache(new MemoryCacheOptions()), Options.Create(new TemplateCollectionConfiguration()));
            var templateProvider = templateProviderFactory.CreateTemplateCollectionProvider(_defaultStu3ToR4TemplateImageReference, string.Empty);
            var imageTemplateProvider = new TemplateProvider(await templateProvider.GetTemplateCollectionAsync(CancellationToken.None));

            var defaultTemplateCollectionProvider = GetDefaultTemplateCollectionProvider();
            var defaultTemplateProvider = new TemplateProvider(await defaultTemplateCollectionProvider.GetTemplateCollectionAsync(CancellationToken.None), isDefaultTemplateProvider: true);

            var fhirProcessor = new FhirProcessor(_processorSettings, FhirConverterLogging.CreateLogger<FhirProcessor>());
            var inputContent = File.ReadAllText(inputFile);

            var imageResult = fhirProcessor.Convert(inputContent, rootTemplate, imageTemplateProvider);
            var folderResult = fhirProcessor.Convert(inputContent, rootTemplate, folderTemplateProvider);
            var defaultProviderResult = fhirProcessor.Convert(inputContent, rootTemplate, defaultTemplateProvider);

            var imageResultObject = JObject.Parse(imageResult);
            var folderResultObject = JObject.Parse(folderResult);
            var defaultProviderResultObject = JObject.Parse(defaultProviderResult);

            Assert.True(JToken.DeepEquals(imageResultObject, folderResultObject));
            Assert.True(JToken.DeepEquals(folderResultObject, defaultProviderResultObject));
        }

        [Theory]
        [MemberData(nameof(GetHl7v2DataAndEntryTemplate))]
        public async Task GivenDefaultTemplateCollection_WhenConvertHl7v2DataCalled_ExpectedFhirResourceReturnedAsync(string hl7v2Data, string entryTemplate)
        {
            if (_containerRegistryInfo == null)
            {
                return;
            }

            var templateCollectionProvider = GetDefaultTemplateCollectionProvider();
            var templateCollection = await templateCollectionProvider.GetTemplateCollectionAsync(CancellationToken.None);
            TestByTemplate(hl7v2Data, entryTemplate, templateCollection);
        }

        [Fact]
        public async Task GivenDefaultTemplateCollection_WhenConvertHl7v2DataCalledWithNonExistentDefaultTemplate_ExceptionWillBeThrownAsync()
        {
            if (_containerRegistryInfo == null)
            {
                return;
            }

            var templateCollectionProvider = GetDefaultTemplateCollectionProvider();
            await Assert.ThrowsAsync<RenderException>(async () => TestByTemplate("ADT01-23.hl7", "NonExistentTemplate", await templateCollectionProvider.GetTemplateCollectionAsync(CancellationToken.None)));
        }

        private void TestByTemplate(string inputFile, string entryTemplate, List<Dictionary<string, Template>> templateProvider)
        {
            var hl7v2Processor = new Hl7v2Processor(_processorSettings, FhirConverterLogging.CreateLogger<Hl7v2Processor>());
            var inputContent = File.ReadAllText(inputFile);
            var actualContent = hl7v2Processor.Convert(inputContent, entryTemplate, new TemplateProvider(templateProvider));

            Assert.True(actualContent.Length != 0);
        }

        private async Task InitOneLayerImageAsync()
        {
            List<string> templateFiles = new List<string> { baseLayerTemplatePath };
            await _containerRegistry.GenerateTemplateImageAsync(_containerRegistryInfo, testOneLayerImageReference, templateFiles);
        }

        private async Task InitMultiLayerImageAsync()
        {
            List<string> templateFiles = new List<string> { baseLayerTemplatePath, userLayerTemplatePath };
            await _containerRegistry.GenerateTemplateImageAsync(_containerRegistryInfo, testMultiLayerImageReference, templateFiles);
        }

        private async Task InitInvalidTarGzImageAsync()
        {
            List<string> templateFiles = new List<string> { invalidTarGzPath };
            await _containerRegistry.GenerateTemplateImageAsync(_containerRegistryInfo, testInvalidImageReference, templateFiles);
        }

        private async Task InitInvalidTemplateImageAsync()
        {
            List<string> templateFiles = new List<string> { invalidTemplatePath };
            await _containerRegistry.GenerateTemplateImageAsync(_containerRegistryInfo, testInvalidTemplateImageReference, templateFiles);
        }

        private async Task PushOneLayerOCIImageAsync()
        {
            string command = $"push {testOneLayerOCIImageReference} {_baseLayerTemplatePath}";
            testOneLayerImageDigest = await ExecuteOrasCommandAsync(command);
        }

        private async Task PushMultiLayersOCIImageAsync()
        {
            string command = $"push {testMultiLayerOCIImageReference} {_baseLayerTemplatePath} {_userLayerTemplatePath}";
            testMultiLayerImageDigest = await ExecuteOrasCommandAsync(command);
        }

        private async Task OrasLogin()
        {
            try
            {
                var command = $"login {_containerRegistryInfo.ContainerRegistryServer} -u {_containerRegistryInfo.ContainerRegistryUsername} -p {_containerRegistryInfo.ContainerRegistryPassword}";
                await OrasClient.OrasExecutionAsync(command);
            }
            catch
            {
                _isOrasValid = false;
            }
        }

        private async Task<string> ExecuteOrasCommandAsync(string command)
        {
            try
            {
                var output = await OrasClient.OrasExecutionAsync(command, Directory.GetCurrentDirectory());
                var digest = GetImageDigest(output);
                return digest.Value;
            }
            catch
            {
                _isOrasValid = false;
                return null;
            }
        }

        private Digest GetImageDigest(string input)
        {
            var digests = Digest.GetDigest(input);
            if (digests.Count == 0)
            {
                throw new OciClientException(TemplateManagementErrorCode.OrasProcessFailed, "Failed to parse image digest.");
            }

            return digests[0];
        }

        private IConvertDataTemplateCollectionProvider GetDefaultTemplateCollectionProvider()
        {
            var templateHostingConfig = new TemplateHostingConfiguration();
            var factory = new ConvertDataTemplateCollectionProviderFactory(
                Options.Create(templateHostingConfig),
                Options.Create(_config),
                cache);
            return factory.CreateTemplateCollectionProvider();
        }
    }
}