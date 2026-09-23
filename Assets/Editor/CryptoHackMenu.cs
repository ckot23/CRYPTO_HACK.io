#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CryptoHack.EditorTools
{
    /// <summary>
    /// Меню редактора: игра запускается и без сцены (Bootstrap сам создаёт объекты),
    /// но для сборки билда сцена нужна. Этот пункт создаёт Assets/Scenes/Game.unity
    /// и добавляет её в список сцен сборки (Build Settings в 2022.3,
    /// Build Profiles в Unity 6.6).
    /// </summary>
    public static class CryptoHackMenu
    {
        const string ScenePath = "Assets/Scenes/Game.unity";

        [MenuItem("CRYPTO_HACK/Создать сцену и добавить в сборку")]
        public static void CreateScene()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            {
                AssetDatabase.CreateFolder("Assets", "Scenes");
            }

            if (System.IO.File.Exists(ScenePath))
            {
                Debug.Log("Сцена уже есть: " + ScenePath);
            }
            else
            {
                Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, ScenePath);
                Debug.Log("Создана сцена " + ScenePath);
            }

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            Debug.Log("Сцена добавлена в список сборки. В Unity 6.6 дальше: File → Build Profiles → Build.");
        }

        [MenuItem("CRYPTO_HACK/Открыть гайд по установке (INSTALL.md)")]
        public static void OpenGuide()
        {
            string path = System.IO.Path.Combine(Application.dataPath, "..", "INSTALL.md");
            if (System.IO.File.Exists(path))
            {
                Application.OpenURL("file://" + System.IO.Path.GetFullPath(path));
            }
            else
            {
                Debug.Log("INSTALL.md не найден рядом с папкой Assets.");
            }
        }
    }
}
#endif
