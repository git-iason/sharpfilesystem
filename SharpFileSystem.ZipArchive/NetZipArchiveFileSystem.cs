﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Compression;
using SharpFileSystem.IO;

namespace SharpFileSystem.SharpZipArchive
{
    public class NetZipArchiveFileSystem : IFileSystem
    {
        public ZipArchive ZipArchive { get; private set; }
        
        public static NetZipArchiveFileSystem Open(Stream s)
        { 
            return new NetZipArchiveFileSystem(new ZipArchive(s,ZipArchiveMode.Update,true));
        }

        public static NetZipArchiveFileSystem Create(Stream s)
        {
            return new NetZipArchiveFileSystem(new ZipArchive(s, ZipArchiveMode.Create, true));
        }

        private NetZipArchiveFileSystem(ZipArchive archive)
        {
            ZipArchive = archive;
        }
        public void Dispose()
        {
            ZipArchive.Dispose();
        }

        protected IEnumerable<ZipArchiveEntry> GetZipEntries()
        {
            return ZipArchive.Entries;
        }
        protected FileSystemPath ToPath(ZipArchiveEntry entry)
        {
            return FileSystemPath.Parse(FileSystemPath.DirectorySeparator + entry.FullName);
        }
        protected string ToEntryPath(FileSystemPath path)
        {
            // Remove heading '/' from path.
            return path.Path.TrimStart(FileSystemPath.DirectorySeparator);
        }

        protected ZipArchiveEntry ToEntry(FileSystemPath path)
        {
            return ZipArchive.GetEntry(ToEntryPath(path));
        }
        public ICollection<FileSystemPath> GetEntities(FileSystemPath path)
        {
            return GetZipEntries().Select(ToPath).Where(path.IsParentOf)
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

            //foreach (var zipArchiveEntry in GetZipEntries())
            //{
            //    var p = ToPath(zipArchiveEntry);
            //    if(p.IsChildOf(path) )
            //        return true;
            //}
            //return false;
        }

        public Stream CreateFile(FileSystemPath path)
        {
            return CreateFile(path,null);
        }

        public Stream CreateFile(FileSystemPath path, byte[] data)
        {
            var zae = ZipArchive.CreateEntry(ToEntryPath(path));
            var stream = zae.Open();

            if (data != null)
            {
                stream.Write(data);
                stream.Close();
            }

            return zae.Open();
        }
        public Stream OpenFile(FileSystemPath path, FileAccess access)
        {
            var zae = ZipArchive.GetEntry(ToEntryPath(path));
            return zae.Open();
        }

        public void CreateDirectory(FileSystemPath path)
        {
            ZipArchive.CreateEntry(ToEntryPath(path));
        }

        public void Delete(FileSystemPath path)
        {
            var zae = ZipArchive.GetEntry(ToEntryPath(path));
            zae.Delete();
        }
    }
}
