using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.IO;

public class ApplyRandomBuildingMaterial : EditorWindow
{
    private const string ShaderName =
        "PlateauTriplanerShader/PlateauTriplanarShader(DualTextures)";

    private const string SideTextureFolder =
        "Assets/BldgTexture";

    private const string GeneratedMaterialFolder =
        "Assets/GeneratedBuildingMaterials";

    // ここを対象タグに変更してください
    private const string TargetTag = "○○";

    [MenuItem("Tools/PLATEAU/Apply Random Building Texture By Tag")]
    public static void Apply()
    {
        DateTime startTime = DateTime.Now;
        Debug.Log($"処理開始: {startTime:yyyy/MM/dd HH:mm:ss}");
        Debug.Log($"対象タグ: {TargetTag}");
        Debug.Log("対象条件: Tag一致 かつ オブジェクト名または元FBXファイル名が bldg から始まるもの");

        int dialogResult = EditorUtility.DisplayDialogComplex(
            "PlateauTriplanerShader の上書き確認",
            "すでに PlateauTriplanerShader が設定されているマテリアルがある場合、どうしますか？\n\n" +
            "「上書きする」：既存の PlateauTriplanerShader マテリアルにも再設定する\n" +
            "「上書きしない」：既存の PlateauTriplanerShader マテリアルはスキップする\n" +
            "「キャンセル」：処理を中止する",
            "上書きする",
            "上書きしない",
            "キャンセル"
        );

        if (dialogResult == 2)
        {
            Debug.Log("ユーザーがキャンセルしたため、処理を中止しました。");
            return;
        }

        bool overwriteExisting = (dialogResult == 0);
        Debug.Log($"既存 PlateauTriplanerShader の上書き: {(overwriteExisting ? "する" : "しない")}");

        Shader shader = Shader.Find(ShaderName);

        if (shader == null)
        {
            Debug.LogError($"Shader が見つかりません: {ShaderName}");
            return;
        }

        Texture2D[] sideTextures = LoadTextures(SideTextureFolder);

        if (sideTextures.Length == 0)
        {
            Debug.LogError($"Side-MainTexture用テクスチャが見つかりません: {SideTextureFolder}");
            return;
        }

        EnsureFolderExists(GeneratedMaterialFolder);

        GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>(true);

        GameObject[] targetObjects = allObjects
            .Where(obj =>
                HasTag(obj, TargetTag) &&
                IsBldgObjectOrFbx(obj) &&
                obj.GetComponent<Renderer>() != null
            )
            .ToArray();

        int totalTargets = targetObjects.Length;

        Debug.Log($"対象オブジェクト数: {totalTargets}");
        Debug.Log($"Side-MainTexture用テクスチャ数: {sideTextures.Length}");

        int targetObjectCount = 0;
        int processedMaterialCount = 0;
        int skippedMaterialCount = 0;
        int createdMaterialAssetCount = 0;
        int reusedMaterialAssetCount = 0;
        int logInterval = 100;

        foreach (GameObject obj in targetObjects)
        {
            Renderer renderer = obj.GetComponent<Renderer>();

            if (renderer == null)
                continue;

            Material[] materials = renderer.sharedMaterials;

            for (int i = 0; i < materials.Length; i++)
            {
                Material originalMat = materials[i];

                if (originalMat == null)
                    continue;

                bool isAlreadyPlateauShader =
                    originalMat.shader != null &&
                    originalMat.shader.name == ShaderName;

                if (isAlreadyPlateauShader && !overwriteExisting)
                {
                    skippedMaterialCount++;
                    continue;
                }

                string safeName = MakeSafeFileName(obj.name);
                string matPath = $"{GeneratedMaterialFolder}/{safeName}_{i}.mat";

                bool createdNew;
                Material targetMat = GetOrCreateMaterialAsset(matPath, originalMat, out createdNew);

                if (targetMat == null)
                {
                    Debug.LogWarning($"Materialアセットの取得・作成に失敗しました: {matPath}");
                    continue;
                }

                if (createdNew)
                    createdMaterialAssetCount++;
                else
                    reusedMaterialAssetCount++;

                targetMat.name = safeName + "_" + i;
                targetMat.shader = shader;

                Texture2D randomSideTex =
                    sideTextures[UnityEngine.Random.Range(0, sideTextures.Length)];

                SetTextureIfExists(
                    targetMat,
                    randomSideTex,
                    new[]
                    {
                        "_SideMainTex",
                        "_Side_MainTexture",
                        "_SideMainTexture",
                        "_MainTex"
                    },
                    "Side-MainTexture"
                );

                EditorUtility.SetDirty(targetMat);

                materials[i] = targetMat;
                processedMaterialCount++;
            }

            renderer.sharedMaterials = materials;
            EditorUtility.SetDirty(renderer);

            targetObjectCount++;

            if (targetObjectCount % logInterval == 0 || targetObjectCount == totalTargets)
            {
                DateTime now = DateTime.Now;
                TimeSpan elapsedNow = now - startTime;

                Debug.Log(
                    $"途中経過: {targetObjectCount}/{totalTargets} 件完了 / " +
                    $"Material処理済み {processedMaterialCount} 個 / " +
                    $"スキップ {skippedMaterialCount} 個 / " +
                    $"新規作成 {createdMaterialAssetCount} 個 / " +
                    $"再利用 {reusedMaterialAssetCount} 個 / " +
                    $"経過 {elapsedNow.TotalSeconds:F1} 秒 / " +
                    $"現在: {obj.name}"
                );
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        DateTime endTime = DateTime.Now;
        TimeSpan elapsed = endTime - startTime;

        Debug.Log($"処理終了: {endTime:yyyy/MM/dd HH:mm:ss}");
        Debug.Log($"処理時間: {elapsed.TotalSeconds:F2} 秒");
        Debug.Log(
            $"完了: タグ '{TargetTag}' の対象オブジェクト {targetObjectCount} 個 / " +
            $"Material処理済み {processedMaterialCount} 個 / " +
            $"スキップ {skippedMaterialCount} 個 / " +
            $"新規作成 {createdMaterialAssetCount} 個 / " +
            $"再利用 {reusedMaterialAssetCount} 個"
        );
    }

    private static bool HasTag(GameObject obj, string tagName)
    {
        try
        {
            return obj.CompareTag(tagName);
        }
        catch (UnityException)
        {
            Debug.LogError($"タグ '{tagName}' がUnityに登録されていません。Tags and Layersで作成してください。");
            return false;
        }
    }

    private static bool IsBldgObjectOrFbx(GameObject obj)
    {
        if (obj.name.StartsWith("bldg", StringComparison.OrdinalIgnoreCase))
            return true;

        GameObject root = PrefabUtility.GetNearestPrefabInstanceRoot(obj);

        if (root == null)
            root = obj.transform.root.gameObject;

        UnityEngine.Object prefabSource = PrefabUtility.GetCorrespondingObjectFromSource(root);

        if (prefabSource == null)
        {
            return root.name.StartsWith("bldg", StringComparison.OrdinalIgnoreCase);
        }

        string assetPath = AssetDatabase.GetAssetPath(prefabSource);

        if (string.IsNullOrEmpty(assetPath))
        {
            return root.name.StartsWith("bldg", StringComparison.OrdinalIgnoreCase);
        }

        string fileName = Path.GetFileNameWithoutExtension(assetPath);

        return fileName.StartsWith("bldg", StringComparison.OrdinalIgnoreCase);
    }

    private static Material GetOrCreateMaterialAsset(string matPath, Material sourceMat, out bool createdNew)
    {
        Material existingMat = AssetDatabase.LoadAssetAtPath<Material>(matPath);

        if (existingMat != null)
        {
            createdNew = false;
            return existingMat;
        }

        Material newMat = new Material(sourceMat);
        AssetDatabase.CreateAsset(newMat, matPath);
        createdNew = true;
        return newMat;
    }

    private static Texture2D[] LoadTextures(string folderPath)
    {
        Debug.Log($"LoadTextures開始: {folderPath}");

        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            Debug.LogError($"指定フォルダが存在しません: {folderPath}");
            return new Texture2D[0];
        }

        string[] textureGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { folderPath });

        Debug.Log($"検出したTexture2D数: {textureGuids.Length}");

        foreach (string guid in textureGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Debug.Log($"検出テクスチャ: {path}");
        }

        Texture2D[] textures = textureGuids
            .Select(guid =>
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

                if (importer != null)
                {
                    bool changed = false;

                    if (importer.maxTextureSize != 1024)
                    {
                        importer.maxTextureSize = 1024;
                        changed = true;
                    }

                    if (importer.textureType != TextureImporterType.Default)
                    {
                        importer.textureType = TextureImporterType.Default;
                        changed = true;
                    }

                    if (changed)
                    {
                        importer.SaveAndReimport();
                        Debug.Log($"Import設定を変更: {path}");
                    }
                }

                return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            })
            .Where(tex => tex != null)
            .ToArray();

        return textures;
    }

    private static void SetTextureIfExists(
        Material material,
        Texture2D texture,
        string[] propertyNames,
        string displayName
    )
    {
        foreach (string propertyName in propertyNames)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetTexture(propertyName, texture);

                Debug.Log(
                    $"{material.name}: {displayName} に {texture.name} を設定しました。Property: {propertyName}"
                );

                return;
            }
        }

        Debug.LogWarning($"{material.name}: {displayName} のプロパティが見つかりませんでした。");
    }

    private static void EnsureFolderExists(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
            return;

        string[] parts = folderPath.Split('/');
        string currentPath = parts[0];

        for (int i = 1; i < parts.Length; i++)
        {
            string nextPath = currentPath + "/" + parts[i];

            if (!AssetDatabase.IsValidFolder(nextPath))
            {
                AssetDatabase.CreateFolder(currentPath, parts[i]);
            }

            currentPath = nextPath;
        }
    }

    private static string MakeSafeFileName(string name)
    {
        string safeName = name;

        foreach (char c in Path.GetInvalidFileNameChars())
        {
            safeName = safeName.Replace(c, '_');
        }

        safeName = safeName.Replace("/", "_").Replace("\\", "_");

        return safeName;
    }
}
