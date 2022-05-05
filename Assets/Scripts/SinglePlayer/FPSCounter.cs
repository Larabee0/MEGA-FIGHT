using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SinglePlayerRunTime
{
    public class FPSCounter : MonoBehaviour
    {
        [SerializeField] private Text FPSOUT;

        private int FPS
        {
            set
            {
                FPSOUT.text = string.Format("{0} fps", value.ToString("D4"));
            }
        }

        void Start()
        {
            if(FPSOUT == null)
            {
                enabled = false;
            }
        }

        void Update()
        {
            FPS = (int)(1f / Time.unscaledDeltaTime);
        }
    }
}