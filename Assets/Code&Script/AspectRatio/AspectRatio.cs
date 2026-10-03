using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Camera))]
public class AspectRatio : MonoBehaviour
{
   public float targetAspectRatio = 16f /9f;
    private Camera _camera;
     void Start()
    {
        _camera = GetComponent<Camera>();    
    }

    void setCameraAspect()
    {
        float windowsAspect = (float)Screen.width / Screen.height;
        float scaleHeight = windowsAspect / targetAspectRatio;
        if(scaleHeight < 1.0f )
        {
            Rect rect = _camera.rect;

            rect.width = 1.0f;
            rect.height = scaleHeight;
            rect.x = 0;
            rect.y = (1.0f - scaleHeight) / 2.0f;
            _camera.rect = rect;
        }
        else
        {
            float scaleWidth = 1.0f / scaleHeight;

            Rect rect = _camera.rect;

            rect.width = scaleWidth;
            rect.height = 1.0f;
            rect.x = (1.0f - scaleWidth) / 2.0f;
            rect.y = 0;

            _camera.rect = rect;

        }
    }
    private void Update()
    {
        setCameraAspect();
    }
}
