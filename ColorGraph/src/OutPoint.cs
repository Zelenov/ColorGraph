using System.Collections.Generic;
using ColorGraph.Lists;


namespace ColorGraph
{
    public class OutPoint : ConnectionPoint 
    {
        public OutPoint(ColorableClass parent)
            : base(parent)
        {
        }

        public OutPoint(ColorableClass parent, GraphConnection connection)
            : base(parent, connection)
        {
        }

        public OutPoint(ColorableClass parent, Color currentColor)
            : base(parent, currentColor)
        {
        }

        public OutPoint(Color currentColor) : base(currentColor) { }

        public override IEnumerable<IColorable> GetPrevPaths()
        {
            return (Parent == null) ? new IColorable[] { } : new IColorable[] { Parent };
        }
        public override IEnumerable<IColorable> GetNextPaths()
        {
            return Connections;
        }
        public override void Flow(IColorable connection, Color newColor)
        {
            if (newColor != null && !FlowIfCan())
            {
                NotifyConnectionBack(connection);
                return;
            }
            base.Flow(connection,newColor);
            if (newColor==null)
            {
                Clear();
            }
            CurrentColor = newColor;
            InvokeOnFlow();
            SplitFlow(Connections.All, graphConnection => graphConnection.Flow(this, CurrentColor));
        }

        
        /// <summary>
        /// All connections signalled false - send nil to all preceding nodes
        /// </summary>
        /// <param name="sender"></param>
        protected override void OnAllFiltered(FilteredList<IColorable> sender)
        {
            if (!SignalIfCan())
                return;
            var exceptionChain = BuildExceptionChain(Connections);
            Signal = new ErrorSignal(new GraphExceptionAllPossibilitiesFailed(this, exceptionChain));
            InvokeOnNotifyBack();
            if (Parent != null)
                Parent.NotifyBack(this, Signal);
        }
        /* public override IEnumerable<KeyValuePair<GraphNode, int>> GetNextNodes()
        {

            var connections = GetAllConnections();
            return connections.Select(connection =>
                {
                    if (connection == null || connection.To == null || connection.To.Parent == null)
                        return new KeyValuePair<GraphNode, int>(null,0);
                    return new KeyValuePair<GraphNode, int>(connection.To.Parent,connection.Length);
                }).Where(graphNode => graphNode.Key != null);

        }*/




       // private readonly List<IColorable> _zeroSignalConnections = new List<IColorable>();


        protected override void NotifyBack(IColorable from, SerialSignal signal)
        {
            signal.StopSending();
            Connections.Filter(from);
        }



        public override ColorableClass ConnectTo(InPoint inPoint, int length = 1)
        {
            GraphConnection.Connect(this, inPoint,length);
            return this;
        }

        public override ColorableClass ConnectTo(OutPoint to, int length = 1)
        {
            return this;
        }

        public override ColorableClass ConnectTo(GraphNode outNode, int length = 1)
        {
            var inPoint = outNode.AddInConnection();
            return ConnectTo(inPoint,length);
        }


    }
}