namespace BandwidthScheduler.Server.Common.DataStructures
{
    public class DictionaryHeap<T> : Heap<T> where T : class
    {
        private readonly Dictionary<T, int> _indexes;

        public DictionaryHeap(SequencingOrder comparison, int size = 0) : base(comparison, size)
        {
            _indexes = new Dictionary<T, int>();
        }

        public override void Add(T element)
        {
            _elements.Add(element);
            _indexes.Add(element, _elements.Count - 1);

            ReCalculateUp(_elements.Count - 1);
        }

        public override T Pop()
        {
            if (_elements.Count == 0)
                throw new IndexOutOfRangeException();

            var result = _elements[0];
            Swap(0, _elements.Count - 1);
            _elements.RemoveAt(_elements.Count - 1);
            _indexes.Remove(result);

            ReCalculateDown(0);

            return result;
        }

        protected override void Swap(int firstIndex, int secondIndex)
        {
            if (firstIndex != secondIndex)
            {
                base.Swap(firstIndex, secondIndex);
                _indexes[_elements[firstIndex]] = firstIndex;
                _indexes[_elements[secondIndex]] = secondIndex;
            }
        }

        public void Remove(T element)
        {
            var index = _indexes[element];

            var result = _elements[index];
            Swap(index, _elements.Count - 1);
            _elements.RemoveAt(_elements.Count - 1);
            _indexes.Remove(result);

            ReCalculateDown(index);
        }

        public bool Contains(T element)
        {
            return _indexes.ContainsKey(element);
        }
    }
}
