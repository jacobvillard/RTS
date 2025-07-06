using _Scripts.GameManagement;
using UnityEngine;
using UnityEngine.UI;

namespace _Scripts.Units {
    
    /// <summary>
    /// Initializes a unit in the game.
    /// </summary>
    public class UnitInit : MonoBehaviour {
        [SerializeField] private UnitSO unit;
        [SerializeField] private Team team;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private SpriteRenderer spriteRendererUnitType;
        [SerializeField] private GameObject targetPosCrossPrefab;
        [SerializeField] private Color playerColor;
        [SerializeField] private Color aiColor;
        
        // Start is called before the first frame update
        private void Start() {
            spriteRenderer = GetComponent<SpriteRenderer>();
            var newUnit = gameObject.AddComponent<Unit>();
            newUnit.Initialize(unit, team);
            newUnit.SetTargetCrossPrefab(targetPosCrossPrefab);
            SetSprite(unit.icon);
            SetTeamLayer();
            AddUnit(newUnit);
            Destroy(this);
        }
        
        /// <summary>
        /// This method is called in the Editor when a serialized field changes (in the Inspector),
        /// or when you manually trigger it by re-importing or editing the script.
        /// </summary>
        private void OnValidate() {
            // Ensure we have a reference to the SpriteRenderer
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();
            
            //Call the method to update the sprite color
            UpdateSpriteColor();
        }

        /// <summary>
        /// Adds a unit to the battle controller.
        /// </summary>
        /// <param name="newUnit"></param>
        private void AddUnit(Unit newUnit) {
            BattleController.Instance.AddUnit(newUnit, team);
        }

        /// <summary>
        /// Sets the team layer of the unit.
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        private void SetTeamLayer() {
            switch (team) {
                case Team.AI:
                    gameObject.layer = LayerMask.NameToLayer("AI");
                    spriteRenderer.color = aiColor;
                    break;
                case Team.Player:
                    gameObject.layer = LayerMask.NameToLayer("Player");
                    spriteRenderer.color = playerColor;
                    break;
                default:
                    Debug.LogError("No team found for unit: " + unit.name);
                    break;
            }
        }
        
        

        /// <summary>
        /// Sets the sprite of the unit.
        /// </summary>
        /// <param name="unitIcon"></param>
        private void SetSprite(Sprite unitIcon) {
            if(spriteRendererUnitType != null) {
                spriteRendererUnitType.sprite = unitIcon;
            }
            else {
                Debug.LogError("No spriteRenderer found for unit: " + unit.name);
            }
        }
        
        
        /// <summary>
        /// Updates the sprite color immediately based on the current team (Editor & Runtime).
        /// </summary>
        private void UpdateSpriteColor() {
            if (spriteRenderer == null) return;

            spriteRenderer.color = team switch {
                Team.AI => aiColor,
                Team.Player => playerColor,
                _ => Color.magenta
            };
        }
    }
}
