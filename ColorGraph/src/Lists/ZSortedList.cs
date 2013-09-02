using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ColorGraph.Lists
{
    public class ZSortedList<T> : IEnumerable<T>
    {
        private readonly List<T> _items = new List<T>();
        private readonly List<int> _hashes = new List<int>(); 
        public IList<T> Items { get { return _items; } }

        public int Count
        {
            get { return _items.Count(); }
        }
        protected T GetItem(int index)
        {
            if (index > _items.Count() || index < 0)
                return default(T);
            return _items[index];
        }
       /* public T this[int index]{
            get { return GetItem(index) } ;
        }*/
        public void Add(IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                Add(item);
            }
        }

        private int GetHashIndex(int hash)
        {
            bool dummy;
            return GetHashIndex(hash, out dummy);
        }
        public IEnumerable<int> GetIndexes(T item)
        {
            var hash = item.GetHashCode();
            bool found;
            int index = GetHashIndex(hash, out found);
            return !found ? new int[0] : GetHashIndexes(index);
        } 
        private IEnumerable<int> GetHashIndexes(int hashIndex)
        {
            var hash = _hashes[hashIndex];
            int l;
            int r;
            for (l = hashIndex; l >= 0; l--)
            {
                if (_hashes[l] != hash)
                    break;
            }
            l++;
            for (r = hashIndex; r < _hashes.Count; r++)
            {
                if (_hashes[r] != hash)
                    break;
            }
            r--;

            return Enumerable.Range(l, r-l+1);
        }
        private int GetHashIndex(int hash, out bool found)
        {
            found = false;
            var l = 0;
            var r = _hashes.Count;
            if (r == 0)
                return 0;
            if (_hashes[0] > hash)
                return 0;
            if (_hashes[r-1] < hash)
                return r;
            var m = 0;
            while (l < r)
            {
                m = l + (r - l)/2;
                var cmp = Math.Sign(_hashes[m].CompareTo(hash));
                switch (cmp)
                {
                    case 0:
                        {
                            found = true;
                            return m;
                        }
                    case 1:
                        {
                            r = m;
                            break;
                        }
                    case -1:
                        {
                            l = m+1;
                            break;
                        }

                }
            }
            found = (_hashes[r] == hash);
            return l + (r - l) / 2;
        }
        private void Insert(T item,int hash, int index)
        {
            if (index < 0 || index >= _hashes.Count)
            {
                _items.Add(item);
                _hashes.Add(hash);
            }
            else
            {
                _hashes.Insert(index,hash);
                _items.Insert(index, item);
            }
        }
        private int GetHash(T item)
        {
            int hash;
            if (item == null)
                hash = 0;
            else
                hash = item.GetHashCode();
            return hash;
        }
        public void Add(T item)
        {
           
            int hash = GetHash(item);
            bool found;
            var hashIndex = GetHashIndex(hash,out found);
            if (!found)
                Insert(item, hash, hashIndex);
        }
        
        public bool Contains(T item)
        {
            int hash = GetHash(item);
            bool found;
            GetHashIndex(hash, out found);
            return found;
        }

        public void Clear()
        {
            _hashes.Clear();
            _items.Clear(); 
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void RemoveRange(int start, int count)
        {
            lock(this)
            {
                _hashes.RemoveRange(start,count);
                _items.RemoveRange(start, count);
            }
        }

        public void AddRange(IEnumerable<T> collection)
        {
            foreach (var item in collection)
            {
                Add(item);    
            }
        }

        public T this[int i]
        {
            get { return _items[i]; }
        }
    }
}
