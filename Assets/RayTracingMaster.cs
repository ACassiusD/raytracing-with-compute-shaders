using UnityEngine;
using UnityEngine.Experimental.Rendering;

public class RayTracingMaster : MonoBehaviour
{
    // Compute shaders arnt like post processing shaders, that take a texture like the screen as an input and modify it.
    // Instead they are used to perform calculations on the GPU. In this case, ray tracing.
    // We do ray tracing calculations on the GPU because it is much faster than doing it on the CPU.
    public ComputeShader rayTracingComputeShader; //A reference to the compute shader we created.
    private RenderTexture renderTargetTexture; //Texture we will render to.
    private Camera camera;
    public Texture SkyboxTexture;

    private void Awake()
    {
        camera = GetComponent<Camera>();
    }

    // OnRenderImage is a Unity method that gets called after the camera finishes rendering all the objects in the scene
    // This makes it an ideal place to implement post-processing effects
    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        SetShaderParameters();
        Render(destination);
    }

    private void Render(RenderTexture destination)
    {
        // Make sure we have a current render target
        InitRenderTexture();

        //Set the target and dispatch the compute shader
        rayTracingComputeShader.SetTexture(0, "Result", renderTargetTexture);

        // Calculating the number of thread groups we need to dispatch
        // Threads are used to divide the workload for parallel processing on the GPU.
        // Each thread group is expected to handle an 8x8 block of pixels
        int threadGroupsX = Mathf.CeilToInt(Screen.width / 8.0f);
        int threadGroupsY = Mathf.CeilToInt(Screen.height / 8.0f);

        rayTracingComputeShader.Dispatch(0, threadGroupsX, threadGroupsY, 1);

        // Blit (Block Image Transfer) copys pixel data from one texture to another
        // In this case, copying it to the screen.
        Graphics.Blit(renderTargetTexture, destination);
    }

    //Create the render texture. A Render texture is not in the scene but rather an off screen buffer / canvis that we can render to.
    private void InitRenderTexture()
    {
        //If the texture has never been created or if the screen size has changed, we need to create the texture again.
        if (renderTargetTexture == null || renderTargetTexture.width != Screen.width || renderTargetTexture.height != Screen.height)
        {
            // Release previous render texture 
            if (renderTargetTexture != null)
                renderTargetTexture.Release();
            
            // Set the new render target for Ray Tracing to the size of the screen
            renderTargetTexture = new RenderTexture(Screen.width, Screen.height, 0,
                RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);

            // Shader has the ability to write to any pixel in the texture at any time.
            renderTargetTexture.enableRandomWrite = true;

            renderTargetTexture.Create();
        }
    }

    // _CameraToWorld - is defining the camera's view in the scene
    
    // _CameraInverseProjection - a tool for translating the flat image on the screen back into the 3D space from which it originated.
    // We esentially need to reverse the process that projected the 3d world space onto the 2d screen space.
    // When using CameraInverseProjection matrix to unproject 2D screen coordinates, we don't get a specific point in 3D space with depth; we get a direction vector. 
    // representing the direction from the camera's viewpoint through the pixel on the virtual film plane (imagine a window that the camera is looking through).
    // To find the depth, or how far away things are, we cast rays along this direction vector and see where they intersect with objects in the scene.
    // The intersection point's distance from the camera gives us the depth. This is calculated for every pixel during the ray tracing process,
    // allowing us to construct the final image with proper depth and perspective.
    private void SetShaderParameters()
    {
        // Tells the shader where the camera is in world space.
        rayTracingComputeShader.SetMatrix("_CameraToWorld", camera.cameraToWorldMatrix);

        // To figure out which direction rays should be cast in the 3d world space from a given pixel on the screen
        rayTracingComputeShader.SetMatrix("_CameraInverseProjection", camera.projectionMatrix.inverse);

        rayTracingComputeShader.SetTexture(0, "_SkyboxTexture", SkyboxTexture);
    }
}
