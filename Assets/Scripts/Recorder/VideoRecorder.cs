using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Unity.Collections.LowLevel.Unsafe;
using System.Text;
using Unity.Collections;
using System.Runtime.InteropServices;

namespace ota.ndi
{
    /// <summary>
    /// Record video file with timeline metadata.
    /// </summary>
    public sealed class VideoRecorder : IDisposable
    {
        private readonly RecordingTimeManager _timeManager;

        public readonly int targetFrameRate;

        private RenderTexture _source = null;
        private RenderTexture _buffer;
        private uint _frameCount = 0;

        private string _metadata;

        public bool IsRecording { get; private set; }

        public bool FixedFrameRate { get; set; } = true;

        public VideoRecorder(RenderTexture source, int targetFrameRate)
        {
            _source = source;
            _buffer = new RenderTexture(source.width, source.height, 0);
            // _buffer = new RenderTexture(source.width, source.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linea);

            this.targetFrameRate = targetFrameRate;
            _timeManager = new RecordingTimeManager(targetFrameRate);
        }

        public void Dispose()
        {
            if (IsRecording)
            {
                EndRecording();
            }
            UnityEngine.Object.Destroy(_buffer);
        }

        /// <summary>
        /// Update metadata and record frame.
        /// </summary>
        public void Update(string metadata)
        {
            if (!IsRecording) { return; }
            _metadata = metadata;
            Graphics.Blit(_source, _buffer);
            AsyncGPUReadback.Request(_buffer, 0, OnSourceReadback);

        }

        /// <summary>
        /// On iOS, warming up at the first time recording is recommended as it takes time.
        /// </summary>
        public void WarmUp()
        {
            var path = GetTemporaryFilePath();
            Avfi.StartRecording(path, _source.width, _source.height);
            Avfi.EndRecording(false);
        }

        public void StartRecording()
        {
            //var path = GetSelectedFilePath();
            var bookmark = GetSecurityScopeBookmark();
            int size = Marshal.SizeOf(bookmark[0]) * bookmark.Length;
            //IntPtr unmanagedPnt = Marshal.AllocHGlobal(size);
            //Marshal.Copy(bookmark, 0, unmanagedPnt, bookmark.Length);
            //Avfi.StartRecording(path, _source.width, _source.height);
             _timeManager.Clear();
            Avfi.StartRecordingUsingBookmark(bookmark, size, _source.width, _source.height);
            //Marshal.FreeHGlobal(unmanagedPnt);
            IsRecording = true;
            _frameCount = 0;
        }

        public void EndRecording()
        {
            AsyncGPUReadback.WaitAllRequests();
            Avfi.EndRecording(true);
            IsRecording = false;
        }

        private static string GetTemporaryFilePath()
        {
            string dir = Application.platform == RuntimePlatform.IPhonePlayer
                ? Application.temporaryCachePath : ".";
            return $"{dir}/{GetFileName()}";
        }

        private static string GetSelectedFilePath()
        {
            var scopePath = NativeDirPickerlib.getSecurityScopeURL();
            return $"{scopePath}{GetFileName()}".Replace("file://", "");
        }        

        private static string GetFileName()
        {
            string sceneName = SceneManager.GetActiveScene().name;
            return $"Record_{sceneName}_{DateTime.Now:MMdd_HHmm_ss}.mp4";
        }        

        private static byte[] GetSecurityScopeBookmark() {
            var size = NativeDirPickerlib.getSecurityScopeBookmark(out IntPtr unmanagedPtr);
            byte[] managedData = new byte[size];
            Marshal.Copy(unmanagedPtr, managedData, 0, size);
            // for (int i = 0; i < size; i++)
            //     Debug.Log(string.Format("managedData[{0}]:{1}", i, mangedData[i]));
            Marshal.FreeHGlobal(unmanagedPtr);
            return managedData;
        }

        private unsafe void OnSourceReadback(AsyncGPUReadbackRequest request)
        {
            if (!IsRecording)
            {
                return;
            }
            double time = _timeManager.getTime(Encoding.UTF8.GetBytes(_metadata));
            // Override time as Unity 2022.2.1f1 doesn't support VFR video playback
            // https://issuetracker.unity3d.com/issues/video-created-with-avfoundation-framework-is-not-played-when-entering-the-play-mode
            if (FixedFrameRate)
            {
                time = _frameCount * (1.0 / targetFrameRate);
            } else {
                if (_timeManager.isSameFrame(time))
                {
                    return;
                }
                time = _timeManager.getTime(Encoding.UTF8.GetBytes(_metadata));
            }

            // Get pixel buffer
            using var pixelData = request.GetData<byte>(0);
            var pixelPtr = NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(pixelData);

            // Get metadata buffer
            var encoding = Encoding.GetEncoding("UTF-8");
            var data = encoding.GetBytes(_metadata);
            NativeArray<byte> metadataarray = new NativeArray<byte>(data, Allocator.Persistent);
            var metadataPtr = NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(metadataarray);
            //var metadataPtr = NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(metadata);


            //Avfi.AppendFrame(pixelPtr, (uint)pixelData.Length, metadataPtr, (uint)metadata.Length, time);
            Avfi.AppendFrame(pixelPtr, (uint)pixelData.Length, metadataPtr, (uint)metadataarray.Length, time);            
            metadataarray.Dispose();

            _frameCount++;
        }

    }

}
