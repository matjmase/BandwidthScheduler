namespace BandwidthScheduler.Server.Common.DataStructures
{
    public class Heap<T>
    {
        protected readonly List<T> _elements;
        protected readonly SequencingOrder _correctOrderCompare;

        public delegate bool SequencingOrder(T first, T second);
        public Heap(SequencingOrder comparison, int size = 0)
        {
            _correctOrderCompare = comparison;
            _elements = new List<T>(size);
        }

        private int GetLeftChildIndex(int elementIndex) => 2 * elementIndex + 1;
        private int GetRightChildIndex(int elementIndex) => 2 * elementIndex + 2;
        private int GetParentIndex(int elementIndex) => (elementIndex - 1) / 2;

        private bool HasLeftChild(int elementIndex) => GetLeftChildIndex(elementIndex) < _elements.Count;
        private bool HasRightChild(int elementIndex) => GetRightChildIndex(elementIndex) < _elements.Count;
        private bool IsRoot(int elementIndex) => elementIndex == 0;

        private T GetLeftChild(int elementIndex) => _elements[GetLeftChildIndex(elementIndex)];
        private T GetRightChild(int elementIndex) => _elements[GetRightChildIndex(elementIndex)];
        private T GetParent(int elementIndex) => _elements[GetParentIndex(elementIndex)];

        protected virtual void Swap(int firstIndex, int secondIndex)
        {
            var first = _elements[firstIndex];
            _elements[firstIndex] = _elements[secondIndex];
            _elements[secondIndex] = first;
        }

        public bool IsEmpty()
        {
            return _elements.Count == 0;
        }

        public int Count() => _elements.Count;

        public T Peek()
        {
            if (_elements.Count == 0)
                throw new IndexOutOfRangeException();

            return _elements[0];
        }

        public virtual T Pop()
        {
            if (_elements.Count == 0)
                throw new IndexOutOfRangeException();

            var result = _elements[0];
            _elements[0] = _elements[_elements.Count - 1];
            _elements.RemoveAt(_elements.Count - 1);

            ReCalculateDown(0);

            return result;
        }

        public virtual void Add(T element)
        {
            _elements.Add(element);

            ReCalculateUp(_elements.Count - 1);
        }

        protected int ReCalculateDown(int index)
        {
            while (HasLeftChild(index))
            {
                var smallerIndex = GetLeftChildIndex(index);
                if (HasRightChild(index) && _correctOrderCompare(GetRightChild(index), GetLeftChild(index)))
                {
                    smallerIndex = GetRightChildIndex(index);
                }

                if (!_correctOrderCompare(_elements[smallerIndex], _elements[index]))
                {
                    break;
                }

                Swap(smallerIndex, index);
                index = smallerIndex;
            }

            return index;
        }

        protected void ReCalculateUp(int index)
        {
            while (!IsRoot(index) && _correctOrderCompare(_elements[index], GetParent(index)))
            {
                var parentIndex = GetParentIndex(index);
                Swap(parentIndex, index);
                index = parentIndex;
            }
        }
    }
}
