using UnityEngine;
using GLTFast;
using System.IO;
using System.Collections.Generic;

namespace Canvastique3D
{
    public class ModelController : MonoBehaviour
    {
        [SerializeField]
        private GameObject spawnPoint;

        [SerializeField]
        private Material modelMaterial;

        private Material originalMaterial;

        private string modelFilePath = null;

        public string getModelFilePath() => modelFilePath;

        public void LoadModel()
        {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
            NativeWindowsFileDialog fileDialog = new NativeWindowsFileDialog();
            string filter = "glTF Files (*.glb;*.gltf)|*.glb;*.gltf|All files (*.*)|*.*";
            string filePath = fileDialog.OpenFileLoader(filter);

            if (!string.IsNullOrEmpty(filePath))
            {
                if (spawnPoint.transform.childCount > 0)
                {
                    foreach (Transform child in spawnPoint.transform)
                    {
                        Destroy(child.gameObject);
                    }

                    Animation[] animations = spawnPoint.GetComponentsInChildren<Animation>();
                    foreach (Animation anim in animations)
                    {
                        Destroy(anim);
                    }

                    Animator[] animators = spawnPoint.GetComponentsInChildren<Animator>();
                    foreach (Animator animator in animators)
                    {
                        Destroy(animator);
                    }
                }
                LoadModelFromPath(filePath);
            }
#endif
        }

        private async void LoadModelFromPath(string path)
        {
            var settings = new ImportSettings
            {
                GenerateMipMaps = true,
                AnisotropicFilterLevel = 3,
                NodeNameMethod = NameImportMethod.OriginalUnique
            };
            var gltfImport = new GltfImport();
            await gltfImport.Load(path, settings);
            var instantiator = new GameObjectInstantiator(gltfImport, spawnPoint.transform);
            var success = await gltfImport.InstantiateMainSceneAsync(instantiator);
            if (success)
            {
                EventManager.instance.TriggerModelLoaded(Path.GetFileName(path), GetMaterialNames());

                // Get the SceneInstance to access the instance's properties
                var sceneInstance = instantiator.SceneInstance;

                if (sceneInstance.Cameras is { Count: > 0 })
                {
                    sceneInstance.Cameras[0].enabled = false;
                }

                if (sceneInstance.Lights != null)
                {
                    foreach (var glTFLight in sceneInstance.Lights)
                    {
                        glTFLight.enabled = false;
                    }
                }

                // Play the default (i.e. the first) animation clip
                var legacyAnimation = instantiator.SceneInstance.LegacyAnimation;
                if (legacyAnimation != null)
                {
                    legacyAnimation.Play();
                }

                modelFilePath = path;
            } 
            else
            {
                modelFilePath = null;
                EventManager.instance.TriggerError("Loading glTF failed!");
            }
        }

        // This method updates the spawnPoint's position
        public void ChangePosition(Vector3Int position)
        {
            spawnPoint.transform.position = (Vector3)position * 0.01f;
        }

        // This method updates the spawnPoint's rotation
        public void ChangeRotation(Vector3Int rotation)
        {
            // Convert the Vector3Int rotation to Euler angles
            spawnPoint.transform.eulerAngles = (Vector3)rotation;
        }

        private Material AssignMaterialToRenderer(Renderer renderer, string materialName, Material material)
        {
            Material[] materials = renderer.sharedMaterials;

            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i].name == materialName)
                {
                    var originalMaterial = materials[i];

                    materials[i] = material;

                    renderer.sharedMaterials = materials;
                    
                    return originalMaterial;
                }
            }
            return null;
        }

        public void AssignMaterialByName(string materialName)
        {
            MeshRenderer[] meshRenderers = spawnPoint.GetComponentsInChildren<MeshRenderer>();
            SkinnedMeshRenderer[] skinnedMeshRenderers = spawnPoint.GetComponentsInChildren<SkinnedMeshRenderer>();

            if (originalMaterial != null)
            {
                foreach (MeshRenderer renderer in meshRenderers)
                {
                    AssignMaterialToRenderer(renderer, modelMaterial.name, originalMaterial);
                }

                foreach (SkinnedMeshRenderer renderer in skinnedMeshRenderers)
                {
                    AssignMaterialToRenderer(renderer, modelMaterial.name, originalMaterial);
                }
            }

            foreach (MeshRenderer renderer in meshRenderers)
            {
                var returnedMaterial = AssignMaterialToRenderer(renderer, materialName, modelMaterial);
                if (returnedMaterial != null)
                {
                    originalMaterial = returnedMaterial;
                }
            }

            foreach (SkinnedMeshRenderer renderer in skinnedMeshRenderers)
            {
                var returnedMaterial = AssignMaterialToRenderer(renderer, materialName, modelMaterial);
                if (returnedMaterial != null)
                {
                    originalMaterial = returnedMaterial;
                }
            }
        }


        private List<string> GetMaterialNames()
        {
            if (spawnPoint.transform.childCount > 0)
            {
                var materialNames = new List<string>();
                MeshRenderer[] meshRenderers = spawnPoint.GetComponentsInChildren<MeshRenderer>();
                foreach (MeshRenderer renderer in meshRenderers)
                {
                    var materials = renderer.sharedMaterials;
                    foreach (Material material in materials)
                    {
                        materialNames.Add(material.name);
                    }
                }

                SkinnedMeshRenderer[] skinnedMeshRenderers = spawnPoint.GetComponentsInChildren<SkinnedMeshRenderer>();
                foreach (SkinnedMeshRenderer renderer in skinnedMeshRenderers)
                {
                    var materials = renderer.sharedMaterials;
                    foreach (Material material in materials)
                    {
                        materialNames.Add(material.name);
                    }
                }

                if(materialNames.Count > 0)
                {
                    foreach(var name in materialNames)
                    {
                        Debug.Log(name);
                    }
                    return new List<string>(materialNames);
                }
                else
                {
                    return null;
                }
            }
            else
            {
                EventManager.instance.TriggerWarning("No model loaded yet!");
                return null;
            }
        }
    }
}
