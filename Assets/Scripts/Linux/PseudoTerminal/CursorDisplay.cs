using System.Text;
using System.Collections.Generic;
using UnityEngine;

namespace Linux.PseudoTerminal
{
    public class CursorDisplay {
        string _textCache;

        protected List<string> ObservedCollection;

        public int CollectionSize { get; protected set; }
        public int CursorPosition { get; protected set; }

        public CursorDisplay() {
            ObservedCollection = new List<string>();

            CollectionSize = CursorPosition = 0;
        }

        public void AddToCollection(string text) {
            ObservedCollection.Add(text);
            CollectionSize++;

            UpdateTextCache();
            
            Move(1);
        }

        public void RemoveFromCollection(int index) {
            ObservedCollection.RemoveAt(index);
            CollectionSize--;

            UpdateTextCache();
        }

        public void RemoveAtCursorPosition(int step) {
            RemoveFromCollection(CursorPosition + step);
            Move(step);
        }

        public bool IsAtEnd() {
            return CursorPosition > CollectionSize - 1;
        }

        public bool IsAtBegin() {
            return CursorPosition == 0;
        }

        public void RemoveLastFromCollection() {
            RemoveFromCollection(CollectionSize - 1);
        }

        public void Move(int step) {
            if (CollectionSize == 0) {
                CursorPosition = 0;
                return;
            }

            CursorPosition += step;

            if (CursorPosition < 0) {
                CursorPosition = 0;
            } else if (IsAtEnd()) {
                CursorPosition = CollectionSize;
            }
        }

        public string DrawText() {
            if (_textCache == null) {
                UpdateTextCache();
            }

            return _textCache; 
        }

        void UpdateTextCache() {
            _textCache = string.Join("", ObservedCollection);
        }
    }
}