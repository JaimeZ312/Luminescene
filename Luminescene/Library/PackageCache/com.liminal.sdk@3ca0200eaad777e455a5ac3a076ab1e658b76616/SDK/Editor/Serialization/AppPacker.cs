using Liminal.SDK.Serialization;
using SevenZip.Compression.LZMA;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using UnityEngine;

namespace Liminal.SDK.Editor.Serialization
{
    /// <summary>
    /// Packs a Liminal App asychronously.
    /// </summary>
    public class AppPacker
    {
        private AppPack mPack;
        private string mOutputPath;
        private Thread mThread;

        private volatile bool mRunning;
        private volatile bool mDone;
        private volatile bool mFaulted;

        #region Properties

        /// <summary>
        /// Indicates if the packing operation has completed.
        /// </summary>
        public bool IsDone
        {
            get { return !mRunning && mDone; }
        }

        /// <summary>
        /// Indicates if the unpacking operation faulted with an exception.
        /// </summary>
        public bool IsFaulted
        {
            get { return !mRunning && mFaulted; }
        }

        /// <summary>
        /// Gets the exception that was thrown if the unpacking operation faulted.
        /// </summary>
        public Exception Exception { get; private set; }

        #endregion

        /// <summary>
        /// Packs an AppPack object into a single, compressed file.
        /// </summary>
        /// <param name="pack">The AppPack to pack.</param>
        /// <param name="outputPath">The output path.</param>
        public AppPacker PackAsync(AppPack pack, string outputPath)
        {
            if (mRunning)
                throw new InvalidOperationException("An unpack operation is already in progress.");

            if (mThread != null)
            {
                mThread.Join();
                mThread = null;
            }

            mRunning = true;
            ResetState();
            mPack = pack;
            mOutputPath = outputPath;

            mThread = new Thread(DoPack)
            {
                IsBackground = true
            };
            mThread.Start();

            return this;
        }

        /// <summary>
        /// Wait for the packing process to complete.
        /// </summary>
        public void Wait()
        {
            if (!mRunning)
                return;

            if (mThread != null)
            {
                mThread.Join();
                mThread = null;
            }
        }

        private void ResetState()
        {
            mDone = false;
            mFaulted = false;
            mPack = null;
            Exception = null;
        }

        private void DoPack()
        {
            var tmpFile = Path.GetTempFileName();
            using (var stream = File.OpenWrite(tmpFile))
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(AppPack.Version);
                writer.Write(AppPack.Identifier);
                writer.Write((ushort)mPack.TargetPlatform);
                writer.Write((uint)mPack.ApplicationId);
                writer.Write((uint)mPack.ApplicationVersion);

                var assemblies = (mPack.Assemblies != null)
                    ? mPack.Assemblies.Where(x => x != null)
                    : null;

                if (assemblies == null || !assemblies.Any())
                {
                    // No assemblies
                    writer.Write((byte)0);
                }
                else
                {
                    writer.Write((byte)assemblies.Count());
                    foreach (var asm in assemblies)
                    {
                        writer.Write(asm.Length);
                        writer.Write(asm);
                    }
                }

                writer.Write(mPack.SceneBundle.Length);
                writer.Write(mPack.SceneBundle);
            }

            // Compress
            using (var inStream = File.OpenRead(tmpFile))
            using (var outStream = File.Create(mOutputPath))
            {
                switch (mPack.CompressionType)
                {
                    case ECompressionType.LMZA:
                        SevenZipHelper.Compress(inStream, outStream);
                        break;
                    case ECompressionType.Uncompressed:
                        inStream.CopyTo(outStream);
                        break;
                    default:
                        SevenZipHelper.Compress(inStream, outStream);
                        break;
                }
            }

            // Delete the temoprary file if it's still there...
            if (File.Exists(tmpFile))
                File.Delete(tmpFile);
        }
    }
}
