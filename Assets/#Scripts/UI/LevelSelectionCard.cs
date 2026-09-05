using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Scripts.UI {
    /// <summary>Serialized card visuals; progress and navigation are owned by the carousel.</summary>
    public class LevelSelectionCard : MonoBehaviour {
        public Button button;
        public LevelCardGraphic frame;
        public LevelCardGraphic numberFrame;
        public TextMeshProUGUI numberLabel;
        public TextMeshProUGUI titleLabel;
        public Image preview;
        public GameObject missingPreview;
        public Image lockedShade;
        public GameObject lockIcon;
        public LevelCardGraphic[] stars;

        private static readonly Color Selected = new Color(0.55f, 0.85f, 0.08f);
        private static readonly Color Available = new Color(0.42f, 0.48f, 0.23f);
        private static readonly Color Locked = new Color(0.39f, 0.40f, 0.35f);

        public void Display(int level, string title, Sprite thumbnail, bool unlocked, int earnedStars, bool selected) {
            numberLabel.text = level.ToString();
            titleLabel.text = title.ToUpperInvariant();
            preview.sprite = thumbnail;
            preview.enabled = thumbnail != null;
            var aspect = preview.GetComponent<AspectRatioFitter>();
            if (aspect != null && thumbnail != null) aspect.aspectRatio = thumbnail.rect.width / thumbnail.rect.height;
            missingPreview.SetActive(thumbnail == null);
            preview.color = unlocked ? Color.white : new Color(0.42f, 0.45f, 0.39f);
            lockedShade.gameObject.SetActive(!unlocked);
            lockIcon.SetActive(!unlocked);
            button.interactable = unlocked;
            var tint = selected ? Selected : unlocked ? Available : Locked;
            frame.color = tint;
            numberFrame.color = tint;
            numberLabel.color = selected || unlocked ? Selected : new Color(0.7f, 0.72f, 0.65f);
            titleLabel.color = unlocked ? new Color(0.94f, 0.94f, 0.86f) : new Color(0.58f, 0.60f, 0.55f);
            for (var i = 0; i < stars.Length; i++)
                stars[i].color = i < earnedStars ? new Color(1f, 0.78f, 0.18f) : new Color(0.26f, 0.29f, 0.22f);
        }
    }
}
