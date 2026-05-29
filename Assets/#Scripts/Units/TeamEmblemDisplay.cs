using UnityEngine;

namespace _Scripts.Units {
    /// <summary>
    /// Applies the current team sprite to a unit emblem SpriteRenderer.
    /// </summary>
    public class TeamEmblemDisplay : MonoBehaviour {

        #region Variables

        [Header("Team")]
        [SerializeField] private Team team = Team.Player;       // Team to display when parent lookup is unavailable.
        [SerializeField] private bool useParentUnitTeam = true; // Uses the parent Unit team when available.

        [Header("Renderer")]
        [SerializeField] private SpriteRenderer emblemRenderer; // SpriteRenderer that displays the team image.

        #endregion
        #region Unity Methods

        private void Awake() {
            if (emblemRenderer == null) {
                emblemRenderer = GetComponent<SpriteRenderer>();
            }
        }

        private void OnEnable() {
            if (TeamVisualManager.Instance != null) {
                TeamVisualManager.Instance.TeamSpriteChanged += HandleTeamSpriteChanged;
            }

            Refresh();
        }

        private void OnDisable() {
            if (TeamVisualManager.Instance != null) {
                TeamVisualManager.Instance.TeamSpriteChanged -= HandleTeamSpriteChanged;
            }
        }

        #endregion
        #region Public Methods

        /// <summary>
        /// Sets the team displayed by this emblem.
        /// </summary>
        /// <param name="newTeam">The team to display.</param>
        public void SetTeam(Team newTeam) {
            team = newTeam;
            Refresh();
        }

        /// <summary>
        /// Applies the current sprite for this emblem's team.
        /// </summary>
        public void Refresh() {
            if (emblemRenderer == null || TeamVisualManager.Instance == null) return;

            var displayTeam = GetDisplayTeam();
            var sprite = TeamVisualManager.Instance.GetTeamSprite(displayTeam);
            emblemRenderer.sprite = sprite;
            emblemRenderer.enabled = sprite != null;
        }

        #endregion
        #region Events

        /// <summary>
        /// Refreshes this emblem when its team image changes.
        /// </summary>
        /// <param name="changedTeam">The team whose sprite changed.</param>
        /// <param name="sprite">The new sprite.</param>
        private void HandleTeamSpriteChanged(Team changedTeam, Sprite sprite) {
            if (changedTeam == GetDisplayTeam()) {
                Refresh();
            }
        }

        #endregion
        #region Helpers

        /// <summary>
        /// Gets the team this emblem should display.
        /// </summary>
        /// <returns>The parent unit team or configured fallback team.</returns>
        private Team GetDisplayTeam() {
            if (!useParentUnitTeam) return team;

            var unit = GetComponentInParent<Unit>();
            return unit != null ? unit.team : team;
        }

        #endregion
    }
}
