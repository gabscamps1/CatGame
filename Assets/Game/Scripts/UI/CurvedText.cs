using UnityEngine;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace XanderDevelops.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TextMeshProUGUI))]
    [ExecuteAlways]
    public class CurvedText : MonoBehaviour
    {
        private TMP_Text textComponent;  // Text component to curve

        [SerializeField, Range(-100f, 100f)]
        [Tooltip("Radius of the curve (higher values = less curve)")]
        private float curve = 30;

        [SerializeField, Range(20f, 100f)]
        [Tooltip("Fixed arc length per character (spacing between characters along the curve)")]
        private float spacing = 30f;

        [SerializeField]
        private float flatnessThreshold = 4000f;

        [SerializeField, Range(-360f, 360f)]
        [Tooltip("Offset to rotate the arc around the center")]
        private float angularOffset = 0f;

        [Header("Animation")]
        [SerializeField]
        [Tooltip("Animate the text along the curve")]
        private bool animate = false;

        [SerializeField, Range(-300f, 300f)]
        [Tooltip("Speed of the animation (same units as Angular Offset, per second). Negative values reverse the direction")]
        private float animationSpeed = 30f;

        [SerializeField]
        [Tooltip("Ignore Time.timeScale (useful for UI while paused)")]
        private bool useUnscaledTime = true;

        [SerializeField]
        [Tooltip("Preview the animation in Edit Mode")]
        private bool animateInEditMode = true;

        private TMP_TextInfo textInfo;
        private float radius;
        private float animatedOffset;
        private float lastTime;
        private bool isUpdating = false;

        private void Awake()
        {
            textComponent = GetComponent<TMP_Text>();
        }

        private void OnEnable()
        {
            textComponent = GetComponent<TMP_Text>();
            lastTime = GetTime();

            // Ensure we only subscribe once
            if (textComponent != null)
            {
                textComponent.OnPreRenderText -= ModifyTextMesh;
                textComponent.OnPreRenderText += ModifyTextMesh;
            }
        }

        private void OnDisable()
        {
            if (textComponent != null)
            {
                textComponent.OnPreRenderText -= ModifyTextMesh;
            }
        }

        private void Update()
        {
            float now = GetTime();
            float dt = now - lastTime;
            lastTime = now;

            if (!animate || textComponent == null) return;
            if (!Application.isPlaying && !animateInEditMode) return;

            animatedOffset += animationSpeed * dt;

            // Keep the value bounded so it never loses float precision
            float circumference = 2f * Mathf.PI * Mathf.Abs(GetRadius());
            if (circumference > 0.0001f)
                animatedOffset = Mathf.Repeat(animatedOffset, circumference);

            textComponent.SetAllDirty();

#if UNITY_EDITOR
            // Makes Update run continuously in Edit Mode
            if (!Application.isPlaying)
                EditorApplication.QueuePlayerLoopUpdate();
#endif
        }

        private float GetTime()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) return (float)EditorApplication.timeSinceStartup;
#endif
            return useUnscaledTime ? Time.unscaledTime : Time.time;
        }

        private float GetRadius()
        {
            return curve != 0 ? flatnessThreshold / curve : flatnessThreshold / 0.001f;
        }

        // Called automatically by Unity when any value in the Inspector is changed.
        private void OnValidate()
        {
            if (textComponent == null) textComponent = GetComponent<TMP_Text>();

            if (!animate) animatedOffset = 0f;

            if (textComponent != null)
            {
                textComponent.SetAllDirty();
            }
        }

        private void ModifyTextMesh(TMP_TextInfo textInfo)
        {
            // Prevent recursion by checking if we are already updating
            if (isUpdating) return;

            isUpdating = true;
            this.textInfo = textInfo;
            UpdateTextCurve();
            isUpdating = false;
        }

        private void UpdateTextCurve()
        {
            if (textComponent == null) return;

            radius = GetRadius();

            // Force an update to text info
            textComponent.ForceMeshUpdate();
            textInfo = textComponent.textInfo;

            if (textInfo == null) return;

            int characterCount = textInfo.characterCount;

            if (characterCount == 0) return;

            float totalArcLength = spacing * (characterCount - 1);
            float currentOffset = angularOffset + animatedOffset;

            for (int i = 0; i < characterCount; i++)
            {
                if (!textInfo.characterInfo[i].isVisible) continue;

                int vertexIndex = textInfo.characterInfo[i].vertexIndex;
                int materialIndex = textInfo.characterInfo[i].materialReferenceIndex;
                Vector3[] vertices = textInfo.meshInfo[materialIndex].vertices;

                Vector3 charMidBaselinePos = new Vector2((vertices[vertexIndex + 0].x + vertices[vertexIndex + 2].x) / 2, textInfo.characterInfo[i].baseLine);
                vertices[vertexIndex + 0] -= charMidBaselinePos;
                vertices[vertexIndex + 1] -= charMidBaselinePos;
                vertices[vertexIndex + 2] -= charMidBaselinePos;
                vertices[vertexIndex + 3] -= charMidBaselinePos;

                // Angle for each character (uses the animated offset)
                float charAngle = (currentOffset + (-totalArcLength / 2f) + i * spacing) / Mathf.Abs(radius) * Mathf.Rad2Deg;

                if (Mathf.Abs(radius) > flatnessThreshold)
                {
                    charAngle = -charAngle;
                }

                float angleRadians = charAngle * Mathf.Deg2Rad;

                if (curve < 1) angleRadians = -angleRadians;

                Vector3 offset = new Vector3(Mathf.Sin(angleRadians) * radius, Mathf.Cos(angleRadians) * radius, 0) - new Vector3(0f, radius, 0f);

                Quaternion rotation = Quaternion.Euler(0, 0, radius > 0 ? -charAngle : charAngle);

                Matrix4x4 matrix = Matrix4x4.TRS(offset, rotation, Vector3.one);

                vertices[vertexIndex + 0] = matrix.MultiplyPoint3x4(vertices[vertexIndex + 0]);
                vertices[vertexIndex + 1] = matrix.MultiplyPoint3x4(vertices[vertexIndex + 1]);
                vertices[vertexIndex + 2] = matrix.MultiplyPoint3x4(vertices[vertexIndex + 2]);
                vertices[vertexIndex + 3] = matrix.MultiplyPoint3x4(vertices[vertexIndex + 3]);
            }

            textComponent.UpdateVertexData();
        }
    }
}