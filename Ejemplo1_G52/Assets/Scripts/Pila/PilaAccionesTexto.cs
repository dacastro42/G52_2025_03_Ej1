using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Gestiona una pila de acciones usando Stack<string> y la expone en la UI.
/// </summary>
public class PilaAccionesTexto : MonoBehaviour
{
    /// <summary>Campo de entrada para escribir una nueva acción.</summary>
    public TMP_InputField inputAccion;

    /// <summary>Texto donde se imprime el contenido actual de la pila.</summary>
    public TMP_Text pilaText;

    /// <summary>Texto para mostrar mensajes (ej. errores o resultado de Peek/Pop).</summary>
    public TMP_Text mensajeText;

    /// <summary>Pila de acciones gestionada por el panel.</summary>
    private Stack<string> pila = new Stack<string>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    /// <summary>Agrega la acción del InputField a la pila.</summary>
    public void PushAccion()
    {
        var valor = (inputAccion != null) ? inputAccion.text : string.Empty;
        if (string.IsNullOrWhiteSpace(valor))
        {
            SetMensaje(" Escribe una acción antes de hacer Push.");
            return;
        }
        pila.Push(valor.Trim());
        inputAccion.text = string.Empty;
        ActualizarVista();
        SetMensaje($" Push: '{valor}'");
    }

    /// <summary>Quita el elemento del tope de la pila si existe.</summary>
    public void PopAccion()
    {
        if (pila.Count == 0)
        {
            SetMensaje(" La pila está vacía. No hay nada que desapilar.");
            return;
        }
        string eliminado = pila.Pop();
        ActualizarVista();
        SetMensaje($" Pop: '{eliminado}'");
    }

    /// <summary>Muestra el elemento del tope sin quitarlo.</summary>
    public void PeekAccion()
    {
        if (pila.Count == 0)
        {
            SetMensaje(" La pila está vacía. No hay tope.");
            return;
        }
        SetMensaje($" Peek: '{pila.Peek()}'");
    }

    /// <summary>Vacía la pila.</summary>
    public void ClearPila()
    {
        pila.Clear();
        ActualizarVista();
        SetMensaje(" Pila vaciada.");
    }

    /// <summary>Actualiza el Text con el contenido de la pila (tope primero).</summary>
    private void ActualizarVista()
    {
        if (pilaText == null) return;

        pilaText.text = "PILA (tope  fondo)\n";
        // Para visualizar del tope hacia el fondo, iteramos el Stack directamente (Stack itera del tope al fondo)
        foreach (var item in pila)
        {
            pilaText.text += $"• {item}\n";
        }
    }
    /// <summary>Imprime mensajes en la UI.</summary>
    private void SetMensaje(string msg)
    {
        if (mensajeText != null) mensajeText.text = msg;
        else Debug.Log(msg);
    }
}
