using DokanNet;
using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Linq.Expressions;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace DokanLab
{
    public class MyFs : IDokanOperations
    {
        // Dictionary which maps directory path to List of FileInformation
        Dictionary<string, List<FileInformation>> fileInfoListByDirPath = new()
        {
            { "\\", new List<FileInformation>() {
                    new FileInformation() {
                        Attributes = FileAttributes.Directory,
                        FileName = "input" },
                    new FileInformation() {
                        Attributes = FileAttributes.Directory,
                        FileName = "output" },
                }
            },
            { "\\input", new List<FileInformation>()
                {
                    new FileInformation() { FileName = "input.txt" }
                }
            },
            { "\\output", new List<FileInformation>()
                {
                    new FileInformation() { FileName = "output.txt" }
                }
            },
        };

        public NtStatus CreateFile(string fileName, DokanNet.FileAccess access, FileShare share, FileMode mode, FileOptions options, FileAttributes attributes, IDokanFileInfo info)
        {
            return DokanResult.Success;
        }

        public void Cleanup(string fileName, IDokanFileInfo info)
        {

        }

        public void CloseFile(string fileName, IDokanFileInfo info)
        {

        }

        // Encode "Hello, world" in buffer.
        byte[] messageBytes = Encoding.UTF8.GetBytes("Hello world");

        public NtStatus ReadFile(string fileName, byte[] buffer, out int bytesRead, long offset, IDokanFileInfo info)
        {
            if (fileName.EndsWith("input.txt"))
            {
                bytesRead = Math.Min(buffer.Length, messageBytes.Length);
                Buffer.BlockCopy(messageBytes, (int)offset, buffer, 0, bytesRead);
            }
            else
            {
                bytesRead = 0;
            }
            return DokanResult.Success;
        }

        public NtStatus WriteFile(string fileName, byte[] buffer, out int bytesWritten, long offset, IDokanFileInfo info)
        {
            bytesWritten = 0;
            return DokanResult.NotImplemented;
        }

        public NtStatus FlushFileBuffers(string fileName, IDokanFileInfo info)
        {
            throw new NotImplementedException();
        }

        public NtStatus GetFileInformation(string fileName, out FileInformation fileInfo, IDokanFileInfo info)
        {
            if (fileInfoListByDirPath.ContainsKey(fileName))
            {
                fileInfo = new FileInformation()
                {
                    FileName = fileName,
                    Attributes = FileAttributes.Directory
                };
            }
            else
            {
                if (fileName.EndsWith("input.txt"))
                {
                    fileInfo = new FileInformation()
                    {
                        FileName = fileName,
                        Attributes = FileAttributes.Normal,
                        Length = messageBytes.Length
                    };
                }
                else
                {
                    fileInfo = new FileInformation()
                    {
                        FileName = fileName,
                        Attributes = FileAttributes.Normal,
                        Length = 0
                    };
                }
            }

            return DokanResult.Success;
        }

        public NtStatus FindFiles(string fileName, out IList<FileInformation> files, IDokanFileInfo info)
        {
            bool isDirectoryFound =
                fileInfoListByDirPath.TryGetValue(
                    fileName, out List<FileInformation>? foundFiles);

            if (isDirectoryFound && foundFiles != null)
            {
                files = foundFiles;
            }
            else
            {
                files = Array.Empty<FileInformation>();
            }

            return DokanResult.Success;
        }

        public NtStatus FindFilesWithPattern(string fileName, string searchPattern, out IList<FileInformation> files, IDokanFileInfo info)
        {
            files = Array.Empty<FileInformation>();
            return DokanResult.NotImplemented;
        }

        public NtStatus SetFileAttributes(string fileName, FileAttributes attributes, IDokanFileInfo info)
        {
            throw new NotImplementedException();
        }

        public NtStatus SetFileTime(string fileName, DateTime? creationTime, DateTime? lastAccessTime, DateTime? lastWriteTime, IDokanFileInfo info)
        {
            throw new NotImplementedException();
        }

        public NtStatus DeleteFile(string fileName, IDokanFileInfo info)
        {
            throw new NotImplementedException();
        }

        public NtStatus DeleteDirectory(string fileName, IDokanFileInfo info)
        {
            throw new NotImplementedException();
        }

        public NtStatus MoveFile(string oldName, string newName, bool replace, IDokanFileInfo info)
        {
            throw new NotImplementedException();
        }

        public NtStatus SetEndOfFile(string fileName, long length, IDokanFileInfo info)
        {
            throw new NotImplementedException();
        }

        public NtStatus SetAllocationSize(string fileName, long length, IDokanFileInfo info)
        {
            throw new NotImplementedException();
        }

        public NtStatus LockFile(string fileName, long offset, long length, IDokanFileInfo info)
        {
            throw new NotImplementedException();
        }

        public NtStatus UnlockFile(string fileName, long offset, long length, IDokanFileInfo info)
        {
            throw new NotImplementedException();
        }

        public NtStatus GetDiskFreeSpace(out long freeBytesAvailable, out long totalNumberOfBytes, out long totalNumberOfFreeBytes, IDokanFileInfo info)
        {
            freeBytesAvailable = 0;
            totalNumberOfBytes = 0;
            totalNumberOfFreeBytes = 0;
            return DokanResult.Success;
        }

        public NtStatus GetVolumeInformation(out string volumeLabel, out FileSystemFeatures features, out string fileSystemName, out uint maximumComponentLength, IDokanFileInfo info)
        {
            volumeLabel = "Moj drajv";
            fileSystemName = "MojFS";
            maximumComponentLength = 256;

            features = FileSystemFeatures.None;

            return NtStatus.Success;
        }

        public NtStatus GetFileSecurity(string fileName, out FileSystemSecurity security, AccessControlSections sections, IDokanFileInfo info)
        {
            if (info.IsDirectory)
            {
                DirectorySecurity directorySecurity = new();
                directorySecurity.AddAccessRule(
                    new FileSystemAccessRule(
                        Environment.UserName,
                        FileSystemRights.FullControl,
                        AccessControlType.Allow));
                security = directorySecurity;
            }
            else
            {
                FileSecurity fileSecurity = new();
                fileSecurity.AddAccessRule(
                    new FileSystemAccessRule(
                        Environment.UserName,
                        FileSystemRights.FullControl,
                        AccessControlType.Allow));
                security = fileSecurity;
            }
            return DokanResult.Success;
        }

        public NtStatus SetFileSecurity(string fileName, FileSystemSecurity security, AccessControlSections sections, IDokanFileInfo info)
        {
            throw new NotImplementedException();
        }

        public NtStatus Mounted(string mountPoint, IDokanFileInfo info)
        {
            return DokanResult.Success;
        }

        public NtStatus Unmounted(IDokanFileInfo info)
        {
            return DokanResult.Success;
        }

        public NtStatus FindStreams(string fileName, out IList<FileInformation> streams, IDokanFileInfo info)
        {
            throw new NotImplementedException();
        }
    }
}
