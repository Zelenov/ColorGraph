using System.Collections.Generic;
using ColorGraph.Lists;


namespace ColorGraph
{
    public class InPoint : ConnectionPoint
    {


       // protected readonly ZSortedList<IColorable> SignalledNil = new ZSortedList<IColorable>();


        public InPoint(ColorableClass parent)
            : base(parent)
        {
            
        }

        public InPoint(ColorableClass parent, GraphConnection connection)
            : base(parent, connection)
        {
      
        }

        public InPoint(ColorableClass parent, Color currentColor)
            : base(parent, currentColor)
        {
           
        }
        public InPoint(Color currentColor) : base(currentColor) { }
       /* public bool CanFlow(Color newColor)
        {
            if (newColor!=null)
                return (!Colored)||(newColor.CompareTo(CurrentColor)==0);
            return (Connections.IsFiltered())Connections.NotFilteredCount <= 1;
        }*/

        /// <summary>
        /// All connections signalled nil - flow nil forth
        /// </summary>
        /// <param name="sender"></param>
        protected override void OnAllFiltered(FilteredList<IColorable> sender)
        {
            Clear();
            InvokeOnFlow();
            Parent.Flow(this, CurrentColor);
            
        }
       


        /* public override IEnumerable<KeyValuePair<GraphNode, int>> GetNextNodes()
        {
            return Parent == null ? new KeyValuePair<GraphNode, int>[] { } : new[] {new KeyValuePair<GraphNode, int>(Parent,0) };
        }*/

        public override IEnumerable<IColorable> GetPrevPaths()
        {
            return Connections;
        }
        public override IEnumerable<IColorable> GetNextPaths()
        {
            return Parent == null ? new IColorable[0] : new IColorable[] {Parent};
        }

        public override void Flow(IColorable connection, Color newColor)
        {
            /*var notSignalledConnections = GetNotSignalledConnections();
                            if (notSignalledConnections.Any())
                            {
                                SplitFlow(Connections, graphConnection => graphConnection.Flow(CurrentColor));
                            }*/
            if (newColor == null) //signalled nil
            {
                Connections.Filter(connection);
                return;
            }
            if (!FlowIfCan())
                return;
            bool doFlow;
            lock (this)
            {
                doFlow = !Connections.IsFiltered(connection);

            }
            if (doFlow)
            {
                base.Flow(connection, newColor);
                CurrentColor = newColor;
                if (Parent!=null)
                Parent.Flow(this,CurrentColor);
                InvokeOnFlow(); //Flow
            }
            else
                NotifyConnectionBack(connection);
        }


    
        /// <summary>
        /// We finished or failed - send result to all preceding colorables. But current have to be last.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="signal"></param>
        protected override void NotifyBack(IColorable from, SerialSignal signal)
        {
            if (!SignalIfCan())
                return;
            Signal = signal;
            base.NotifyBack(from, signal);
            
            List<IColorable> currentColor = new List<IColorable>();
            List<IColorable> otherColor = new List<IColorable>();
            foreach (var connection in Connections.NotFiltered)
            {
                if (connection==null)
                    continue;
                if (connection.GetCurrentColor() == CurrentColor) 
                    currentColor.Add(connection);
                else
                    otherColor.Add(connection);
            }
            SplitNotify(otherColor,signal);
            SplitNotify(currentColor, signal);
            InvokeOnNotifyBack();
        }

        public override ColorableClass ConnectTo(GraphNode to, int length = 1)
        {
            if (Parent != null)
            {
                GraphNode graphNode = Parent as GraphNode;
                if (graphNode!=null)
                    graphNode.Input.Remove(this);
            }
            to.AddInConnection(this);
            Parent = to;
            return null;
        }
    }
}