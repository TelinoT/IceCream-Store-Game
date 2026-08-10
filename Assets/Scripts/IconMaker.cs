using UnityEngine;
using System.IO;

public class IconMaker : MonoBehaviour
{
    public Camera snapCam;
    public int resWidth = 512;
    public int resHeight = 512;
    
    [Tooltip("Change this name before clicking Take Picture!")]
    public string saveName = "Icon_Basket"; 
    
    [Tooltip("If checked, turns the entire 3D model into a flat white silhouette.")]
    public bool makePureWhite = true;

    [ContextMenu("Take Picture")]
    public void TakePicture()
    {
        if (snapCam == null) snapCam = GetComponent<Camera>();

        // --- FIX 1: Add 8x Anti-Aliasing to the RenderTexture for buttery smooth edges! ---
        RenderTexture rt = new RenderTexture(resWidth, resHeight, 24);
        rt.antiAliasing = 8; 
        snapCam.targetTexture = rt;
        
        Texture2D screenShot = new Texture2D(resWidth, resHeight, TextureFormat.ARGB32, false);
        snapCam.Render();
        RenderTexture.active = rt;
        
        screenShot.ReadPixels(new Rect(0, 0, resWidth, resHeight), 0, 0);
        
        if (makePureWhite)
        {
            Color[] pixels = screenShot.GetPixels();
            for (int i = 0; i < pixels.Length; i++)
            {
                // --- FIX 2: No more blunt clipping! 
                // We just force the color to pure white, but keep the EXACT original 
                // soft transparency of the anti-aliased edge.
                pixels[i] = new Color(1f, 1f, 1f, pixels[i].a);
            }
            screenShot.SetPixels(pixels);
            screenShot.Apply();
        }
        
        snapCam.targetTexture = null;
        RenderTexture.active = null; 
        DestroyImmediate(rt);
        
        byte[] bytes = screenShot.EncodeToPNG();
        string path = Application.dataPath + "/CustomIcons/" + "Icon_" + saveName + ".png";
        File.WriteAllBytes(path, bytes);
        
        Debug.Log("📸 Snap! Saved buttery-smooth icon to: " + path);
        
        #if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
        #endif
    }
}