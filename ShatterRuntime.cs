using System.Collections;
using RayFire;
using UnityEngine;

[SelectionBase, RequireComponent(typeof(MeshCollider)),
 RequireComponent(typeof(MeshRenderer)), RequireComponent(typeof(MeshFilter))]
public class ShatterRuntime : MonoBehaviour
    {
        private GameObject fragmentRoot;
        private MeshRenderer meshRenderer;
        private MeshCollider meshCollider;
        private RayfireRigid rayfireRigid;
        private RayfireShatter rayfireShatter;
        private bool shattered;
        
        
        [Header("Editor Settings")]
        public MaterialType fragmentType;
        public int fragmentCount = 10;
        public int fragmentLayerIndex;
        public float fragmentLife = 3f;
        public bool fragmentUseGravity = true;

        [Space(5)] [Header("Optional Settings")]
        public bool parentFragments;
        public Transform parentForFragments;
        public bool changeInnerMaterial;
        public Material innerMaterial;
        public bool useParticleAfterShatter;
        public ParticleSystem shatterParticlePrefab;
        public Vector3 particleLocalPosition = Vector3.zero;
        public Vector3 particleForward = Vector3.up;
        

        private IEnumerator Start()
        {
            if (!gameObject.activeSelf) yield break;
            meshRenderer = GetComponent<MeshRenderer>();
            meshCollider = GetComponent<MeshCollider>();
            rayfireShatter = gameObject.AddComponent<RayfireShatter>();
            rayfireShatter.voronoi.amount = fragmentCount;
            rayfireShatter.mode = FragmentMode.Runtime;
            if (changeInnerMaterial)
            {
                rayfireShatter.material.innerMaterial = innerMaterial;
            }
            rayfireShatter.Fragment();
            rayfireShatter.centerPosition = transform.position + Vector3.up;
            rayfireShatter.voronoi.centerBias = 1;
            fragmentRoot = rayfireShatter.rootChildList[0].gameObject;
            fragmentRoot.transform.SetParent(transform);
            rayfireRigid = fragmentRoot.AddComponent<RayfireRigid>();
            rayfireRigid.activation.layer = "Fragments";
            rayfireRigid.physics.materialType = fragmentType;
            rayfireRigid.physics.useGravity = fragmentUseGravity; 
            rayfireRigid.fading.fadeType = FadeType.Destroy;
            rayfireRigid.fading.lifeTime = fragmentLife;
            rayfireRigid.fading.lifeVariation = .5f;
            rayfireRigid.fading.onActivation = true;
            rayfireRigid.fading.fadeTime = .1f;
            rayfireRigid.objectType = ObjectType.MeshRoot;
            fragmentRoot.SetActive(false);
        }
        public void ShatterObject(float forceMagnitude = 1f, Vector3 forceDirection = default, bool explode = false)
        {
            if (!gameObject.activeSelf || shattered) return;
            if (useParticleAfterShatter)
            {
                var shatterParticleInstance = Instantiate(shatterParticlePrefab);
                var particleInstanceTransform = shatterParticleInstance.transform;
                particleInstanceTransform.position = transform.position + particleLocalPosition;
                particleInstanceTransform.forward = particleForward;
            }
            shattered = true;
            fragmentRoot.SetActive(true);
            meshRenderer.enabled = false;
            meshCollider.enabled = false;
            rayfireRigid.Initialize();
            foreach (var fragment in rayfireShatter.fragmentsAll)
            {
                var direction = explode
                    ? (fragment.transform.position - meshCollider.bounds.center).normalized
                    : forceDirection.normalized;
                fragment.transform.SetParent(parentFragments ? parentForFragments : null, true);
                fragment.layer = fragmentLayerIndex;
                fragment.GetComponent<Rigidbody>().AddForce(direction * forceMagnitude, ForceMode.VelocityChange);
            }
            rayfireRigid.Fade();
        }
    }
