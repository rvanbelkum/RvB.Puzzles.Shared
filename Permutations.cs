namespace RvB.Puzzles.Shared;

public static class Permutations {
    public static Pandita<T> GetUniquePermutations<T>(IEnumerable<T> elements) where T : IComparable<T>
        => new(elements);

    public static Heaps<T> GetPermutations<T>(IEnumerable<T> elements)
        => new(elements);

    public struct Pandita<T> where T : IComparable<T> {
        private readonly T[] _elements;
        private bool _firstEnumerator = true;

        public Pandita(IEnumerable<T> elements) {
            _elements = [.. elements];
        }

        public Pandita<T> GetEnumerator() {
            if (_firstEnumerator) {
                _firstEnumerator = false;
                return this;
            }
            return new(_elements) { _firstEnumerator = false };
        }

        public readonly ReadOnlySpan<T> Current => _elements.AsSpan();
        
        public readonly bool MoveNext() {
            int i = _elements.Length - 2;
            while (i >= 0 && _elements[i].CompareTo(_elements[i + 1]) >= 0) {
                i--;
            }
            if (i < 0)
                return false;
            int j = _elements.Length - 1;
            while (_elements[j].CompareTo(_elements[i]) <= 0) {
                j--;
            }
            (_elements[i], _elements[j]) = (_elements[j], _elements[i]);
            Reverse(_elements, i + 1);
            return true;

            static void Reverse(Span<T> input, int start) {
                int i = start;
                int j = input.Length - 1;
                while (i < j) {
                    (input[i], input[j]) = (input[j], input[i]);
                    i++;
                    j--;
                }
            }
        }
    }

    public struct Heaps<T> {
        private readonly T[] _elements;
        private int[]? _indices;
        private int _currIndex;
        private bool _firstEnumerator = true;

        public Heaps(IEnumerable<T> elements) {
            _elements = [.. elements];
        }

        public Heaps<T> GetEnumerator() {
            if (_firstEnumerator) {
                _firstEnumerator = false;
                return this;
            }
            return new(_elements) { _firstEnumerator = false };
        }

        public readonly ReadOnlySpan<T> Current => _elements.AsSpan();

        public bool MoveNext() {
            var elements = _elements;
            if (_indices == null) {
                _indices = new int[elements.Length];
                _currIndex = 1;
                return true;
            }
            var currIndex = _currIndex;
            var indices = _indices;
            while (currIndex < elements.Length) {
                if (indices[currIndex] < currIndex) {
                    if (currIndex % 2 == 0) {
                        (elements[0], elements[currIndex]) = (elements[currIndex], elements[0]);
                    } else {
                        (elements[indices[currIndex]], elements[currIndex]) = (elements[currIndex], elements[indices[currIndex]]);
                    }
                    indices[currIndex] += 1;
                    _currIndex = 1;
                    return true;
                } else {
                    indices[currIndex] = 0;
                    currIndex += 1;
                }
            }
            _currIndex = currIndex;
            return false;
        }
    }
}
