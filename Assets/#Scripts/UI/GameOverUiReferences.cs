using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Scripts.UI {
    /// <summary>
    /// Holds scene-local game-over UI references for persistent managers.
    /// </summary>
    public class GameOverUiReferences : MonoBehaviour {

        #region Variables

        [Header("Game Over UI")]
        [SerializeField] private GameObject gameOverRoot; // Parent object shown when the battle ends.
        [SerializeField] private GameObject wonButton;    // Button shown when the player wins.
        [SerializeField] private GameObject lostButton;   // Button shown when the player loses.
        [SerializeField] private TextMeshProUGUI resultText; // Text updated with YOU WON or YOU LOST.

        private static readonly List<GameOverUiReferences> RegisteredReferences = new(); // All loaded game-over UI reference providers.

        public GameObject GameOverRoot => gameOverRoot != null ? gameOverRoot : gameObject;
        public GameObject WonButton => wonButton;
        public GameObject LostButton => lostButton;

        #endregion
        #region Unity Methods

        private void Awake() {
            Register();
            ResolveMissingReferences();
        }

        private void OnEnable() {
            Register();
            ResolveMissingReferences();
        }

        private void OnDestroy() {
            RegisteredReferences.Remove(this);
        }

        private void OnValidate() {
            ResolveMissingReferences();
        }

        #endregion
        #region Public Methods

        /// <summary>
        /// Finds the best game-over UI reference provider for the current scene.
        /// </summary>
        /// <returns>The preferred reference provider, or null.</returns>
        public static GameOverUiReferences GetCurrent() {
            RemoveMissingReferences();

            var activeScene = SceneManager.GetActiveScene();
            GameOverUiReferences activeFallback = null;
            GameOverUiReferences anyFallback = null;

            foreach (var reference in RegisteredReferences) {
                if (reference == null) continue;

                reference.ResolveMissingReferences();

                if (reference.gameObject.scene == activeScene) {
                    Debug.Log($"[BattleDebug] GameOver UI selected from active scene on '{reference.name}'.");
                    return reference;
                }

                if (activeFallback == null && reference.gameObject.activeInHierarchy) {
                    activeFallback = reference;
                }

                anyFallback ??= reference;
            }

            var fallback = activeFallback != null ? activeFallback : anyFallback;
            if (fallback != null) {
                Debug.LogWarning(
                    $"[BattleDebug] GameOver UI using fallback '{fallback.name}' from scene '{fallback.gameObject.scene.name}'. " +
                    $"Active scene is '{activeScene.name}'.");
            }

            return fallback;
        }

        /// <summary>
        /// Hides the game-over UI.
        /// </summary>
        public void Hide() {
            ResolveMissingReferences();
            GameOverRoot.SetActive(false);
            wonButton?.SetActive(false);
            lostButton?.SetActive(false);
        }

        /// <summary>
        /// Shows the game-over UI for the winning team.
        /// </summary>
        /// <param name="winningTeam">The winning team label from BattleController.</param>
        public void Show(string winningTeam) {
            ResolveMissingReferences();
            GameOverRoot.SetActive(true);

            var playerWon = winningTeam == "Player";
            SetResultText(playerWon ? "YOU WON" : "YOU LOST");
            wonButton?.SetActive(playerWon);
            lostButton?.SetActive(!playerWon);

            Debug.Log(
                $"[BattleDebug] GameOverUiReferences.Show called. Winner={winningTeam}, " +
                $"RootActive={GameOverRoot.activeSelf}, WonActive={(wonButton != null && wonButton.activeSelf)}, " +
                $"LostActive={(lostButton != null && lostButton.activeSelf)}.");
        }

        #endregion
        #region Helpers

        /// <summary>
        /// Registers this provider for persistent manager lookup.
        /// </summary>
        private void Register() {
            if (!RegisteredReferences.Contains(this)) {
                RegisteredReferences.Add(this);
            }
        }

        /// <summary>
        /// Removes destroyed providers from the registry.
        /// </summary>
        private static void RemoveMissingReferences() {
            RegisteredReferences.RemoveAll(reference => reference == null);
        }

        /// <summary>
        /// Finds known child objects when references have not been assigned manually.
        /// </summary>
        private void ResolveMissingReferences() {
            gameOverRoot ??= FindChildGameObject("GameOver");
            gameOverRoot ??= gameObject;
            wonButton ??= FindChildGameObject("GameWonBtn");
            lostButton ??= FindChildGameObject("GameLostBtn");
            resultText ??= FindChildText("GameOverTxt");
        }

        /// <summary>
        /// Finds a child GameObject by name, including inactive children.
        /// </summary>
        /// <param name="childName">The child object name.</param>
        /// <returns>The matching child GameObject, or null.</returns>
        private GameObject FindChildGameObject(string childName) {
            var children = GetComponentsInChildren<Transform>(true);
            foreach (var child in children) {
                if (child != null && child.name == childName) {
                    return child.gameObject;
                }
            }

            return null;
        }

        /// <summary>
        /// Finds a child TMP label by name, including inactive children.
        /// </summary>
        /// <param name="childName">The child object name.</param>
        /// <returns>The matching TMP label, or null.</returns>
        private TextMeshProUGUI FindChildText(string childName) {
            var texts = GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var text in texts) {
                if (text != null && text.name == childName) {
                    return text;
                }
            }

            return null;
        }

        /// <summary>
        /// Updates the result label when one is assigned.
        /// </summary>
        /// <param name="value">The result text to show.</param>
        private void SetResultText(string value) {
            if (resultText != null) {
                resultText.text = value;
            }
        }

        #endregion
    }
}
