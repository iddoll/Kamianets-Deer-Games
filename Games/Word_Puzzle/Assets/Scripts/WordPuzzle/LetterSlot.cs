using UnityEngine;

namespace WordPuzzle
{
    /// <summary>
    /// A target cell beneath the chest. Each slot expects one specific letter (the target word's
    /// character at <see cref="Index"/>). Tracks whether it is filled and by which letter.
    /// </summary>
    public class LetterSlot : MonoBehaviour
    {
        public int Index { get; private set; }
        public char Expected { get; private set; }
        public bool Occupied { get; private set; }
        public FloatingLetter PlacedLetter { get; private set; }

        public RectTransform Rect { get; private set; }

        public void Initialize(int index, char expected)
        {
            Index = index;
            Expected = expected;
            Rect = (RectTransform)transform;
            Occupied = false;
            PlacedLetter = null;
        }

        /// <summary>Center of this slot expressed in its parent's anchored coordinates.</summary>
        public Vector2 CenterAnchoredPosition => Rect.anchoredPosition;

        public bool Matches(FloatingLetter letter)
        {
            return !Occupied && letter != null && letter.Character == Expected;
        }

        public void Fill(FloatingLetter letter)
        {
            Occupied = true;
            PlacedLetter = letter;
        }

        public void Clear()
        {
            Occupied = false;
            PlacedLetter = null;
        }
    }
}
