using UnityEngine;
using UnityEngine.UI;
using Unity.XR.CoreUtils;
using UnityEngine.XR.ARFoundation;

namespace ota.ndi
{
    /// <summary>
    /// An utility button to start/stop AR recording.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class ARRecordButton : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Hide this button in release build")]
        private bool _hideInReleaseBuild = true;

        [SerializeField]
        private Sprite _iconStart;

        [SerializeField]
        private Sprite _iconStop;

        private MP4Recorder _recorderController;

        private VideoRecorder _recorder => _recorderController?.recorder;

        private Button _button;

        private void Awake()
        {
            if (_hideInReleaseBuild && !Debug.isDebugBuild)
            {
                gameObject.SetActive(false);
                return;
            }

            //var origin = FindObjectOfType<XROrigin>();
            var origin = FindObjectOfType<ARSessionOrigin>();
            if (origin == null)
            {
                Debug.LogError("ARRecorder requires ARSessionOrigin in the scene");
                gameObject.SetActive(false);
                return;
            }
        }

        private void Start() {
            _recorderController = FindObjectOfType<MP4Recorder>();
        }

        private void OnEnable()
        {
            _button = GetComponent<Button>();
            _button.image.sprite = _iconStart;
            _button.onClick.AddListener(OnRecordButtonClicked);
        }

        private void OnDisable()
        {
            if (_button == null) { return; }
            _button.onClick.RemoveListener(OnRecordButtonClicked);
        }

        private void OnRecordButtonClicked()
        {
            if (_recorder == null)
            {
                return;
            }

            if (_recorder.IsRecording)
            {
                _recorder.EndRecording();
                _button.image.overrideSprite = null;
            }
            else
            {
                _recorder.StartRecording();
                _button.image.overrideSprite = _iconStop;
            }
        }

        private T AddOrGetComponent<T>() where T : Component
        {
            if (TryGetComponent<T>(out var component))
            {
                return component;
            }
            return gameObject.AddComponent<T>();
        }
    }
}
