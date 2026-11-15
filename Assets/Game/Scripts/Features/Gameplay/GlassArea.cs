using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class GlassArea : MonoBehaviour
{
    [Header("Pote")]
    [SerializeField, Min(0)] private int playerIndex; // Mesmo valor do PlayerId do dono do pote (0 a 3).

    [Header("Colliders (filhos do pote)")]
    [SerializeField] private BoxCollider2D movementBounds; // Área em que o jogador pode andar.
    [SerializeField] private Collider2D floor; // Chão (o que tem o FruitCollider).
    [SerializeField] private Collider2D leftWall;
    [SerializeField] private Collider2D rightWall;
    [SerializeField] private GlassMaxHeight maxHeight; // Area de derrota.

    private static readonly List<GlassArea> all = new();

    public int PlayerIndex => playerIndex;
    public BoxCollider2D MovementBounds => movementBounds;
    public GlassMaxHeight MaxHeight => maxHeight;

    /// <summary> Face interna da parede esquerda. </summary>
    public float InnerLeftX => leftWall.bounds.max.x;

    /// <summary> Face interna da parede direita. </summary>
    public float InnerRightX => rightWall.bounds.min.x;

    /// <summary> Topo do chão. </summary>
    public float FloorY => floor.bounds.max.y;

    /// <summary> Base da zona de derrota (a linha vermelha). </summary>
    public float DangerLineY => maxHeight.DangerLineY;

    public static bool TryGet(int playerIndex, out GlassArea glassArea)
    {
        foreach (GlassArea current in all)
        {
            if (current.playerIndex != playerIndex)
                continue;

            glassArea = current;
            return true;
        }

        glassArea = null;
        return false;
    }

    private void OnEnable()
    {
        if (TryGet(playerIndex, out GlassArea other) && other != this)
            CatGame.Core.Logger.LogWarning($"[GlassArea] Dois potes com o Player Index {playerIndex}: {other.name} e {name}");

        if (!all.Contains(this))
            all.Add(this);
    }

    private void OnDisable()
    {
        all.Remove(this);
    }

    private void OnDrawGizmosSelected()
    {
        if (floor == null || leftWall == null || rightWall == null)
            return;

        // Desenha o interior do pote usado pela IA, para conferir na cena.
        float top = maxHeight != null ? DangerLineY : FloorY + 10f;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(new Vector3(InnerLeftX, FloorY), new Vector3(InnerRightX, FloorY));
        Gizmos.DrawLine(new Vector3(InnerLeftX, FloorY), new Vector3(InnerLeftX, top));
        Gizmos.DrawLine(new Vector3(InnerRightX, FloorY), new Vector3(InnerRightX, top));

        Gizmos.color = Color.red;
        Gizmos.DrawLine(new Vector3(InnerLeftX, top), new Vector3(InnerRightX, top));
    }
}
