using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using TMPro;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace ota.ndi
{

    public class MP4Recorder : MonoBehaviour
    {
        [SerializeField] private ComputeShader _encodeCompute;

        [SerializeField] private bool _enableAlpha = false;

        [SerializeField] private int _frameRateNumerator = 30000;

        [SerializeField] private int _frameRateDenominator = 1001;

        [SerializeField] private Camera _arcamera;

        [SerializeField] private RawImage _depthPreview;

        [SerializeField] private RawImage _stencilPreview;

        [SerializeField] private TextMeshProUGUI _resolutionInfoText;

        [SerializeField] private TextMeshProUGUI _statusInfoText;

        [SerializeField] private Shader _shader;

        [SerializeField]
        [Tooltip("The AROcclusionManager which will produce depth textures.")]
        AROcclusionManager m_OcclusionManager;

        [SerializeField]
        [Tooltip("The ARCameraManager which will produce frame events.")]
        ARCameraManager m_CameraManager;

        [Space]

        [SerializeField] float _minDepth = 0.2f;

        [SerializeField] float _maxDepth = 3.2f;

        [Space]

        [SerializeField] OtavjMeshManager _otavjMeshManager;

        [SerializeField] InputHandler _inputHandler;

        private IntPtr _sendInstance;

        private FormatConverter _formatConverter;

        private int _width;

        private int _height;

        private NativeArray<byte>? _nativeArray;

        private byte[] _bytes;

        private Texture2D _sourceOriginTexture;

        private Texture2D _sourceDepthTexture;

        private Texture2D _sourceStencilTexture;

        private float _textureAspectRatio = 1.0f;

        private Material _muxMaterial;

        private RenderTexture _senderRT;

        private Matrix4x4 _projection;

        private OtaFrameMetadata _metadata;

        private String _metadataStr;

        private String _metadataStr4bg;

        private BgMetadataManager _bgmetadatamanager;

        public Options recordingOptions;

        [System.Serializable]
        public struct Options
        {
            [Min(640)]
            public int width;
            [Min(480)]
            public int height;
            [Range(10, 60)]
            public int targetFrameRate;
        }

        public VideoRecorder recorder;

        void Awake() {
            //とりあえず決め打ちで初期化
            recordingOptions = new Options
            {
                width = 1920,
                height = 1440,
                targetFrameRate = 60,
            };
        }

        void Start()
        {
            _formatConverter = new FormatConverter(_encodeCompute);
            _muxMaterial = new Material(_shader);
            _senderRT = new RenderTexture(recordingOptions.width, recordingOptions.width, 0);
            //_senderRT = new RenderTexture(1920, 1080, 0);
            _senderRT.Create();
            recorder = new VideoRecorder(_senderRT, recordingOptions.targetFrameRate);
            _bgmetadatamanager = new BgMetadataManager();
        }

        void OnEnable()
        {
            // Camera callback setup
            m_CameraManager.frameReceived += OnCameraFrameEventReceived;
            //m_OcclusionManager.frameReceived += OnOcclusionFrameEventReceived;
        }

        void OnDisable()
        {
            // Camera callback termination
            m_CameraManager.frameReceived -= OnCameraFrameEventReceived;
            //m_OcclusionManager.frameReceived -= OnOcclusionFrameEventReceived;
        }

        void OnDestroy()
        {
            ReleaseInternalObjects();
        }

        private void ReleaseInternalObjects()
        {
            Destroy(_muxMaterial);
            Destroy(_senderRT);
            if (_nativeArray != null)
            {
                _nativeArray.Value.Dispose();
                _nativeArray = null;
            }
            recorder.Dispose();
        }

        private void Update()
        {
            // Camera manager related information text is displayed
            var config = m_CameraManager.currentConfiguration;
            var configtext = $"{config?.width}x{config?.height}{((bool)(config?.framerate.HasValue) ? $" at {config?.framerate.Value} Hz" : "")}{(config?.depthSensorSupported == Supported.Supported ? " depth sensor" : "")}";
            _resolutionInfoText.text = configtext;
            _statusInfoText.text = "Position: " + Utils.FormatVector3Position(_arcamera.transform.position) + "\n" + "Rotation: " + Utils.FormatVector3Position(_arcamera.transform.rotation.eulerAngles) + "\n" + "Projection:\n" + _projection;

            // Caluculate aspectratio
            float textureAspectRatio = (m_OcclusionManager.humanDepthTexture == null) ? 1.0f : ((float)m_OcclusionManager.humanDepthTexture.width / (float)m_OcclusionManager.humanDepthTexture.height);
            if (_textureAspectRatio != textureAspectRatio)
            {
                UpdateRawImage(textureAspectRatio);
            }

            // [Debug用]Previewに格納
            //_depthPreview.texture = _sourceDepthTexture;
            _stencilPreview.texture = _sourceStencilTexture;
        }

        private void UpdateRawImage(float textureAspectRatio)
        {
            float minDimension = 500.0f;
            float maxDimension = Mathf.Round(minDimension * textureAspectRatio);
            Vector2 rectSize = new Vector2(maxDimension, minDimension);

            // Update the raw image dimensions and the raw image material parameters.
            _depthPreview.rectTransform.sizeDelta = rectSize;
            _stencilPreview.rectTransform.sizeDelta = rectSize;
            _stencilPreview.rectTransform.position = new Vector3(_depthPreview.rectTransform.position.x + maxDimension, _depthPreview.rectTransform.position.y, _depthPreview.rectTransform.position.z);
        }

        unsafe private void OnCameraFrameEventReceived(ARCameraFrameEventArgs cameraFrameEventArgs)
        {

            //1. CreateCameraFeedTexture
            RefreshCameraFeedTexture();

            _metadata = new OtaFrameMetadata();

            //2. Meke XML for Metadata used by NDI connection
            makeXML4Metadata(cameraFrameEventArgs);

            //3. Meke XML for BG verices related Metadata used by NDI connection
            //if (_otavjMeshManager.m_MeshMap.Count == 0) return;
            makeXML4BgMetadata();

            //4. Record MP4
            recordMP4();

            
            // //3. Create UYVA image
            // ComputeBuffer converted = Capture();
            // if (converted == null)
            // {
            //     return;
            // }

            // //4. Send Image via NDI
            // SendVideo(converted);

            // //5. Meke XML for BG verices related Metadata used by NDI connection
            // if (_otavjMeshManager.m_MeshMap.Count == 0) return;
            // var isSend = makeXML4BgMetadata();

            // //6. Send Metadata via NDI
            // if (!isSend) return;
            // SendMetadata();
        }

        unsafe private void RefreshCameraFeedTexture()
        {
            XRCpuImage image;
            if (!m_CameraManager.TryAcquireLatestCpuImage(out image))
                return;

            var conversionParams = new XRCpuImage.ConversionParams
            (
                image,
                TextureFormat.RGBA32,
                XRCpuImage.Transformation.None
            );

            if (_sourceOriginTexture == null || _sourceOriginTexture.width != image.width || _sourceOriginTexture.height != image.height)
            {
                _sourceOriginTexture = new Texture2D(conversionParams.outputDimensions.x,
                                         conversionParams.outputDimensions.y,
                                         conversionParams.outputFormat, false);
            }

            var buffer = _sourceOriginTexture.GetRawTextureData<byte>();
            image.Convert(conversionParams, new IntPtr(buffer.GetUnsafePtr()), buffer.Length);

            _sourceOriginTexture.Apply();

            buffer.Dispose();
            image.Dispose();

            _sourceDepthTexture = m_OcclusionManager.environmentDepthTexture;
            _sourceStencilTexture = m_OcclusionManager.humanStencilTexture;

            _muxMaterial.SetTexture("_SourceTex", _sourceOriginTexture);
            _muxMaterial.SetTexture("_HumanStencil", _sourceStencilTexture);
            _muxMaterial.SetTexture("_EnvironmentDepth", _sourceDepthTexture);
            var range = new Vector2(_minDepth, _maxDepth);
            _muxMaterial.SetVector("_DepthRange", range);
            _senderRT.Release();
            Graphics.Blit(null, _senderRT, _muxMaterial, 0);

            // test
            //Camera.main.targetTexture = _senderRT;
            _depthPreview.texture = _senderRT;
        }
        private void makeXML4Metadata(ARCameraFrameEventArgs args)
        {
            if (args.projectionMatrix.HasValue)
            {
                _projection = args.projectionMatrix.Value;

                // Aspect ratio compensation (camera vs. 16:9)
                //_projection[1, 1] *= (16.0f / 9) / _camera.aspect;
            }

            BasicMetadata metainfo = new BasicMetadata(_arcamera.transform.position, _arcamera.transform.rotation, _projection, _minDepth, _maxDepth, _inputHandler.GetToggles(), _inputHandler.GetSliders());
            _metadata.camera = metainfo;

            // String jsonString = JsonConvert.SerializeObject(metainfo);
            // JsonConvert.SerializeObject(metainfo);
            // _metadataStr = $"<metadata><![CDATA[{jsonString}]]></metadata>";
            //Debug.Log(_metadataStr);
        }


        private void makeXML4BgMetadata()
        {
            _metadata.background = null;
            if (_otavjMeshManager.m_MeshMap.Count == 0) return;
            if (_bgmetadatamanager.SentMeshMap == null)
            {
                _bgmetadatamanager.InitializeManager(_otavjMeshManager.m_MeshMap);
                return;
            }
            var bgmeshinfo = _bgmetadatamanager.getDeltaVerticesInfo(_otavjMeshManager.m_MeshMap);
            var deletedMesh = _bgmetadatamanager.getDeletedMeshList(_otavjMeshManager.m_MeshMap);
            if (bgmeshinfo.Count == 0 && deletedMesh.Count == 0)
            {
                return;
            }
            BackgroundMetadata bgmetainfo = new BackgroundMetadata(bgmeshinfo, deletedMesh);
            _metadata.background = bgmetainfo;

            // String jsonString = JsonConvert.SerializeObject(metainfo);
            
            // _metadataStr4bg = $"<metadata><![CDATA[{jsonString}]]></metadata>";
            //Debug.Log(_metadataStr4bg);
            return;
        }

        private void recordMP4() {
            recorder.Update(JsonConvert.SerializeObject(_metadata));
        }
    }
}