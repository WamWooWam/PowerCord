using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Provider;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using Microsoft.PowerShell.Commands;
using PowerCord.Helpers;
using DriveNotFoundException = System.Management.Automation.DriveNotFoundException;

namespace PowerCord.Commands
{
    [Cmdlet("Get", "File")]
    public class GetFileCommand : ContentCommandBase
    {
        private class DiscordFileObject : PSDiscordObject
        {
            private string _fileName;
            private Stream _stream;

            public DiscordFileObject(string fileName, Stream str)
            {
                _fileName = fileName;
                _stream = str;
            }

            public override async Task ProcessAsync(PSDiscordCmdletContext ctx)
            {
                var builder = new DiscordMessageBuilder()
                    .WithFile(_fileName, _stream);

                await ctx.Channel.SendMessageAsync(builder);
            }

            public override void Dispose()
            {
                _stream.Dispose();
            }
        }

        /// <summary>
        /// Gets the content of an item at the specified path.
        /// </summary>
        protected override void ProcessRecord()
        {
            // Get the content readers
            CmdletProviderContext currentContext = CmdletProviderContext;
            var contentStreams = this.GetContentStreams(Path, currentContext);

            try
            {
                // Iterate through the content holders reading the content
                foreach (var stream in contentStreams)
                {
                    WriteObject(new DiscordFileObject(System.IO.Path.GetFileName(stream.Name), stream));
                }
            }
            finally
            {

            }
        }

        /// <summary>
        /// Gets the IContentReaders for the current path(s)
        /// </summary>
        /// <returns>
        /// An array of IContentReaders for the current path(s)
        /// </returns>
        internal List<FileStream> GetContentStreams(string[] readerPaths, CmdletProviderContext currentCommandContext)
        {
            // Resolve all the paths into PathInfo objects
            var pathInfos = ResolvePaths(readerPaths, false, true, currentCommandContext);

            // Create the results array
            var results = new List<FileStream>();

            foreach (var pathInfo in pathInfos)
            {
                // For each path, get the content writer
                Collection<FileStream> readers = null;

                try
                {
                    string pathToProcess = WildcardPattern.Escape(pathInfo.Path);
                    if (currentCommandContext.SuppressWildcardExpansion)
                    {
                        pathToProcess = pathInfo.Path;
                    }

                    readers = GetStream(new[] { pathToProcess }, currentCommandContext);
                    results.AddRange(readers);
                }
                catch (PSNotSupportedException notSupported)
                {
                    WriteError(
                        new ErrorRecord(
                            notSupported.ErrorRecord,
                            notSupported));
                    continue;
                }
                catch (DriveNotFoundException driveNotFound)
                {
                    WriteError(
                        new ErrorRecord(
                            driveNotFound.ErrorRecord,
                            driveNotFound));
                    continue;
                }
                catch (ProviderNotFoundException providerNotFound)
                {
                    WriteError(
                        new ErrorRecord(
                            providerNotFound.ErrorRecord,
                            providerNotFound));
                    continue;
                }
                catch (ItemNotFoundException pathNotFound)
                {
                    WriteError(
                        new ErrorRecord(
                            pathNotFound.ErrorRecord,
                            pathNotFound));
                    continue;
                }
            }

            return results;
        }

        internal Collection<FileStream> GetStream(string[] paths, CmdletProviderContext context)
        {
            if (paths == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(paths));
            }

            ProviderInfo provider = null;
            CmdletProvider providerInstance = null;
            var results = new Collection<FileStream>();

            foreach (string path in paths)
            {
                if (path == null)
                {
                    throw PSTraceSource.NewArgumentNullException(nameof(paths));
                }

                Collection<string> providerPaths =
                   SessionState.Internal.Globber.GetGlobbedProviderPathsFromMonadPath(path, false, context, out provider, out providerInstance);

                foreach (string providerPath in providerPaths)
                {
                    if (File.Exists(providerPath))
                    {
                        results.Add(File.OpenRead(providerPath));
                    }
                }
            }

            return results;
        }
    }
}
