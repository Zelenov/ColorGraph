using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ColorGraph
{
    public class GraphResult: ICollection<KeyValuePair<ConnectionPoint, GraphPath>>, IEnumerable<KeyValuePair<ConnectionPoint, GraphPath>>
    {
        protected Dictionary<ConnectionPoint, GraphPath> Result = new Dictionary<ConnectionPoint, GraphPath>();
        public void Add(ConnectionPoint key, GraphPath value)
        {
            Result.Add(key,value);
        }
        public GraphPath this[ConnectionPoint point]
        {
            get
            {
                GraphPath res;
                if (!Result.TryGetValue(point,out res))
                    return null;
                return res;
            }
            
        }

        public IEnumerator<KeyValuePair<ConnectionPoint, GraphPath>> GetEnumerator()
        {
            return Result.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(KeyValuePair<ConnectionPoint, GraphPath> item)
        {
            ((ICollection<KeyValuePair<ConnectionPoint, GraphPath>>)Result).Add(item);
        }

        public void Clear()
        {
            Result.Clear();
        }

        bool ICollection<KeyValuePair<ConnectionPoint, GraphPath>>.Contains(KeyValuePair<ConnectionPoint, GraphPath> item)
        {
            return ((ICollection<KeyValuePair<ConnectionPoint, GraphPath>>)Result).Contains(item);
        }

        void ICollection<KeyValuePair<ConnectionPoint, GraphPath>>.CopyTo(KeyValuePair<ConnectionPoint, GraphPath>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<ConnectionPoint, GraphPath>>)Result).CopyTo(array,arrayIndex);
        }

        bool ICollection<KeyValuePair<ConnectionPoint, GraphPath>>.Remove(KeyValuePair<ConnectionPoint, GraphPath> item)
        {
            return ((ICollection<KeyValuePair<ConnectionPoint, GraphPath>>)Result).Remove(item);
        }

        public int Count { get { return Result.Count; } }
        public bool IsReadOnly { get { return ((ICollection<KeyValuePair<ConnectionPoint, GraphPath>>)Result).IsReadOnly; } }
    }
    public class GraphPath
    {
        public List<Color> ColorPath = new List<Color>();
        public List<IColorable> NodePath = new List<IColorable>();
        public Exception Exception = null;

        public Color LastColor { get { return ColorPath.Count == 0 ? null : ColorPath[ColorPath.Count - 1]; } }
    }
}
