using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TiltBrush
{
    public abstract class PolyhydraPopUpWindowBase : PopUpWindow
    {
        [SerializeField] protected float m_ColorTransitionDuration;
        [SerializeField] protected GameObject ButtonPrefab;

        protected float m_ColorTransitionValue;
        protected Material m_ColorBackground;
        protected PolyhydraPanel ParentPanel;
        
        override protected void BaseUpdate() {
            
            base.BaseUpdate();

            m_UIComponentManager.SetColor(Color.white);

            // TODO: Make linear into smooth step!
            if (m_ColorBackground &&
                m_TransitionValue == m_TransitionDuration &&
                m_ColorTransitionValue < m_ColorTransitionDuration) {
                m_ColorTransitionValue += Time.deltaTime;
                if (m_ColorTransitionValue > m_ColorTransitionDuration) {
                    m_ColorTransitionValue = m_ColorTransitionDuration;
                }
                float greyVal = 1 - m_ColorTransitionValue / m_ColorTransitionDuration;
                m_ColorBackground.color = new Color(greyVal, greyVal, greyVal);
            }
        }

        protected override void UpdateOpening() {
            if (m_ColorBackground && m_TransitionValue == 0) {
                m_ColorBackground.color = Color.white;
            }
            base.UpdateOpening();
        }

        protected override void UpdateClosing() {
            if (m_ColorBackground) {
                float greyVal = 1 - m_TransitionValue / m_TransitionDuration;
                m_ColorBackground.color = new Color(greyVal, greyVal, greyVal);
            }
            base.UpdateClosing();
        }
        
        public override void Init(GameObject rParent, string sText) {
    
            m_ColorBackground = m_Background.GetComponent<MeshRenderer>().material;
            base.Init(rParent, sText);
            ParentPanel = FindObjectOfType<PolyhydraPanel>();
            CreateButtons();
        }

        protected abstract Array GetButtonList();

        private void CreateButtons()
        {
            float buttonSpacing = m_ButtonWidth * 0.25f;
            float baseButtonLeftBuffer = m_ButtonWidth + buttonSpacing;
            float totalWindowWidth = transform.localScale.x;

            Vector3 vTransformedBase = transform.TransformPoint(m_BaseButtonOffset);

            var buttonEnumArray = GetButtonList();
            List<string> buttonLabels = new List<string>();
            foreach (var item in buttonEnumArray)
            {
                buttonLabels.Add(item.ToString());
            }
            
            for(int i = 0; i < buttonLabels.Count; i++)
            {
      
                GameObject rButton = (GameObject)Instantiate(ButtonPrefab);

                Vector3 vOffset = transform.right;
                vOffset *= (totalWindowWidth * -0.5f) + baseButtonLeftBuffer + ((m_ButtonWidth + buttonSpacing) * (i % 3f));
                vOffset.y += -0.15f * Mathf.FloorToInt(i / 3f);

                Vector3 vButtonPos = vTransformedBase + vOffset;
                rButton.transform.position = vButtonPos;

                Vector3 vButtonScale = rButton.transform.localScale;
                vButtonScale.Set(m_ButtonWidth, m_ButtonWidth, m_ButtonWidth);
                rButton.transform.localScale = vButtonScale;

                rButton.transform.rotation = transform.rotation;
                rButton.transform.parent = transform;

                Renderer rButtonRenderer = rButton.GetComponent<Renderer>();
                rButtonRenderer.material.mainTexture = Resources.Load<Texture2D>(GetButtonTexture(i));

                PolyhydraThingButton rButtonScript = rButton.GetComponent<PolyhydraThingButton>();
                rButtonScript.ButtonIndex = i;
                rButtonScript.parentPopup = this;
                rButtonScript.SetDescriptionText(buttonLabels[i].ToString());
                rButtonScript.RegisterComponent();
            }
        }

        protected abstract string GetButtonTexture(int i);
        override public void UpdateUIComponents(Ray rCastRay, bool inputValid, Collider parentCollider) {
            if (m_IsLongPressPopUp) {
                // Don't bother updating the popup if we're a long press and we're closing.
                if (m_CurrentState == State.Closing) {
                    return;
                }
                // If this is a long press popup and we're done holding the button down, get out.
                if (m_CurrentState == State.Standard && !inputValid) {
                    RequestClose();
                }
            }

            base.UpdateUIComponents(rCastRay, inputValid, parentCollider);
        }

        public virtual void PolyhydraThingButtonPressed(int ButtonIndex)
        {
            ParentPanel.PolyhydraModel.Validate();
            ParentPanel.PolyhydraModel.MakePolyhedron();
        }
    }
}