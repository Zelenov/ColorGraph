using System;
using System.Collections.Generic;
using System.Threading;
using ColorGraph.Lists;


namespace ColorGraph
{
    public abstract class ConnectionPoint : Vertex
    {
        protected readonly FilteredList<IColorable> Connections = new FilteredList<IColorable>();


        

        public ColorableClass Parent { get; protected set; }
        public ConnectionPoint(ColorableClass parent)
        {
            Parent = parent;
            Connections.OnAllFiltered += OnAllFiltered;
        }

        protected virtual void OnAllFiltered(FilteredList<IColorable> sender) { }

        public ConnectionPoint(ColorableClass parent, GraphConnection connection)
            : this(parent)
        {
            AddConection(connection);
        }


        public ConnectionPoint(ColorableClass parent, Color currentColor)
            : this(parent)
        {
            CurrentColor = currentColor;
        }
        public ConnectionPoint(Color currentColor):this((GraphNode)null)
        {
            CurrentColor = currentColor;
        }

        //  public ConnectionPoint{}

      

        public GraphConnection AddConection(GraphConnection connection)
        {
            if (connection!=null)
                Connections.Add(connection);
            return connection;
        }

        protected static void SplitFlow(IEnumerable<IColorable> selectedConnections, Action<IColorable> proc)
        {

            foreach (var graphConnection in selectedConnections)
            {
                IColorable connection = graphConnection;
                new Thread(() => proc(connection)) {IsBackground = true}.Start();
            }
            /*GraphConnection last = null;
            
            foreach (var graphConnection in selectedConnections)
            {
                if (last!=null)
                {
                    GraphConnection connection = graphConnection;
                    new Thread(() => proc(connection)) { IsBackground = true }.Start();
                }
                else
                    last = graphConnection;
                   
            }
            if (last!=null)
                proc(last);*/
        }


        protected override void Clear()
        {
            lock (this)
            {
                base.Clear();
                Connections.ClearFilters();
            }
        }
       

        // public abstract IEnumerable<KeyValuePair<GraphNode,int>> GetNextNodes();

    }
}