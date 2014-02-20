using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Chutzpah.Wrappers
{
    public class FileSystemWrapper : IFileSystemWrapper
    {
        public string GetRandomFileName()
        {
            return Path.GetRandomFileName();
        }

        public string GetTemporaryFolder(string leaf)
        {
            var folder = Path.GetTempPath();
            var tempFolder = Path.Combine(folder, leaf);
            Directory.CreateDirectory(tempFolder);
            return tempFolder;
        }

        public void MoveFile(string sourceFilename, string destFilename)
        {
            File.Move(sourceFilename, destFilename);
        }

        public void CopyFile(string sourceFilename, string destFilename, bool overwrite=true)
        {
            File.Copy(sourceFilename, destFilename, overwrite);
        }

        public void SetFileAttributes(string fileName, FileAttributes attributes)
        {
            File.SetAttributes(fileName, attributes);
        }

        public void MoveDirectory(string sourceDirectory, string destDirectory)
        {
            if (!sourceDirectory.Equals(destDirectory, StringComparison.OrdinalIgnoreCase))
            {
                Directory.Move(sourceDirectory, destDirectory);
            }
        }

        public DateTime GetCreationTime(string path)
        {
            return File.GetCreationTime(path);
        }

        public DateTime GetLastWriteTime(string path)
        {
            return File.GetLastWriteTime(path);
        }

        public DateTime GetLastAccessTime(string path)
        {
            return File.GetLastAccessTime(path);
        }

        public bool FileExists(string path)
        {
            return File.Exists(path);
        }

        public bool FolderExists(string path)
        {
            return Directory.Exists(path);
        }

        public void DeleteFile(string path)
        {
            if (FileExists(path))
                File.Delete(path);
        }

        public void WriteAllText(string path, string text)
        {
            File.WriteAllText(path, text);
        }

        public void DeleteDirectory(string path, bool recursive)
        {
            if (FolderExists(path))
                Directory.Delete(path, recursive);
        }

        public string GetDirectoryName(string path)
        {
            return Path.GetDirectoryName(path);
        }

        public void CreateDirectory(string path)
        {
            Directory.CreateDirectory(path);
        }

        public string GetFullPath(string path)
        {
            return Path.GetFullPath(path);
        }

        public IEnumerable<string> GetDirectories(string directory)
        {
            return Directory.GetDirectories(directory);
        }

        public string[] GetFiles(string path, string searchPattern, SearchOption searchOption)
        {
            return Directory.GetFiles(path, searchPattern, searchOption);
        }

        public string GetFileName(string path)
        {
            return Path.GetFileName(path);
        }

        public Stream Open(string path)
        {
            return File.Open(path, FileMode.OpenOrCreate);
        }

        public Stream Open(string path, FileMode mode, FileAccess access)
        {
            return new FileStream(path, mode, access);
        }

        public void Save(string path, Stream stream)
        {
            using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write))
            {
                byte[] data = GetContent(stream);
                fs.Write(data, 0, data.Length);
            }
        }

        public byte[] GetContent(Stream stream)
        {
            byte[] data = new byte[stream.Length];
            int remaining = data.Length;
            int offset = 0;

            while (remaining > 0)
            {
                int read = stream.Read(data, offset, remaining);
                remaining -= read;
                offset += read;
            }

            stream.Position = 0;

            return data;
        }

        public string GetText(string path)
        {
            return Retry(() => File.ReadAllText(path));
        }

        public string[] GetLines(string path)
        {
            return File.ReadAllLines(path);
        }

        public void Save(string path, string contents)
        {
            File.WriteAllText(path,contents);
        }

        private T Retry<T>(Func<T> action, int maxCount = 3)
        {
            var count = 0;
            while (count++ < maxCount)
            {
                try
                {
                    return action();
                }
                catch(IOException)
                {
                    if (count >= maxCount) throw;
                }
            }

            throw new IOException("Unable to perform IO operation");
        }

    }
}