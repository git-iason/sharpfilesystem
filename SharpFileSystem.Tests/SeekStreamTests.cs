using System.IO;
using NUnit.Framework;
using SharpFileSystem.SharpZipLib;

namespace SharpFileSystem.Tests
{
    /// <summary>
    /// a small test for <see cref="SeekStream" />
    /// </summary>
    [TestFixture]
    public class SeekStreamTests
    {
        [Test]
        public void ProcessTestFiles()
        {
            var stream1 = new MemoryStream(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 });
            var stream2 = new MemoryStream(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 });

            var seekStream = new global::SharpFileSystem.SharpZipLib.SeekStream(stream1);

            Assert.AreEqual(seekStream.ReadByte(), stream2.ReadByte());

            seekStream.Seek(3, SeekOrigin.Begin);
            stream2.Seek(3, SeekOrigin.Begin);
            Assert.AreEqual(seekStream.ReadByte(), stream2.ReadByte());

            seekStream.Seek(3, SeekOrigin.Current);
            stream2.Seek(3, SeekOrigin.Current);
            Assert.AreEqual(seekStream.ReadByte(), stream2.ReadByte());

            seekStream.Seek(-2, SeekOrigin.Current);
            stream2.Seek(-2, SeekOrigin.Current);
            Assert.AreEqual(seekStream.ReadByte(), stream2.ReadByte());

            seekStream.Seek(-2, SeekOrigin.End);
            stream2.Seek(-2, SeekOrigin.End);
            Assert.AreEqual(seekStream.ReadByte(), stream2.ReadByte());
        }
    }
}
