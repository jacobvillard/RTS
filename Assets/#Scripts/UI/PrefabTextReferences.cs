using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace _Scripts.UI {
    /// <summary>
    /// Registers TMP labels from a prefab root so gameplay scripts can find UI text without scene references.
    /// </summary>
    public class PrefabTextReferences : MonoBehaviour {

        #region Types

        /// <summary>
        /// Known text slots used by gameplay and UI scripts.
        /// </summary>
        public enum TextSlot {
            Money,
            InfantryUnitCost,
            CavalryUnitCost,
            MusketUnitCost,
            SelectedUnitClass,
            SelectedUnitHealth,
            SelectedUnitSpeed
        }

        /// <summary>
        /// One text slot assigned on the prefab root.
        /// </summary>
        [System.Serializable]
        private class TextReference {
            public TextSlot slot;           // Purpose of this TMP label.
            public TextMeshProUGUI text;    // TMP label assigned inside the prefab.
        }

        #endregion
        #region Variables

        [Header("Text References")]
        [SerializeField] private List<TextReference> textReferences = new(); // TMP labels owned by this prefab.

        private static readonly Dictionary<TextSlot, List<TextMeshProUGUI>> ActiveTexts = new(); // Active text lookup by slot.

        #endregion
        #region Unity Methods

        private void OnEnable() {
            RegisterTexts();
        }

        private void OnDisable() {
            UnregisterTexts();
        }

        #endregion
        #region Public Methods

        /// <summary>
        /// Gets an active TMP label for a known slot.
        /// </summary>
        /// <param name="slot">The text slot being requested.</param>
        /// <param name="text">The active TMP label, when found.</param>
        /// <returns>True when a matching active TMP label exists.</returns>
        public static bool TryGetText(TextSlot slot, out TextMeshProUGUI text) {
            text = null;
            if (!ActiveTexts.TryGetValue(slot, out var texts)) return false;

            texts.RemoveAll(item => item == null);
            if (texts.Count == 0) return false;

            text = texts[texts.Count - 1];
            return text != null;
        }

        #endregion
        #region Registration

        /// <summary>
        /// Adds this prefab's configured text references to the global lookup.
        /// </summary>
        private void RegisterTexts() {
            foreach (var textReference in textReferences) {
                if (textReference == null || textReference.text == null) continue;

                if (!ActiveTexts.TryGetValue(textReference.slot, out var texts)) {
                    texts = new List<TextMeshProUGUI>();
                    ActiveTexts[textReference.slot] = texts;
                }

                if (!texts.Contains(textReference.text)) {
                    texts.Add(textReference.text);
                }
            }
        }

        /// <summary>
        /// Removes this prefab's text references from the global lookup.
        /// </summary>
        private void UnregisterTexts() {
            foreach (var textReference in textReferences) {
                if (textReference == null || textReference.text == null) continue;
                if (!ActiveTexts.TryGetValue(textReference.slot, out var texts)) continue;

                texts.Remove(textReference.text);
                texts.RemoveAll(item => item == null);

                if (texts.Count == 0) {
                    ActiveTexts.Remove(textReference.slot);
                }
            }
        }

        #endregion
    }
}
