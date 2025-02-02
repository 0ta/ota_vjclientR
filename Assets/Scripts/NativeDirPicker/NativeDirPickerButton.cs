using UnityEngine;
using UnityEngine.UI;
using Unity.XR.CoreUtils;
using System.Runtime.InteropServices;
using TMPro;

namespace ota.ndi
{
    /// <summary>
    /// An utility button to pop-up native directory chooser page.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class NativeDirPickerButton : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _filePathText;

        private Button _button;

        private string _selectedBookmark = null; 

        private void OnEnable()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(OnDirPickerButtonClicked);
        }

        private void OnDisable()
        {
            if (_button == null) { return; }
            _button.onClick.RemoveListener(OnDirPickerButtonClicked);
        }

        private void OnDirPickerButtonClicked()
        {
            NativeDirPickerlib.pickDirWithSecurityScope();
        }

        private void Update()
        {
            string filePath = NativeDirPickerlib.getSecurityScopeURL();
            if (string.IsNullOrWhiteSpace(filePath))
            {
                _filePathText.text = "No directory selected";
            } else {
                _filePathText.text = filePath;
            }
        }
    }
}
