using System;


namespace ColorGraph
{
    public class DoneSignal : SerialSignal
    {
        public override string ToString()
        {
            return "Done";
        }
        public new object Clone()
        {
            DoneSignal clone = new DoneSignal();
            return clone;
        }
    }
    public class UndoSignal : BroadcastSignal
    {
        public override void Process(ColorableClass obj, bool repetition)
        {
            if (repetition)
                return;
            var node = obj as GraphNode;
            if (node != null)
            {
                node.Undo();
                node.Stop();
            }
        }
        public override string ToString()
        {
            return "Undo";
        }
        public new object Clone()
        {
            UndoSignal clone = new UndoSignal();
            return clone;
        }
    }
    public class StopSignal : BroadcastSignal
    {
        public override void Process(ColorableClass obj, bool repetition)
        {
            if (repetition)
                return;
            var node = obj as GraphNode;
            if (node != null)
            {
                node.Stop();
            }
        }
        public override string ToString()
        {
            return "Stop";
        }
        public new object Clone()
        {
            StopSignal clone = new StopSignal();
            return clone;
        }
    }

    public class ErrorSignal : SerialSignal
    {
        public readonly Exception Error;
        public ErrorSignal(Exception error)
        {
            Error = error;
        }

        public override string ToString()
        {
            return "Error: " + ((Error == null) ? "null" : Error.Message);
        }
        public new object Clone()
        {
            ErrorSignal clone = new ErrorSignal(Error);
            return clone;
        }
    }

}
