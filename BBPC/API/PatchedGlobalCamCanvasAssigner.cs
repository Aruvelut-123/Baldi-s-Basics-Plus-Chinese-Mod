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
            if (canvas != null)
            {
                canvas.worldCamera = Singleton<GlobalCam>.Instance.Cam;
                canvas.planeDistance = planeDistance;
            }
        }
    }
}
