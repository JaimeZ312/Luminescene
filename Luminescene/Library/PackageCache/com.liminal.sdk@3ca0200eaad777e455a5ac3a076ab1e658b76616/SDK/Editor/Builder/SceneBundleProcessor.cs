using System;
using System.IO;
using System.Text;

namespace Liminal.SDK.Editor.Build
{
    internal class SceneBundleProcessor
    {
        private readonly byte[] mProjectAsmNameBytes;
        private readonly byte[] mReplaceAsmNameBytes;
        
        public SceneBundleProcessor(string projectAsmName, string replaceAsmName)
        {
            // TODO: I'd prefer a better solution than this - asm name should be able to be anything - ideally this would
            // be done before the bundles are built so we don't have to modify it, but there's no way to change the MonoScript
            // assembly references without losing the data on the components after the bundle is built
            if (replaceAsmName.Length > projectAsmName.Length)
                throw new ArgumentException("Replacement name cannot be longer than original project name.");

            // Setup find/replace constants
            mProjectAsmNameBytes = Encoding.UTF8.GetBytes(projectAsmName + ".dll");
            mReplaceAsmNameBytes = Encoding.UTF8.GetBytes(replaceAsmName + ".dll");

            // Ensure replacement is the same length as the original value
            Array.Resize(ref mReplaceAsmNameBytes, mProjectAsmNameBytes.Length);
        }

        /// <summary>
        /// Processes the scene asset bundle at the specified path.
        /// </summary>
        /// <param name="assetBundlPath">The path to the bundle.</param>
        public void Process(string assetBundlPath)
        {
            if (!File.Exists(assetBundlPath))
                throw new FileNotFoundException("Input file not found", assetBundlPath);

            // Open the input file and create a temporary file for the post-processed scene
            var tempPath = assetBundlPath + ".tmp";
            using (var inStream = File.OpenRead(assetBundlPath))
            using (var outStream = File.Create(tempPath))
            {
                BinaryReplace(inStream, mProjectAsmNameBytes, outStream, mReplaceAsmNameBytes);
            }
            
            // Delete the original and rename the processed scene
            File.Delete(assetBundlPath);
            File.Move(tempPath, assetBundlPath);
        }
        
        /// <summary>
        /// Finds a sequence of bytes within an input stream and replaces the values in the target stream.
        /// </summary>
        /// <param name="sourceStream">The stream to read from</param>
        /// <param name="sourceSeq">The sequence of bytes to find</param>
        /// <param name="targetStream">The stream to write to</param>
        /// <param name="targetSeq">The sequence of bytes to replace</param>
        private void BinaryReplace(FileStream sourceStream, byte[] sourceSeq, FileStream targetStream, byte[] targetSeq)
        {
            int b;
            long foundSeqOffset = -1;
            int searchByteCursor = 0;

            while ((b = sourceStream.ReadByte()) != -1)
            {
                if (sourceSeq[searchByteCursor] == b)
                {
                    if (searchByteCursor == sourceSeq.Length - 1)
                    {
                        targetStream.Write(targetSeq, 0, targetSeq.Length);
                        searchByteCursor = 0;
                        foundSeqOffset = -1;
                    }
                    else
                    {
                        if (searchByteCursor == 0)
                        {
                            foundSeqOffset = sourceStream.Position - 1;
                        }

                        ++searchByteCursor;
                    }
                }
                else
                {
                    if (searchByteCursor == 0)
                    {
                        targetStream.WriteByte((byte)b);
                    }
                    else
                    {
                        targetStream.WriteByte(sourceSeq[0]);
                        sourceStream.Position = foundSeqOffset + 1;
                        searchByteCursor = 0;
                        foundSeqOffset = -1;
                    }
                }
            }
        }
    }
}
