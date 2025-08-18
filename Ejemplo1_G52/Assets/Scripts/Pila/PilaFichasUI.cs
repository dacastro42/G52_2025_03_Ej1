using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
/// <summary>
/// Pila visual con elementos UI (Image) apilados en un RectTransform.
/// Demuestra Stack<T> usando Canvas/UI sin SpriteRenderer.
/// </summary>
public class PilaFichasUI : MonoBehaviour
{
    [Header("Referencias UI")]
    /// <summary>Contenedor donde se apilan las fichas (debe ser hijo del Canvas).</summary>
    public RectTransform stackRootUI;
    /// <summary>Prefab UI de la ficha: debe tener Image (+ CanvasGroup para fade).</summary>
    public Image fichaUIPrefab;
    /// <summary>Texto opcional para mensajes/estado.</summary>
    public TMP_Text mensajeText; // puedes usar TMP_Text si prefieres

    [Header("Catálogo de sprites (UI)")]
    /// <summary>Sprites disponibles para apilar.</summary>
    public List<Sprite> catalogoSprites = new List<Sprite>();

    [Header("Parámetros de apilado (en píxeles)")]
    /// <summary>Separación vertical entre fichas (px).</summary>
    public float offsetY = 80f;
    /// <summary>Duración de las animaciones (s).</summary>
    public float animTiempo = 0.18f;
    /// <summary>Escala final de cada ficha.</summary>
    public Vector3 escalaFinal = Vector3.one;

    /// <summary>Pila de elementos UI instanciados (tope = último agregado).</summary>
    private Stack<Image> pila = new Stack<Image>();

    /// <summary>
    /// Push: instancia una ficha UI con el sprite del índice dado y la anima hasta su posición.
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
        if (stackRootUI == null || fichaUIPrefab == null)
        {
            SetMsg(" Asigna StackRootUI y FichaUIPrefab en el Inspector.");
            return;
        }

        // Instanciar como hijo del contenedor UI
        Image img = Instantiate(fichaUIPrefab, stackRootUI);
        img.sprite = catalogoSprites[indiceSprite];
        img.SetNativeSize(); // ajusta al tamaño del sprite en píxeles (opcional)

        // Asegurar CanvasGroup (para fade)
        var cg = img.GetComponent<CanvasGroup>();
        if (cg == null) cg = img.gameObject.AddComponent<CanvasGroup>();
        cg.alpha = 0f;

        // Posición destino (apilado vertical, anclado al centro)
        int altura = pila.Count; // 0,1,2...
        var rt = img.rectTransform;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f); // base en el centro-inferior del contenedor
        Vector2 destino = new Vector2(0f, altura * offsetY);

        // Posición inicial: un poco a la derecha, y escala 0
        rt.anchoredPosition = destino + new Vector2(200f, 0f);
        rt.localScale = Vector3.zero;

        pila.Push(img);
        StartCoroutine(AnimarUI_MoveScaleFade(rt, destino, escalaFinal, 0f, 1f, animTiempo));

        SetMsg($" Push: {img.sprite.name} (tope={pila.Count})");
    }

    /// <summary>
    /// Pop: elimina la ficha del tope con animación de salida.
    /// </summary>
    public void PopFicha()
    {
        if (pila.Count == 0)
        {
            SetMsg(" Pila vacía. Nada que desapilar.");
            return;
        }

        Image tope = pila.Pop();
        var rt = tope.rectTransform;
        var cg = tope.GetComponent<CanvasGroup>();
        string nombre = (tope.sprite != null) ? tope.sprite.name : tope.name;

        // Animación de salida: mover a la derecha + shrink + fade
        Vector2 destino = rt.anchoredPosition + new Vector2(200f, 0f);
        StartCoroutine(AnimarUI_MoveScaleFadeAndDestroy(rt, destino, Vector3.zero, cg, 0.5f, animTiempo));

        // Reacomodar visual de las restantes (bajar una posición)
        ReordenarPilaVisual();

        SetMsg($" Pop: {nombre} (tope={pila.Count})");
    }

    /// <summary>
    /// Peek: resalta la ficha del tope temporalmente (pequeño "pulse").
    /// </summary>
    public void PeekFicha()
    {
        if (pila.Count == 0)
        {
            SetMsg(" Pila vacía. No hay tope.");
            return;
        }

        Image tope = pila.Peek();
        StartCoroutine(PulseUI(tope.rectTransform, 1.08f, 0.12f));
        SetMsg($" Peek: {tope.sprite?.name ?? tope.name}");
    }

    /// <summary>
    /// Clear: vacía toda la pila con animación rápida.
    /// </summary>
    public void ClearPila()
    {
        while (pila.Count > 0)
        {
            var img = pila.Pop();
            var rt = img.rectTransform;
            var cg = img.GetComponent<CanvasGroup>();
            StartCoroutine(AnimarUI_MoveScaleFadeAndDestroy(
                rt, rt.anchoredPosition + new Vector2(160f, 0f), Vector3.zero, cg, 0f, animTiempo * 0.8f));
        }
        SetMsg(" Pila vaciada.");
    }

    /// <summary>
    /// Recoloca las fichas según su nuevo índice (0..n-1) en el contenedor UI.
    /// </summary>
    private void ReordenarPilaVisual()
    {
        int i = 0;
        foreach (Transform child in stackRootUI)
        {
            var rt = child as RectTransform;
            if (rt == null) continue;
            Vector2 destino = new Vector2(0f, i * offsetY);
            StartCoroutine(AnimarUI_MoveScaleFade(rt, destino, escalaFinal, 1f, 1f, animTiempo * 0.7f));
            i++;
        }
    }

    //  Helpers UI

    private IEnumerator AnimarUI_MoveScaleFade(
        RectTransform rt, Vector2 destino, Vector3 escalaObjetivo, float alphaIni, float alphaFin, float duracion)
    {
        if (rt == null) yield break;
        var cg = rt.GetComponent<CanvasGroup>();
        if (cg == null) cg = rt.gameObject.AddComponent<CanvasGroup>();

        Vector2 p0 = rt.anchoredPosition;
        Vector3 s0 = rt.localScale;
        float a0 = alphaIni, a1 = alphaFin;

        float t = 0f;
        while (t < duracion)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duracion);
            float e = k * k * (3f - 2f * k); // ease in-out

            rt.anchoredPosition = Vector2.Lerp(p0, destino, e);
            rt.localScale = Vector3.Lerp(s0, escalaObjetivo, e);
            cg.alpha = Mathf.Lerp(a0, a1, e);
            yield return null;
        }
        rt.anchoredPosition = destino;
        rt.localScale = escalaObjetivo;
        cg.alpha = a1;
    }

    private IEnumerator AnimarUI_MoveScaleFadeAndDestroy(
        RectTransform rt, Vector2 destino, Vector3 escalaObjetivo, CanvasGroup cg, float alphaFin, float duracion)
    {
        if (rt == null) yield break;
        if (cg == null) cg = rt.gameObject.AddComponent<CanvasGroup>();

        Vector2 p0 = rt.anchoredPosition;
        Vector3 s0 = rt.localScale;
        float a0 = cg.alpha;

        float t = 0f;
        while (t < duracion)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duracion);
            float e = k * k * (3f - 2f * k);

            rt.anchoredPosition = Vector2.Lerp(p0, destino, e);
            rt.localScale = Vector3.Lerp(s0, escalaObjetivo, e);
            cg.alpha = Mathf.Lerp(a0, alphaFin, e);
            yield return null;
        }
        Destroy(rt.gameObject);
    }

    private IEnumerator PulseUI(RectTransform rt, float escalaMax, float dur)
    {
        if (rt == null) yield break;

        Vector3 s0 = rt.localScale;
        Vector3 s1 = s0 * escalaMax;

        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / dur);
            rt.localScale = Vector3.Lerp(s0, s1, k);
            yield return null;
        }
        t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / dur);
            rt.localScale = Vector3.Lerp(s1, s0, k);
            yield return null;
        }
        rt.localScale = s0;
    }

    private void SetMsg(string msg)
    {
        if (mensajeText != null) mensajeText.text = msg;
        else Debug.Log(msg);
    }
}