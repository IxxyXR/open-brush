﻿// Copyright 2020 The Tilt Brush Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using UnityEngine;
using System.Collections;

namespace TiltBrush
{

    public class MonoscopicControlScript : MonoBehaviour
    {
        private float m_xScale = 5f;
        private float m_yScale = 2.4f;
        private float m_drawSensitivity = 0.2f;
        private float m_yClamp = 85f;

        private float m_zCameraOffset = 2.5f;
        private float m_minZCameraOffset = 2.0f;
        private float m_maxZCameraOffset = 5.0f;
        private float m_zOffsetInterval = 0.5f;

        private float m_translateSpeed = 0.02f;
        private float m_maxMovementSpeed = 1.0f;
        private float m_minMovementSpeed = 0.02f;
        private float m_scrollSpeed = .01f;

        private Vector3 m_syncedTransform;
        private Vector3 m_cameraRotation;
        private Vector3 m_cameraPosition;
        private Vector3 movementOffset;
        private bool isMoving;
        private bool isDrawMode = false;

        public bool isUiVisible = false;

        void Start()
        {
            if (!App.Instance.IsMonoscopicMode())
            {
                return;
            }

            Screen.SetResolution(1920, 1080, false);
        }

        void FixedUpdate()
        {
            if (!App.Instance.IsMonoscopicMode())
            {
                return;
            }

            // Toggle draw mode
            if (!isMoving && InputManager.m_Instance.GetKeyboardShortcutDown(
                InputManager.KeyboardShortcut.ToggleMonoCameraDrawMode))
            {
                isDrawMode = !isDrawMode;
            }
            
            if (InputManager.m_Instance.GetKeyboardShortcutDown(
                InputManager.KeyboardShortcut.ToggleMonoUi))
            {
                isUiVisible = !isUiVisible;
                SketchControlsScript.m_SketchSurfacePanel.EnableAllTools(isUiVisible);
            }

            m_cameraPosition = transform.localPosition;
            movementOffset = new Vector3(0.0f, 0.0f, 0.0f);

            // Adjust distance betwen cursor and camera
            if (InputManager.m_Instance.GetKeyboardShortcutDown(
                InputManager.KeyboardShortcut.MonoCameraIncreaseCursorDistance))
            {
                m_zCameraOffset = Mathf.Min(m_zCameraOffset + m_zOffsetInterval, m_maxZCameraOffset);
            }
            else if (!isMoving && InputManager.m_Instance.GetKeyboardShortcutDown(
              InputManager.KeyboardShortcut.MonoCameraDecreaseCursorDistance))
            {
                m_zCameraOffset = Mathf.Max(m_zCameraOffset - m_zOffsetInterval, m_minZCameraOffset);
            }

            // Adjust movement speed from scroll wheel
            if (Input.GetAxis("Mouse ScrollWheel") > 0f)
            {
                m_translateSpeed = Mathf.Min(m_translateSpeed + m_scrollSpeed, m_maxMovementSpeed);
            }
            else if (Input.GetAxis("Mouse ScrollWheel") < 0f)
            {
                m_translateSpeed = Mathf.Max(m_translateSpeed - m_scrollSpeed, m_minMovementSpeed);
            }

            Debug.Log(m_translateSpeed);

            transform.localEulerAngles = m_cameraRotation;
            isMoving = false;

            // Movement inputs
            if (!isDrawMode)
            {
                if (InputManager.m_Instance.GetKeyboardShortcut(
                    InputManager.KeyboardShortcut.MonoCameraForward))
                {
                    isMoving = true;
                    movementOffset += transform.forward * m_translateSpeed;
                }

                if (InputManager.m_Instance.GetKeyboardShortcut(
                    InputManager.KeyboardShortcut.MonoCameraBackward))
                {
                    isMoving = true;
                    movementOffset -= transform.forward * m_translateSpeed;
                }

                if (InputManager.m_Instance.GetKeyboardShortcut(
                    InputManager.KeyboardShortcut.MonoCameraLeft))
                {
                    isMoving = true;
                    movementOffset -= transform.right * m_translateSpeed;
                }

                if (InputManager.m_Instance.GetKeyboardShortcut(
                    InputManager.KeyboardShortcut.MonoCameraRight))
                {
                    isMoving = true;
                    movementOffset += transform.right * m_translateSpeed;
                }

                if (InputManager.m_Instance.GetKeyboardShortcut(
                    InputManager.KeyboardShortcut.MonoCameraUp))
                {
                    isMoving = true;
                    movementOffset += transform.up * m_translateSpeed;
                }

                if (InputManager.m_Instance.GetKeyboardShortcut(
                    InputManager.KeyboardShortcut.MonoCameraDown))
                {
                    isMoving = true;
                    movementOffset -= transform.up * m_translateSpeed;
                }
            }

            m_cameraPosition += movementOffset;

            transform.localPosition = m_cameraPosition;
            transform.localEulerAngles = m_cameraRotation;

            // Use mouse position to control camera rotation.
            if (!isDrawMode)
            {
                m_syncedTransform = transform.position + (transform.forward * m_zCameraOffset);
                // Mouse's x coordinate corresponds to camera's rotation around y axis.
                m_cameraRotation.y += Input.GetAxis("Mouse X") * m_xScale;
                if (m_cameraRotation.y <= -180)
                {
                    m_cameraRotation.y += 360;
                }
                else if (m_cameraRotation.y > 180)
                {
                    m_cameraRotation.y -= 360;
                }

                // Mouse's y coordinate corresponds to camera's rotation around x axis.
                m_cameraRotation.x -= Input.GetAxis("Mouse Y") * m_yScale;
                m_cameraRotation.x = Mathf.Clamp(m_cameraRotation.x, -m_yClamp, m_yClamp);

            }
            else
            {
                m_syncedTransform = SketchControlsScript.m_Instance.getSketchSurfacePos() + movementOffset;
                m_syncedTransform += (Input.GetAxis("Mouse Y") * m_drawSensitivity * transform.up);
                m_syncedTransform += (Input.GetAxis("Mouse X") * m_drawSensitivity * transform.right);
            }

            SketchControlsScript.m_Instance.SyncMonoPosition(m_syncedTransform, m_cameraRotation);
        }

        private string log;
        private const int MAXCHARS = 10000;
        private Queue myLogQueue = new Queue();

        void OnEnable()
        {
            Application.logMessageReceived += HandleLog;
        }

        void OnDisable()
        {
            Application.logMessageReceived -= HandleLog;
        }

        void HandleLog(string logString, string stackTrace, LogType type)
        {
            myLogQueue.Enqueue("\n [" + type + "] : " + logString);
            if (type == LogType.Exception)
                myLogQueue.Enqueue("\n" + stackTrace);
        }

        void Update()
        {
            while (myLogQueue.Count > 0)
                log = myLogQueue.Dequeue() + log;
            if (log.Length > MAXCHARS)
                log = log.Substring(0, MAXCHARS);
        }

        void OnGUI()
        {
            // GUILayout.Label(log);
        }
    }
} // namespace TiltBrush
