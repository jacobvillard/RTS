using _Scripts.Units;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace _Scripts.Buildings {
    /// <summary>
    /// Captures an objective when one team has units inside its capture radius.
    /// </summary>
    public class CapturableBuilding : MonoBehaviour {

        #region Variables

        [Header("Capture")]
        [SerializeField] private CaptureOwner startingOwner = CaptureOwner.Neutral; // Owner used when the scene starts.
        [SerializeField] private float captureRadius = 2f;                          // Units inside this radius contest ownership.
        [SerializeField] private float captureSeconds = 3f;                         // Time required to flip ownership.
        [SerializeField] private float refreshInterval = 0.25f;                     // Seconds between capture checks.

        [Header("Flags")]
        [SerializeField] private List<SpriteRenderer> flagSprites = new();          // Flag sprites tinted to show ownership.
        [SerializeField] private Color neutralFlagColour = Color.white;             // Flag colour when nobody owns the building.
        [SerializeField] private Color aiFlagColour = Color.red;                    // Flag colour when AI owns the building.
        [SerializeField] private Color playerFlagColour = new(0f, 0.8549f, 1f);     // Flag colour when player owns the building.
        [SerializeField] private float capturePulseSpeed = 5f;                      // Speed used while flags pulse during capture.
        [SerializeField] private Color capturePulseColour = Color.white;            // Colour flags pulse toward while being captured.

        [Header("Events")]
        [SerializeField] private UnityEvent capturedByPlayer; // Invoked when player captures this building.
        [SerializeField] private UnityEvent capturedByAi;     // Invoked when AI captures this building.
        [SerializeField] private UnityEvent becameNeutral;    // Invoked when ownership clears.

        private readonly Collider2D[] _nearbyColliders = new Collider2D[96]; // Reused capture query buffer.
        private CaptureOwner _currentOwner;                                  // Current owner.
        private CaptureOwner _capturingOwner;                                // Team currently making progress.
        private float _captureProgress;                                      // Progress toward captureSeconds.
        private float _nextRefreshTime;                                      // Next allowed capture check.

        public CaptureOwner CurrentOwner => _currentOwner;
        public bool HasTeamOwner => _currentOwner is CaptureOwner.AI or CaptureOwner.Player;

        #endregion
        #region Unity Methods

        private void Awake() {
            SetOwner(startingOwner);
        }

        private void Update() {
            UpdateFlagPulse();

            if (Time.time < _nextRefreshTime) return;

            _nextRefreshTime = Time.time + refreshInterval;
            RefreshCapture();
        }

        #endregion
        #region Capture

        /// <summary>
        /// Gets the owning team when this building is owned by a battle team.
        /// </summary>
        /// <param name="team">The owning team.</param>
        /// <returns>True when a team owns this building.</returns>
        public bool TryGetOwnerTeam(out Team team) {
            switch (_currentOwner) {
                case CaptureOwner.AI:
                    team = Team.AI;
                    return true;
                case CaptureOwner.Player:
                    team = Team.Player;
                    return true;
                default:
                    team = default;
                    return false;
            }
        }

        /// <summary>
        /// Checks nearby units and advances capture progress.
        /// </summary>
        private void RefreshCapture() {
            CountUnits(out var playerUnits, out var aiUnits);
            var contestedOwner = GetContestedOwner(playerUnits, aiUnits);
            if (contestedOwner == CaptureOwner.Neutral) {
                _captureProgress = 0f;
                _capturingOwner = CaptureOwner.Neutral;
                UpdateFlagColours();
                return;
            }

            if (contestedOwner == _currentOwner) {
                _captureProgress = 0f;
                _capturingOwner = CaptureOwner.Neutral;
                UpdateFlagColours();
                return;
            }

            if (_capturingOwner != contestedOwner) {
                _capturingOwner = contestedOwner;
                _captureProgress = 0f;
            }

            _captureProgress += refreshInterval;
            if (_captureProgress >= captureSeconds) {
                SetOwner(contestedOwner);
            }
        }

        /// <summary>
        /// Counts living units inside the capture radius.
        /// </summary>
        /// <param name="playerUnits">Number of player units found.</param>
        /// <param name="aiUnits">Number of AI units found.</param>
        private void CountUnits(out int playerUnits, out int aiUnits) {
            playerUnits = 0;
            aiUnits = 0;

            var count = Physics2D.OverlapCircleNonAlloc(transform.position, captureRadius, _nearbyColliders);
            for (var i = 0; i < count; i++) {
                var unit = _nearbyColliders[i] != null
                    ? _nearbyColliders[i].GetComponentInParent<Unit>()
                    : null;

                if (unit == null || !unit.IsAlive) continue;

                if (unit.team == Team.Player) {
                    playerUnits++;
                }
                else {
                    aiUnits++;
                }
            }
        }

        /// <summary>
        /// Chooses the team with uncontested capture pressure.
        /// </summary>
        /// <param name="playerUnits">Player unit count.</param>
        /// <param name="aiUnits">AI unit count.</param>
        /// <returns>The team currently making progress.</returns>
        private static CaptureOwner GetContestedOwner(int playerUnits, int aiUnits) {
            if (playerUnits > 0 && aiUnits == 0) return CaptureOwner.Player;
            if (aiUnits > 0 && playerUnits == 0) return CaptureOwner.AI;

            return CaptureOwner.Neutral;
        }

        /// <summary>
        /// Applies ownership and raises capture events.
        /// </summary>
        /// <param name="owner">New owner.</param>
        private void SetOwner(CaptureOwner owner) {
            _currentOwner = owner;
            _capturingOwner = CaptureOwner.Neutral;
            _captureProgress = 0f;
            UpdateFlagColours();

            switch (_currentOwner) {
                case CaptureOwner.Player:
                    capturedByPlayer?.Invoke();
                    break;
                case CaptureOwner.AI:
                    capturedByAi?.Invoke();
                    break;
                default:
                    becameNeutral?.Invoke();
                    break;
            }
        }

        /// <summary>
        /// Tints every assigned flag sprite to match the current owner.
        /// </summary>
        private void UpdateFlagColours() {
            var flagColour = GetFlagColour(_currentOwner);
            foreach (var flagSprite in flagSprites) {
                if (flagSprite == null) continue;

                flagSprite.color = flagColour;
            }
        }

        /// <summary>
        /// Pulses assigned flag sprites while another team is actively capturing.
        /// </summary>
        private void UpdateFlagPulse() {
            if (_capturingOwner == CaptureOwner.Neutral) return;

            var captureColour = GetFlagColour(_capturingOwner);
            var pulseAmount = (Mathf.Sin(Time.time * capturePulseSpeed) + 1f) * 0.5f;
            var flagColour = Color.Lerp(captureColour, capturePulseColour, pulseAmount);

            foreach (var flagSprite in flagSprites) {
                if (flagSprite == null) continue;

                flagSprite.color = flagColour;
            }
        }

        /// <summary>
        /// Gets the flag colour used for one ownership state.
        /// </summary>
        /// <param name="owner">Owner to display.</param>
        /// <returns>The configured owner colour.</returns>
        private Color GetFlagColour(CaptureOwner owner) {
            switch (owner) {
                case CaptureOwner.Player:
                    return playerFlagColour;
                case CaptureOwner.AI:
                    return aiFlagColour;
                default:
                    return neutralFlagColour;
            }
        }

        #endregion
    }
}
