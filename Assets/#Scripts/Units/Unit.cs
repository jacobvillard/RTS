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
    public enum UnitType { Infantry, Ranged, Cavalry }
    
    /// <summary>
    /// The team of the unit in the game.
    /// </summary>
    public enum Team { AI, Player }
    
    /// <summary>
    /// The state of the unit in the game.
    /// </summary>
    public enum UnitState { Hold, Advance, Charge, Desert }


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
        [SerializeField]private float moveSpeed = 1f;               // The move speed of the unit
        [SerializeField]private float attackRange = 1f;             // The attack range of the unit
        [SerializeField]private float attackDamage = 20;            // The attack damage to the unit
        [SerializeField]private float attackCooldown = 1f;          // The attack cooldown of the unit
        [SerializeField] private float angularDrag = 8f;            // How quickly collision spin settles after impact
        [Header("AI Response")]
        [SerializeField] private float aiAssistCallRadius = 3f;     // Distance used by AI units to call nearby allies for help
        [SerializeField] private float aiMusketPathRangeMultiplier = 1.25f; // How much longer than musket range an AI path can be before retreating
        [SerializeField] private float aiMusketRetreatPadding = 2f; // Extra distance AI tries to add when retreating from muskets
        private float _attackTimer;                                 // Timer for attack cooldown
        private Unit _currentTarget;                                // The current target unit
        public Vector2 destination;                                 // The destination of the unit
        public bool IsAlive => health > 0;                          // Is the unit alive?
        public UnitType ClassType => unitType;                      // The unit class used by UI and combat rules
        public float CurrentHealth => Mathf.Max(health, 0f);        // Current health clamped for UI display
        public float CalculatedMoveSpeed => moveSpeed * _currentSpeedMultiplier; // Current speed after terrain modifiers
        public float AttackRange => attackRange;                    // Attack range exposed for AI response checks
        [SerializeField] private SpriteRenderer spriteRenderer;     // Main sprite renderer
        [SerializeField] private SpriteRenderer childSpriteRenderer;// Child sprite renderer 
        private NavMeshAgent _agent;                                // The NavMeshAgent component of the unit
        private Rigidbody2D _rigidbody2D;                           // Rigidbody that performs the actual 2D movement/collision
        private Vector2 _holdPosition;                               // Holds the position we should remain at when in "Hold" state
        private UnitState _previousState;                           // Keep track of previous state to detect changes
        [SerializeField] private float arrowSpeed = 5f;             // Speed of the arrow
        [SerializeField] private bool debugTargeting;               // Logs target detection and attack decisions for this unit
        private GameObject _targetPosCrossPrefab;                   // The target position cross prefab
        private readonly List<MapTerrainZone> _activeTerrainZones = new();
        private readonly List<MapTerrainZone> _activeForestZones = new();
        private const float MinimumStoppingDistance = 0.55f;
        private float _currentSpeedMultiplier = 1f;
        private bool _isFollowingManualMoveCommand;                // True while obeying a player-issued movement command
        public bool IsInForest => _activeForestZones.Count > 0;
        
        
        #endregion
        #region Initialization

        private void Awake() {
            if (!spriteRenderer) spriteRenderer = GetComponent<SpriteRenderer>();
            if (!childSpriteRenderer && transform.childCount > 0) 
                childSpriteRenderer = transform.GetChild(0).GetComponent<SpriteRenderer>();

            ConfigureRigidbody2D();
        }

        private void ConfigureRigidbody2D() {
            _rigidbody2D = GetComponent<Rigidbody2D>();
            if (_rigidbody2D == null) return;

            _rigidbody2D.bodyType = RigidbodyType2D.Dynamic;
            _rigidbody2D.gravityScale = 0f;
            _rigidbody2D.angularDrag = angularDrag;
            _rigidbody2D.velocity = Vector2.zero;
            _rigidbody2D.angularVelocity = 0f;
        }

        private void Start() {
            _agent = GetComponent<NavMeshAgent>();
            _agent.updatePosition = _rigidbody2D == null;
            _agent.updateRotation = false;
            _agent.updateUpAxis = false;
            UpdateStoppingDistance();
            UpdateAgentSpeed();
            
            _holdPosition = new Vector2(transform.position.x,transform.position.y) ;
            
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
            moveSpeed = type.moveSpeed;
            attackRange = type.attackRange;
            attackDamage = teamInit == Team.Player
                ? type.attackDamage * 0.99f 
                : type.attackDamage;
            attackCooldown = type.attackCooldown;
            aiAssistCallRadius = type.aiAssistCallRadius;
            aiMusketPathRangeMultiplier = type.aiMusketPathRangeMultiplier;
            aiMusketRetreatPadding = type.aiMusketRetreatPadding;
            this.team = teamInit;
            UpdateStoppingDistance();
        }


        

        public void EnterTerrainZone(MapTerrainZone terrainZone) {
            if (terrainZone == null || _activeTerrainZones.Contains(terrainZone)) return;

            _activeTerrainZones.Add(terrainZone);
            if (terrainZone.ProvidesForestCover && !_activeForestZones.Contains(terrainZone)) {
                _activeForestZones.Add(terrainZone);
            }

            UpdateAgentSpeed();
        }

        public void ExitTerrainZone(MapTerrainZone terrainZone) {
            if (terrainZone == null) return;

            _activeTerrainZones.Remove(terrainZone);
            _activeForestZones.Remove(terrainZone);
            UpdateAgentSpeed();
        }
        
        #endregion
        #region Update
        private void Update() {
            if (!IsAlive) return;

            RefreshTargetUnits();   // Refresh the list of target units
            UpdateCurrentTarget();  // Update the current target
            TryAttack();            // Attempt to attack
            StateManagement();      // Manage the state of the unit
            FixZedPos();            // Fix Z position
        }

        private void FixedUpdate() {
            if (!IsAlive || _agent == null || _rigidbody2D == null) return;

            var nextPosition = Vector2.MoveTowards(
                _rigidbody2D.position,
                new Vector2(_agent.nextPosition.x, _agent.nextPosition.y),
                _agent.speed * Time.fixedDeltaTime);

            _rigidbody2D.MovePosition(nextPosition);
            _agent.nextPosition = transform.position;
        }

        /// <summary>
        /// Fixes the Z position of the unit.
        /// </summary>
        private void FixZedPos() {
            var fixedPos = transform.position;
            fixedPos.z = 0f;
            transform.position = fixedPos;
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

            if (_currentTarget != null && _currentTarget.IsAlive) {
                MoveTowards(_currentTarget.transform.position);
            }
            else {
                _currentTarget = BattleController.Instance.FindClosestTarget(this, team);
            }
        }
        

        #endregion
        #region Targeting
        
        /// <summary>
        /// Updates _targetUnits by collecting all opposing units
        /// within a certain distance, angle, etc.
        /// </summary>
        private void RefreshTargetUnits()
        {
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
            
            if(closest == null) return;
            
            _currentTarget = closest;
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

        private bool CanSeeUnit(Unit target) {
            if (!target.IsInForest) return true;
            if (!IsInForest) return false;

            foreach (var forestZone in _activeForestZones) {
                if (target._activeForestZones.Contains(forestZone)) return true;
            }

            return false;
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

            var hits = Physics2D.RaycastAll(start, end - start, distance);
            foreach (var hit in hits) {
                if (hit.collider == null || hit.collider.isTrigger || hit.collider.gameObject == gameObject) continue;
                if (hit.collider.GetComponentInParent<Unit>() != null) continue;
                LogTargeting($"LOS blocked by '{hit.collider.gameObject.name}' on layer '{LayerMask.LayerToName(hit.collider.gameObject.layer)}'.");
                return false;
            }

            return true;
        }
    
        #endregion
        #region Movement
        
        /// <summary>
        /// Sets the destination of the unit.
        /// </summary>
        /// <param name="targetPosition"></param>
        public void SetDestination(Vector2 targetPosition) {
            Debug.Log("Setting destination to: " + targetPosition);
            
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

            _agent.stoppingDistance = unitType == UnitType.Ranged
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
                _agent.speed = moveSpeed * _currentSpeedMultiplier;
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

            if (attacker.ClassType == UnitType.Ranged) {
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
            if (_attackTimer >= attackCooldown) {
                _attackTimer = 0f;
                LogTargeting($"attack cooldown ready. Targets available: {targetUnits.Count}. Current target: {(_currentTarget != null ? _currentTarget.name : "none")}");
                
                
                if (unitType == UnitType.Ranged) {
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
            if (team == Team.Player && IsInForest) {
                LogTargeting("musket did not fire: player ranged unit is inside forest.");
                return;
            }

            UpdateCurrentTarget();
            if (_currentTarget == null || !IsValidTarget(_currentTarget)) {
                LogTargeting("musket did not fire: no valid current target.");
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
        
        private void DamageUnit(Unit target) {
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

            switch (unitType) {
                case UnitType.Infantry when enemy.unitType == UnitType.Ranged:
                case UnitType.Ranged when enemy.unitType == UnitType.Cavalry:
                case UnitType.Cavalry when enemy.unitType == UnitType.Infantry:
                    hasAdvantage = true;
                    break;
            }

            return hasAdvantage ? attackDamage * 2f : attackDamage;
        }

        private void LogTargeting(string message) {
            if (!debugTargeting) return;

            Debug.Log($"[Targeting:{name} | {team} | {unitType}] {message}", this);
        }
        
        #endregion
    }
}
