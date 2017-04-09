using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ICSharpCode.SharpZipLib.Zip;
using SharpFileSystem.FileSystems;

namespace SharpFileSystem.SharpZipLib
{
    public class SharpZipLibFileSystem: IFileSystem
    {
        public ZipFile ZipFile { get; set; }

        public static SharpZipLibFileSystem Open(Stream s)
        {
            var zip = new ZipFile(s) {UseZip64 = UseZip64.Off};
            return new SharpZipLibFileSystem(zip);
        }

        public static SharpZipLibFileSystem Create(Stream s)
        {
            var zip = ZipFile.Create(s);
            zip.UseZip64 = UseZip64.Off;
            return new SharpZipLibFileSystem(zip);
        }

        private SharpZipLibFileSystem(ZipFile zipFile)
        {
            ZipFile = zipFile;
        }

        public void Dispose()
        {
            if (ZipFile.IsUpdating)
                ZipFile.CommitUpdate();
            ZipFile.Close();
        }

        protected FileSystemPath ToPath(ZipEntry entry)
        {
            return FileSystemPath.Parse(FileSystemPath.DirectorySeparator + entry.Name);
        }

        protected string ToEntryPath(FileSystemPath path)
        {
            // Remove heading '/' from path.
            return path.Path.TrimStart(FileSystemPath.DirectorySeparator);
        }

        protected ZipEntry ToEntry(FileSystemPath path)
        {
            return ZipFile.GetEntry(ToEntryPath(path));
        }

        protected IEnumerable<ZipEntry> GetZipEntries()
        {
            return ZipFile.Cast<ZipEntry>();
        }

        public ICollection<FileSystemPath> GetEntities(FileSystemPath path)
        {
            return GetZipEntries()
                .Select(ToPath)
                .Where(entryPath => path.IsParentOf(entryPath))
                .Select(entryPath => entryPath.ParentPath == path
                    ? entryPath
                    : path.AppendDirectory(entryPath.RemoveParent(path).GetDirectorySegments()[0])
                    )
                .Distinct()
                .ToList();
        }

        public bool Exists(FileSystemPath path)
        {
            if (path.IsFile)
                return ToEntry(path) != null;
            return GetZipEntries()
                .Select(ToPath)
                .Any(entryPath => entryPath.IsChildOf(path));
        }

        public Stream CreateFile(FileSystemPath path)
        {
            //BeginUpdate is required before adding entries to the zip file
            ZipFile.BeginUpdate();

            var entry = new MemoryZipEntry();
            ZipFile.Add(entry, ToEntryPath(path));
             return PrepareStream(entry.GetSource());
           // return entry.GetSource();
        }

        private SeekStream PrepareStream(Stream entry)
        {
            var stream = new SeekStream(entry);
            stream.Seek(0, SeekOrigin.End);
            //seek back to the beginning so the stream is ready to be read from or written to
            stream.Seek(0, SeekOrigin.Begin);

            stream.DataWritten += FinishUpdate;
            return stream;
        }

        public void FinishUpdate()
        {
            if (ZipFile.IsUpdating)
                ZipFile.CommitUpdate();
        }
        public Stream OpenFile(FileSystemPath path, FileAccess access)
        {
            if (access == FileAccess.Write || access==FileAccess.ReadWrite)
                ZipFile.BeginUpdate();
           
            return PrepareStream(ZipFile.GetInputStream(ToEntry(path)));
        }

        public void CreateDirectory(FileSystemPath path)
        {
            ZipFile.AddDirectory(ToEntryPath(path));
        }

        public void Delete(FileSystemPath path)
        {
            ZipFile.Delete(ToEntryPath(path));
        }

        public class MemoryZipEntry: MemoryFileSystem.MemoryFile, IStaticDataSource
        {
            public Stream GetSource()
            {
                return new MemoryFileSystem.MemoryFileStream(this);
            }
        }
    }
}
