using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ColorGraph.Lists;

namespace ColorGraph
{
    public class Graph
    {
        //protected Dictionary<ColorableClass, Signal> NodesToNotify = new Dictionary<ColorableClass, Signal>();
        protected FilteredList<ColorableClass>  NodesToNotify = new FilteredList<ColorableClass>();
        protected Dictionary<ColorableClass,Color> StartColors = new Dictionary<ColorableClass, Color>();
        protected FilteredList<ColorableClass> Last = new FilteredList<ColorableClass>();
        protected FilteredList<ColorableClass> First = new FilteredList<ColorableClass>();


        protected object StartingLock = new object();
        protected object ClearingLock = new object();
        protected object StopingLock = new object();
        protected object CollectionLock = new object();
        protected ManualResetEvent StartedEvent = new ManualResetEvent(true);
        protected ManualResetEvent ClearingEvent = new ManualResetEvent(true);
        protected ManualResetEvent StopingEvent = new ManualResetEvent(true);
        protected bool WasStoped = false;
        protected bool WasCleared = false;

        public event OnNotifyBackEvent OnGlobalError;

        public bool IsWorking { get { return StartedEvent.WaitOne(0); } }
        public bool IsStoping { get { return StopingEvent.WaitOne(0); } }
        public bool IsClearing { get { return ClearingEvent.WaitOne(0); } }

        public Graph()
        {
            NodesToNotify.OnAllFiltered += AllNotifyNodesSignalled;
            Last.OnAllFiltered += AllLastFinished;
            First.OnAllFiltered += AllFirstSignalled;
        }


        protected void InvokeGlobalError(IColorable sender, Signal stop)
        {
            if (IsWorking)
            {
                OnNotifyBackEvent handler = OnGlobalError;
                if (handler != null) handler(sender, stop);
            }
        }

        /// <summary>
        /// Stoping finished
        /// </summary>
        /// <param name="sender"></param>
        private void AllFirstSignalled(FilteredList<ColorableClass> sender)
        {
            WasStoped = true;
            StopingEvent.Set();
        }

        /// <summary>
        /// Clearing finished
        /// </summary>
        /// <param name="sender"></param>
        private void AllLastFinished(FilteredList<ColorableClass> sender)
        {
            WasCleared = true;
            ClearingEvent.Set();
        }

        private void AllNotifyNodesSignalled(FilteredList<ColorableClass> sender)
        {
            Finish();
        }

        public void Add(IEnumerable<ColorableClass> nodes)
        {
            Last.Clear();
            First.Clear();
            foreach (var graphNode in nodes)
            {
                Add(graphNode);
            }
        }

        public ColorableClass Add(ColorableClass node)
        {
            if (!NodesToNotify.Contains(node))
                NodesToNotify.Add(node);
            return node;
        }

        protected void Traverse(IEnumerable<ColorableClass> start, Func<IColorable, IEnumerable<IColorable>> nextFunc, Action<IColorable> traverseEveryProc, Action<IColorable> traverseLastProc)
        {
        
            var visited = new ZSortedList<IColorable>();

            var queue = new Queue<IColorable>(start.Select(el => (IColorable)el));
            while (queue.Count != 0)
            {
                IColorable current = queue.Dequeue();
                if (visited.Contains(current))
                    continue;
                visited.Add(current);
                if (traverseEveryProc != null)
                    traverseEveryProc(current);
                IEnumerable<IColorable> outConnections = nextFunc(current);
                int added = 0;
                foreach (var outConn in outConnections)
                {
                    queue.Enqueue(outConn);
                    added++;
                }
                if (traverseLastProc != null && added == 0 && (current is ColorableClass))
                {
                    traverseLastProc(current);
                }
            }
        }
        protected void TraverseLast(IEnumerable<ColorableClass> start, Func<IColorable, IEnumerable<IColorable>> nextFunc, Action<IColorable> traverseLastProc)
        {
            Traverse(start, nextFunc, null, traverseLastProc);
        }
        protected List<ColorableClass> TraverseLast(IEnumerable<ColorableClass> start, Func<IColorable, IEnumerable<IColorable>> nextFunc)
        {
            var res = new List<ColorableClass>();
            TraverseLast(start, nextFunc, current =>
                                          {
                                              var colorableClass = current as ColorableClass;
                                              if (colorableClass != null)
                                                  res.Add(colorableClass);
                                          });
            
            
                   
              
            
             return res;
           
        }

        protected void GetLast(IEnumerable<ColorableClass> startFrom)
        {
            if (Equals(Last, startFrom))
                startFrom = new List<ColorableClass>(Last);
            if (Last.Count != 0)
                Last.Clear();
            Last.AddRange(TraverseLast(startFrom, current => current.GetNextPaths()));
        }
        protected void GetLast()
        {
            if (Last.Count != 0)
                return;
            GetLast(NodesToNotify);
        }

        protected void GetFirst()
        {
            if (First.Count != 0)
                return;
            GetLast();
            First.AddRange(TraverseLast(Last, current => current.GetPrevPaths()));
        }

        public void GetStartColors()
        {
            GetFirst();
            StartColors.Clear();
            foreach (var first in First)
            {
                var color = first.GetCurrentColor();
                if (!StartColors.ContainsKey(first))
                    StartColors.Add(first, color);
                if (color == null && NodesToNotify.Contains(first))
                    NodesToNotify.Filter(first);
            }


        }
        public void Undo()
        {
            UndoAsync();
            StopingEvent.WaitOne();
        }


        /// <summary>
        /// Sending false signal to all finish nodes. This causes all nodes, that haven't work yet to be uncallable, and all working nodes to stop.
        /// So, the graph finishes with false signals.
        /// </summary>
        public void UndoAsync()
        {
            lock (StopingLock)
            {
                if (WasStoped)
                    return;
                StopingEvent.WaitOne();
                StopingEvent.Reset();
            }
            System.Diagnostics.Debug.WriteLine("Undo");
            PrepareFlow();
            First.ClearFilters();
            foreach (var firstNode in First)
            {
                firstNode.OnNotifyBack += OnFirstFinished;
            }
            foreach (var last in Last)
            {
                IColorable last1 = last;
                new Thread(() => last1.NotifyBack(last1, new UndoSignal())) { IsBackground = true }.Start();
            }
        }
        public void Stop()
        {
            StopAsync();
            StopingEvent.WaitOne();
        }

       
        /// <summary>
        /// Sending false signal to all finish nodes. This causes all nodes, that haven't work yet to be uncallable, and all working nodes to stop.
        /// So, the graph finishes with false signals.
        /// </summary>
        public void StopAsync()
        {
            lock (StopingLock)
            {
                if (WasStoped)
                    return;
                StopingEvent.WaitOne();
                StopingEvent.Reset();
            }
            System.Diagnostics.Debug.WriteLine("Stoping");
            PrepareFlow();
            First.ClearFilters();
            foreach (var firstNode in First)
            {
                firstNode.OnNotifyBack += OnFirstFinished;
            }
            foreach (var last in Last)
            {
                IColorable last1 = last;
                new Thread(() => last1.NotifyBack(last1, new StopSignal())) { IsBackground = true }.Start();
            }
        }
        public void Start()
        {
            StartAsync();
            StartedEvent.WaitOne();
        }
        public void PrepareFlow()
        {
           
            GetLast();
            GetFirst();
            
            foreach (var last in Last)
            {
                if (!(last is FinishNode))
                    last.ConnectTo(new FinishNode());
            }
            GetLast(Last);
            foreach (var firstNode in First)
            {
                firstNode.OnNotifyBack -= OnFirstFinished;
            }
            foreach (var notifyNode in NodesToNotify)
            {
                notifyNode.OnNotifyBack-= OnNotifyNodeFinished;
            }
            foreach (var lastNode in Last)
            {
                lastNode.OnFlow -= OnLastFlow;
            }
            
        }
        public void StartAsync()
        {
            //enter only one thread
            lock (StartingLock)
            {
                //if working when wait for work to finish
                StartedEvent.WaitOne();
                //Start working
                StartedEvent.Reset();
            }
            GetStartColors();
            if (NodesToNotify.FilteredCount == NodesToNotify.Count)
            {
                if (NodesToNotify.Count == 0)
                    Finish();
                return;
            }
            Stop();
            ClearGraph();
            System.Diagnostics.Debug.WriteLine("Starting");
            WasCleared = false;
            WasStoped = false;
            PrepareFlow();
            NodesToNotify.ClearFilters();
            foreach (var notifyNode in NodesToNotify)
            {
                notifyNode.OnNotifyBack += OnNotifyNodeFinished;
            } 
            //Set onNotifyBackEvent for all nodes
            if (OnGlobalError!=null)
            {
                Traverse(First, current => current.GetNextPaths(), current =>
                                                                       {
                                                                           var colorableClass =
                                                                               current as ColorableClass;
                                                                           if (colorableClass != null)
                                                                               colorableClass.OnNotifyBack +=
                                                                                   InvokeGlobalError;
                                                                       },
                                                                       null);
            }
            foreach (var first in First)
            {
                IColorable first1 = first;
                
                Color color;
                if (StartColors.ContainsKey(first))
                    color = StartColors[first];
                else
                    color = null;
                new Thread(() => first1.Flow(first1, color)) { IsBackground = true }.Start();
            }
        }



        private void OnFirstFinished(IColorable sender, Signal stop)
        {
            if (sender is ColorableClass)
                First.Filter((ColorableClass)sender);
        }

        private void OnLastFlow(IColorable connection, Color newcolor)
        {
            if (newcolor==null && (connection is ColorableClass))
                Last.Filter((ColorableClass)connection);
        }

        public void ClearGraph()
        {
            ClearGraphAsync();
            ClearingEvent.WaitOne();
        }
        /// <summary>
        /// Sends null to all first points - all nodes reset their clear state.
        /// </summary>
        public void ClearGraphAsync()
        {
            Stop();
            lock (ClearingLock)
            {
                if (WasCleared)
                    return;
                //if working when wait for work to finish
                ClearingEvent.WaitOne();
                StopingEvent.WaitOne();
                //TODO fuck
                //StartedEvent.WaitOne();
                //Start working (sending null
                ClearingEvent.Reset();
            }
            System.Diagnostics.Debug.WriteLine("Clearing");
            PrepareFlow();
            Last.ClearFilters();
            foreach (var lastNode in Last)
            {
                lastNode.OnFlow += OnLastFlow;
            } 
            foreach (var first in First)
            {
                IColorable first1 = first;
                new Thread(()=> first1.Flow(first1, null)) { IsBackground = true }.Start();
            }
        }

            

        private void OnNotifyNodeFinished(IColorable sender, Signal result)
        {
            if (!(sender is ColorableClass))
                return;
            System.Diagnostics.Debug.WriteLine(String.Format("Finished {0} ({1}, color={2})", result, GetType(), sender.GetCurrentColor() == null ? "null" : sender.GetCurrentColor() .ToStringDemuxed()));
            NodesToNotify.Filter((ColorableClass)sender);

        }


            /*bool doFinish = false;}
            lock (CollectionLock)
            {
                if (!NodesToNotify.Contains(senderClass))
                    return;
                NodesToNotify[senderClass] = result;
                FinishedCount++;
                doFinish = FinishedCount == NodesToNotify.Count;
            }
            if (doFinish)
            {
                Finish();
            }*/
        public event OnFinishGraphEvent OnFinish;

        public void InvokeOnFinish(GraphResult result)
        {
            OnFinishGraphEvent handler = OnFinish;
            if (handler != null) handler(result);
        }

        private void Finish()
        {
            var dic = new GraphResult();
            lock (CollectionLock)
            {
                foreach (ConnectionPoint connectionPoint in NodesToNotify)
                {

                    GraphPath colorList = GetColorList(connectionPoint);
                    dic.Add(connectionPoint, colorList);


                }
            }
            
            InvokeOnFinish(dic);
            Undo();
            StartedEvent.Set();
        }

        private class GraphPathNode:IComparable<GraphPathNode>
        {
            protected bool Equals(GraphPathNode other)
            {
                return Length == other.Length && Equals(Current, other.Current) && Equals(Prev, other.Prev);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != GetType()) return false;
                return Equals((GraphPathNode) obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hashCode = Length;
                    hashCode = (hashCode*397) ^ (Current != null ? Current.GetHashCode() : 0);
                    hashCode = (hashCode*397) ^ (Prev != null ? Prev.GetHashCode() : 0);
                    return hashCode;
                }
            }

            protected readonly int Length;
            public readonly IColorable Current;
            public readonly GraphPathNode Prev;

            public GraphPathNode(GraphPathNode prev, IColorable current)
            {
                Prev = prev;
                Current = current;
                int newLen = 0;
                if (prev != null && prev.Current != null)
                {
                    var edge = prev.Current as IEdge;
                    if (edge != null)
                    {
                        newLen = edge.GetLength();
                    }
                    if (prev.Current is IVertex && current!=null && current is IVertex)
                    {
                        newLen = ((IVertex)prev.Current).GetLengthBeetween(current);
                    } 
                }
                if (prev != null)
                {
                    Length = prev.Length + newLen;
                }
                else if (current != null)
                {
                    Length = newLen;
                }
                else
                {
                    Length = int.MinValue;
                }
            }

            public int CompareTo(GraphPathNode other)
            {
                if ((object)other == null)
                {
                    return 1;
                }
                return Length.CompareTo(other.Length);
            }
            public static bool operator <(GraphPathNode a, GraphPathNode b)
            {
                if (((object)a == null) || ((object)b == null))
                {
                    return false;
                }
                return a.CompareTo(b) < 0;
            }

            public static bool operator >(GraphPathNode a, GraphPathNode b)
            {
                if (((object)a == null) || ((object)b == null))
                {
                    return false;
                }
                return a.CompareTo(b) > 0;
            }
            public static bool operator ==(GraphPathNode a, GraphPathNode b)
            {
                if (System.Object.ReferenceEquals(a, b))
                {
                    return true;
                }

                // If one is null, but not both, return false.
                if (((object)a == null) || ((object)b == null))
                {
                    return false;
                }
                return a.CompareTo(b) == 0;
            }

            public static bool operator !=(GraphPathNode a, GraphPathNode b)
            {
                return !(a == b);
            }
        }
      
       /* private IEnumerable<GraphPath> GetPossibleOutputs(GraphNode graphNode)
        {
            if (Color.NullOrBlack(graphNode.Result))
                return new GraphPath[] { };

            var allConnections =
                graphNode.GetAllOutConnections().Where(
                    graphConnection =>
                    !Color.NullOrBlack(graphConnection.CurrentColor) &&
                    (graphConnection.To != null) &&
                    (graphConnection.To.Parent != null) &&
                    !(graphConnection.To.Parent is FinishNode) &&
                    (graphConnection.To.CurrentColor == graphConnection.CurrentColor));
            return allConnections.Select(
                graphConnection => new GraphPath(graphConnection.To.Parent, graphConnection.Length, graphConnection.To.Parent.Result));

        }*/
        private GraphPath GetColorList(IColorable connectionPoint)
        {

            GraphPathNode maxPath = new GraphPathNode(null, null);
            var queue = new Queue<GraphPathNode>();
            //adding first item to queue
            queue.Enqueue(new GraphPathNode(null, connectionPoint));
            //work while queue not empty - width search
            do
            {
                //get next item
                GraphPathNode current = queue.Dequeue();


                IColorable[] graphPaths = null;
                
                //setMax = true if we need to compare current length with max
                IColorable last = current.Current;
               
                bool setMax = (last is FinishNode);
                if (!setMax)
                {
                    //increase length
                   
                    //get all outputs
                    IEnumerable<IColorable> possibleOutputs = last.GetNextPaths();

                    //null check
                    graphPaths = possibleOutputs as IColorable[] ?? possibleOutputs.ToArray();
                    
                    //if there is no output from current node or node have no color - we nedd to compare current length with max
                    setMax = !graphPaths.Any() || (last.GetCurrentColor() == null);
                }
                if (setMax)
                {
                    //compare maximal path with current
                    if (maxPath < current)
                    {
                        maxPath = current;
                    }
                }
                else
                {
                    //we have node to explore 
                    //get current color

                   
                    //for all branches out of this node
                    foreach (IColorable t in graphPaths)
                    {
                        //make new path to branch
                        GraphPathNode newPath = new GraphPathNode(current,t);
                        /*
                        //get color of branch
                        Color newColor = t.GetCurrentColor();
                        if (newColor != null)
                        {
                            //if color contains previous color 
                            if (newColor.Contains(currentColor))
                                //then we set it al last color in path
                                newPath.LastColor = newColor;
                            else
                                //else - color changed and we add it
                                newPath.PathTo.Add(newColor);
                            
                        }*/
                        //add new path to que to analize
                        if (t!=null)
                            queue.Enqueue(newPath);
                    }
                }
            } while (queue.Count != 0);

            GraphPath res = MarkAsResult(maxPath);
            return res;
        }

        private GraphPath MarkAsResult(GraphPathNode maxPath)
        {
            var res = new GraphPath();
            
            if (maxPath != null && maxPath.Current!=null)
            {
                Color nextColor = maxPath.Current.GetCurrentColor();
                var current = maxPath.Prev;
                while (current != null && current.Current != null)
                {
                    res.NodePath.Add(current.Current);
                    Color prevColor = current.Current.GetCurrentColor();
                    if (nextColor == null)
                    {
                        var signal = current.Current.GetSignal();
                        var errorSignal = signal as ErrorSignal;
                        if (errorSignal != null)
                            res.Exception = errorSignal.Error;
                        nextColor = prevColor;
                    }
                    else
                    {
                        if (prevColor != null)
                        {
                            //if color contains previous color 
                            if (!nextColor.Contains(prevColor))
                            {
                                res.ColorPath.Add(nextColor);
                                nextColor = prevColor;
                            }
                        }

                        var undoable = current.Current as IUndoable;
                        if (undoable != null)
                        {
                            undoable.SetAlreadyUndone(true);
                        }
                    }
                    current = current.Prev;
                }
                res.ColorPath.Add(nextColor);
            }
            res.ColorPath.Reverse();
            res.NodePath.Reverse();
            return res;
        }
    }

    public delegate void OnFinishGraphEvent(GraphResult result);
}
