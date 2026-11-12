
using UnityEngine;
using UnityEditor;

public static class BuildFX5045Vise
{
    [MenuItem("Tools/FX5045/Repair Vise V2 Materials")]
    public static void RepairMaterials()
    {
        GameObject vise = GameObject.Find("VISE_ASSEMBLY_V2");

        if (vise == null)
        {
            Debug.LogError("VISE_ASSEMBLY_V2 not found!");
            return;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");

        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Simple Lit");

        if (shader == null)
        {
            Debug.LogError("URP shader not found!");
            return;
        }

        Material castIron = CreateMaterial(
            "Vise_Cast_Iron", new Color(0.19f, 0.23f, 0.28f), 0.65f, 0.35f, shader);

        Material steel = CreateMaterial(
            "Vise_Machined_Steel", new Color(0.48f, 0.53f, 0.59f), 0.9f, 0.65f, shader);

        Material darkSteel = CreateMaterial(
            "Vise_Dark_Steel", new Color(0.10f, 0.12f, 0.15f), 0.7f, 0.35f, shader);

        Material jaw = CreateMaterial(
            "Vise_Hardened_Jaws", new Color(0.32f, 0.36f, 0.40f), 0.9f, 0.6f, shader);

        Material screw = CreateMaterial(
            "Vise_Screw", new Color(0.65f, 0.68f, 0.72f), 0.95f, 0.7f, shader);

        Material black = CreateMaterial(
            "Vise_Recesses", new Color(0.035f, 0.04f, 0.045f), 0.25f, 0.25f, shader);

        Material brass = CreateMaterial(
            "Vise_Brass", new Color(0.58f, 0.43f, 0.20f), 0.85f, 0.65f, shader);

        Renderer[] renderers = vise.GetComponentsInChildren<Renderer>(true);

        int fixedCount = 0;

        foreach (Renderer r in renderers)
        {
            string n = r.gameObject.name.ToUpperInvariant();

            if (n.Contains("THREAD") || n.Contains("SCREW") ||
                n.Contains("COLLAR") || n.Contains("HUB"))
                r.sharedMaterial = screw;

            else if (n.Contains("JAW_PLATE"))
                r.sharedMaterial = jaw;

            else if (n.Contains("HANDWHEEL_GRIP"))
                r.sharedMaterial = black;

            else if (n.Contains("HANDWHEEL_CENTER") ||
                     n.Contains("BOLT"))
                r.sharedMaterial = brass;

            else if (n.Contains("HANDWHEEL") ||
                     n.Contains("GUIDE_WEAR") ||
                     n.Contains("TOP_MACHINED") ||
                     n.Contains("GUIDE_RAIL") ||
                     n.Contains("SIDE_RIB"))
                r.sharedMaterial = steel;

            else if (n.Contains("SLOT") ||
                     n.Contains("RECESS"))
                r.sharedMaterial = black;

            else if (n.Contains("CASTING") ||
                     n.Contains("JAW_BODY") ||
                     n.Contains("BASE") ||
                     n.Contains("RIB"))
                r.sharedMaterial = castIron;

            else
                r.sharedMaterial = darkSteel;

            fixedCount++;
        }

        EditorUtility.SetDirty(vise);

        Debug.Log("Vise V2 materials repaired. Renderers: " + fixedCount);
    }

    static Material CreateMaterial(
        string name, Color color, float metallic, float smoothness, Shader shader)
    {
        string folder = "Assets/GeneratedViseMaterials";

        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets", "GeneratedViseMaterials");

        string path = folder + "/" + name + ".mat";

        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }

        mat.shader = shader;
        mat.color = color;

        if (mat.HasProperty("_Metallic"))
            mat.SetFloat("_Metallic", metallic);

        if (mat.HasProperty("_Smoothness"))
            mat.SetFloat("_Smoothness", smoothness);

        EditorUtility.SetDirty(mat);
        return mat;
    }
}
