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
        private float _attackTimer;                                 // Timer for attack cooldown
        private Unit _currentTarget;                                // The current target unit
        public Vector2 destination;                                 // The destination of the unit
        public bool IsAlive => health > 0;                          // Is the unit alive?
        [SerializeField] private SpriteRenderer spriteRenderer;     // Main sprite renderer
        [SerializeField] private SpriteRenderer childSpriteRenderer;// Child sprite renderer 
        private NavMeshAgent _agent;                                // The NavMeshAgent component of the unit
        private Vector2 _holdPosition;                               // Holds the position we should remain at when in "Hold" state
        private UnitState _previousState;                           // Keep track of previous state to detect changes
        [SerializeField] private float arrowSpeed = 5f;             // Speed of the arrow
        private GameObject _targetPosCrossPrefab;                   // The target position cross prefab
        
        
        #endregion
        #region Initialization

        private void Awake() {
            if (!spriteRenderer) spriteRenderer = GetComponent<SpriteRenderer>();
            if (!childSpriteRenderer && transform.childCount > 0) 
                childSpriteRenderer = transform.GetChild(0).GetComponent<SpriteRenderer>();
           
        }

        private void Start() {
            _agent = GetComponent<NavMeshAgent>();
            _agent.updateRotation = false;
            _agent.updateUpAxis = false;
            _agent.speed = moveSpeed;
            
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
            this.team = teamInit;
        }
        
        #endregion
        #region Update
        private void Update() {
            if (!IsAlive) return;

            TryAttack();            // Attempt to attack
            RefreshTargetUnits();   // Refresh the list of target units
            UpdateCurrentTarget();  // Update the current target
            StateManagement();      // Manage the state of the unit
            FixZedPos();            // Fix Z position
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
            _agent.SetDestination(_holdPosition);
        }

        /// <summary>
        /// Advances the unit towards the target position.
        /// </summary>
        private void Advance() {
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
            // 1. Get all opposing units from the BattleController
            var opposingUnits = BattleController.Instance.GetOpposingUnits(team);
            
    
            // 3. Loop through each opposing unit and check conditions
            foreach (var candidate in opposingUnits)
            {
                // Skip if dead
                if (candidate == null || !candidate.IsAlive) 
                    if(targetUnits.Contains(candidate)) targetUnits.Remove(candidate);
        
                // Distance check
                var dist = Vector2.Distance(transform.position, candidate.transform.position);
                if (dist > attackRange)  // Example: only consider up to 10 units away
                    if(targetUnits.Contains(candidate)) targetUnits.Remove(candidate);
                
        
                // Optionally check line of sight
                if (!HasLineOfSight(candidate))
                    if(targetUnits.Contains(candidate)) targetUnits.Remove(candidate);
        
                // If all conditions pass, add to _targetUnits
                if(dist < attackRange ) targetUnits.Add(candidate);
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
                if(t == null) return;
                var dist = Vector2.Distance(transform.position, t.transform.position);
                if (!(dist < closestDist)) continue;
                closestDist = dist;
                closest = t;
            }
            
            if(closest == null) return;
            
            _currentTarget = closest;
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
            
            // Raycast from "start" to "end"
            var hitInfo = Physics2D.Raycast(start, end - start, Vector2.Distance(start, end));
            if (hitInfo.collider == null) 
                return false; // We didn't hit anything, so maybe no collider at all.

            // If we hit the target itself, we consider it a clear line of sight
            // Otherwise, we must have hit some obstacle.
            return (hitInfo.collider.gameObject == target.gameObject);
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
            var cross = Instantiate(_targetPosCrossPrefab, targetPosition, Quaternion.identity);
            var crossSpriteRenderer = cross.GetComponent<SpriteRenderer>();
            if (NavMesh.SamplePosition(targetPosition, out hit, 0.1f, 1 << NavMesh.GetAreaFromName("Walkable"))) {
                destination = targetPosition;                   // Set the destination
                SetState(UnitState.Advance);                    // Change state to "Advance"
                BattleController.Instance.ClearSelectedUnit();  // Clear the selected unit
            }
            else {
                Debug.LogWarning("Invalid destination: " + targetPosition);
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
        
        #endregion
        #region Damage and Death
        
        /// <summary>
        /// Attempts to attack the target unit.
        /// </summary>
        private void TryAttack() {
            _attackTimer += Time.deltaTime;
            if (_attackTimer >= attackCooldown) {
                _attackTimer = 0f;
                
                
                if (unitType == UnitType.Ranged)
                {
                    foreach (var targetUnit in targetUnits)
                    {
                        // Check if target is in range
                        if (targetUnit && Vector2.Distance(transform.position, targetUnit.transform.position) <= attackRange)
                        {
                            // Calculate flight time = distance / arrowSpeed
                            var distance    = Vector2.Distance(transform.position, targetUnit.transform.position);
                            var flightTime  = distance / arrowSpeed;

                            // Start a delayed damage coroutine
                            StartCoroutine(DelayedDamage(targetUnit, flightTime));
                        }
                    }
                }
                else
                {
                    // Melee logic: immediate damage
                    foreach (var targetUnit in targetUnits)
                    {
                        if (targetUnit && Vector2.Distance(transform.position, targetUnit.transform.position) <= attackRange)
                        {
                            DamageUnit(targetUnit);
                        }
                    }
                }
            }
        }
        
        
        /// <summary>
        /// Waits 'flightTime' seconds, then deals damage (if the target is still valid).
        /// </summary>
        private IEnumerator DelayedDamage(Unit target, float flightTime) {
            yield return new WaitForSeconds(flightTime);

            // Check if target is still alive & in range when "arrow" would arrive
            if (target && target.IsAlive)
            {
                var dist = Vector2.Distance(transform.position, target.transform.position);
                if (dist <= attackRange)
                {
                    DamageUnit(target);
                }
            }
        }
        
        private void DamageUnit(Unit target) {
            //Deal damage
            target.TakeDamage(CalculateDamage(target));
                    
            //If the target is still in range, charge
            if (Vector2.Distance(_agent.destination, transform.position) <= attackRange) {
                SetState(UnitState.Charge);
            }
        }
        
        /// <summary>
        /// Takes damage from an enemy unit.
        /// </summary>
        /// <param name="amount"></param>
        private void TakeDamage(float amount) {
            health -= amount;           // Reduce health
            if (health <= 0) FadeOutAndDestroy(); // Check for death
        }
        
        /// <summary>
        /// Starts the fade-out process and destroys the object when done.
        /// </summary>
        private void FadeOutAndDestroy(float duration = 1f) {
            BattleController.Instance.RemoveUnit(this);
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
        
        #endregion
    }
}