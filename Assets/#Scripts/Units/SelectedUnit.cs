using _Scripts.GameManagement;
using UnityEngine;
using UnityEngine.EventSystems;

namespace _Scripts.Units {
    /// <summary>
    /// Handles hover and selected visuals for a unit.
    /// </summary>
    public class SelectedUnit : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {

        #region Variables

        [Header("Visuals")]
        [SerializeField] private GameObject outline; // Selection/hover outline object.

        private bool _isSelected; // True while this unit is actively selected.
        public bool isDead;       // Prevents selection visuals after death.

        #endregion
        #region Unity Methods
    
        private void Start() {
            if (outline == null && transform.childCount > 0) {
                outline = transform.GetChild(0).gameObject;
            }

            if (outline != null) {
                outline.SetActive(false);
            }
        }

        #endregion
        #region Pointer Events
        
        /// <summary>
        /// Shows the outline when the pointer enters the unit.
        /// </summary>
        /// <param name="eventData">Pointer event information.</param>
        public void OnPointerEnter(PointerEventData eventData) {
            if (isDead || outline == null) return;

            outline.SetActive(true);
        }
        
        /// <summary>
        /// Hides the outline when the pointer exits and the unit is not selected.
        /// </summary>
        /// <param name="eventData">Pointer event information.</param>
        public void OnPointerExit(PointerEventData eventData) {
            if (_isSelected || outline == null) return;

            outline.SetActive(false);
        }

        #endregion
        #region Selection
        
        /// <summary>
        /// Selects this unit through the battle controller.
        /// </summary>
        public void SelectUnit() {
            if (_isSelected) return;

            Debug.Log("<color=red>Unit Selected:</color>");
            AudioManager.Instance?.PlayUnitSelected();
            BattleController.Instance.SelectUnit(GetComponentInParent<Unit>());
            _isSelected = true;
        }
        
        /// <summary>
        /// Deselects this unit and hides its outline.
        /// </summary>
        public void DeselectUnit() {
            if (outline != null) {
                outline.SetActive(false);
            }

            _isSelected = false;
        }

        #endregion
    }
}
