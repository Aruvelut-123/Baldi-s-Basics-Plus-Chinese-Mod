using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BBPC.API
{
    public class PatchedGlobalCamCanvasAssigner : MonoBehaviour
    {
        public Canvas? canvas;

        [SerializeField]
        private float planeDistance = 0.31f;

        private void Awake()
        {
            canvas.worldCamera = Singleton<GlobalCam>.Instance.Cam;
            canvas.planeDistance = planeDistance;
        }
    }
}
