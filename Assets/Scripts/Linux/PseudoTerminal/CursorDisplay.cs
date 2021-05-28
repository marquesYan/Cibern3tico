using System.Text;
using System.Collections.Generic;
using UnityEngine;

namespace Linux.PseudoTerminal
{
    public class CursorDisplay {
        string _textCache;
        protected bool IsFreshCollectionShadow;
        protected bool IsVisible = true;
        protected int FlashCount = 0;

        protected List<string> CollectionShadow;
        protected List<string> ObservedCollection;

        public int CollectionSize { get; protected set; }
        public int CursorPosition { get; protected set; }
        public int MaxFlashCount;
        public string Cursor;

        public CursorDisplay(string cursor, int maxFlashCount) {
            Cursor = cursor;
            MaxFlashCount = maxFlashCount;

            ObservedCollection = new List<string>();
            CollectionSize = CursorPosition = 0;

            CopyCollection();
        }

        public void AddToCollection(string text) {
            ObservedCollection.Add(text);
            CollectionShadow.Add(text);
            CollectionSize++;
        }

        public void RemoveFromCollection(int index) {
            ObservedCollection.RemoveAt(index);
            CollectionShadow.RemoveAt(index);
            CollectionSize--;
        }

        public void RemoveAtCursorPosition(int step) {
            RemoveFromCollection(CursorPosition + step);
        }

        public bool IsAtEnd() {
            return CursorPosition > CollectionSize - 1;
        }

        public void RemoveLastFromCollection() {
            RemoveFromCollection(CollectionSize - 1);
        }

        public void Move(int step) {
            // Always make a copy when cursor changed 
            CopyCollection();

            if (CollectionSize == 0) {
                CursorPosition = 0;
                return;
            }

            CursorPosition += step;

            if (CursorPosition < 0) {
                CursorPosition = 0;
            } else if (CursorPosition > CollectionSize) {
                CursorPosition = CollectionSize;
            }
        }

        public string Draw() {
            if (IsVisible && IsFreshCollectionShadow) {
                IsFreshCollectionShadow = false;
                CollectionShadow.Insert(CursorPosition, "|");

                _textCache = null;
            }

            if (FlashCount > MaxFlashCount) {
                IsVisible = !IsVisible;
                FlashCount = 0;
            } else {
                FlashCount++;
            }

            if (_textCache == null) {
                _textCache = string.Join("", CollectionShadow);
            }

            return _textCache;
        }

        void CopyCollection() {
            CollectionShadow = new List<string>(ObservedCollection);
            IsFreshCollectionShadow = true;
        }
    }
}