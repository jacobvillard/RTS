using System;
using System.Collections;
using System.Collections.Generic;
using _Scripts.GameManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

namespace _Scripts.Units {
    /// <summary>
    /// The type of unit in the game.
    /// </summary>
    public enum UnitType { Infantry, Ranged, Cavalry, Officer, Scout, Pikemen, Skirmisher, Dragoon, Bannerman }
    
    /// <summary>
    /// The team of the unit in the game.
    /// </summary>
    public enum Team { AI, Player }
    
    /// <summary>
    /// The state of the unit in the game.
    /// </summary>
    public enum UnitState { Hold, Advance, Charge, Follow, Desert }


    /// <summary>
    /// This script is responsible for handling the behavior of a unit in the game.
    /// </summary>
    public class Unit : MonoBehaviour {

        #region Variables
        
        [Header("Unit Properties")]
        [SerializeField]private UnitType unitType;                  // The type of unit
        public Team team;                                           // The team of the unit
        public List<Unit> targetUnits = new ();                     // List of target units
        [SerializeField]private UnitState cState;                   // Current state of the unit
        [SerializeField]private float health = 100;                 // The health of the unit
        [SerializeField]private float maxHealth = 100;              // The maximum health for healing caps.
        [SerializeField]private float moveSpeed = 1f;               // The move speed of the unit
        [SerializeField]private float attackRange = 1f;             // The attack range of the unit
        [SerializeField]private float attackDamage = 20;            // The attack damage to the unit
        [SerializeField]private float attackCooldown = 1f;          // The attack cooldown of the unit
        [SerializeField] private float angularDrag = 8f;            // How quickly collision spin settles after impact
        [Header("Role Behaviour")]
        [SerializeField] private bool canDismount = true;           // Allows Dragoons to swap to infantry-style fighting on contact.
        [SerializeField] private float followStoppingDistance = 0.8f; // Distance kept from an officer/leader while following.
        [SerializeField] private GameObject playerDismountedPrefab; // Prefab spawned when a player Dragoon dismounts.
        [SerializeField] private GameObject aiDismountedPrefab;     // Prefab spawned when an AI Dragoon dismounts.
        [Header("AI Response")]
        [SerializeField] private float aiAssistCallRadius = 3f;     // Distance used by AI units to call nearby allies for help
        [SerializeField] private float aiMusketPathRangeMultiplier = 1.25f; // How much longer than musket range an AI path can be before retreating
        [SerializeField] private float aiMusketRetreatPadding = 2f; // Extra distance AI tries to add when retreating from muskets
        [Header("Performance")]
        [SerializeField] private float targetRefreshInterval = 0.15f; // Seconds between expensive target/line-of-sight refreshes.
        private float _attackTimer;                                 // Timer for attack cooldown
        private Unit _currentTarget;                                // The current target unit
        private Unit _followTarget;                                 // Friendly unit this unit should follow.
        public Vector2 destination;                                 // The destination of the unit
        public bool IsAlive => health > 0;                          // Is the unit alive?
        public UnitType ClassType => unitType;                      // The unit class used by UI and combat rules
        public float CurrentHealth => Mathf.Max(health, 0f);        // Current health clamped for UI display
        public float MaxHealth => maxHealth;                        // Maximum health exposed for healing/capture logic.
        public float CalculatedMoveSpeed => moveSpeed * _currentSpeedMultiplier * _strategicMoveSpeedMultiplier; // Current speed after terrain modifiers
        public float AttackRange => attackRange;                    // Attack range exposed for AI response checks
        [SerializeField] private SpriteRenderer spriteRenderer;     // Main sprite renderer
        [SerializeField] private SpriteRenderer childSpriteRenderer;// Child sprite renderer 
        private NavMeshAgent _agent;                                // The NavMeshAgent component of the unit
        private Rigidbody2D _rigidbody2D;                           // Rigidbody that performs the actual 2D movement/collision
        private Vector2 _holdPosition;                               // Holds the position we should remain at when in "Hold" state
        private UnitState _previousState;                           // Keep track of previous state to detect changes
        [SerializeField] private float arrowSpeed = 5f;             // Speed of the arrow
        [SerializeField] private bool debugTargeting;               // Logs target detection and attack decisions for this unit
        [Header("Musket Behaviour")]
        [SerializeField] private float musketVisionConeAngle = 70f; // Total cone angle muskets can fire within.
        [SerializeField] private float musketTurnSpeed = 40f;       // Degrees per second used when rotating toward a musket target.
        [SerializeField] private float playerMusketForwardAngleOffset = 90f; // Player barrel direction from Rigidbody2D right.
        [SerializeField] private float aiMusketForwardAngleOffset = 90f;     // AI barrel direction from Rigidbody2D right.
        [SerializeField] private bool requireMusketTargetInCone = true; // Requires muskets to face targets before firing.
#if UNITY_EDITOR
        [Header("Editor Visualization")]
        [SerializeField] private bool drawLineOfSightGizmos = true; // Draws attack-range and LOS gizmos when this unit is selected.
#endif
        private GameObject _targetPosCrossPrefab;                   // The target position cross prefab
        private readonly List<IMapTerrainEffect> _activeTerrainZones = new();
        private readonly List<IMapTerrainEffect> _activeForestZones = new();
        private const float MinimumStoppingDistance = 0.55f;
        private const float NavMeshStartSampleRadius = 2f;
        private const string PlaceableLayerName = "Placeable";
        private const string NoCollisionLayerName = "NoCol";
        private float _currentSpeedMultiplier = 1f;
        private float _damageBoostMultiplier = 1f;                  // Temporary damage multiplier from banners/buildings.
        private float _strategicMoveSpeedMultiplier = 1f;           // Team-wide speed multiplier from captured objectives.
        private float _strategicAttackRateMultiplier = 1f;          // Team-wide attack-rate multiplier from captured objectives.
        private float _moraleBoostUntil;                           // Time until morale icon should remain active.
        private float _strategicBuffUntil;                          // Time until strategic buff icon should remain active.
        private float _healingUntil;                                // Time until healing icon should remain active.
        private bool _hasStrategicBuff;                             // True while an owned building explicitly buffs this unit.
        private bool _isFollowingManualMoveCommand;                // True while obeying a player-issued movement command
        private bool _isDragoonDismounted;                         // True after a Dragoon has entered infantry fighting mode.
        private bool _wasInPreGame;                                // True while this unit was last synced during setup
        private float _nextTargetRefreshTime;                      // Next time this unit can rebuild target data.
        private float _nextTargetDebugTime;                        // Next time targeting debug can print for this unit
        private float _musketFacingAngle;                          // Desired musket aim angle applied during the physics step.
        private string _lastTargetDebugMessage;                    // Last targeting debug line used for throttling
        private readonly RaycastHit2D[] _lineOfSightHits = new RaycastHit2D[12]; // Reused raycast buffer to avoid combat allocations.
#if UNITY_EDITOR
        private readonly RaycastHit2D[] _gizmoLineOfSightHits = new RaycastHit2D[12]; // Editor-only LOS visualizer buffer.
#endif
        public bool IsInForest => _activeForestZones.Count > 0;
        public bool HasMoraleBoost => _moraleBoostUntil > 0f && Time.time <= _moraleBoostUntil;
        public bool HasStrategicBuff => _hasStrategicBuff;
        public bool IsBeingHealed => _healingUntil > 0f && Time.time <= _healingUntil;
        
        
        #endregion
        #region Initialization

        private void Awake() {
            if (!spriteRenderer) spriteRenderer = GetComponent<SpriteRenderer>();
            if (!childSpriteRenderer && transform.childCount > 0) 
                childSpriteRenderer = transform.GetChild(0).GetComponent<SpriteRenderer>();

            ConfigureRigidbody2D();
            _agent = GetComponent<NavMeshAgent>();
            ConfigureNavMeshAgent();

            if (GameManager.Instance != null && GameManager.Instance.IsPreGame()) {
                SetAgentActive(false);
            }
        }

        private void ConfigureRigidbody2D() {
            _rigidbody2D = GetComponent<Rigidbody2D>();
            if (_rigidbody2D == null) return;

            _rigidbody2D.bodyType = RigidbodyType2D.Dynamic;
            _rigidbody2D.gravityScale = 0f;
            _rigidbody2D.angularDrag = angularDrag;
            _rigidbody2D.interpolation = RigidbodyInterpolation2D.Interpolate;
            _rigidbody2D.velocity = Vector2.zero;
            _rigidbody2D.angularVelocity = 0f;
        }

        /// <summary>
        /// Aligns the NavMeshAgent with the unit's current transform position.
        /// </summary>
        private void SyncAgentToTransform() {
            if (_agent == null) return;

            if (!_agent.enabled) return;

            var targetPosition = transform.position;
            if (NavMesh.SamplePosition(transform.position, out var hit, NavMeshStartSampleRadius, NavMesh.AllAreas)) {
                targetPosition = hit.position;
            }

            _agent.Warp(targetPosition);
            _agent.nextPosition = targetPosition;

            if (_rigidbody2D != null) {
                _rigidbody2D.position = targetPosition;
                _rigidbody2D.velocity = Vector2.zero;
                _rigidbody2D.angularVelocity = 0f;
            }

            transform.position = new Vector3(targetPosition.x, targetPosition.y, transform.position.z);
        }

        private void Start() {
            _agent ??= GetComponent<NavMeshAgent>();
            ConfigureNavMeshAgent();
            _wasInPreGame = GameManager.Instance != null && GameManager.Instance.IsPreGame();
            SetAgentActive(!_wasInPreGame);
            SyncAgentToTransform();
            UpdateStoppingDistance();
            UpdateAgentSpeed();
            SetMusketFacingFromTransform();
            
            _holdPosition = new Vector2(transform.position.x,transform.position.y) ;
            
        }

        /// <summary>
        /// Keeps NavMeshAgent pathing separate from Rigidbody2D movement and rotation.
        /// </summary>
        private void ConfigureNavMeshAgent() {
            if (_agent == null) return;

            _agent.updatePosition = _rigidbody2D == null;
            _agent.updateRotation = false;
            _agent.updateUpAxis = false;
        }

        /// <summary>
        /// Enables the NavMeshAgent only when active movement should be allowed.
        /// </summary>
        /// <param name="active">Whether the agent should be active.</param>
        private void SetAgentActive(bool active) {
            if (_agent == null) return;

            if (_agent.enabled == active) return;

            _agent.enabled = active;
        }

        /// <summary>
        /// Initializes musket aim from the placed unit rotation.
        /// </summary>
        private void SetMusketFacingFromTransform() {
            if (!IsRangedUnit()) return;

            _musketFacingAngle = GetBodyAngleForForwardDirection(GetDefaultMusketForwardDirection());
        }

        /// <summary>
        /// Sets the target cross prefab for the unit.
        /// </summary>
        /// <param name="targetPosCrossPrefab"></param>
        public void SetTargetCrossPrefab(GameObject targetPosCrossPrefab) {
            _targetPosCrossPrefab = targetPosCrossPrefab;
            
        }

        /// <summary>
        /// Initializes the unit with the given type and team.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="teamInit"></param>
        public void Initialize(UnitSO type, Team teamInit) {
            unitType = type.unitType;
            health = type.health;
            maxHealth = type.health;
            moveSpeed = type.moveSpeed;
            attackRange = type.attackRange;
            attackDamage = teamInit == Team.Player
                ? type.attackDamage * 0.99f 
                : type.attackDamage;
            attackCooldown = type.attackCooldown;
            canDismount = type.canDismount;
            playerDismountedPrefab = type.playerDismountedPrefab;
            aiDismountedPrefab = type.aiDismountedPrefab;
            aiAssistCallRadius = type.aiAssistCallRadius;
            aiMusketPathRangeMultiplier = type.aiMusketPathRangeMultiplier;
            aiMusketRetreatPadding = type.aiMusketRetreatPadding;
            this.team = teamInit;
            UpdateStoppingDistance();
            SetMusketFacingFromTransform();
        }


        

        public void EnterTerrainZone(IMapTerrainEffect terrainZone) {
            if (terrainZone == null || _activeTerrainZones.Contains(terrainZone)) return;

            _activeTerrainZones.Add(terrainZone);
            if (terrainZone.ProvidesForestCover && !_activeForestZones.Contains(terrainZone)) {
                _activeForestZones.Add(terrainZone);
            }

            UpdateAgentSpeed();
        }

        public void ExitTerrainZone(IMapTerrainEffect terrainZone) {
            if (terrainZone == null) return;

            _activeTerrainZones.Remove(terrainZone);
            _activeForestZones.Remove(terrainZone);
            UpdateAgentSpeed();
        }

        /// <summary>
        /// Re-syncs runtime movement state when the battle leaves setup.
        /// </summary>
        public void PrepareForBattle() {
            SetAgentActive(true);
            SyncAgentToTransform();
            SyncHoldPosition();
            destination = transform.position;
            _isFollowingManualMoveCommand = false;
            _wasInPreGame = false;
            LogTargeting(
                $"prepared for battle. AgentEnabled={(_agent != null && _agent.enabled)}, Position={transform.position}.");
        }
        
        #endregion
        #region Update
        private void Update() {
            if (!IsAlive) return;

            if (GameManager.Instance != null && GameManager.Instance.IsPreGame()) {
                SyncHoldPosition();
                SetAgentActive(false);
                _wasInPreGame = true;
                FixZedPos();
                return;
            }

            EnsureReadyForActiveGame();
            RefreshTargetingIfNeeded();
            UpdateMusketFacing();
            TryAttack();            // Attempt to attack
            StateManagement();      // Manage the state of the unit
            FixZedPos();            // Fix Z position
        }

        private void FixedUpdate() {
            if (!IsAlive || _agent == null || _rigidbody2D == null) return;
            if (GameManager.Instance != null && GameManager.Instance.IsPreGame()) {
                _rigidbody2D.velocity = Vector2.zero;
                SetAgentActive(false);
                _wasInPreGame = true;
                return;
            }

            EnsureReadyForActiveGame();
            var nextPosition = Vector2.MoveTowards(
                _rigidbody2D.position,
                new Vector2(_agent.nextPosition.x, _agent.nextPosition.y),
                _agent.speed * Time.fixedDeltaTime);

            _rigidbody2D.MovePosition(nextPosition);
            ApplyMusketBodyRotation();
            _agent.nextPosition = new Vector3(nextPosition.x, nextPosition.y, transform.position.z);
        }

        /// <summary>
        /// Applies one final sync when the game leaves setup before movement resumes.
        /// </summary>
        private void EnsureReadyForActiveGame() {
            if (!_wasInPreGame) return;

            PrepareForBattle();
        }

        /// <summary>
        /// Fixes the Z position of the unit.
        /// </summary>
        private void FixZedPos() {
            var fixedPos = transform.position;
            fixedPos.z = 0f;
            transform.position = fixedPos;
        }

        /// <summary>
        /// Keeps the unit's hold point at its scene position while setup is active.
        /// </summary>
        private void SyncHoldPosition() {
            _holdPosition = transform.position;

            if (_agent != null) {
                if (_agent.enabled) {
                    _agent.nextPosition = transform.position;
                }
            }
        }
        

        #endregion
        #region States

        /// <summary>
        /// Manages the state of the unit.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private void StateManagement() {
            // Check if our state just changed; if so, handle the logic (e.g., record hold position)
            if (cState != _previousState) {
                OnStateChanged(cState);
                _previousState = cState;
            }
            
            // State-specific logic
            switch (cState) {
                case UnitState.Hold:
                    Hold();
                    break;
                case UnitState.Advance:
                    Advance();
                    break;
                case UnitState.Charge:
                    Charge();
                    break;
                case UnitState.Follow:
                    Follow();
                    break;
                case UnitState.Desert:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Runs once whenever the state changes. We can do one-time setups here.
        /// </summary>
        private void OnStateChanged( UnitState newState) {
            if (newState == UnitState.Hold) {
                // Record the current position as the "hold position"
                _holdPosition = transform.position;
            }
        }
        
        /// <summary>
        /// Advances the unit towards the hold position.
        /// </summary>
        private void Hold() {
            _isFollowingManualMoveCommand = false;
            UpdateStoppingDistance();
            _agent.SetDestination(_holdPosition);
        }

        /// <summary>
        /// Advances the unit towards the target position.
        /// </summary>
        private void Advance() {
            if (_isFollowingManualMoveCommand && HasReachedManualDestination()) {
                _isFollowingManualMoveCommand = false;
                UpdateStoppingDistance();
            }

            MoveTowards(destination);
        }
        
        
        /// <summary>
        /// Sets the state of the unit.
        /// </summary>
        /// <param name="newState"></param>
        private void SetState(UnitState newState) {
            cState = newState;
        }

        /// <summary>
        /// Advances the unit towards the closest target.
        /// </summary>
        private void Charge() {
            if (_isFollowingManualMoveCommand) {
                _isFollowingManualMoveCommand = false;
                UpdateStoppingDistance();
            }

            if (_currentTarget != null && _currentTarget.IsAlive && CanContinueChasingTarget(_currentTarget)) {
                MoveTowards(_currentTarget.transform.position);
            }
            else {
                ClearCurrentTarget();
                _currentTarget = FindClosestCombatTarget();
            }
        }

        /// <summary>
        /// Checks whether this unit is still allowed to chase its current target.
        /// </summary>
        /// <param name="target">The target being chased.</param>
        /// <returns>True when the target remains visible or already engaged.</returns>
        private bool CanContinueChasingTarget(Unit target) {
            if (target == null || !target.IsAlive) return false;
            if (IsAlreadyEngagedWith(target)) return true;

            return CanSeeUnit(target) && HasLineOfSight(target);
        }

        /// <summary>
        /// Clears the current target from focused and cached target state.
        /// </summary>
        private void ClearCurrentTarget() {
            if (_currentTarget != null) {
                targetUnits.Remove(_currentTarget);
            }

            _currentTarget = null;
        }

        /// <summary>
        /// Follows a friendly leader while keeping a small spacing buffer.
        /// </summary>
        private void Follow() {
            if (_followTarget == null || !_followTarget.IsAlive) {
                SetState(UnitState.Hold);
                return;
            }

            if (Vector2.Distance(transform.position, _followTarget.transform.position) <= followStoppingDistance) {
                MoveTowards(transform.position);
                return;
            }

            MoveTowards(_followTarget.transform.position);
        }

        /// <summary>
        /// Orders this unit to hold its current position.
        /// </summary>
        public void CommandHold() {
            _isFollowingManualMoveCommand = false;
            _followTarget = null;
            SyncHoldPosition();
            SetState(UnitState.Hold);
        }

        /// <summary>
        /// Orders this unit to advance to a supplied world position.
        /// </summary>
        /// <param name="targetPosition">The position to move toward.</param>
        public void CommandAdvance(Vector2 targetPosition) {
            destination = targetPosition;
            _isFollowingManualMoveCommand = false;
            _followTarget = null;
            UpdateStoppingDistance();
            SetState(UnitState.Advance);
        }

        /// <summary>
        /// Orders this unit to follow a friendly leader.
        /// </summary>
        /// <param name="leader">The unit to follow.</param>
        public void CommandFollow(Unit leader) {
            if (leader == null || leader == this || leader.team != team) return;

            _isFollowingManualMoveCommand = false;
            _followTarget = leader;
            UpdateStoppingDistance();
            SetState(UnitState.Follow);
        }

        /// <summary>
        /// Orders this unit to attack a specific target.
        /// </summary>
        /// <param name="target">The enemy target.</param>
        public void CommandAttack(Unit target) {
            if (target == null || !target.IsAlive || target.team == team) return;

            TryAttackSpecificUnit(target);
        }
        

        #endregion
        #region Targeting
        
        /// <summary>
        /// Refreshes expensive target data on a short interval instead of every frame.
        /// </summary>
        private void RefreshTargetingIfNeeded() {
            if (Time.time < _nextTargetRefreshTime) return;

            _nextTargetRefreshTime = Time.time + Mathf.Max(0.02f, targetRefreshInterval);
            RefreshTargetUnits();
            UpdateCurrentTarget();
        }

        /// <summary>
        /// Updates _targetUnits by collecting all opposing units
        /// within a certain distance, angle, etc.
        /// </summary>
        private void RefreshTargetUnits()
        {
            if (BattleController.Instance == null) {
                LogTargeting("cannot refresh targets: no BattleController instance.");
                return;
            }

            var opposingUnits = BattleController.Instance.GetOpposingUnits(team);

            LogTargeting($"checking {opposingUnits.Count} opposing units. Current target list: {targetUnits.Count}");

            targetUnits.RemoveAll(candidate => !IsValidTarget(candidate));

            foreach (var candidate in opposingUnits) {
                if (!IsValidTarget(candidate) || targetUnits.Contains(candidate)) continue;
                targetUnits.Add(candidate);
                LogTargeting($"added target '{candidate.name}'. Target list now: {targetUnits.Count}");
            }
        }
        
        /// <summary>
        /// A simple example of picking which target to focus on out of the list.
        /// Removes any that are dead or do not have LOS.
        /// </summary>
        private void UpdateCurrentTarget() {
            // Pick the closest
            Unit closest = null;
            // Remove any null entries
            targetUnits.Remove(null); 

            var closestDist = Mathf.Infinity;
            foreach (var t in targetUnits) {
                if(t == null) continue;
                var dist = Vector2.Distance(transform.position, t.transform.position);
                if (!(dist < closestDist)) continue;
                closestDist = dist;
                closest = t;
            }
            
            if(closest == null) {
                _currentTarget = null;
                LogTargeting($"no current target selected. Valid target list count: {targetUnits.Count}.");
                return;
            }
            
            _currentTarget = closest;
            LogTargeting($"current target set to '{_currentTarget.name}'.");
        }

        /// <summary>
        /// Checks whether another unit can be considered a valid combat target right now.
        /// </summary>
        /// <param name="candidate">Potential opposing unit.</param>
        /// <returns>True when the unit is alive, in range, visible, and unobstructed.</returns>
        public bool CanTargetForCombat(Unit candidate) {
            return IsValidTarget(candidate);
        }

        private bool IsValidTarget(Unit candidate) {
            if (candidate == null) {
                LogTargeting("candidate rejected: null.");
                return false;
            }

            if (!candidate.IsAlive) {
                LogTargeting($"candidate '{candidate.name}' rejected: dead.");
                return false;
            }

            var distance = Vector2.Distance(transform.position, candidate.transform.position);
            if (distance > attackRange) {
                LogTargeting($"candidate '{candidate.name}' rejected: distance {distance:0.00} > range {attackRange:0.00}.");
                return false;
            }

            if (!CanSeeUnit(candidate)) {
                LogTargeting($"candidate '{candidate.name}' rejected: hidden by forest rules.");
                return false;
            }

            if (!HasLineOfSight(candidate)) {
                LogTargeting($"candidate '{candidate.name}' rejected: line of sight blocked.");
                return false;
            }

            LogTargeting($"candidate '{candidate.name}' valid at distance {distance:0.00}.");
            return true;
        }

        /// <summary>
        /// Finds the closest enemy this unit can currently see, ignoring attack range.
        /// </summary>
        /// <returns>The closest visible enemy, or null.</returns>
        public Unit FindClosestVisibleEnemy() {
            if (BattleController.Instance == null) return null;

            Unit closest = null;
            var closestDistance = Mathf.Infinity;
            var opposingUnits = BattleController.Instance.GetOpposingUnits(team);

            foreach (var candidate in opposingUnits) {
                if (candidate == null || !candidate.IsAlive) continue;
                if (!CanSeeUnit(candidate) || !HasLineOfSight(candidate)) continue;

                var distance = Vector2.Distance(transform.position, candidate.transform.position);
                if (distance >= closestDistance) continue;

                closestDistance = distance;
                closest = candidate;
            }

            return closest;
        }

        /// <summary>
        /// Finds the closest enemy currently valid for normal combat retargeting.
        /// </summary>
        /// <returns>The closest in-range visible enemy, or null.</returns>
        private Unit FindClosestCombatTarget() {
            if (BattleController.Instance == null) return null;

            Unit closest = null;
            var closestDistance = Mathf.Infinity;
            var opposingUnits = BattleController.Instance.GetOpposingUnits(team);

            foreach (var candidate in opposingUnits) {
                if (!IsValidTarget(candidate)) continue;

                var distance = Vector2.Distance(transform.position, candidate.transform.position);
                if (distance >= closestDistance) continue;

                closestDistance = distance;
                closest = candidate;
            }

            return closest;
        }

        /// <summary>
        /// Checks whether a target sits inside this musket's forward firing cone.
        /// </summary>
        /// <param name="target">The unit to check.</param>
        /// <returns>True when the target is inside the configured cone.</returns>
        private bool IsTargetInsideMusketCone(Unit target) {
            if (!requireMusketTargetInCone || !IsRangedUnit()) return true;
            if (target == null) return false;

            var toTarget = ((Vector2)target.transform.position - (Vector2)transform.position).normalized;
            if (toTarget == Vector2.zero) return true;

            var angle = Vector2.Angle(GetMusketForward(), toTarget);
            return angle <= musketVisionConeAngle * 0.5f;
        }

        /// <summary>
        /// Gets the resting direction for a musket team.
        /// </summary>
        /// <returns>Up for player muskets and down for AI muskets.</returns>
        private Vector2 GetDefaultMusketForwardDirection() {
            return team == Team.Player ? Vector2.up : Vector2.down;
        }

        /// <summary>
        /// Converts a forward direction into the body angle needed by Rigidbody2D.
        /// </summary>
        /// <param name="direction">The direction the musket should face.</param>
        /// <returns>The body Z rotation in degrees.</returns>
        private float GetBodyAngleForForwardDirection(Vector2 direction) {
            return Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - GetMusketForwardAngleOffset();
        }

        /// <summary>
        /// Gets the local visual forward offset for this team.
        /// </summary>
        /// <returns>The local barrel angle measured from Rigidbody2D right.</returns>
        private float GetMusketForwardAngleOffset() {
            return team == Team.Player
                ? playerMusketForwardAngleOffset
                : aiMusketForwardAngleOffset;
        }

        /// <summary>
        /// Gets the current musket aim direction without using the physics body's rotation.
        /// </summary>
        /// <returns>The musket forward direction using the configured local forward offset.</returns>
        private Vector2 GetMusketForward() {
            var radians = (_musketFacingAngle + GetMusketForwardAngleOffset()) * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
        }

        /// <summary>
        /// Rotates muskets toward their current target using the configured local forward offset.
        /// </summary>
        private void UpdateMusketFacing() {
            if (!IsRangedUnit()) return;

            if (_currentTarget != null && _currentTarget.IsAlive && CanSeeUnit(_currentTarget) && HasLineOfSight(_currentTarget)) {
                RotateMusketTowards(_currentTarget.transform.position);
                return;
            }

            RotateMusketTowardsTargetPoint();
        }

        /// <summary>
        /// Aims at the current movement/hold point when no enemy is available.
        /// </summary>
        private void RotateMusketTowardsTargetPoint() {
            var targetPoint = _isFollowingManualMoveCommand || cState == UnitState.Advance
                ? destination
                : _holdPosition;

            if (Vector2.Distance(transform.position, targetPoint) <= MinimumStoppingDistance) {
                RotateMusketTowardsTargetDirection(targetPoint);
                return;
            }

            RotateMusketTowards(targetPoint);
        }

        /// <summary>
        /// Keeps ranged units facing toward their destination direction after arriving.
        /// </summary>
        /// <param name="targetPoint">The point the unit was travelling toward.</param>
        private void RotateMusketTowardsTargetDirection(Vector2 targetPoint) {
            var direction = targetPoint - (Vector2)transform.position;
            if (direction.sqrMagnitude <= 0.0001f) return;

            var targetAngle = GetBodyAngleForForwardDirection(direction.normalized);
            _musketFacingAngle = Mathf.MoveTowardsAngle(_musketFacingAngle, targetAngle, musketTurnSpeed * Time.deltaTime);
            if (_rigidbody2D == null) {
                transform.rotation = Quaternion.Euler(0f, 0f, _musketFacingAngle);
            }
        }

        /// <summary>
        /// Smoothly returns the musket to its team-facing default direction.
        /// </summary>
        private void RotateMusketTowardsDefaultFacing() {
            _musketFacingAngle = Mathf.MoveTowardsAngle(
                _musketFacingAngle,
                GetBodyAngleForForwardDirection(GetDefaultMusketForwardDirection()),
                musketTurnSpeed * Time.deltaTime);

            if (_rigidbody2D == null) {
                transform.rotation = Quaternion.Euler(0f, 0f, _musketFacingAngle);
            }
        }

        /// <summary>
        /// Rotates musket aim so the configured local forward points toward the supplied world position.
        /// </summary>
        /// <param name="targetPosition">The position to face.</param>
        private void RotateMusketTowards(Vector3 targetPosition) {
            var direction = (Vector2)targetPosition - (Vector2)transform.position;
            if (direction.sqrMagnitude <= Mathf.Epsilon) return;

            var targetAngle = GetBodyAngleForForwardDirection(direction.normalized);
            _musketFacingAngle = Mathf.MoveTowardsAngle(_musketFacingAngle, targetAngle, musketTurnSpeed * Time.deltaTime);
            if (_rigidbody2D == null) {
                transform.rotation = Quaternion.Euler(0f, 0f, _musketFacingAngle);
            }
        }

        /// <summary>
        /// Applies musket aim through Rigidbody2D so collision rotation stays inside the physics step.
        /// </summary>
        private void ApplyMusketBodyRotation() {
            if (!IsRangedUnit()) return;
            if (_rigidbody2D == null) return;

            _rigidbody2D.MoveRotation(_musketFacingAngle);
            _rigidbody2D.angularVelocity = 0f;
        }

        /// <summary>
        /// Checks forest concealment before line-of-sight rules are applied.
        /// </summary>
        /// <param name="target">The target unit to test.</param>
        /// <returns>True when forest concealment allows visibility.</returns>
        private bool CanSeeUnit(Unit target) {
            if (target == null) return false;
            if (IsAlreadyEngagedWith(target)) return true;
            if (!target.IsInForest) return true;

            return SharesForestAreaWith(target);
        }

        /// <summary>
        /// Checks whether two units occupy the same forest hiding area.
        /// </summary>
        /// <param name="target">Target unit to compare against.</param>
        /// <returns>True when both units are in the same forest patch.</returns>
        private bool SharesForestAreaWith(Unit target) {
            if (target == null || !IsInForest || !target.IsInForest) return false;

            foreach (var forestZone in _activeForestZones) {
                if (forestZone == null || !target._activeForestZones.Contains(forestZone)) continue;

                if (forestZone is TilemapForestZone tilemapForestZone) {
                    var ownAreaId = tilemapForestZone.GetForestAreaId(this);
                    var targetAreaId = tilemapForestZone.GetForestAreaId(target);
                    if (ownAreaId >= 0 && ownAreaId == targetAreaId) return true;

                    continue;
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks whether two units are already actively fighting each other.
        /// </summary>
        /// <param name="target">The other unit.</param>
        /// <returns>True when either unit has the other as its current target.</returns>
        private bool IsAlreadyEngagedWith(Unit target) {
            return target != null && (_currentTarget == target || target._currentTarget == this);
        }
        
        /// <summary>
        /// Checks if there's a clear line of sight (no wall or obstacle in between).
        /// For 2D, we can do a simple Raycast2D from our position to the target.
        /// 
        /// Note: You'd need to set layer masks to ignore or detect correct layers.
        /// </summary>
        private bool HasLineOfSight(Unit target) {
            if (target == null) return false;

            Vector2 start = transform.position;
            Vector2 end = target.transform.position;
            var distance = Vector2.Distance(start, end);
            if (distance <= Mathf.Epsilon) return true;

            var direction = (end - start).normalized;
            var hitCount = Physics2D.RaycastNonAlloc(start, direction, _lineOfSightHits, distance);
            for (var i = 0; i < hitCount; i++) {
                var hit = _lineOfSightHits[i];
                if (!IsLineOfSightBlocker(hit.collider)) continue;

                LogTargeting($"LOS blocked by '{hit.collider.gameObject.name}' on layer '{LayerMask.LayerToName(hit.collider.gameObject.layer)}'.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Checks whether a collider should block combat line of sight.
        /// </summary>
        /// <param name="collider">The collider hit by the line-of-sight raycast.</param>
        /// <returns>True when the collider should block attacks.</returns>
        private bool IsLineOfSightBlocker(Collider2D collider) {
            if (collider == null || collider.isTrigger || collider.gameObject == gameObject) return false;
            if (collider.GetComponentInParent<Unit>() != null) return false;
            if (collider.GetComponentInParent<MapTerrainZone>() != null) return false;
            if (collider.GetComponentInParent<TilemapForestZone>() != null) return false;
            if (collider.gameObject.layer == LayerMask.NameToLayer(PlaceableLayerName)) return false;
            if (collider.gameObject.layer == LayerMask.NameToLayer(NoCollisionLayerName)) return false;

            return true;
        }
        #endregion
        #region Editor Visualization

#if UNITY_EDITOR

        /// <summary>
        /// Draws editor-only attack range and line-of-sight rays for visual debugging.
        /// </summary>
        private void OnDrawGizmosSelected() {
            if (!drawLineOfSightGizmos) return;

            DrawAttackRangeGizmo();
            if (!Application.isPlaying || BattleController.Instance == null) return;

            var opposingUnits = BattleController.Instance.GetOpposingUnits(team);
            foreach (var candidate in opposingUnits) {
                if (candidate == null || candidate == this || !candidate.IsAlive) continue;

                var distance = Vector2.Distance(transform.position, candidate.transform.position);
                if (distance > attackRange) continue;

                DrawLineOfSightGizmo(candidate);
            }
        }

        /// <summary>
        /// Draws the unit's configured attack range.
        /// </summary>
        private void DrawAttackRangeGizmo() {
            Gizmos.color = new Color(1f, 1f, 1f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, attackRange);

            if (!IsRangedUnit() || !requireMusketTargetInCone) return;

            DrawMusketConeGizmo();
        }

        /// <summary>
        /// Draws the configured musket cone using the configured local forward offset.
        /// </summary>
        private void DrawMusketConeGizmo() {
            var halfAngle = musketVisionConeAngle * 0.5f;
            var forward = Application.isPlaying
                ? GetMusketForward()
                : (Vector2)(Quaternion.Euler(0f, 0f, GetMusketForwardAngleOffset()) * transform.right);
            var leftDirection = Quaternion.Euler(0f, 0f, halfAngle) * forward;
            var rightDirection = Quaternion.Euler(0f, 0f, -halfAngle) * forward;

            Gizmos.color = new Color(1f, 0.85f, 0.1f, 0.65f);
            Gizmos.DrawLine(transform.position, transform.position + leftDirection * attackRange);
            Gizmos.DrawLine(transform.position, transform.position + rightDirection * attackRange);
        }

        /// <summary>
        /// Draws a green ray when LOS is clear, or red to the first blocking collider.
        /// </summary>
        /// <param name="target">The target unit being checked.</param>
        private void DrawLineOfSightGizmo(Unit target) {
            Vector2 start = transform.position;
            Vector2 end = target.transform.position;
            var distance = Vector2.Distance(start, end);
            if (distance <= Mathf.Epsilon) return;

            var direction = (end - start).normalized;
            var hitCount = Physics2D.RaycastNonAlloc(start, direction, _gizmoLineOfSightHits, distance);
            for (var i = 0; i < hitCount; i++) {
                var hit = _gizmoLineOfSightHits[i];
                if (!IsLineOfSightBlocker(hit.collider)) continue;

                Gizmos.color = Color.red;
                Gizmos.DrawLine(start, hit.point);
                Gizmos.DrawWireSphere(hit.point, 0.12f);

                Gizmos.color = new Color(1f, 0f, 0f, 0.25f);
                Gizmos.DrawLine(hit.point, end);
                return;
            }

            Gizmos.color = target == _currentTarget
                ? Color.yellow
                : Color.green;
            Gizmos.DrawLine(start, end);
            Gizmos.DrawWireSphere(end, 0.08f);
        }
#endif
        #endregion
        #region Movement
        
        /// <summary>
        /// Sets the destination of the unit.
        /// </summary>
        /// <param name="targetPosition"></param>
        public void SetDestination(Vector2 targetPosition) {
            LogTargeting("setting destination to: " + targetPosition);
            
            // OverlapSphere in 3D, OverlapCircle in 2D:
            // ReSharper disable once Unity.PreferNonAllocApi
            var hits2D = Physics2D.OverlapCircleAll(targetPosition, 0.1f);
            NavMeshAgent agent = null;
            foreach (var hit2d in hits2D) {
                agent = hit2d.GetComponent<NavMeshAgent>();
                
            }
            
            //If the agent is not null and the agent is on the same team, return
            if (agent != null) {
                if (agent.gameObject.GetComponent<Unit>().team == team) {
                    return;
                }
            }
            
            //Check if the target position is on the NavMesh
            // ReSharper disable once NotAccessedOutParameterVariable
            NavMeshHit hit; 
            var crossPosition = new Vector3(targetPosition.x, targetPosition.y, -0.1f);
            var cross = Instantiate(_targetPosCrossPrefab, crossPosition, Quaternion.identity);
            var crossSpriteRenderer = cross.GetComponent<SpriteRenderer>();
            if (NavMesh.SamplePosition(targetPosition, out hit, 0.1f, 1 << NavMesh.GetAreaFromName("Walkable"))) {
                destination = targetPosition;                   // Set the destination
                _isFollowingManualMoveCommand = true;
                UpdateStoppingDistance();
                AudioManager.Instance?.PlayMoveOrder(crossPosition);
                SetState(UnitState.Advance);                    // Change state to "Advance"
                BattleController.Instance.ClearSelectedUnit();  // Clear the selected unit
            }
            else {
                Debug.LogWarning("Invalid destination: " + targetPosition);
                AudioManager.Instance?.PlayBadUnitPosition(crossPosition);
                crossSpriteRenderer.color = Color.red;        // Set the cross's color to red
            }
            
            //Start the fade out coroutine for the cross
            StartCoroutine(FadeOutSpriteRendererCoroutine(1f, crossSpriteRenderer));
            
            
            

        }


        /// <summary>
        /// Fades out the cross sprite renderer over 'duration' seconds.
        /// </summary>
        /// <param name="duration"></param>
        /// <param name="spriteRendererFade"></param>
        /// <returns></returns>
        private static IEnumerator FadeOutSpriteRendererCoroutine(float duration, SpriteRenderer spriteRendererFade) {
            var elapsed = 0f;

            // Record the starting colors (including alpha)
            var startColor = spriteRendererFade.color;

            // Fade loop
            while (elapsed < duration) {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);

                // Lerp alpha from full (startColor.a) to 0
                var newAlpha = Mathf.Lerp(startColor.a, 0f, t);

                // Apply to both SpriteRenderers
                spriteRendererFade.color = new Color(
                    startColor.r, 
                    startColor.g, 
                    startColor.b, 
                    newAlpha);
                

                yield return null;
            }

            // Ensure final alpha is set to 0
            spriteRendererFade.color = new Color(
                startColor.r, 
                startColor.g, 
                startColor.b, 
                0f);
            
            Destroy(spriteRendererFade.gameObject);
        }
        

        /// <summary>
        /// Navigates the unit towards a point.
        /// </summary>
        /// <param name="point"></param>
        private void MoveTowards(Vector2 point) {
            if (_agent == null || !_agent.enabled) {
                LogTargeting($"cannot move: agent missing or disabled. Agent exists={_agent != null}, enabled={(_agent != null && _agent.enabled)}.");
                return;
            }

            if ((Vector2)transform.position != point) {
                _agent.SetDestination(point);
            }
        }

        private void UpdateStoppingDistance() {
            if (_agent == null) return;

            if (_isFollowingManualMoveCommand) {
                _agent.stoppingDistance = MinimumStoppingDistance;
                return;
            }

            _agent.stoppingDistance = IsRangedUnit()
                ? Mathf.Max(MinimumStoppingDistance, attackRange * 0.8f)
                : Mathf.Max(MinimumStoppingDistance, attackRange * 0.7f);
        }

        /// <summary>
        /// Checks whether a player-issued move order has reached its destination.
        /// </summary>
        /// <returns>True when the unit is close enough to the ordered point.</returns>
        private bool HasReachedManualDestination() {
            return Vector2.Distance(transform.position, destination) <= MinimumStoppingDistance;
        }



        private void UpdateAgentSpeed() {
            _currentSpeedMultiplier = 1f;

            foreach (var terrainZone in _activeTerrainZones) {
                if (terrainZone == null) continue;
                _currentSpeedMultiplier = Mathf.Min(_currentSpeedMultiplier, terrainZone.MoveSpeedMultiplier);
            }

            if (_agent != null) {
                _agent.speed = moveSpeed * _currentSpeedMultiplier * _strategicMoveSpeedMultiplier;
            }
        }
        
        #endregion
        #region AI Response

        /// <summary>
        /// Handles AI reactions after this unit has been attacked.
        /// </summary>
        /// <param name="attacker">The unit that caused the damage.</param>
        private void HandleAiAttackResponse(Unit attacker) {
            if (team != Team.AI || attacker == null || !attacker.IsAlive) return;

            AudioManager.Instance?.PlayAiAlert(transform.position, this);
            CallNearbyAiUnits(attacker);

            if (attacker.IsRangedUnit()) {
                RespondToMusketAttack(attacker);
                return;
            }

            TryAttackSpecificUnit(attacker);
        }

        /// <summary>
        /// Asks nearby AI allies to help against the attacker.
        /// </summary>
        /// <param name="attacker">The unit that damaged this AI unit.</param>
        private void CallNearbyAiUnits(Unit attacker) {
            if (BattleController.Instance == null) return;

            var friendlyUnits = BattleController.Instance.GetFriendlyUnits(team);
            foreach (var friendlyUnit in friendlyUnits) {
                if (friendlyUnit == null || friendlyUnit == this || !friendlyUnit.IsAlive) continue;
                if (Vector2.Distance(transform.position, friendlyUnit.transform.position) > aiAssistCallRadius) continue;

                friendlyUnit.TryAttackSpecificUnit(attacker);
            }
        }

        /// <summary>
        /// Decides whether an AI unit should charge or retreat after being shot by a musket.
        /// </summary>
        /// <param name="attacker">The musket unit that fired.</param>
        private void RespondToMusketAttack(Unit attacker) {
            if (IsFightingDifferentUnit(attacker)) return;

            if (CanReachMusketWithoutLongPath(attacker)) {
                TryAttackSpecificUnit(attacker);
                return;
            }

            RetreatFromMusket(attacker);
        }

        /// <summary>
        /// Forces this unit to focus a known attacker when it is not already fighting someone else.
        /// </summary>
        /// <param name="attacker">The unit to attack.</param>
        private void TryAttackSpecificUnit(Unit attacker) {
            if (attacker == null || !attacker.IsAlive) return;
            if (IsFightingDifferentUnit(attacker)) return;
            if (!CanSeeUnit(attacker) && !IsAlreadyEngagedWith(attacker)) return;
            if (!HasLineOfSight(attacker) && !IsAlreadyEngagedWith(attacker)) return;

            _currentTarget = attacker;
            if (!targetUnits.Contains(attacker)) {
                targetUnits.Add(attacker);
            }

            _isFollowingManualMoveCommand = false;
            UpdateStoppingDistance();
            SetState(UnitState.Charge);
        }

        /// <summary>
        /// Checks if this unit is already actively fighting another living unit.
        /// </summary>
        /// <param name="attacker">The attacker asking for a response.</param>
        /// <returns>True when a different target should keep priority.</returns>
        private bool IsFightingDifferentUnit(Unit attacker) {
            return _currentTarget != null &&
                   _currentTarget != attacker &&
                   _currentTarget.IsAlive &&
                   Vector2.Distance(transform.position, _currentTarget.transform.position) <= attackRange;
        }

        /// <summary>
        /// Checks whether the NavMesh path to a musket attacker is close enough to charge.
        /// </summary>
        /// <param name="attacker">The musket attacker.</param>
        /// <returns>True when the path is short enough to fight back.</returns>
        private bool CanReachMusketWithoutLongPath(Unit attacker) {
            if (_agent == null) return false;

            var path = new NavMeshPath();
            if (!_agent.CalculatePath(attacker.transform.position, path)) return false;
            if (path.status != NavMeshPathStatus.PathComplete) return false;

            return GetPathLength(path) <= attacker.AttackRange * aiMusketPathRangeMultiplier;
        }

        /// <summary>
        /// Calculates the distance along a NavMesh path.
        /// </summary>
        /// <param name="path">The path to measure.</param>
        /// <returns>The total corner-to-corner path length.</returns>
        private static float GetPathLength(NavMeshPath path) {
            if (path == null || path.corners.Length < 2) return 0f;

            var length = 0f;
            for (var i = 1; i < path.corners.Length; i++) {
                length += Vector3.Distance(path.corners[i - 1], path.corners[i]);
            }

            return length;
        }

        /// <summary>
        /// Moves away from a musket attacker until this unit should be outside their attack range.
        /// </summary>
        /// <param name="attacker">The musket attacker to retreat from.</param>
        private void RetreatFromMusket(Unit attacker) {
            if (_agent == null) return;

            var awayDirection = ((Vector2)transform.position - (Vector2)attacker.transform.position).normalized;
            if (awayDirection == Vector2.zero) {
                awayDirection = Vector2.right;
            }

            var currentDistance = Vector2.Distance(transform.position, attacker.transform.position);
            var retreatDistance = Mathf.Max(aiMusketRetreatPadding, attacker.AttackRange - currentDistance + aiMusketRetreatPadding);
            var retreatPosition = (Vector2)transform.position + awayDirection * retreatDistance;

            if (NavMesh.SamplePosition(retreatPosition, out var hit, retreatDistance, NavMesh.AllAreas)) {
                destination = hit.position;
                _isFollowingManualMoveCommand = false;
                UpdateStoppingDistance();
                SetState(UnitState.Advance);
            }
        }

        #endregion
        #region Damage and Death
        
        /// <summary>
        /// Attempts to attack the target unit.
        /// </summary>
        private void TryAttack() {
            _attackTimer += Time.deltaTime;
            var adjustedCooldown = attackCooldown / Mathf.Max(0.01f, _strategicAttackRateMultiplier);
            if (_attackTimer >= adjustedCooldown) {
                _attackTimer = 0f;
                LogTargeting($"attack cooldown ready. Targets available: {targetUnits.Count}. Current target: {(_currentTarget != null ? _currentTarget.name : "none")}");
                
                
                if (IsRangedUnit()) {
                    FireMusketAtClosestTarget();
                }
                else {
                    foreach (var targetUnit in targetUnits)
                    {
                        if (IsValidTarget(targetUnit))
                        {
                            LogTargeting($"melee attacking '{targetUnit.name}'.");
                            DamageUnit(targetUnit);
                        }
                    }
                }
            }
        }

        private void FireMusketAtClosestTarget() {
            RefreshTargetingIfNeeded();
            if (_currentTarget == null || !IsValidTarget(_currentTarget)) {
                LogTargeting("musket did not fire: no valid current target.");
                return;
            }

            RotateMusketTowards(_currentTarget.transform.position);
            if (!IsTargetInsideMusketCone(_currentTarget)) {
                LogTargeting($"musket did not fire: target '{_currentTarget.name}' outside firing cone.");
                return;
            }

            LogTargeting($"musket firing at '{_currentTarget.name}'.");
            AudioManager.Instance?.PlayMusketShot(transform.position, this);
            MusketProjectile.Spawn(this, _currentTarget, CalculateDamage(_currentTarget), arrowSpeed);
        }

        public void ApplyProjectileDamage(Unit shooter, float damage) {
            if (shooter == null || !IsAlive) return;
            TakeDamage(damage, shooter);
        }

        /// <summary>
        /// Applies damage that does not come from a normal unit attacker.
        /// </summary>
        /// <param name="damage">Damage to apply.</param>
        public void ApplyDirectDamage(float damage) {
            if (!IsAlive) return;

            TakeDamage(damage);
        }
        
        private void DamageUnit(Unit target) {
            DismountDragoonIfNeeded(target);
            var damage = CalculateDamage(target);
            LogTargeting($"damaging '{target.name}' for {damage:0.00}.");
            AudioManager.Instance?.PlayMeleeHit(target.transform.position, unitType, this);
            //Deal damage
            target.TakeDamage(damage, this);
                    
            //If the target is still in range, charge
            if (Vector2.Distance(_agent.destination, transform.position) <= attackRange) {
                SetState(UnitState.Charge);
            }
        }
        
        /// <summary>
        /// Takes damage from an enemy unit.
        /// </summary>
        /// <param name="amount"></param>
        /// <param name="attacker">The unit that caused the damage.</param>
        private void TakeDamage(float amount, Unit attacker = null) {
            health -= amount;           // Reduce health
            if (health <= 0) {
                FadeOutAndDestroy(); // Check for death
                return;
            }

            HandleAiAttackResponse(attacker);
        }

        /// <summary>
        /// Applies a temporary damage multiplier from aura/building effects.
        /// </summary>
        /// <param name="multiplier">The final damage multiplier.</param>
        public void SetDamageBoostMultiplier(float multiplier) {
            _damageBoostMultiplier = Mathf.Max(0f, multiplier);
            _moraleBoostUntil = _damageBoostMultiplier > 1f
                ? Time.time + 0.35f
                : 0f;
        }

        /// <summary>
        /// Applies team-wide strategic buffs from captured objectives.
        /// </summary>
        /// <param name="moveSpeedMultiplier">Multiplier applied to movement speed.</param>
        /// <param name="attackRateMultiplier">Multiplier applied to attack/reload rate.</param>
        /// <param name="showBuffIcon">Whether a strategic source is actively affecting this unit.</param>
        public void SetStrategicBuffs(float moveSpeedMultiplier, float attackRateMultiplier, bool showBuffIcon = false) {
            _strategicMoveSpeedMultiplier = Mathf.Max(0.01f, moveSpeedMultiplier);
            _strategicAttackRateMultiplier = Mathf.Max(0.01f, attackRateMultiplier);
            _hasStrategicBuff = showBuffIcon;
            _strategicBuffUntil = showBuffIcon
                ? Time.time + 0.8f
                : 0f;
            UpdateAgentSpeed();
        }

        /// <summary>
        /// Heals this unit without exceeding the supplied health cap.
        /// </summary>
        /// <param name="amount">Health restored this tick.</param>
        /// <param name="maximumHealth">The highest health this heal can reach.</param>
        public void Heal(float amount, float maximumHealth) {
            if (!IsAlive) return;

            maximumHealth = Mathf.Max(0f, maximumHealth);
            if (health >= maximumHealth) return;

            var previousHealth = health;
            health = Mathf.Min(maximumHealth, health + Mathf.Max(0f, amount));
            if (health > previousHealth) {
                _healingUntil = Time.time + 0.35f;
            }
        }
        
        /// <summary>
        /// Starts the fade-out process and destroys the object when done.
        /// </summary>
        private void FadeOutAndDestroy(float duration = 1f) {
            BattleController.Instance.RemoveUnit(this);
            AudioManager.Instance?.StopSoundsForOwner(this);
            AudioManager.Instance?.StopSoundsNear(transform.position, 1.5f);
            AudioManager.Instance?.PlayUnitDeath(transform.position, unitType);
            StartCoroutine(FadeOutCoroutine(duration));
        }   
        
        // ReSharper disable Unity.PerformanceAnalysis
        /// <summary>
        /// Fades both sprite renderers' alpha to 0 over 'duration', then triggers OnDeath.
        /// </summary>
        private IEnumerator FadeOutCoroutine(float duration) {
            if (!spriteRenderer || !childSpriteRenderer) {
                Debug.LogWarning("Missing SpriteRenderers for fade-out.");
                OnDeath(); // If either is missing, just proceed to OnDeath.
                yield break;
            }
            
            //Check if the unit is selected, if so, clear the selected unit
            if (BattleController.Instance.SelectedUnit == this) {
                BattleController.Instance.ClearSelectedUnit();
            }
            
            //If the unit is a player unit
            if(team == Team.Player) {
                //Ensure the button is not interactable
                var btn = GetComponentInChildren<Button>();
                btn.interactable = false;
                
                //Ensure the unit is not highlighted
                var selectedUnit = GetComponentInChildren<SelectedUnit>();
                selectedUnit.isDead = true;
            }

            
            //Call the fade out coroutine for both sprite renderers
            StartCoroutine(FadeOutSpriteRendererCoroutine(duration, spriteRenderer)) ;
            StartCoroutine(FadeOutSpriteRendererCoroutine(duration, childSpriteRenderer)) ;

            
            //Wait for the duration
            yield return new WaitForSeconds(duration);
            
            //Call OnDeath
            OnDeath();
        }

        /// <summary>
        /// Simple example OnDeath method.
        /// </summary>
        private void OnDeath() {
            Destroy(gameObject);
        }

        /// <summary>
        /// calculates the damage to deal to an enemy unit.
        /// </summary>
        /// <param name="enemy"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private float CalculateDamage(Unit enemy)
        {
            // Simple RPS logic:
            // Infantry > Ranged > Cavalry > Infantry (loop)
            // For simplicity, let's say if you have the advantage, double damage.
            var hasAdvantage = false;

            var attackerType = GetEffectiveCombatType();
            var defenderType = enemy.GetEffectiveCombatType();

            switch (attackerType) {
                case UnitType.Infantry when defenderType == UnitType.Ranged:
                case UnitType.Pikemen when defenderType == UnitType.Cavalry:
                case UnitType.Pikemen when defenderType == UnitType.Dragoon:
                case UnitType.Ranged when defenderType == UnitType.Cavalry:
                case UnitType.Skirmisher when defenderType == UnitType.Cavalry:
                case UnitType.Cavalry when defenderType == UnitType.Infantry:
                case UnitType.Dragoon when defenderType == UnitType.Infantry:
                    hasAdvantage = true;
                    break;
            }

            var damage = hasAdvantage ? attackDamage * 2f : attackDamage;
            return damage * _damageBoostMultiplier;
        }

        /// <summary>
        /// Checks whether this unit uses musket-style ranged behaviour.
        /// </summary>
        /// <returns>True for muskets and skirmishers.</returns>
        public bool IsRangedUnit() {
            return unitType == UnitType.Ranged || unitType == UnitType.Skirmisher;
        }

        /// <summary>
        /// Gets this unit's combat type after special role conversion.
        /// </summary>
        /// <returns>The type used for advantage checks.</returns>
        private UnitType GetEffectiveCombatType() {
            return unitType == UnitType.Dragoon && _isDragoonDismounted
                ? UnitType.Infantry
                : unitType;
        }

        /// <summary>
        /// Converts Dragoons to infantry-style fighting once they reach an enemy.
        /// </summary>
        /// <param name="target">The enemy in contact.</param>
        private void DismountDragoonIfNeeded(Unit target) {
            if (unitType != UnitType.Dragoon || _isDragoonDismounted || !canDismount || target == null) return;
            if (Vector2.Distance(transform.position, target.transform.position) > attackRange) return;

            _isDragoonDismounted = true;
            var dismountedPrefab = team == Team.Player
                ? playerDismountedPrefab
                : aiDismountedPrefab;

            if (dismountedPrefab != null) {
                StartCoroutine(ReplaceWithDismountedUnit(dismountedPrefab));
                return;
            }

            UpdateStoppingDistance();
        }

        /// <summary>
        /// Replaces this Dragoon with a dismounted unit prefab.
        /// </summary>
        /// <param name="dismountedPrefab">Prefab to spawn.</param>
        /// <returns>Coroutine enumerator.</returns>
        private IEnumerator ReplaceWithDismountedUnit(GameObject dismountedPrefab) {
            var replacement = Instantiate(dismountedPrefab, transform.position, transform.rotation);
            var replacementUnit = replacement.GetComponent<Unit>();
            var attempts = 0;

            while (replacementUnit == null && attempts < 3) {
                attempts++;
                yield return null;
                replacementUnit = replacement.GetComponent<Unit>();
            }

            if (replacementUnit != null) {
                replacementUnit.OverrideHealth(health, replacementUnit.MaxHealth);
            }

            BattleController.Instance?.RemoveUnit(this);
            Destroy(gameObject);
        }

        /// <summary>
        /// Overrides runtime health when another unit transforms into this one.
        /// </summary>
        /// <param name="currentHealth">Health to inherit.</param>
        /// <param name="maximumHealth">Maximum health to inherit.</param>
        public void OverrideHealth(float currentHealth, float maximumHealth) {
            maxHealth = Mathf.Max(1f, maximumHealth);
            health = Mathf.Clamp(currentHealth, 1f, maxHealth);
        }

        private void LogTargeting(string message) {
            if (!debugTargeting) return;
            if (Time.time < _nextTargetDebugTime && message == _lastTargetDebugMessage) return;

            _lastTargetDebugMessage = message;
            _nextTargetDebugTime = Time.time + 1f;
            Debug.Log($"[BattleDebug][Targeting:{name} | {team} | {unitType}] {message}", this);
        }
        
        #endregion
    }
}
