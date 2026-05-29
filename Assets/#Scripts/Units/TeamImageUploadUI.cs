using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Scripts.Units {
    /// <summary>
    /// UI bridge for assigning a custom image path to a team visual.
    /// </summary>
    public class TeamImageUploadUI : MonoBehaviour {

        #region Variables

        [Header("Team")]
        [SerializeField] private Team targetTeam = Team.Player; // Team this upload control modifies.

        [Header("Input")]
        [SerializeField] private TMP_InputField pathInput; // User-provided png or jpg path.

        [Header("Output")]
        [SerializeField] private Image previewImage;       // Optional UI preview image.
        [SerializeField] private TextMeshProUGUI statusText; // Optional status/error text.

        #endregion
        #region Unity Methods

        private void OnEnable() {
            RefreshPreview();
        }

        #endregion
        #region Public Methods

        /// <summary>
        /// Sets the target team from a UI dropdown or button.
        /// </summary>
        /// <param name="teamIndex">0 for AI, 1 for Player.</param>
        public void SetTargetTeam(int teamIndex) {
            targetTeam = teamIndex == 0 ? Team.AI : Team.Player;
            RefreshPreview();
        }

        /// <summary>
        /// Imports the image path currently typed into the input field.
        /// </summary>
        public void UploadFromInputPath() {
            if (TeamVisualManager.Instance == null) {
                SetStatus("No TeamVisualManager in scene.");
                return;
            }

            var path = pathInput != null ? pathInput.text : string.Empty;
            if (TeamVisualManager.Instance.TrySetTeamImageFromPath(targetTeam, path)) {
                RefreshPreview();
                SetStatus("Updated " + targetTeam + " image.");
            }
            else {
                SetStatus("Could not load image path.");
            }
        }

        /// <summary>
        /// Receives a path from code or a future file browser plugin.
        /// </summary>
        /// <param name="path">The selected image path.</param>
        public void UploadFromPath(string path) {
            if (pathInput != null) {
                pathInput.text = path;
            }

            UploadFromInputPath();
        }

        /// <summary>
        /// Clears the custom image for the configured team.
        /// </summary>
        public void ClearTeamImage() {
            if (TeamVisualManager.Instance == null) {
                SetStatus("No TeamVisualManager in scene.");
                return;
            }

            TeamVisualManager.Instance.ClearTeamImage(targetTeam);
            RefreshPreview();
            SetStatus("Reset " + targetTeam + " image.");
        }

        #endregion
        #region Preview

        /// <summary>
        /// Updates the optional UI preview image.
        /// </summary>
        private void RefreshPreview() {
            if (previewImage == null || TeamVisualManager.Instance == null) return;

            var sprite = TeamVisualManager.Instance.GetTeamSprite(targetTeam);
            previewImage.sprite = sprite;
            previewImage.enabled = sprite != null;
        }

        /// <summary>
        /// Writes optional status text.
        /// </summary>
        /// <param name="message">The message to show.</param>
        private void SetStatus(string message) {
            if (statusText != null) {
                statusText.text = message;
            }
        }

        #endregion
    }
}
