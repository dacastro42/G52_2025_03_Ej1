using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Gestiona una pila visual de fichas 2D (sprites) con animaciones básicas.
/// Demuestra el uso de Stack&lt;T&gt; en un contexto 2D.
/// </summary>
public class PilaFichas2D : MonoBehaviour
{
    [Header("Referencias de escena")]
    /// <summary>Raíz (Transform) donde se apilan las fichas desde y=0 hacia arriba.</summary>
    public Transform stackRoot;
    /// <summary>Prefab base de la ficha; debe incluir SpriteRenderer (o se le añadirá).</summary>
    public GameObject fichaPrefab;
    /// <summary>Texto para mensajes de estado; si no se asigna, usa Debug.Log.</summary>
    public TMP_Text mensajeText;

    [Header("Catálogo de sprites")]
    /// <summary>Sprites disponibles para instanciar como fichas.</summary>
    public List<Sprite> catalogoSprites = new List<Sprite>();

    [Header("Parámetros de apilado")]
    /// <summary>Separación vertical entre fichas.</summary>
    public float offsetY = 0.6f;
    /// <summary>Duración de las animaciones de entrada/salida.</summary>
    public float animTiempo = 0.18f;
    /// <summary>Escala final de cada ficha dentro de la pila.</summary>
    public Vector3 escalaFinal = Vector3.one;

    /// <summary>Pila con las fichas instanciadas (el tope es el último en entrar).</summary>
    private Stack<GameObject> pila = new Stack<GameObject>();

    /// <summary>
    /// Realiza un Push instanciando una nueva ficha con el sprite del catálogo en 'indiceSprite'.
    /// </summary>
    public void PushFicha(int indiceSprite)
    {
        if (catalogoSprites == null || catalogoSprites.Count == 0)
        {
            SetMsg(" No hay sprites en el catálogo.");
            return;
        }
        if (indiceSprite < 0 || indiceSprite >= catalogoSprites.Count)
        {
            SetMsg(" Índice de sprite inválido.");
            return;
        }
        if (stackRoot == null || fichaPrefab == null)
        {
            SetMsg(" Asigna stackRoot y fichaPrefab en el Inspector.");
            return;
        }

        // Instanciar algo a la derecha y animarlo hasta el tope
        Vector3 spawnPos = stackRoot.position + new Vector3(1.2f, 0f, 0f);
        GameObject go = Instantiate(fichaPrefab, spawnPos, Quaternion.identity, stackRoot);

        // SpriteRenderer
        var sr = go.GetComponent<SpriteRenderer>();
        if (sr == null) sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = catalogoSprites[indiceSprite];

        // Posición destino (altura = cantidad actual)
        int altura = pila.Count;
        Vector3 destino = stackRoot.position + new Vector3(0f, altura * offsetY, 0f);

        // Arranca a escala 0 para "aparecer"
        go.transform.localScale = Vector3.zero;

        pila.Push(go);
        StartCoroutine(AnimarMoveAndScale(go.transform, destino, escalaFinal, animTiempo));

        SetMsg($" Push: {sr.sprite.name} (tope={pila.Count})");
    }

    /// <summary>
    /// Quita (Pop) la ficha del tope si existe, con animación de salida.
    /// </summary>
    public void PopFicha()
    {
        if (pila.Count == 0)
        {
            SetMsg(" Pila vacía. Nada que desapilar.");
            return;
        }

        GameObject tope = pila.Pop();
        var sr = tope.GetComponent<SpriteRenderer>();
        string nombre = (sr != null && sr.sprite != null) ? sr.sprite.name : tope.name;

        // Animación de salida: hacia la derecha y haciendo "shrink"
        Vector3 destino = tope.transform.position + new Vector3(0.8f, 0f, 0f);
        StartCoroutine(AnimarMoveAndScaleAndDestroy(tope.transform, destino, Vector3.zero, animTiempo, tope));

        // Reacomodar visualmente los hijos restantes bajo el stackRoot
        ReordenarPilaVisual();

        SetMsg($" Pop: {nombre} (tope={pila.Count})");
    }

    /// <summary>
    /// Peek: resalta la ficha del tope temporalmente sin quitarla.
    /// </summary>
    public void PeekFicha()
    {
        if (pila.Count == 0)
        {
            SetMsg(" Pila vacía. No hay tope.");
            return;
        }

        GameObject tope = pila.Peek();
        var sr = tope.GetComponent<SpriteRenderer>();
        if (sr != null) StartCoroutine(ResaltarSprite(sr, 0.25f));

        string nombre = (sr != null && sr.sprite != null) ? sr.sprite.name : tope.name;
        SetMsg($" Peek: {nombre}");
    }

    /// <summary>
    /// Vacía la pila destruyendo todas las fichas con una animación rápida.
    /// </summary>
    public void ClearPila()
    {
        while (pila.Count > 0)
        {
            GameObject go = pila.Pop();
            StartCoroutine(AnimarMoveAndScaleAndDestroy(
                go.transform,
                go.transform.position + new Vector3(0.6f, 0f, 0f),
                Vector3.zero,
                animTiempo * 0.8f,
                go));
        }
        SetMsg(" Pila vaciada.");
    }

    /// <summary>
    /// Recoloca las fichas existentes para que queden alineadas verticalmente según su orden.
    /// </summary>
    private void ReordenarPilaVisual()
    {
        if (stackRoot == null) return;

        // Recoger todos los hijos actuales
        var hijos = new List<Transform>();
        for (int i = 0; i < stackRoot.childCount; i++)
            hijos.Add(stackRoot.GetChild(i));

        // Orden aproximado por altura Y
        hijos.Sort((a, b) => a.position.y.CompareTo(b.position.y));

        // Reasignar posiciones escalonadas
        for (int i = 0; i < hijos.Count; i++)
        {
            Vector3 destino = stackRoot.position + new Vector3(0f, i * offsetY, 0f);
            StartCoroutine(AnimarMoveAndScale(hijos[i], destino, escalaFinal, animTiempo * 0.8f));
        }
    }

    // 
    // Helpers de animación y mensajes
    // 

    /// <summary>
    /// Mueve y escala suavemente un transform hacia un destino en un tiempo dado.
    /// </summary>
    private IEnumerator AnimarMoveAndScale(Transform t, Vector3 destino, Vector3 escalaObjetivo, float duracion)
    {
        if (t == null) yield break;

        Vector3 origenPos = t.position;
        Vector3 origenEsc = t.localScale;
        float elapsed = 0f;

        while (elapsed < duracion)
        {
            elapsed += Time.deltaTime;
            float k = Mathf.Clamp01(elapsed / duracion);
            // Suavizado leve
            float s = Smooth01(k);

            t.position = Vector3.Lerp(origenPos, destino, s);
            t.localScale = Vector3.Lerp(origenEsc, escalaObjetivo, s);
            yield return null;
        }

        t.position = destino;
        t.localScale = escalaObjetivo;
    }

    /// <summary>
    /// Mueve y escala, y al final destruye el GameObject asociado.
    /// </summary>
    private IEnumerator AnimarMoveAndScaleAndDestroy(Transform t, Vector3 destino, Vector3 escalaObjetivo, float duracion, GameObject goADestruir)
    {
        yield return AnimarMoveAndScale(t, destino, escalaObjetivo, duracion);
        if (goADestruir != null) Destroy(goADestruir);
    }

    /// <summary>
    /// Efecto de "destello" cambiando el color del SpriteRenderer y volviendo luego a su color original.
    /// </summary>
    private IEnumerator ResaltarSprite(SpriteRenderer sr, float duracion)
    {
        if (sr == null) yield break;

        Color original = sr.color;
        Color resaltado = original * 1.4f; // un poco más brillante
        sr.color = resaltado;
        yield return new WaitForSeconds(duracion);
        sr.color = original;
    }

    /// <summary>
    /// Curva de suavizado (ease-in-out cúbico simple).
    /// </summary>
    private float Smooth01(float t)
    {
        // ease in-out (cubic)
        return t * t * (3f - 2f * t);
    }

    /// <summary>
    /// Muestra un mensaje en la interfaz o en la consola.
    /// </summary>
    private void SetMsg(string msg)
    {
        if (mensajeText != null) mensajeText.text = msg;
        else Debug.Log(msg);
    }
}