using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pool de gotas de líquido. Pre-instancia <see cref="poolSize"/> copias del prefab
/// y las reutiliza para evitar GC durante el juego.
///
/// Coloca este componente en un GameObject vacío (p.ej. "LiquidDropletPool")
/// que sea hijo de la botella o de la escena.
/// </summary>
public class LiquidDropletPool : MonoBehaviour
{
    [Header("Prefab")]
    [Tooltip("Prefab con MeshFilter(Quad) + MeshRenderer + LiquidDroplet + Rigidbody + SphereCollider.")]
    [SerializeField] LiquidDroplet dropletPrefab;

    [Header("Pool")]
    [Tooltip("Número máximo de gotas simultáneas.\nRegla: poolSize >= maxDropRate × dropLifetime (120 × 2.5 = 300).")]
    [SerializeField] int poolSize = 300;

    readonly Queue<LiquidDroplet> _available = new();

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void Awake()
    {
        if (dropletPrefab == null)
        {
            Debug.LogError("[LiquidDropletPool] Falta asignar el prefab.", this);
            return;
        }

        var all = new LiquidDroplet[poolSize];
        for (int i = 0; i < poolSize; i++)
        {
            LiquidDroplet d = Instantiate(dropletPrefab, transform);
            d.gameObject.SetActive(false);
            d.gameObject.name = $"Droplet_{i:00}";
            _available.Enqueue(d);
            all[i] = d;
        }

        // Las gotas no colisionan entre sí — se ignoran todos los pares una vez al inicio.
        for (int i = 0; i < all.Length; i++)
            for (int j = i + 1; j < all.Length; j++)
                Physics.IgnoreCollision(all[i].GetComponent<Collider>(),
                                        all[j].GetComponent<Collider>(), true);
    }

    // ── API pública ───────────────────────────────────────────────────────────

    /// <summary>
    /// Devuelve una gota disponible o <c>null</c> si el pool está agotado.
    /// </summary>
    public LiquidDroplet Get()
    {
        return _available.Count > 0 ? _available.Dequeue() : null;
    }

    /// <summary>
    /// Devuelve una gota al pool. Llamado automáticamente por <see cref="LiquidDroplet.Release"/>.
    /// </summary>
    public void Return(LiquidDroplet drop)
    {
        _available.Enqueue(drop);
    }

    // ── Debug ─────────────────────────────────────────────────────────────────

    public int Available => _available.Count;
    public int Total     => poolSize;
}
