using UnityEditor;
using UnityEngine;
/// <summary>
/// skript for the rotation of the camera around a target, useful for observing the sphere
/// Created with help of chatgpt
/// </summary>
public class OrbitCamera : MonoBehaviour
{
    public Transform target; 
    public float distance = 10f;
    public float minDistance = 2f;
    public float maxDistance = 20f;
    public float sensitivity = 2f;
    public float scrollSpeed = 5f;

    private float yaw = 0f;
    private float pitch = 0f;

    void Start()
    {
        if (target != null)
        {
            Vector3 angles = transform.eulerAngles;
            yaw = angles.y;
            pitch = angles.x;
        }
    }

    void Update()
    {
        if (target == null) return;
        //Makes sure zooming only when mouse is in game
#if UNITY_EDITOR
        if (EditorWindow.focusedWindow != null && EditorWindow.focusedWindow.titleContent.text != "Game")
            return;
#endif
        // Rotate on right mouse button drag
        if (Input.GetMouseButton(1))
        {
            yaw += Input.GetAxis("Mouse X") * sensitivity;
            pitch -= Input.GetAxis("Mouse Y") * sensitivity;
            pitch = Mathf.Clamp(pitch, -80f, 80f); // Prevent flipping
        }

        // Zoom with scroll wheel
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.0001f)
        {
            float zoomFactor = 1f - scroll * scrollSpeed * 0.1f;
            distance *= zoomFactor;
            distance = Mathf.Clamp(distance, minDistance, maxDistance);
        }

        // Apply transformation
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
        Vector3 offset = rotation * new Vector3(0, 0, -distance);
        transform.position = target.position + offset;
        transform.LookAt(target.position);

        Camera.main.depthTextureMode |= DepthTextureMode.Depth;

    }
}
