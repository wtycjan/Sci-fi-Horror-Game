﻿using NaughtyAttributes;
using System;
using UnityEngine;

namespace VHS
{    
    public class CameraController : MonoBehaviour
    {
        #region Variables
            #region Data
                [Space,Header("Data")]
                [SerializeField] private CameraInputData camInputData = null;

                [Space,Header("Custom Classes")]
                [SerializeField] private CameraZoom cameraZoom = null;
                [SerializeField] private CameraSwaying cameraSway = null;

            #endregion

            #region Settings
                [Space,Header("Look Settings")]
                [SerializeField] private Vector2 sensitivity = Vector2.zero;
                [SerializeField] private Vector2 smoothAmount = Vector2.zero;
                [SerializeField] [MinMaxSlider(-90f,90f)] private Vector2 lookAngleMinMax = Vector2.zero;
            #endregion

            #region Private
               private float m_yaw;
               private float m_pitch;

               private float mouseSensivity;

               private float m_desiredYaw;
               private float m_desiredPitch;
                private float m_tilt;
                private float _tiltAmount=0;
                private bool isTilting = false;

                #region Components                    
                    private Transform m_pitchTranform;
                    private Camera m_cam;

                #endregion
            #endregion
            
        #endregion

        #region BuiltIn Methods  
            void Awake()
            {

                GetComponents();
                InitValues();
                InitComponents();
                ChangeCursorState();
            }
        void Start()
        {
            SetSensivity();
        }

            void LateUpdate()
            {
                CalculateRotation();
                SmoothRotation();
                ApplyRotation();
                HandleZoom();
                //Peak();
            }

        private void SetSensivity()
        {
            mouseSensivity = PlayerPrefs.GetFloat("sensivity-volume", 0.8f);
        }
        #endregion

        #region Custom Methods
        void GetComponents()
            {
                m_pitchTranform = transform.GetChild(0).transform;
                m_cam = GetComponentInChildren<Camera>();
            }

            void InitValues()
            {
                m_yaw = transform.eulerAngles.y;
                m_desiredYaw = m_yaw;
            }

            void InitComponents()
            {
                cameraZoom.Init(m_cam, camInputData);
                cameraSway.Init(m_cam.transform);
            }

            void CalculateRotation()
            {
                SetSensivity();
                m_desiredYaw += camInputData.InputVector.x * sensitivity.x * mouseSensivity * Time.deltaTime;
                m_desiredPitch -= camInputData.InputVector.y * sensitivity.y * mouseSensivity * Time.deltaTime;

                m_desiredPitch = Mathf.Clamp(m_desiredPitch,lookAngleMinMax.x,lookAngleMinMax.y);
            }

            void SmoothRotation()
            {
                m_yaw = Mathf.Lerp(m_yaw,m_desiredYaw, smoothAmount.x * Time.deltaTime);
                m_pitch = Mathf.Lerp(m_pitch,m_desiredPitch, smoothAmount.y * Time.deltaTime);
                m_tilt = Mathf.Lerp(m_tilt, _tiltAmount, 2.5f*Time.deltaTime);
        }

            void ApplyRotation()
            {

                transform.eulerAngles = new Vector3(0f,m_yaw, m_tilt);
                m_pitchTranform.localEulerAngles = new Vector3(m_pitch,0f,0f);
            }

            public void HandleSway(Vector3 _inputVector,float _rawXInput)
            {
                cameraSway.SwayPlayer(_inputVector,_rawXInput);
            }

            void HandleZoom()
            {
                if(camInputData.ZoomClicked || camInputData.ZoomReleased)
                    cameraZoom.ChangeFOV(this);

            }

            public void ChangeRunFOV(bool _returning)
            {
                cameraZoom.ChangeRunFOV(_returning,this);
            }

            void ChangeCursorState()
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

        /*void Peak()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            _tiltAmount = 14;
        }
        else if (Input.GetKeyUp(KeyCode.Q))
        {
            _tiltAmount = 0;
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            _tiltAmount = -14;
        }
        else if (Input.GetKeyUp(KeyCode.E))
        {
            _tiltAmount = 0;
        }
    }*/
        #endregion
    }
}
