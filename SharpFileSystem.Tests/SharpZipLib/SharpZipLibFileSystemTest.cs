using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using ICSharpCode.SharpZipLib.Zip;
using NUnit.Framework;
using SharpFileSystem.IO;
using SharpFileSystem.SharpZipLib;

namespace SharpFileSystem.Tests.SharpZipLib
{
    [TestFixture]
    public class SharpZipLibFileSystemTest
    {
        private Stream zipStream;
        private SharpZipLibFileSystem fileSystem;

        [OneTimeSetUp]
        public void Initialize()
        {
            var memoryStream = new MemoryStream();
            zipStream = memoryStream;
            var zipOutput = new ZipOutputStream(zipStream);
            zipOutput.UseZip64 = UseZip64.Off;
            var fileContentString = "this is a file";
            var fileContentBytes = Encoding.ASCII.GetBytes(fileContentString);
            zipOutput.PutNextEntry(new ZipEntry("textfileA.txt")
            {
                Size = fileContentBytes.Length
            });
            zipOutput.Write(fileContentBytes);
            zipOutput.PutNextEntry(new ZipEntry("directory/fileInDirectory.txt"));
            zipOutput.Finish();
            memoryStream.Position = 0;
            fileSystem = SharpZipLibFileSystem.Open(zipStream);
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            fileSystem.Dispose();
            zipStream.Dispose();
        }

        private readonly FileSystemPath directoryPath = FileSystemPath.Parse("/directory/");
        private readonly FileSystemPath textfileAPath = FileSystemPath.Parse("/textfileA.txt");
        private readonly FileSystemPath fileInDirectoryPath = FileSystemPath.Parse("/directory/fileInDirectory.txt");

        [Test]
        public void GetEntitiesOfRootTest()
        {
            CollectionAssert.AreEquivalent(new[]
            {
                textfileAPath,
                directoryPath
            }, fileSystem.GetEntities(FileSystemPath.Root).ToArray());
        }

        [Test]
        public void GetEntitiesOfDirectoryTest()
        {
            CollectionAssert.AreEquivalent(new[]
            {
                fileInDirectoryPath
            }, fileSystem.GetEntities(directoryPath).ToArray());
        }

        [Test]
        public void ExistsTest()
        {
            Assert.IsTrue(fileSystem.Exists(FileSystemPath.Root));
            Assert.IsTrue(fileSystem.Exists(textfileAPath));
            Assert.IsTrue(fileSystem.Exists(directoryPath));
            Assert.IsTrue(fileSystem.Exists(fileInDirectoryPath));
            Assert.IsFalse(fileSystem.Exists(FileSystemPath.Parse("/nonExistingFile")));
            Assert.IsFalse(fileSystem.Exists(FileSystemPath.Parse("/nonExistingDirectory/")));
            Assert.IsFalse(fileSystem.Exists(FileSystemPath.Parse("/directory/nonExistingFileInDirectory")));
        }

        [Test]
        public void EmbeddedZipTest()
        {
            var memoryStream = new MemoryStream();
            zipStream = memoryStream;
            var zipOutput = new ZipOutputStream(zipStream);

            var fileContentString = "this is a file";
            var fileContentBytes = Encoding.ASCII.GetBytes(fileContentString);
            zipOutput.PutNextEntry(new ZipEntry("textfileA.txt")
            {
                Size = fileContentBytes.Length
            });
            zipOutput.Write(fileContentBytes);
            zipOutput.PutNextEntry(new ZipEntry("directory/fileInDirectory.txt"));
            zipOutput.Finish();
            memoryStream.Position = 0;

            var zipData = zipStream.ReadAllBytes();
            var fs = fileSystem.CreateFile(FileSystemPath.Parse("/internalZip.zip"));
            fs.Write(zipData);
            fs.Close();

            Assert.IsTrue(fileSystem.Exists(FileSystemPath.Parse("/internalZip.zip")));


            var zip = fileSystem.OpenFile(FileSystemPath.Parse("/internalZip.zip"), FileAccess.Read);

           Assert.IsTrue(zip.Length>0);
            var newFs = SharpZipLibFileSystem.Open(zip);

            var textFile = newFs.OpenFile(textfileAPath, FileAccess.Read);
            var text = textFile.ReadAllText();
            Assert.IsTrue(string.Equals(text,fileContentString));
        }

        [Test]
        public void MultiWriteMultiReadTest()
        {
            var fileA = FileSystemPath.Parse("/file.txt");
            var fileB = FileSystemPath.Parse("/fileB.txt");
            var fileC = FileSystemPath.Parse("/fileC.txt");

            var fileContentStringA = "this is a file";
            var fileContentStringB = "this is b file";
            var fileContentStringC = "this is c file";
            var fileContentBytes = Encoding.ASCII.GetBytes(fileContentStringA);
            var fileContentBytesB = Encoding.ASCII.GetBytes(fileContentStringB);
            var fileContentBytesC = Encoding.ASCII.GetBytes(fileContentStringC);

            var fs = fileSystem.CreateFile(fileA);
            fs.Write(fileContentBytes);
            fs.Close();


            fs = fileSystem.CreateFile(fileB);
            fs.Write(fileContentBytesB);
            fs.Close();

            Assert.IsTrue(fileSystem.Exists(fileB));

            var textFile = fileSystem.OpenFile(fileB, FileAccess.Read);
            var text = textFile.ReadAllText();
            Assert.IsTrue(string.Equals(text, fileContentStringB));


            Assert.IsTrue(fileSystem.Exists(fileA));

            textFile = fileSystem.OpenFile(fileA, FileAccess.ReadWrite);
            text = textFile.ReadAllText();
            Assert.IsTrue(string.Equals(text, fileContentStringA));

            textFile.Write(fileContentBytesC);
            textFile.Close();

            textFile = fileSystem.OpenFile(fileA, FileAccess.ReadWrite);
            text = textFile.ReadAllText();
            Assert.IsTrue(string.Equals(text, fileContentStringC));
        }
    }
}
