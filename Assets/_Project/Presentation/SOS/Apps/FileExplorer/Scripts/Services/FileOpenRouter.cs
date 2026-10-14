using System;

namespace Atlas.SOS.FileSystem
{
    // Implemented by the app launch system.
    public interface IFileAppLauncher
    {
        // Return true only when the request was delivered successfully.
        bool TryOpenFile(
            string appId,
            FileOpenRequest request,
            out string error);
    }

    // Implemented by apps capable of displaying filesystem content.
    public interface IFileContentReceiver
    {
        // Return false for unsupported or unresolved content.
        bool TryOpenFile(
            FileOpenRequest request,
            out string error);
    }

    public sealed class FileOpenRouter
    {
        private readonly FileAssociationCatalog associations;
        private readonly IFileAppLauncher launcher;

        public FileOpenRouter(
            FileAssociationCatalog associations,
            IFileAppLauncher launcher)
        {
            this.associations = associations != null
                ? associations
                : throw new ArgumentNullException(nameof(associations));

            this.launcher = launcher ??
                throw new ArgumentNullException(nameof(launcher));
        }

        public bool TryOpen(
            FileOpenRequest request,
            out string error)
        {
            error = null;

            if (request == null)
            {
                error = "No file-open request was provided.";
                return false;
            }

            if (request.File == null)
            {
                error = "The selected file has no filesystem definition.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(request.UserId))
            {
                error = "The file-open request has no user ID.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(request.File.Id))
            {
                error = "The selected file has no file ID.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(request.FilePath))
            {
                error = "The selected file has no filesystem path.";
                return false;
            }

            if (request.File.Hidden)
            {
                error = "The selected file is hidden.";
                return false;
            }

            if (!associations.TryGetAppId(
                request.File.FileType,
                out string appId,
                out error))
            {
                return false;
            }

            if (!launcher.TryOpenFile(
                appId,
                request,
                out error))
            {
                if (string.IsNullOrWhiteSpace(error))
                {
                    error =
                        $"App '{appId}' could not open " +
                        $"'{request.File.Name}'.";
                }

                return false;
            }

            return true;
        }
    }
}