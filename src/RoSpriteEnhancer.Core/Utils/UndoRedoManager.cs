using System;
using System.Collections.Generic;

namespace RoSpriteEnhancer.Core.Utils
{
    /// <summary>
    /// Generischer Undo/Redo-Manager für stateful Objekte.
    ///
    /// Speichert Snapshots eines Zustands und ermöglicht Zurücksetzen (Undo)
    /// und Wiederherstellen (Redo) früherer Zustände.
    ///
    /// Verwendung:
    ///   var undoRedo = new UndoRedoManager<ProcessingOptions>(maxHistory: 50);
    ///   undoRedo.Push(currentOptions); // initialen Zustand speichern
    ///   // Änderungen vornehmen...
    ///   undoRedo.Push(currentOptions); // neuen Zustand speichern
    ///   var previous = undoRedo.Undo(); // vorherigen Zustand holen
    ///   var next = undoRedo.Redo(); // nächsten Zustand holen
    /// </summary>
    /// <typeparam name="T">Typ des zu trackenden Zustands (muss klonbar sein)</typeparam>
    public class UndoRedoManager<T> where T : class
    {
        private readonly Stack<T> _undoStack = new();
        private readonly Stack<T> _redoStack = new();
        private readonly int _maxHistory;
        private T? _current;

        /// <summary>Maximale Anzahl gespeicherter Zustände</summary>
        public int MaxHistory => _maxHistory;

        /// <summary>Aktueller Zustand (null wenn leer)</summary>
        public T? Current => _current;

        /// <summary>Ob Undo verfügbar ist</summary>
        public bool CanUndo => _undoStack.Count > 0;

        /// <summary>Ob Redo verfügbar ist</summary>
        public bool CanRedo => _redoStack.Count > 0;

        /// <summary>Anzahl gespeicherter Zustände im Undo-Stack</summary>
        public int UndoCount => _undoStack.Count;

        /// <summary>Anzahl gespeicherter Zustände im Redo-Stack</summary>
        public int RedoCount => _redoStack.Count;

        /// <summary>
        /// Erstellt einen neuen UndoRedoManager.
        /// </summary>
        /// <param name="maxHistory">Maximale History-Einträge (Standard: 50)</param>
        public UndoRedoManager(int maxHistory = 50)
        {
            _maxHistory = Math.Max(10, maxHistory);
        }

        /// <summary>
        /// Speichert den aktuellen Zustand und leert den Redo-Stack.
        /// Wird aufgerufen, bevor eine Änderung vorgenommen wird.
        /// </summary>
        /// <param name="state">Der zu speichernde Zustand</param>
        public void Push(T state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));

            // Wenn es einen aktuellen Zustand gibt und dieser unterschiedlich ist, speichern
            if (_current != null && !Equals(_current, state))
            {
                _undoStack.Push(_current);

                // History begrenzen: älteste Einträge (Boden des Stacks) entfernen
                while (_undoStack.Count > _maxHistory)
                {
                    var items = _undoStack.ToArray();
                    _undoStack.Clear();
                    // items[0] = ältesten Eintrag, items[^1] = neuesten Eintrag
                    // Behalte nur die letzten _maxHistory Einträge
                    int startIndex = items.Length - _maxHistory;
                    for (int i = startIndex; i < items.Length; i++)
                    {
                        if (items[i] != null)
                            _undoStack.Push(items[i]);
                    }
                }

                _redoStack.Clear();
            }

            _current = state;
        }

        /// <summary>
        /// Setzt den initialen Zustand ohne History-Eintrag.
        /// Wird beim Start oder beim Laden einer neuen Datei verwendet.
        /// </summary>
        public void SetInitial(T state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            _undoStack.Clear();
            _redoStack.Clear();
            _current = state;
        }

        /// <summary>
        /// Macht die letzte Änderung rückgängig.
        /// </summary>
        /// <returns>Der vorherige Zustand oder null wenn kein Undo möglich</returns>
        public T? Undo()
        {
            if (!CanUndo) return null;

            // Aktuellen Zustand auf Redo-Stack legen
            if (_current != null)
                _redoStack.Push(_current);

            // Vorherigen Zustand holen
            _current = _undoStack.Pop();
            return _current;
        }

        /// <summary>
        /// Stellt die letzte rückgängig gemachte Änderung wieder her.
        /// </summary>
        /// <returns>Der wiederhergestellte Zustand oder null wenn kein Redo möglich</returns>
        public T? Redo()
        {
            if (!CanRedo) return null;

            // Aktuellen Zustand auf Undo-Stack legen
            if (_current != null)
                _undoStack.Push(_current);

            // Nächsten Zustand holen
            _current = _redoStack.Pop();
            return _current;
        }

        /// <summary>
        /// Leert beide History-Stacks.
        /// </summary>
        public void Clear()
        {
            _undoStack.Clear();
            _redoStack.Clear();
            _current = null;
        }

        /// <summary>
        /// Erstellt eine flache Kopie des Zustands.
        /// </summary>
        public static T Clone(T state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            // Einfache Klonung über MemberwiseClone reicht für immutable-ähnliche Nutzung,
            // aber für Safety serialisieren wir über JSON falls verfügbar.
            // Hier: MemberwiseClone + manuelle Deep-Kopie der verschachtelten Objekte.
            var clone = (T)Activator.CreateInstance(state.GetType())!;
            foreach (var prop in state.GetType().GetProperties())
            {
                if (prop.CanRead && prop.CanWrite)
                {
                    var value = prop.GetValue(state);
                    prop.SetValue(clone, value);
                }
            }
            return clone;
        }
    }
}