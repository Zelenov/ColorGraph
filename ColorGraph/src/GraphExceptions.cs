using System;


namespace ColorGraph
{
    class GraphException : Exception
    {
        public IColorable Sender;
        public GraphException(IColorable sender)
        {
            Sender = sender;
        }
    }
    class GraphExceptionAllPossibilitiesFailed:GraphException
    {
        public Exception[] Errors;
        public GraphExceptionAllPossibilitiesFailed(IColorable sender, Exception[] errors)
            : base(sender)
        {
            Errors = errors;
        }
    }
    class GraphExceptionAlreadyColored : GraphException
    {
        public GraphExceptionAlreadyColored(IColorable sender)
            : base(sender)
        {
        }
    }
}
