using _Scripts.GameManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

namespace _Scripts.Units {
    /// <summary>
    /// Manages the selected unit.
    /// </summary>
    public class SelectedUnit : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
        // The outline object
        [SerializeField] private GameObject outline;
        private bool _isSelected;
        public bool isDead;
    
        private void Start() {
            if(outline == null) outline = transform.GetChild(0).gameObject;
            outline.SetActive(false);
        }
        
        //On pointer enter, show the outline
        public void OnPointerEnter(PointerEventData eventData) {
            if(isDead) return;
            outline.SetActive(true);
        }
        
        /// <summary>
        /// On pointer exit, hide the outline.
        /// </summary>
        /// <param name="eventData"></param>
        public void OnPointerExit(PointerEventData eventData) {
            if(_isSelected) return;
            outline.SetActive(false);
        }
        
        /// <summary>
        /// Selects the unit.
        /// </summary>
        public void SelectUnit() {
            if(_isSelected) return;
            Debug.Log("<color=red>Unit Selected:</color>");
            BattleController.Instance.SelectUnit(GetComponentInParent<Unit>());
            _isSelected = true;
        }
        
        /// <summary>
        /// Deselects the unit.
        /// </summary>
        public void DeselectUnit() {
            outline.SetActive(false);
            _isSelected = false;
        }
    }
}
