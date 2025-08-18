using UnityEngine;
using UnityEngine.Events;

namespace PresentFutures.XRAI.Florence
{
    public class TouchControllerEvent : MonoBehaviour
    {
        public UnityEvent OnControllerButtonPressed;

        // Update is called once per frame
        void Update()
        {
            if (OVRInput.GetDown(OVRInput.RawButton.A))
            {
                OnControllerButtonPressed?.Invoke();
            }
        }
    }
}
