using UnityEngine;

namespace TiltBrush
{
    public class PolyhydraPopUpWindowBase : PopUpWindow
    {
        [SerializeField] protected float m_ColorTransitionDuration;
        [SerializeField] protected GameObject ButtonPrefab;
  
        protected float m_ColorTransitionValue;
        protected Material m_ColorBackground;

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
    }
}