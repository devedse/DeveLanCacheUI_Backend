using AsyncKeyedLock;
using DeveLanCacheUI_Backend.EpicManifestParser.Json;
using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace DeveLanCacheUI_Backend.EpicManifestParser.UE;

/// <summary>
/// UE FBuildPatchAppManifest struct
/// </summary>
public class FBuildPatchAppManifest
{
    /// <summary/>
    public FManifestMeta Meta { get; internal set; } = null!;
    /// <summary/>
    public IReadOnlyList<FChunkInfo> ChunkList { get; internal set; } = null!;
    /// <summary/>
    public IReadOnlyList<FFileManifest> Files { get; internal set; } = null!;
    /// <summary/>
    public IReadOnlyList<FCustomField> CustomFields { get; internal set; } = null!;
    /// <summary/>
    public IReadOnlyDictionary<FGuid, FChunkInfo> Chunks { get; internal set; } = null!;

    /// <summary/>
    public int64 TotalBuildSize { get; internal set; }
    /// <summary/>
    public int64 TotalDownloadSize { get; internal set; }

    internal ManifestParseOptions Options { get; init; } = null!;
    internal AsyncKeyedLocker<FGuid> ChunksLocker { get; set; } = null!;

    internal FBuildPatchAppManifest() { }

    /// <summary>
    /// Finds a file by <see cref="FFileManifest.FileName"/>.
    /// </summary>
    /// <param name="fileName">The filename to find.</param>
    /// <param name="comparisonType">The type to compare the filename.</param>
    /// <returns>The <see cref="FFileManifest"/> instance if the the file was found; otherwise, <see langword="null"/>.</returns>
    public FFileManifest? FindFile(string fileName, StringComparison comparisonType = StringComparison.Ordinal)
        => TryFindFile(fileName, comparisonType, out var file) ? file : null;

    /// <summary>
    /// Tries to find a file by <see cref="FFileManifest.FileName"/> using <see cref="StringComparison.Ordinal"/> to compare it.
    /// </summary>
    /// <param name="fileName">The filename to find.</param>
    /// <param name="fileManifest">The find result.</param>
    /// <returns><see langword="true"/> if the the file was found; otherwise, <see langword="false"/>.</returns>
    public bool TryFindFile(string fileName, [NotNullWhen(true)] out FFileManifest? fileManifest)
        => TryFindFile(fileName, StringComparison.Ordinal, out fileManifest);

    /// <summary>
    /// Tries to find a file by <see cref="FFileManifest.FileName"/>.
    /// </summary>
    /// <param name="fileName">The filename to find.</param>
    /// <param name="comparisonType">The type to compare the filename.</param>
    /// <param name="fileManifest">The find result.</param>
    /// <returns><see langword="true"/> if the the file was found; otherwise, <see langword="false"/>.</returns>
    public bool TryFindFile(string fileName, StringComparison comparisonType, [NotNullWhen(true)] out FFileManifest? fileManifest)
    {
        foreach (var file in Files)
        {
            if (!file.FileName.Equals(fileName, comparisonType))
                continue;

            fileManifest = file;
            return true;
        }

        fileManifest = null;
        return false;
    }

    /// <summary>
    /// Get the chunk sub-directory name
    /// </summary>
    public string GetChunkSubdir() => Meta.FeatureLevel switch
    {
        > EFeatureLevel.StoredAsBinaryData => "ChunksV4",
        > EFeatureLevel.StoresDataGroupNumbers => "ChunksV3",
        > EFeatureLevel.StartStoringVersion => "ChunksV2",
        _ => "Chunks"
    };

    /// <summary>
    /// Helper function to decide whether the passed in data is a JSON string we expect to deserialize a manifest from
    /// </summary>
    /// <returns><see langword="true"/> if the <paramref name="dataInput"/> is JSON; otherwise, <see langword="false"/>.</returns>
    public static bool IsJson(ManifestRoData dataInput)
    {
        // The best we can do is look for the mandatory first character open curly brace,
        // it will be within the first 4 characters (may have BOM)
        var span = dataInput
#if !NET9_0_OR_GREATER
			.Span
#endif
            ;
        for (var idx = 0; idx < 4 && idx < span.Length; ++idx)
        {
            if (span[idx] == '{')
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Deserializes a binary or JSON manifest
    /// </summary>
    /// <inheritdoc cref="DeserializeBinary(ManifestRoData,Action{ManifestParseOptions}?)"/>
    public static FBuildPatchAppManifest Deserialize(ManifestRoData dataInput, Action<ManifestParseOptions>? optionsBuilder = null)
    {
        var options = new ManifestParseOptions();
        optionsBuilder?.Invoke(options);
        return Deserialize(dataInput, options);
    }

    /// <summary>
    /// Deserializes a JSON manifest
    /// </summary>
    /// <param name="dataInput">The span to parse from</param>
    /// <param name="optionsBuilder">Builder for options/configuration to parse</param>
    public static FBuildPatchAppManifest DeserializeJson(ManifestRoData dataInput, Action<ManifestParseOptions>? optionsBuilder = null)
    {
        var options = new ManifestParseOptions();
        optionsBuilder?.Invoke(options);
        return DeserializeJson(dataInput, options);
    }

    /// <summary>
    /// Deserializes a binary manifest
    /// </summary>
    /// <param name="dataInput">The span to parse from</param>
    /// <param name="optionsBuilder">Builder for options/configuration to parse</param>
    /// <exception cref="NotSupportedException">Manifest is encrypted or older than <see cref="EFeatureLevel.StoredAsBinaryData"/></exception>
    /// <exception cref="InvalidOperationException">Data is compressed and zlib-ng instance was null</exception>
    /// <exception cref="FileLoadException">Error while parsing</exception>
    /// <exception cref="InvalidDataException">Hashes do not match</exception>
    public static FBuildPatchAppManifest DeserializeBinary(ManifestRoData dataInput, Action<ManifestParseOptions>? optionsBuilder = null)
    {
        var options = new ManifestParseOptions();
        optionsBuilder?.Invoke(options);
        return DeserializeBinary(dataInput, options);
    }

    /// <summary>
    /// Deserializes a binary or JSON manifest
    /// </summary>
    /// <inheritdoc cref="DeserializeBinary(ManifestRoData,ManifestParseOptions)"/>
    public static FBuildPatchAppManifest Deserialize(ManifestRoData dataInput, ManifestParseOptions options)
    {
        return IsJson(dataInput) ? DeserializeJson(dataInput, options) : DeserializeBinary(dataInput, options);
    }

    /// <summary>
    /// Deserializes a JSON manifest
    /// </summary>
    /// <param name="dataInput">The span to parse from</param>
    /// <param name="options">Options/Configuration to parse</param>
    public static FBuildPatchAppManifest DeserializeJson(ManifestRoData dataInput, ManifestParseOptions options)
    {
        var reader = JsonNode.Parse(dataInput
#if !NET9_0_OR_GREATER
			.Span
#endif
        )!.AsObject();

        var featureLevel = reader["ManifestFileVersion"].GetBlob(EFeatureLevel.CustomFields);
        if (featureLevel == EFeatureLevel.BrokenJsonVersion)
            featureLevel = EFeatureLevel.StoresChunkFileSizes;

        var meta = new FManifestMeta
        {
            FeatureLevel = featureLevel,
            AppID = reader["AppID"].GetBlob<uint32>(),
            AppName = reader["AppNameString"].GetString(),
            BuildVersion = reader["BuildVersionString"].GetString(),
            LaunchExe = reader["LaunchExeString"].GetString(),
            LaunchCommand = reader["LaunchCommand"].GetString(),
            PrereqName = reader["PrereqName"].GetString(),
            PrereqPath = reader["PrereqPath"].GetString(),
            PrereqArgs = reader["PrereqArgs"].GetString(),
            UninstallExe = "",
            UninstallCommand = "",
        };

        var jsonFileManifestList = reader["FileManifestList"]!.AsArray();
        var fileManifests = new FFileManifest[jsonFileManifestList.Count];
        var fileManifestsSpan = fileManifests.AsSpan();

        //var allDataGuids = new HashSet<FGuid>();
        var mutableChunkInfoLookup = new Dictionary<FGuid, FChunkInfo>();

        for (var i = 0; i < fileManifestsSpan.Length; i++)
        {
            var jsonFileManifest = jsonFileManifestList[i]!;
            var fileManifest = fileManifestsSpan[i] = new FFileManifest
            {
                FileName = jsonFileManifest["Filename"].GetString(),
                FileHash = jsonFileManifest["FileHash"].GetBlob<FSHAHash>(),
                InstallTags = jsonFileManifest["InstallTags"].Parse<string[]>([]),
                SymlinkTarget = jsonFileManifest["SymlinkTarget"].GetString()
            };
            var jsonFileChunkParts = jsonFileManifest["FileChunkParts"]!.AsArray();
            fileManifest.ChunkPartsArray = new FChunkPart[jsonFileChunkParts.Count];
            var chunkPartsSpan = fileManifest.ChunkPartsArray.AsSpan();
            for (var j = 0; j < chunkPartsSpan.Length; j++)
            {
                var jsonFileChunkPart = jsonFileChunkParts[j]!;
                var chunkPartGuid = jsonFileChunkPart["Guid"].GetFGuid();
                var chunkPartOffset = jsonFileChunkPart["Offset"].GetBlob<uint32>();
                var chunkPartSize = jsonFileChunkPart["Size"].GetBlob<uint32>();
                chunkPartsSpan[j] = new FChunkPart(chunkPartGuid, chunkPartOffset, chunkPartSize);

                ref var lookupChunk = ref CollectionsMarshal.GetValueRefOrAddDefault(mutableChunkInfoLookup, chunkPartGuid, out var exists);
                if (!exists)
                {
                    lookupChunk = new FChunkInfo
                    {
                        Guid = chunkPartGuid
                    };
                }
            }

            if (jsonFileManifest["bIsUnixExecutable"].Get<bool>())
                fileManifest.FileMetaFlags |= EFileMetaFlags.UnixExecutable;
            if (jsonFileManifest["bIsReadOnly"].Get<bool>())
                fileManifest.FileMetaFlags |= EFileMetaFlags.ReadOnly;
            if (jsonFileManifest["bIsCompressed"].Get<bool>())
                fileManifest.FileMetaFlags |= EFileMetaFlags.Compressed;
        }

        var chunkList = new FChunkInfo[mutableChunkInfoLookup.Count];
        var chunkListSpan = chunkList.AsSpan();
        var chunkIndex = 0;
        foreach (var chunk in mutableChunkInfoLookup.Values)
        {
            chunkListSpan[chunkIndex++] = chunk;
        }

        var hasChunkHashList = false;
        var jsonChunkHashListNode = reader["ChunkHashList"];
        if (jsonChunkHashListNode is not null)
        {
            var jsonChunkHashList = jsonChunkHashListNode.AsObject();

            foreach (var (guidString, jsonChunkHash) in jsonChunkHashList)
            {
                var guid = new FGuid(guidString);
                var chunkHash = jsonChunkHash.GetBlob<uint64>();
                mutableChunkInfoLookup[guid].Hash = chunkHash;
            }

            hasChunkHashList = true;
        }

        var jsonChunkShaListNode = reader["ChunkShaList"];
        if (jsonChunkShaListNode is not null)
        {
            var jsonChunkShaList = jsonChunkShaListNode.AsObject();

            foreach (var (guidString, jsonSha) in jsonChunkShaList)
            {
                var guid = new FGuid(guidString);
                var chunkSha = jsonSha.GetSha();
                mutableChunkInfoLookup[guid].ShaHash = chunkSha;
            }
        }

        var prereqIds = reader["PrereqIds"].Deserialize<string[]>();
        if (prereqIds is null)
        {
            // TODO: https://github.com/EpicGames/UnrealEngine/blob/8c31706601135aadf2f957fb76e2af46f04a8ef9/Engine/Source/Runtime/Online/BuildPatchServices/Private/BuildPatchManifest.cpp#L602
            meta.PrereqIds = [];
        }
        else
        {
            meta.PrereqIds = prereqIds;
        }

        var jsonDataGroupListNode = reader["DataGroupList"];
        if (jsonDataGroupListNode is not null)
        {
            var jsonDataGroupList = jsonDataGroupListNode.AsObject();

            foreach (var (guidString, jsonDataGroup) in jsonDataGroupList)
            {
                var guid = new FGuid(guidString);
                var dataGroup = jsonDataGroup.GetBlob<uint8>();
                mutableChunkInfoLookup[guid].GroupNumber = dataGroup;
            }
        }
        else
        {
            // TODO: https://github.com/EpicGames/UnrealEngine/blob/8c31706601135aadf2f957fb76e2af46f04a8ef9/Engine/Source/Runtime/Online/BuildPatchServices/Private/BuildPatchManifest.cpp#L635
            //       https://github.com/EpicGames/UnrealEngine/blob/8c31706601135aadf2f957fb76e2af46f04a8ef9/Engine/Source/Runtime/Core/Private/Misc/Crc.cpp#L592
        }

        var hasChunkFilesizeList = false;
        var jsonChunkFilesizeListNode = reader["ChunkFilesizeList"];
        if (jsonChunkFilesizeListNode is not null)
        {
            var jsonChunkFilesizeList = jsonChunkFilesizeListNode.AsObject();

            foreach (var (guidString, jsonFileSize) in jsonChunkFilesizeList)
            {
                var guid = new FGuid(guidString);
                var fileSize = jsonFileSize.GetBlob<int64>();
                mutableChunkInfoLookup[guid].FileSize = fileSize;
            }

            hasChunkFilesizeList = true;
        }

        if (!hasChunkFilesizeList)
        {
            // Missing chunk list, version before we saved them compressed. Assume original fixed chunk size of 1 MiB.
            foreach (var chunk in chunkListSpan)
            {
                chunk.FileSize = 1048576;
            }
        }

        if (reader.TryGetPropertyValue("bIsFileData", out var jsonIsFileData))
        {
            meta.bIsFileData = jsonIsFileData.Get<bool>();
        }
        else
        {
            meta.bIsFileData = !hasChunkHashList;
        }

        FCustomField[]? customFields = null;
        var jsonCustomFieldsNode = reader["CustomFields"];
        if (jsonCustomFieldsNode is not null)
        {
            var jsonCustomFields = jsonCustomFieldsNode.AsObject();
            customFields = new FCustomField[jsonCustomFields.Count];
            var customFieldIndex = 0;

            foreach (var (name, jsonValue) in jsonCustomFields)
            {
                customFields[customFieldIndex++] = new FCustomField
                {
                    Name = name,
                    Value = jsonValue.GetString()
                };
            }
        }

        meta.BuildId = FManifestMeta.GetBackwardsCompatibleBuildId(meta);

        var manifest = new FBuildPatchAppManifest
        {
            Meta = meta,
            ChunkList = chunkList,
            Files = fileManifests,
            CustomFields = customFields ?? [],
            Chunks = mutableChunkInfoLookup,
            Options = options
        };
        manifest.PostSetup();

        // FileDataList.OnPostLoad();
        {
            Array.Sort(fileManifests);
            for (var i = 0; i < fileManifestsSpan.Length; i++)
            {
                var file = fileManifestsSpan[i];
                file.Manifest = manifest;
                foreach (var chunkPart in file.ChunkPartsArray.AsSpan())
                {
                    file.FileSize += chunkPart.Size;
                }
            }
        }

        return manifest;
    }

    /// <summary>
    /// Deserializes a binary manifest
    /// </summary>
    /// <param name="dataInput">The span to parse from</param>
    /// <param name="options">Options/Configuration to parse</param>
    /// <exception cref="NotSupportedException">Manifest is encrypted or older than <see cref="EFeatureLevel.StoredAsBinaryData"/></exception>
    /// <exception cref="InvalidOperationException">Data is compressed and zlib-ng instance was null</exception>
    /// <exception cref="FileLoadException">Error while parsing</exception>
    /// <exception cref="InvalidDataException">Hashes do not match</exception>
    public static FBuildPatchAppManifest DeserializeBinary(ManifestRoData dataInput, ManifestParseOptions options)
    {
        var fileReader = new ManifestReader(dataInput);
        byte[]? manifestRawDataBuffer = null;

        try
        {
            var header = new FManifestHeader(ref fileReader);

            if (header.Version < EFeatureLevel.StoredAsBinaryData)
                throw new NotSupportedException("Manifests below feature level StoredAsBinaryData are not supported");
            if (header.StoredAs.HasFlag(EManifestStorageFlags.Encrypted))
                throw new NotSupportedException("Encrypted manifests are not supported");
            if (header.StoredAs.HasFlag(EManifestStorageFlags.Compressed) && options.Decompressor is null)
                throw new InvalidOperationException("Data is compressed and decompressor delegate was null");

            ManifestData manifestRawData;

            if (header.StoredAs.HasFlag(EManifestStorageFlags.Compressed))
            {
                manifestRawDataBuffer = ArrayPool<byte>.Shared.Rent(header.DataSizeCompressed + header.DataSizeUncompressed);
                manifestRawData = manifestRawDataBuffer
#if NET9_0_OR_GREATER
                    .AsSpan
#else
					.AsMemory
#endif
                        (header.DataSizeCompressed, header.DataSizeUncompressed);

                var manifestCompressedData = manifestRawDataBuffer.AsSpan(0, header.DataSizeCompressed);
                fileReader.Read(manifestCompressedData);

                var result = options.Decompressor!.Invoke(
                    options.DecompressorState,
                    manifestRawDataBuffer, 0, header.DataSizeCompressed,
                    manifestRawDataBuffer, header.DataSizeCompressed, header.DataSizeUncompressed);
                if (!result)
                    throw new FileLoadException("Failed to uncompress data");
            }
            else if (header.StoredAs == EManifestStorageFlags.None)
            {
                manifestRawDataBuffer = ArrayPool<byte>.Shared.Rent(header.DataSizeCompressed);
                manifestRawData = manifestRawDataBuffer
#if NET9_0_OR_GREATER
                    .AsSpan
#else
					.AsMemory
#endif
                        (0, header.DataSizeCompressed);
                fileReader.Read(manifestRawData
#if !NET9_0_OR_GREATER
					.Span
#endif
                );
            }
            else
            {
                throw new UnreachableException("Manifest has invalid or unknown storage flags");
            }

            var hash = FSHAHash.Compute(manifestRawData
#if !NET9_0_OR_GREATER
				.Span
#endif
            );
            if (header.SHAHash != hash)
                throw new InvalidDataException($"Hash does not match. expected: {header.SHAHash}, actual: {hash}");

            var reader = new ManifestReader(manifestRawData);
            var chunks = new Dictionary<FGuid, FChunkInfo>();
            var manifest = new FBuildPatchAppManifest
            {
                Chunks = chunks,
                Options = options
            };
            manifest.Meta = new FManifestMeta(ref reader);
            manifest.ChunkList = FChunkInfo.ReadChunkDataList(ref reader, chunks);
            manifest.Files = FFileManifest.ReadFileDataList(ref reader, manifest);
            manifest.CustomFields = FCustomField.ReadCustomFields(ref reader);
            manifest.PostSetup();
            return manifest;
        }
        finally
        {
            if (manifestRawDataBuffer is not null)
                ArrayPool<byte>.Shared.Return(manifestRawDataBuffer);
        }
    }

    private void PostSetup()
    {
        foreach (var file in Files)
        {
            TotalBuildSize += file.FileSize;
        }

        foreach (var chunk in ChunkList)
        {
            TotalDownloadSize += chunk.FileSize;
        }

        if (!string.IsNullOrEmpty(Options.ChunkBaseUrl))
        {
            ChunksLocker = new AsyncKeyedLocker<FGuid>(lockerOptions =>
            {
                lockerOptions.MaxCount = 1;
                lockerOptions.PoolSize = 128;
                lockerOptions.PoolInitialFill = 64;
            });

            Options.CreateDefaultClient();
        }
    }
}
