using System.Collections;
using System.Collections.Generic;
using System.Linq;


namespace ColorGraph.Lists
{
    public class FilteredList<T>:IEnumerable<T>
    {

        protected readonly ZSortedList<T> List = new ZSortedList<T>();
        protected readonly List<int> Flags = new List<int>();
        public int FilteredCount { get; private set; }
        public int NotFilteredCount { get
        {
            int c;
            lock (this)
            {
                c = List.Count - FilteredCount;
            }
            return c;
        } }


        public event OnAllFilteredEvent<T> OnAllFiltered;
        public FilteredList() { }
        public FilteredList(OnAllFilteredEvent<T> onAllFiltered)
        {
            OnAllFiltered = onAllFiltered;
        }

        public FilteredList(IEnumerable<T> collection)
        {
            AddRange(collection);
           
        }

        public int Count { get { return List.Count; } }
      
        public void InvokeOnAllFiltered()
        {
            OnAllFilteredEvent<T> handler = OnAllFiltered;
            if (handler != null) handler(this);
        }

        //protected readonly Dictionary<T,bool> Dic = new Dictionary<T, bool>();
        public void Add(T item)
        {
            lock(this)
            {
                List.Add(item);
                if (Flags.Count<List.Count)
                    Flags.Add(0);
            }
           // Dic.Add(item);
        }
        public void Clear()
        {
            lock (this)
            {
                Flags.Clear();
                List.Clear();
            }
        }
        public void ClearFilters()
        {
            lock(this)
            {
                for (int i = 0; i < Flags.Count; i++)
                {
                    Flags[i] = 0;
                }
                FilteredCount = 0;
            }
        }
        public bool Contains(T item)
        {
            return List.Contains(item);
        }
        public void Filter(T item)
        {
            bool doNotify = false;
            lock (this)
            {
                IEnumerable<int> found = List.GetIndexes(item);
                foreach (var i in found)
                {
                    FilteredCount++;
                    Flags[i] = FilteredCount;
 
                }
                if (FilteredCount == List.Count)
                    doNotify = true;
            }
            if (doNotify)
                InvokeOnAllFiltered();
        }
        public bool IsFiltered(T item)
        {
            bool signalled = false;
            lock(this)
            {
                IEnumerable<int> found = List.GetIndexes(item);
                if (found.Any(i => Flags[i]>0))
                    signalled = true;
            }
            return signalled;
        }

        public ElementList All
        {
            get { return new ElementList(List); }
        }
        public ElementList NotFiltered
        {
            get { return new ElementList(List.Where((el, inx) => Flags[inx] == 0)); }
        }
        public ElementList Filtered
        {
            
            get
            {
                return new ElementList(List.Where((el, inx) => Flags[inx] > 0)
                    .Select((el, inx) => new { el, inx = Flags[inx] })
                    .OrderBy(pair => pair.inx)
                    .Select(pair => pair.el)
                    );
            }
        }
        public struct ElementList:IEnumerable<T>
        {
            private readonly IEnumerable<T> _list;
            public ElementList(IEnumerable<T> list)
            {
                _list = new List<T>(list);
            }
            public IEnumerator<T> GetEnumerator()
            {
                return _list.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
        public IEnumerator<T> GetEnumerator()
        {
            return List.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Remove(T item)
        {
            lock (this)
            {
                IEnumerable<int> found = List.GetIndexes(item);
                var enumerable = found as int[] ?? found.ToArray();
                if (enumerable.Any())
                {
                    int l = enumerable.First();
                    int c = enumerable.Count();
                    List.RemoveRange(l, c);
                    Flags.RemoveRange(l,c);
                }
            }
        }

        public void AddRange(IEnumerable<T> collection)
        {
            lock (this)
            {
                List.AddRange(collection);
                Flags.AddRange(Enumerable.Repeat(0, collection.Count()));
            }
        }

        public void RemoveRange(int index, int count)
        {
            if (count==0)
                return;
            lock (this)
            {
                List.RemoveRange(index,count);
                Flags.RemoveRange(index, count);
            }
        }

        public T this[int i]
        {
            get { return List[i]; }
        }
    }

    public delegate void OnAllFilteredEvent<T>(FilteredList<T> sender);
}
