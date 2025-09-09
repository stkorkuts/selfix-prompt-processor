﻿using LanguageExt;
using Selfix.PromptProcessor.Shared.Extensions;

namespace Selfix.PromptProcessor.Application.Abstractions;

public interface IFileService
{
    IO<Stream> Open(string path, FileMode mode, FileAccess access, FileShare share);
}

public static class FileServiceExtensions
{
    public static IO<Unit> WriteStreamToFile(this IFileService fileService, string filePath, Stream source,
        CancellationToken cancellationToken) =>
        from destination in fileService.OpenWrite(filePath)
        from unit in IO.liftAsync(() => source.CopyToAsync(destination, cancellationToken).ToUnit())
        from _ in destination.DisposeAsyncIO()
        select unit;
    
    public static IO<Stream> OpenRead(this IFileService fileService, string path) =>
        fileService.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
    
    public static IO<Stream> OpenWrite(this IFileService fileService, string path) =>
        fileService.Open(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
}