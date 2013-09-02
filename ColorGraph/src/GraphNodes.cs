using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;


namespace ColorGraph
{

    public abstract class GraphNodeWithInput<TInput> : GraphNode where TInput : Color
    {

        public override void Stop()
        {

        }
        public override void Undo()
        {

        }
        public abstract void Work(List<TInput> goodColors, List<Color> badColors);
        public override void DoWork()
        {

            List<Color> currentColors = GetNotNullInput().Select(inPoint => inPoint.GetCurrentColor()).ToList();
            List<TInput> goodColors =
                currentColors.Where(flow => (flow is TInput)).Select(flow => flow as TInput).ToList();
            List<Color> badColors = currentColors.Where(flow => (flow != null) && !(flow is TInput) && !(flow is IBlack)).ToList();

            /*if (!goodColors.Any())
                Flow(new Black());               
            else
            */
            Work(goodColors, badColors);

        }
    }

    public delegate Color OnWork(List<IntColor> goodColors, List<Color> badColors);
    public class ActionNode : GraphNodeWithInput<IntColor>
    {
        private readonly OnWork _onWork;
        public ActionNode(OnWork onWork)
        {
            _onWork = onWork;
        }

        public override void Work(List<IntColor> goodColors, List<Color> badColors)
        {
            Result = _onWork(goodColors,badColors);
            if (Result != null)
            {
                FlowResult();
            }
        }
    }

    public class Summator:GraphNodeWithInput<IntColor>
    {

        public override void Work(List<IntColor> goodColors, List<Color> badColors)
        {
            Result = new IntColor(goodColors.Sum(color => color.Value));
            FlowResult();
        }
    }

    public class Stringer : GraphNodeWithInput<IntColor>
    {
        public override void Work(List<IntColor> goodColors, List<Color> badColors)
        {
            Result = Color.Mix(goodColors.Select(intColor => new StringColor(intColor.Value.ToString(CultureInfo.InvariantCulture)) as Color ));
            FlowResult();
        }
    }
    public class ReplaceNode : GraphNode
    {
        public InPoint Replacement;
        public ReplaceNode()
        {
            Replacement = new InPoint(this);
        }
        public override void DoWork()
        {
            FlowResult(Replacement.GetCurrentColor());
        }

        public override void Stop()
        {
            
        }

        public override void Undo()
        {
           
        }
    }
    public class CompareNode : GraphNodeWithInput<IntColor>
    {
       
        public InPoint CompareWith;
        public InPoint CompareOperation;
        public CompareNode()
        {
            CompareWith = AddInvisiblePoint();
            CompareOperation = AddInvisiblePoint();
        }
        public override void Work(List<IntColor> goodColors, List<Color> badColors)
        {
            int[] values = goodColors.Select(intColor => intColor.Value).ToArray();
            bool res = false;

            var compareType = ((CompareColor) CompareOperation.GetCurrentColor());
            var compareWith = ((IntColor)CompareWith.GetCurrentColor());
            if (compareType == null || compareWith == null)
            {
                GoBack(new ErrorSignal(new Exception("Bad arguments")));
                return;
            }
            foreach (var value in values)
            {
                switch (compareType.Value)
                {
                    case (CompareType.Less):
                        {
                            res = value < compareWith.Value;
                            break;
                        }
                    case (CompareType.LessOrEqual):
                        {
                            res = value <= compareWith.Value;
                            break;
                        }
                    case (CompareType.Equal):
                        {
                            res = value == compareWith.Value;
                            break;
                        }
                    case (CompareType.MoreOrEqual):
                        {
                            res = value >= compareWith.Value;
                            break;
                        }
                    case (CompareType.More):
                        {
                            res = value > compareWith.Value;
                            break;
                        }
                    case (CompareType.NotEqual):
                        {
                            res = value != compareWith.Value;
                            break;
                        }
                }
                if (!res)
                    break;
            }
            switch (res)
            {
                case (true):
                    {
                        Result = Color.Mix(goodColors.Cast<Color>());
                        FlowResult();
                        break;
                    }
                case (false):
                    {
                        GoBack(new ErrorSignal(new Exception("Comparison failed")));
                        break;
                    }
            }
           
        }
    }
    class FinishNode : GraphNode
    {

        public override void DoWork()
        {
           if (GetNotNullInput().Any())
                GoBack(new DoneSignal());
        }

        public override void Stop()
        {
            
        }

        public override void Undo()
        {
            
        }
    }


    public class MixNode : GraphNode
    {

        public override void DoWork()
        {
            var inputs =
                        GetNotNullInput()
                            .Select(inPoint => inPoint.GetCurrentColor())
                            .ToArray();
            
            if (!inputs.Any())
            {
                GoBack(new ErrorSignal(new Exception("bad input")));
            }
            else
            {
                Result=Color.Mix(inputs);
                FlowResult();
            }
        }

        public override void Stop()
        {
            
        }

        public override void Undo()
        {
            
        }
    }
}
