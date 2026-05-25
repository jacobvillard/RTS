using _Scripts.GameManagement;
using UnityEngine;

namespace _Scripts.Units {
    /// <summary>
    /// Converts a prefab's serialized setup data into a runtime Unit component.
    /// </summary>
    public class UnitInit : MonoBehaviour {

        #region Variables

        [Header("Unit")]
        [SerializeField] private UnitSO unit; // Unit stats to apply at runtime.
        [SerializeField] private Team team;   // Team assigned to the spawned unit.

        [Header("Sprites")]
        [SerializeField] private SpriteRenderer spriteRenderer;         // Main team-coloured sprite.
        [SerializeField] private SpriteRenderer spriteRendererUnitType; // Unit-type icon sprite.
        [SerializeField] private Color playerColor;                     // Player team colour.
        [SerializeField] private Color aiColor;                         // AI team colour.

        [Header("UI")]
        [SerializeField] private GameObject targetPosCrossPrefab; // Movement target cross prefab.

        #endregion
        #region Unity Methods
        
        private void Start() {
            spriteRenderer = GetComponent<SpriteRenderer>();

            var newUnit = gameObject.AddComponent<Unit>();
            newUnit.Initialize(unit, team);
            newUnit.SetTargetCrossPrefab(targetPosCrossPrefab);

            SetSprite(unit != null ? unit.icon : null);
            SetTeamLayer();
            AddUnit(newUnit);
            Destroy(this);
        }
        
        private void OnValidate() {
            if (spriteRenderer == null) {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }
            
            UpdateSpriteColor();
        }

        #endregion
        #region Registration

        /// <summary>
        /// Adds a runtime unit to the battle controller.
        /// </summary>
        /// <param name="newUnit">The new runtime unit.</param>
        private void AddUnit(Unit newUnit) {
            if (BattleController.Instance != null) {
                BattleController.Instance.AddUnit(newUnit, team);
            }
        }

        #endregion
        #region Visuals

        /// <summary>
        /// Sets the GameObject layer and team colour.
        /// </summary>
        private void SetTeamLayer() {
            switch (team) {
                case Team.AI:
                    gameObject.layer = LayerMask.NameToLayer("AI");
                    SetMainSpriteColour(aiColor);
                    break;
                case Team.Player:
                    gameObject.layer = LayerMask.NameToLayer("Player");
                    SetMainSpriteColour(playerColor);
                    break;
                default:
                    Debug.LogError("No team found for unit: " + (unit != null ? unit.name : name));
                    break;
            }
        }

        /// <summary>
        /// Sets the icon sprite that identifies the unit type.
        /// </summary>
        /// <param name="unitIcon">The icon to display.</param>
        private void SetSprite(Sprite unitIcon) {
            if (spriteRendererUnitType != null) {
                spriteRendererUnitType.sprite = unitIcon;
            }
            else {
                Debug.LogError("No spriteRenderer found for unit: " + (unit != null ? unit.name : name));
            }
        }
        
        /// <summary>
        /// Updates the main sprite colour based on the selected team.
        /// </summary>
        private void UpdateSpriteColor() {
            if (spriteRenderer == null) return;

            spriteRenderer.color = team switch {
                Team.AI => aiColor,
                Team.Player => playerColor,
                _ => Color.magenta
            };
        }

        /// <summary>
        /// Applies colour to the main sprite when available.
        /// </summary>
        /// <param name="colour">The colour to apply.</param>
        private void SetMainSpriteColour(Color colour) {
            if (spriteRenderer != null) {
                spriteRenderer.color = colour;
            }
        }

        #endregion
    }
}
