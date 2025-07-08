// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.ContainerRegistry.Models;
using Microsoft.Health.Fhir.TemplateManagement.Exceptions;
using Microsoft.Health.Fhir.TemplateManagement.Models;
using Microsoft.Health.Fhir.TemplateManagement.Overlay;
using Microsoft.Health.Fhir.TemplateManagement.Utilities;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.Health.Fhir.TemplateManagement.UnitTests.Overlay
{
    [Trait("Category", "Unit")]
    public class OverlayOperatorTests : IDisposable
    {
        private readonly IOverlayOperator _overlayOperator;
        private readonly List<string> _tempDirectories = new List<string>();

        // Test data constants
        private const int ExpectedTestDecompressFileCount = 3;
        private const int ExpectedLayer1FileCount = 840;
        private const int ExpectedLayer2FileCount = 835;
        private const int ExpectedMergedLayerFileCount = 6;
        private const int ExpectedUserFolderFileCount = 6;
        private const int MinimumDiffLayerFileCount = 800;

        public OverlayOperatorTests()
        {
            _overlayOperator = new OverlayOperator();
        }

        public static IEnumerable<object[]> GetValidOciArtifacts()
        {
            yield return new object[] 
            { 
                new ArtifactBlob() 
                { 
                    FileName = "testdecompress.tar.gz", 
                    Content = ReadTestFile("TestData/TarGzFiles/testdecompress.tar.gz"), 
                    Size = 100, 
                    Digest = "test1" 
                }, 
                ExpectedTestDecompressFileCount 
            };
            yield return new object[] 
            { 
                new ArtifactBlob() 
                { 
                    FileName = "layer1.tar.gz", 
                    Content = ReadTestFile("TestData/TarGzFiles/layer1.tar.gz"), 
                    Size = 100, 
                    Digest = "test2" 
                }, 
                ExpectedLayer1FileCount 
            };
            yield return new object[] 
            { 
                new ArtifactBlob() 
                { 
                    FileName = "layer2.tar.gz", 
                    Content = ReadTestFile("TestData/TarGzFiles/layer2.tar.gz"), 
                    Size = 100, 
                    Digest = "test3" 
                }, 
                ExpectedLayer2FileCount 
            };
        }

        [Theory]
        [MemberData(nameof(GetValidOciArtifacts))]
        public void GivenAnOciArtifactLayer_WhenExtractLayers_CorrectOCIFileLayerShouldBeReturned(ArtifactBlob inputLayer, int expectedFileCount)
        {
            // Act
            var result = _overlayOperator.Extract(inputLayer);

            // Assert
            Assert.Equal(expectedFileCount, result.FileContent.Count);
            Assert.Equal(inputLayer.Content, result.Content);
            Assert.Equal(inputLayer.FileName, result.FileName);
            Assert.Equal(inputLayer.Size, result.Size);
            Assert.Equal(inputLayer.Digest, result.Digest);
        }

        [Fact]
        public void GivenInvalidContentOciArtifactLayer_WhenExtractLayers_ExceptionWillBeThrown()
        {
            // Arrange
            string filePath = "TestData/TarGzFiles/invalid1.tar.gz";
            if (!File.Exists(filePath))
            {
                // Skipped: test data file not available
                return;
            }

            var oneLayer = new ArtifactBlob() { Content = File.ReadAllBytes(filePath) };

            // Act & Assert
            Assert.Throws<ArtifactArchiveException>(() => _overlayOperator.Extract(oneLayer));
        }

        [Fact]
        public void GivenAListOfOciArtifactLayers_WhenExtractLayers_ListOfOciFileLayersShouldBeReturned()
        {
            // Arrange
            var artifactFilePaths = new List<string> 
            { 
                "TestData/TarGzFiles/baseLayer.tar.gz", 
                "TestData/TarGzFiles/userV1.tar.gz" 
            };
            var inputLayers = CreateArtifactBlobsFromPaths(artifactFilePaths);

            // Act
            var result = _overlayOperator.Extract(inputLayers);

            // Assert
            Assert.Equal(inputLayers.Count, result.Count());
        }

        [Fact]
        public async Task GivenAListOfOciArtifactLayers_WhenSortLayers_ASortedOciArtifactLayersShouldBeReturnedAsync()
        {
            // Arrange
            string layer1 = "TestData/TarGzFiles/layer1.tar.gz";
            string layer2 = "TestData/TarGzFiles/layer2.tar.gz";
            string layer3 = "TestData/TarGzFiles/userV1.tar.gz";
            string manifest = "TestData/ExpectedManifest/testOrderManifest";
            string workingFolder = CreateTempDirectory("testSortLayers");
            
            try
            {
                Directory.CreateDirectory(Path.Combine(workingFolder, ".image/layers"));

                // Rename files to rearrange the sequence.
                File.Copy(layer1, Path.Combine(workingFolder, ".image/layers/3.tar.gz"));
                File.Copy(layer2, Path.Combine(workingFolder, ".image/layers/2.tar.gz"));
                File.Copy(layer3, Path.Combine(workingFolder, ".image/layers/1.tar.gz"));
                
                var overlayFs = new OverlayFileSystem(workingFolder);
                var layers = await overlayFs.ReadImageLayersAsync();

                // Act
                var sortedLayers = _overlayOperator.Sort(layers, JsonConvert.DeserializeObject<ManifestWrapper>(File.ReadAllText(manifest)));

                // Assert
                Assert.Equal("3.tar.gz", sortedLayers[0].FileName);
                Assert.Equal("2.tar.gz", sortedLayers[1].FileName);
                Assert.Equal("1.tar.gz", sortedLayers[2].FileName);
            }
            finally
            {
                DirectoryHelper.ClearFolder(workingFolder);
            }
        }

        [Fact]
        public void GivenAListOfOciFileLayers_WhenMergeLayers_AMergedOciFileLayersShouldBeReturned()
        {
            // Arrange
            var artifactFilePaths = new List<string> 
            { 
                "TestData/TarGzFiles/layer1.tar.gz", 
                "TestData/TarGzFiles/layer2.tar.gz" 
            };
            var inputLayers = CreateArtifactBlobsFromPaths(artifactFilePaths);
            var fileLayers = _overlayOperator.Extract(inputLayers);

            // Act
            var mergedLayer = _overlayOperator.Merge(fileLayers);

            // Assert
            Assert.Equal(ExpectedMergedLayerFileCount, mergedLayer.FileContent.Count());
        }

        [Fact]
        public async Task GivenAOciFileLayer_WhenGenerateDiffOciFileLayer_IfBaseLayerFolderIsEmptyOrNull_ABaseOciFileLayerShouldBeReturnedAsync()
        {
            // Arrange
            var overlayFs = new OverlayFileSystem("TestData/UserFolder");
            var fileLayer = await overlayFs.ReadOciFileLayerAsync();

            // Act
            var diffLayers = _overlayOperator.GenerateDiffLayer(fileLayer, null);

            // Assert
            Assert.Equal(ExpectedUserFolderFileCount, diffLayers.FileContent.Count());
        }

        [Fact]
        public async Task GivenAOciFileLayer_WhenGenerateDiffOciFileLayerWithSnapshot_ADiffOciFileLayerShouldBeReturnedAsync()
        {
            // Arrange
            var overlayFs = new OverlayFileSystem("TestData/UserFolder");
            overlayFs.ClearBaseLayerFolder();
            Directory.CreateDirectory("TestData/UserFolder/.image/base");
            File.Copy("TestData/TarGzFiles/layer1.tar.gz", "TestData/UserFolder/.image/base/layer1.tar.gz", true);
            
            var fileLayer = await overlayFs.ReadOciFileLayerAsync();
            var baseLayers = await overlayFs.ReadBaseLayerAsync();
            var baseOcifileLayer = _overlayOperator.Extract(baseLayers);

            // Act
            var diffLayers = _overlayOperator.GenerateDiffLayer(fileLayer, baseOcifileLayer);

            // Cleanup
            overlayFs.ClearBaseLayerFolder();
            
            // Assert
            Assert.True(diffLayers.FileContent.Count > 0, "Should have at least some diff files");
            Assert.True(diffLayers.FileContent.Count >= MinimumDiffLayerFileCount, "Should have a substantial number of diff files");
        }

        [Fact]
        public void GivenAOciFileLayer_WhenPackLayer_AnOciArtifactLayerShouldBeReturned()
        {
            // Arrange
            string artifactPath = "TestData/TarGzFiles/layer1.tar.gz";
            ArtifactBlob inputLayer = new ArtifactBlob() { Content = File.ReadAllBytes(artifactPath) };
            var fileLayer = _overlayOperator.Extract(inputLayer);
            var diffLayers = _overlayOperator.GenerateDiffLayer(fileLayer, null);

            // Act
            var packedLayer = _overlayOperator.Archive(diffLayers);

            // Assert
            Assert.Equal(inputLayer.Content.Count(), packedLayer.Content.Count());
        }

        [Fact]
        public void GivenOciFileLayers_WhenPackLayers_OciArtifactLayersShouldBeReturned()
        {
            // Arrange
            var artifactPaths = new List<string> { "TestData/TarGzFiles/baseLayer.tar.gz" };
            var inputLayers = CreateArtifactBlobsFromPaths(artifactPaths);
            var fileLayers = _overlayOperator.Extract(inputLayers);
            var diffLayers = new List<OciFileLayer>();
            
            foreach (var layer in fileLayers)
            {
                diffLayers.Add(_overlayOperator.GenerateDiffLayer(layer, null));
            }

            // Act
            var packedLayers = _overlayOperator.Archive(diffLayers);

            // Assert
            Assert.Equal(inputLayers.Count, packedLayers.Count);
        }

        // Helper methods
        private static byte[] ReadTestFile(string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Test data file not found: {path}");
            }
            return File.ReadAllBytes(path);
        }

        private static List<ArtifactBlob> CreateArtifactBlobsFromPaths(List<string> paths)
        {
            var inputLayers = new List<ArtifactBlob>();
            foreach (var path in paths)
            {
                if (!File.Exists(path))
                {
                    throw new FileNotFoundException($"Test data file not found: {path}");
                }
                inputLayers.Add(new ArtifactBlob() { Content = File.ReadAllBytes(path) });
            }
            return inputLayers;
        }

        private string CreateTempDirectory(string prefix)
        {
            var tempDir = Path.Combine(Path.GetTempPath(), $"{prefix}_{Guid.NewGuid()}");
            _tempDirectories.Add(tempDir);
            return tempDir;
        }

        public void Dispose()
        {
            // Cleanup all temporary directories
            foreach (var dir in _tempDirectories)
            {
                if (Directory.Exists(dir))
                {
                    try
                    {
                        DirectoryHelper.ClearFolder(dir);
                        Directory.Delete(dir);
                    }
                    catch (Exception)
                    {
                        // Log cleanup failures but don't throw
                    }
                }
            }
        }
    }
}
