﻿using System.Collections;
using UnityEngine;
using NaughtyAttributes;
using System;

namespace VHS
{
    [RequireComponent(typeof(CharacterController))]
    public class FirstPersonController : MonoBehaviour
    {
        #region Variables

        #region Private Serialized     
        #region Data
        [Space, Header("Data")]
        [SerializeField] public MovementInputData movementInputData = null;
        [SerializeField] private HeadBobData headBobData = null;

        #endregion

        #region Locomotion
        [Space, Header("Locomotion Settings")]
        [SerializeField] private float crouchSpeed = 1f;
        [SerializeField] private float walkSpeed = 2f;
        [SerializeField] private float runSpeed = 3f;
        [SerializeField] private float jumpSpeed = 5f;
        [Slider(0f, 1f)] [SerializeField] private float moveBackwardsSpeedPercent = 0.5f;
        [Slider(0f, 1f)] [SerializeField] private float moveSideSpeedPercent = 0.75f;
        #endregion

        #region Run Settings
        [Space, Header("Run Settings")]
        [Slider(-1f, 1f)] [SerializeField] private float canRunThreshold = 0.8f;
        [Slider(1f, 10f)] [SerializeField] private float staminaLimit = 5f;
        [SerializeField] private AnimationCurve runTransitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        #endregion

        #region Crouch Settings
        [Space, Header("Crouch Settings")]
        [Slider(0.2f, 0.9f)] [SerializeField] private float crouchPercent = 0.6f;
        [SerializeField] private float crouchTransitionDuration = 1f;
        [SerializeField] private AnimationCurve crouchTransitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        #endregion

        #region Landing Settings
        [Space, Header("Landing Settings")]
        [Slider(0.05f, 0.5f)] [SerializeField] private float lowLandAmount = 0.1f;
        [Slider(0.2f, 0.9f)] [SerializeField] private float highLandAmount = 0.6f;
        [SerializeField] private float landTimer = 0.5f;
        [SerializeField] private float landDuration = 1f;
        [SerializeField] private AnimationCurve landCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        #endregion

        #region Gravity
        [Space, Header("Gravity Settings")]
        [SerializeField] private float gravityMultiplier = 2.5f;
        [SerializeField] private float stickToGroundForce = 5f;

        [SerializeField] private LayerMask groundLayer = ~0;
        [Slider(0f, 1f)] [SerializeField] private float rayLength = 0.1f;
        [Slider(0.01f, 1f)] [SerializeField] private float raySphereRadius = 0.1f;
        #endregion

        #region Wall Settings
        [Space, Header("Check Wall Settings")]
        [SerializeField] private LayerMask obstacleLayers = ~0;
        [Slider(0f, 1f)] [SerializeField] private float rayObstacleLength = 0.1f;
        [Slider(0.01f, 1f)] [SerializeField] private float rayObstacleSphereRadius = 0.1f;

        #endregion

        #region Smooth Settings
        [Space, Header("Smooth Settings")]
        [Range(1f, 100f)] [SerializeField] private float smoothRotateSpeed = 5f;
        [Range(1f, 100f)] [SerializeField] private float smoothInputSpeed = 5f;
        [Range(1f, 100f)] [SerializeField] private float smoothVelocitySpeed = 5f;
        [Range(1f, 100f)] [SerializeField] private float smoothFinalDirectionSpeed = 5f;
        [Range(1f, 100f)] [SerializeField] private float smoothHeadBobSpeed = 5f;

        [Space]
        [SerializeField] private bool experimental = false;
        [InfoBox("It should smooth our player movement to not start fast and not stop fast but it's somehow jerky", InfoBoxType.Warning)]
        [Tooltip("If set to very high it will stop player immediately after releasing input, otherwise it just another smoothing to our movement to make our player not move fast immediately and not stop immediately")]
        [ShowIf("experimental")] [Range(1f, 100f)] [SerializeField] private float smoothInputMagnitudeSpeed = 5f;

        #endregion
        #endregion
        #region Private Non-Serialized
        #region Components / Custom Classes / Caches
        private CharacterController m_characterController;
        private Transform m_yawTransform;
        private Transform m_camTransform;
        private HeadBob m_headBob;
        private CameraController m_cameraController;
        private Sounds sounds;
        private Sounds sounds2;
        private Sounds sounds3;


        private RaycastHit m_hitInfo;
        private IEnumerator m_CrouchRoutine;
        private IEnumerator m_LandRoutine;

        private bool rechargeStamina = false, tired = false;
        public bool isHoldingBreath = false;

        public float stamina = 0f, breath = 0f, maxBreath = 9f, maxStamina = 5.3f;
        [SerializeField] private ProgressBarCircle breathBar;
        [SerializeField] public GameObject breathBarObject;
        [SerializeField] private ProgressBar staminaBar;
        [SerializeField] public GameObject staminaBarObject;
        private NetworkServerUI network;
        #endregion

        #region Debug
        [Space]
        [BoxGroup("DEBUG")] [SerializeField] [ReadOnly] private Vector2 m_inputVector;
        [BoxGroup("DEBUG")] [SerializeField] [ReadOnly] private Vector2 m_smoothInputVector;

        [Space]
        [BoxGroup("DEBUG")] [SerializeField] [ReadOnly] private Vector3 m_finalMoveDir;
        [BoxGroup("DEBUG")] [SerializeField] [ReadOnly] private Vector3 m_smoothFinalMoveDir;
        [Space]
        [BoxGroup("DEBUG")] [SerializeField] [ReadOnly] private Vector3 m_finalMoveVector;

        [Space]
        [BoxGroup("DEBUG")] [SerializeField] [ReadOnly] private float m_currentSpeed;
        [BoxGroup("DEBUG")] [SerializeField] [ReadOnly] private float m_smoothCurrentSpeed;
        [BoxGroup("DEBUG")] [SerializeField] [ReadOnly] private float m_finalSmoothCurrentSpeed;
        [BoxGroup("DEBUG")] [SerializeField] [ReadOnly] private float m_walkRunSpeedDifference;


        [Space]
        [BoxGroup("DEBUG")] [SerializeField] [ReadOnly] private float m_finalRayLength;
        [BoxGroup("DEBUG")] [SerializeField] [ReadOnly] private bool m_hitWall;
        [BoxGroup("DEBUG")] [SerializeField] [ReadOnly] private bool m_isGrounded;
        [BoxGroup("DEBUG")] [SerializeField] [ReadOnly] private bool m_previouslyGrounded;

        [Space]
        [BoxGroup("DEBUG")] [SerializeField] [ReadOnly] private float m_initHeight;
        [BoxGroup("DEBUG")] [SerializeField] [ReadOnly] private float m_crouchHeight;
        [BoxGroup("DEBUG")] [SerializeField] [ReadOnly] private Vector3 m_initCenter;
        [BoxGroup("DEBUG")] [SerializeField] [ReadOnly] private Vector3 m_crouchCenter;
        [Space]
        [BoxGroup("DEBUG")] [SerializeField] [ReadOnly] private float m_initCamHeight;
        [BoxGroup("DEBUG")] [SerializeField] [ReadOnly] private float m_crouchCamHeight;
        [BoxGroup("DEBUG")] [SerializeField] [ReadOnly] private float m_crouchStandHeightDifference;
        [BoxGroup("DEBUG")] [SerializeField] [ReadOnly] private bool m_duringCrouchAnimation;
        [BoxGroup("DEBUG")] [SerializeField] [ReadOnly] private bool m_duringRunAnimation;
        [Space]
        [BoxGroup("DEBUG")] [SerializeField] [ReadOnly] private float m_inAirTimer;

        [Space]
        [BoxGroup("DEBUG")] [ShowIf("experimental")] [SerializeField] [ReadOnly] private float m_inputVectorMagnitude;
        [BoxGroup("DEBUG")] [ShowIf("experimental")] [SerializeField] [ReadOnly] private float m_smoothInputVectorMagnitude;
        #endregion
        #endregion

        #endregion

        #region BuiltIn Methods     
        protected virtual void Start()
        {
            GetComponents();
            InitVariables();
        }

        protected virtual void Update()
        {
            showBreathBar();
            showStaminaBar();
            if (m_yawTransform != null)
                RotateTowardsCamera();

            if (m_characterController)
            {
                // Check if Grounded,Wall etc

                CheckIfGrounded();
                CheckIfWall();

                // Apply Smoothing
                SmoothInput();
                SmoothSpeed();
                SmoothDir();

                if (experimental)
                    SmoothInputMagnitude();

                // Calculate Movement
                CalculateMovementDirection();
                CalculateSpeed();
                CalculateFinalMovement();

                // Handle Player Movement, Gravity, Jump, Crouch etc.
                HandleCrouch();
                HandleHeadBob();
                HandleRunFOV();
                HandleCameraSway();
                HandleLanding();
                HandleState();

                ApplyGravity();
                ApplyMovement();
                HoldBreath();

                MovementSounds();
                RechargeStamina();
                RechargeBreath();
                ChangeBreathSystem();
                m_previouslyGrounded = m_isGrounded;
            }
        }

        private void showStaminaBar()
        {

            if (staminaBar.BarValue >= 100f)
            {
                staminaBar.Hide();
            }
            else if (m_duringRunAnimation)
            {
                staminaBar.Show();
            }
            staminaBar.BarValue = (maxStamina - stamina) * (100/maxStamina);

        }

        private void showBreathBar()
        {

            if (breathBar.BarValue >= 100f)
            {
                breathBar.Hide();
            }
            else if (isHoldingBreath)
            {
                breathBar.Show();
            }
            breathBar.BarValue = (maxBreath - breath) * (100/maxBreath);

        }

        private void ChangeBreathSystem()
        {
            if (sounds2.IsPlaying() || isHoldingBreath)
                sounds3.audioSource.Stop();
            else
                sounds3.StartNormalBreath();
        }

        /*
            private void OnDrawGizmos()
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere((transform.position + m_characterController.center) - Vector3.up * m_finalRayLength, raySphereRadius);
            }
         */

        #endregion

        #region Custom Methods
        #region Initialize Methods    
        protected virtual void GetComponents()
        {

            m_characterController = GetComponent<CharacterController>();
            m_cameraController = GetComponentInChildren<CameraController>();
            m_yawTransform = m_cameraController.transform;
            m_camTransform = GetComponentInChildren<Camera>().transform;
            m_headBob = new HeadBob(headBobData, moveBackwardsSpeedPercent, moveSideSpeedPercent);
            sounds = GameObject.FindGameObjectWithTag("PlayerSounds").GetComponent<Sounds>();
            sounds2 = GameObject.FindGameObjectWithTag("PlayerSounds2").GetComponent<Sounds>();
            sounds3 = GameObject.FindGameObjectWithTag("PlayerSounds3").GetComponent<Sounds>();


        }

        protected virtual void InitVariables()
        {
            // Calculate where our character center should be based on height and skin width
            m_characterController.center = new Vector3(0f, m_characterController.height / 2f + m_characterController.skinWidth, 0f);

            m_initCenter = m_characterController.center;
            m_initHeight = m_characterController.height;

            m_crouchHeight = m_initHeight * crouchPercent;
            m_crouchCenter = (m_crouchHeight / 2f + m_characterController.skinWidth) * Vector3.up;

            m_crouchStandHeightDifference = m_initHeight - m_crouchHeight;

            m_initCamHeight = m_yawTransform.localPosition.y;
            m_crouchCamHeight = m_initCamHeight - m_crouchStandHeightDifference;

            // Sphere radius not included. If you want it to be included just decrease by sphere radius at the end of this equation
            m_finalRayLength = rayLength + m_characterController.center.y;

            m_isGrounded = true;
            m_previouslyGrounded = true;

            m_inAirTimer = 0f;
            m_headBob.CurrentStateHeight = m_initCamHeight;

            m_walkRunSpeedDifference = runSpeed - walkSpeed;

            sounds3.StartNormalBreath();

            //createBars(); 
            network = GameObject.FindGameObjectWithTag("NetworkManager").GetComponent<NetworkServerUI>();

        }

        private void createBars()
        {
            //breathBarObject.transform.position = new Vector4(-100f, 100f, 0f);
            //staminaBarObject.transform.position = new Vector4(-150f, -555f, 0f);
            staminaBarObject.SetActive(true);
            breathBarObject.SetActive(true);
            //breathBarObject.transform.position = new Vector4(128f, 640f, 0f);
            //staminaBarObject.transform.position = new Vector4(192f,  57f, 6f);
            staminaBarObject.SetActive(false);
            breathBarObject.SetActive(false);

        }
        #endregion

        #region Smoothing Methods
        protected virtual void SmoothInput()
        {
            m_inputVector = movementInputData.InputVector.normalized;
            m_smoothInputVector = Vector2.Lerp(m_smoothInputVector, m_inputVector, Time.deltaTime * smoothInputSpeed);
            //Debug.DrawRay(transform.position, new Vector3(m_smoothInputVector.x,0f,m_smoothInputVector.y), Color.green);
        }

        protected virtual void SmoothSpeed()
        {
            m_smoothCurrentSpeed = Mathf.Lerp(m_smoothCurrentSpeed, m_currentSpeed, Time.deltaTime * smoothVelocitySpeed);

            if (movementInputData.IsRunning && CanRun())
            {
                float _walkRunPercent = Mathf.InverseLerp(walkSpeed, runSpeed, m_smoothCurrentSpeed);
                m_finalSmoothCurrentSpeed = runTransitionCurve.Evaluate(_walkRunPercent) * m_walkRunSpeedDifference + walkSpeed;
            }
            else
            {
                m_finalSmoothCurrentSpeed = m_smoothCurrentSpeed;
            }
        }

        protected virtual void SmoothDir()
        {

            m_smoothFinalMoveDir = Vector3.Lerp(m_smoothFinalMoveDir, m_finalMoveDir, Time.deltaTime * smoothFinalDirectionSpeed);
            Debug.DrawRay(transform.position, m_smoothFinalMoveDir, Color.yellow);
        }

        protected virtual void SmoothInputMagnitude()
        {
            m_inputVectorMagnitude = m_inputVector.magnitude;
            m_smoothInputVectorMagnitude = Mathf.Lerp(m_smoothInputVectorMagnitude, m_inputVectorMagnitude, Time.deltaTime * smoothInputMagnitudeSpeed);
        }
        #endregion

        #region Locomotion Calculation Methods
        protected virtual void CheckIfGrounded()
        {
            Vector3 _origin = transform.position + m_characterController.center;

            bool _hitGround = Physics.SphereCast(_origin, raySphereRadius, Vector3.down, out m_hitInfo, m_finalRayLength, groundLayer);
            Debug.DrawRay(_origin, Vector3.down * (m_finalRayLength), Color.red);

            m_isGrounded = _hitGround ? true : false;
        }

        protected virtual void CheckIfWall()
        {

            Vector3 _origin = transform.position + m_characterController.center;
            RaycastHit _wallInfo;

            bool _hitWall = false;

            if (movementInputData.HasInput && m_finalMoveDir.sqrMagnitude > 0)
                _hitWall = Physics.SphereCast(_origin, rayObstacleSphereRadius, m_finalMoveDir, out _wallInfo, rayObstacleLength, obstacleLayers);
            Debug.DrawRay(_origin, m_finalMoveDir * rayObstacleLength, Color.blue);

            m_hitWall = _hitWall ? true : false;
        }

        protected virtual bool CheckIfRoof() /// TO FIX
        {
            Vector3 _origin = transform.position;
            RaycastHit _roofInfo;

            bool _hitRoof = Physics.SphereCast(_origin, raySphereRadius, Vector3.up, out _roofInfo, m_initHeight);

            return _hitRoof;
        }

        protected virtual bool CanRun()
        {
            Vector3 _normalizedDir = Vector3.zero;

            if (m_smoothFinalMoveDir != Vector3.zero)
                _normalizedDir = m_smoothFinalMoveDir.normalized;

            float _dot = Vector3.Dot(transform.forward, _normalizedDir);
            return _dot >= canRunThreshold && stamina <= staminaLimit && !tired && !isHoldingBreath && !movementInputData.IsCrouching ? true : false;
        }

        protected virtual void CalculateMovementDirection()
        {

            Vector3 _vDir = transform.forward * m_smoothInputVector.y;
            Vector3 _hDir = transform.right * m_smoothInputVector.x;

            Vector3 _desiredDir = _vDir + _hDir;
            Vector3 _flattenDir = FlattenVectorOnSlopes(_desiredDir);

            m_finalMoveDir = _flattenDir;
        }

        protected virtual Vector3 FlattenVectorOnSlopes(Vector3 _vectorToFlat)
        {
            if (m_isGrounded)
                _vectorToFlat = Vector3.ProjectOnPlane(_vectorToFlat, m_hitInfo.normal);

            return _vectorToFlat;
        }

        protected virtual void CalculateSpeed()
        {
            m_currentSpeed = movementInputData.IsRunning && CanRun() ? runSpeed : walkSpeed;
            m_currentSpeed = movementInputData.IsCrouching ? crouchSpeed : m_currentSpeed;
            m_currentSpeed = !movementInputData.HasInput ? 0f : m_currentSpeed;
            m_currentSpeed = movementInputData.InputVector.y == -1 ? m_currentSpeed * moveBackwardsSpeedPercent : m_currentSpeed;
            m_currentSpeed = movementInputData.InputVector.x != 0 && movementInputData.InputVector.y == 0 ? m_currentSpeed * moveSideSpeedPercent : m_currentSpeed;
        }

        protected virtual void CalculateFinalMovement()
        {
            float _smoothInputVectorMagnitude = experimental ? m_smoothInputVectorMagnitude : 1f;
            Vector3 _finalVector = m_smoothFinalMoveDir * m_finalSmoothCurrentSpeed * _smoothInputVectorMagnitude;

            // We have to assign individually in order to make our character jump properly because before it was overwriting Y value and that's why it was jerky now we are adding to Y value and it's working
            m_finalMoveVector.x = _finalVector.x;
            m_finalMoveVector.z = _finalVector.z;

            if (m_characterController.isGrounded) // Thanks to this check we are not applying extra y velocity when in air so jump will be consistent
                m_finalMoveVector.y += _finalVector.y; //so this makes our player go in forward dir using slope normal but when jumping this is making it go higher so this is weird
        }
        #endregion

        #region Crouching Methods
        protected virtual void HandleCrouch()
        {
            if (movementInputData.CrouchClicked && m_isGrounded)
                InvokeCrouchRoutine();
        }

        protected virtual void InvokeCrouchRoutine()
        {
            if (movementInputData.IsCrouching)
                if (CheckIfRoof())
                    return;

            if (m_LandRoutine != null)
                StopCoroutine(m_LandRoutine);

            if (m_CrouchRoutine != null)
                StopCoroutine(m_CrouchRoutine);

            m_CrouchRoutine = CrouchRoutine();
            StartCoroutine(m_CrouchRoutine);
        }

        protected virtual IEnumerator CrouchRoutine()
        {
            m_duringCrouchAnimation = true;

            float _percent = 0f;
            float _smoothPercent = 0f;
            float _speed = 1f / crouchTransitionDuration;

            float _currentHeight = m_characterController.height;
            Vector3 _currentCenter = m_characterController.center;

            float _desiredHeight = movementInputData.IsCrouching ? m_initHeight : m_crouchHeight;
            Vector3 _desiredCenter = movementInputData.IsCrouching ? m_initCenter : m_crouchCenter;

            Vector3 _camPos = m_yawTransform.localPosition;
            float _camCurrentHeight = _camPos.y;
            float _camDesiredHeight = movementInputData.IsCrouching ? m_initCamHeight : m_crouchCamHeight;

            movementInputData.IsCrouching = !movementInputData.IsCrouching;
            m_headBob.CurrentStateHeight = movementInputData.IsCrouching ? m_crouchCamHeight : m_initCamHeight;

            while (_percent < 1f)
            {
                _percent += Time.deltaTime * _speed;
                _smoothPercent = crouchTransitionCurve.Evaluate(_percent);

                m_characterController.height = Mathf.Lerp(_currentHeight, _desiredHeight, _smoothPercent);
                m_characterController.center = Vector3.Lerp(_currentCenter, _desiredCenter, _smoothPercent);

                _camPos.y = Mathf.Lerp(_camCurrentHeight, _camDesiredHeight, _smoothPercent);
                m_yawTransform.localPosition = _camPos;

                yield return null;
            }

            m_duringCrouchAnimation = false;
        }

        #endregion

        #region Landing Methods
        protected virtual void HandleLanding()
        {
            if (!m_previouslyGrounded && m_isGrounded)
            {
                InvokeLandingRoutine();
            }
        }

        protected virtual void InvokeLandingRoutine()
        {
            if (m_LandRoutine != null)
                StopCoroutine(m_LandRoutine);

            m_LandRoutine = LandingRoutine();
            StartCoroutine(m_LandRoutine);
        }

        protected virtual IEnumerator LandingRoutine()
        {
            float _percent = 0f;
            float _landAmount = 0f;

            float _speed = 1f / landDuration;

            Vector3 _localPos = m_yawTransform.localPosition;
            float _initLandHeight = _localPos.y;

            _landAmount = m_inAirTimer > landTimer ? highLandAmount : lowLandAmount;

            while (_percent < 1f)
            {
                _percent += Time.deltaTime * _speed;
                float _desiredY = landCurve.Evaluate(_percent) * _landAmount;

                _localPos.y = _initLandHeight + _desiredY;
                m_yawTransform.localPosition = _localPos;

                yield return null;
            }
        }
        #endregion

        #region Locomotion Apply Methods

        protected virtual void HandleHeadBob()
        {

            if (movementInputData.HasInput && m_isGrounded && !m_hitWall)
            {
                if (!m_duringCrouchAnimation) // we want to make our head bob only if we are moving and not during crouch routine
                {
                    m_headBob.ScrollHeadBob(movementInputData.IsRunning && CanRun(), movementInputData.IsCrouching, movementInputData.InputVector);
                    m_yawTransform.localPosition = Vector3.Lerp(m_yawTransform.localPosition, (Vector3.up * m_headBob.CurrentStateHeight) + m_headBob.FinalOffset, Time.deltaTime * smoothHeadBobSpeed);
                }
            }
            else // if we are not moving or we are not grounded
            {
                if (!m_headBob.Resetted)
                {
                    m_headBob.ResetHeadBob();
                }

                if (!m_duringCrouchAnimation) // we want to reset our head bob only if we are standing still and not during crouch routine
                    m_yawTransform.localPosition = Vector3.Lerp(m_yawTransform.localPosition, new Vector3(0f, m_headBob.CurrentStateHeight, 0f), Time.deltaTime * smoothHeadBobSpeed);
            }

            //m_camTransform.localPosition = Vector3.Lerp(m_camTransform.localPosition,m_headBob.FinalOffset,Time.deltaTime * smoothHeadBobSpeed);
        }

        protected virtual void HandleCameraSway()
        {
            m_cameraController.HandleSway(m_smoothInputVector, movementInputData.InputVector.x);
        }

        protected virtual void HandleRunFOV()
        {
            if (movementInputData.HasInput && m_isGrounded && !m_hitWall)
            {
                if (movementInputData.RunClicked && CanRun())
                {
                    sounds.Sound2Loop();
                    sounds3.StartRunBreathSound();
                    rechargeStamina = false;
                    m_duringRunAnimation = true;
                    m_cameraController.ChangeRunFOV(false);
                }

                if (movementInputData.IsRunning && CanRun() && !m_duringRunAnimation)
                {
                    if (!sounds.IsPlaying())
                    {
                        sounds3.StartRunBreathSound();
                        sounds.Sound2Loop();
                    }

                    m_duringRunAnimation = true;
                    rechargeStamina = false;
                    m_cameraController.ChangeRunFOV(false);
                }
                stamina += Time.deltaTime;
            }

            if (movementInputData.RunReleased || !movementInputData.HasInput || m_hitWall || (m_currentSpeed != runSpeed && movementInputData.IsRunning) || stamina >= staminaLimit)
            {
                if (m_duringRunAnimation)
                {
                    m_duringRunAnimation = false;
                    sounds3.StopRunBreath();
                    m_cameraController.ChangeRunFOV(true);
                }

                sounds.Stop();
                if (stamina >= staminaLimit)
                {
                    tired = true;
                    if (!sounds2.IsPlaying())
                        sounds2.Sound1();
                }
                rechargeStamina = true;

            }
        }
        protected virtual void HandleJump()
        {
            if (movementInputData.JumpClicked && !movementInputData.IsCrouching)
            {
                //m_finalMoveVector.y += jumpSpeed /* m_currentSpeed */; // we are adding because ex. when we are going on slope we want to keep Y value not overwriting it
                m_finalMoveVector.y = jumpSpeed /* m_currentSpeed */; // turns out that when adding to Y it is too much and it doesn't feel correct because jumping on slope is much faster and higher;

                m_previouslyGrounded = true;
                m_isGrounded = false;
            }
        }
        protected virtual void ApplyGravity()
        {
            if (m_characterController.isGrounded) // if we would use our own m_isGrounded it would not work that good, this one is more precise
            {
                m_inAirTimer = 0f;
                m_finalMoveVector.y = -stickToGroundForce;

                //HandleJump(); no jumping
            }
            else
            {
                m_inAirTimer += Time.deltaTime;
                m_finalMoveVector += Physics.gravity * gravityMultiplier * Time.deltaTime;
            }
        }

        protected virtual void ApplyMovement()
        {
            m_characterController.Move(m_finalMoveVector * Time.deltaTime);
        }

        protected virtual void RotateTowardsCamera()
        {
            Quaternion _currentRot = transform.rotation;
            Quaternion _desiredRot = m_yawTransform.rotation;

            transform.rotation = Quaternion.Slerp(_currentRot, _desiredRot, Time.deltaTime * smoothRotateSpeed);
        }

        protected virtual void HoldBreath()
        {
            if (Input.GetKeyDown(KeyCode.Q) && breath < maxBreath/2)
            {
                m_cameraController.ChangeRunFOV(false);
                if (!sounds2.IsPlaying())
                    sounds2.Sound2();
                isHoldingBreath = true;
            }
            else if ((Input.GetKeyUp(KeyCode.Q) && isHoldingBreath) || breath > maxBreath)
            {
                m_cameraController.ChangeRunFOV(true);
                if (breath > ((maxBreath/2)+1) )
                {
                    if (!sounds2.IsPlaying())
                        sounds2.Sound4();
                }
                else
                {
                    if (!sounds2.IsPlaying())
                    {
                        sounds2.Sound3();
                    }
                }

                isHoldingBreath = false;
            }

        }

        protected virtual void MovementSounds()
        {
            if (movementInputData.HasInput && !sounds.IsPlaying() && !movementInputData.IsRunning && !movementInputData.IsCrouching)
            {
                sounds3.StopRunBreath();
                sounds.Sound1Loop();
            }
            else if ((!movementInputData.HasInput && sounds.IsPlaying()) || movementInputData.CrouchClicked)
            {
                print("stop_sound");
                sounds.Stop();
            }

        }
        protected void RechargeStamina()
        {
            if (rechargeStamina && stamina > 0)
                stamina -= Time.deltaTime * staminaLimit / 2.4f;

            if (stamina < .5f)
                tired = false;
        }
        protected void RechargeBreath()
        {
            if (isHoldingBreath)
                breath += Time.deltaTime;
            else if (!isHoldingBreath && breath > 0)
                breath -= Time.deltaTime * 2;
        }

        protected void HandleState()
        {
            if (isHoldingBreath && (movementInputData.IsCrouching || !movementInputData.HasInput))
                network.ServerSendMessage("PlayerState0");  //hold breath
            else if (!isHoldingBreath && (movementInputData.IsCrouching || !movementInputData.HasInput))
                network.ServerSendMessage("PlayerState1");  //stand
            else if(movementInputData.IsRunning)
                network.ServerSendMessage("PlayerState3");  //sprint
            else
                network.ServerSendMessage("PlayerState2");  //walk
        }

        #endregion
        #endregion
    }
}