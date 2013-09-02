using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using ColorGraph;

namespace TestColorGraph
{
    class Program
    {
        static Graph graph = new Graph();
        private static int counter = 0;
        static void Main(string[] args)
        {
             //Console.Write(Sum(1,3));
            Console.Write(Compare(1, 1));
            Console.ReadLine();
        }
        static void Write(object text)
        {
            System.Diagnostics.Debug.Write(text);
            Console.Write(text);
        }
        /*
         *  var firstTryNode = new ActionNode(delegate(List<IntColor> colors, List<Color> badColors)
                {
                    var sum = colors.Sum(color => color.Value);
                    return sum < 5 ? (Color) null : new IntColor(sum);
                });
            var secondTryNode = new ActionNode(delegate(List<IntColor> colors, List<Color> badColors)
            {
                var sum = colors.Sum(color => color.Value);
                return sum < 10 ? (Color)null : new IntColor(sum);
            });
         */
        static string Compare(int x, int compareWith)
        {
            var xPoint = new OutPoint(new IntColor(x)) { Name = "xPoint" };
            var compareWithPoint = new OutPoint(new IntColor(compareWith)) { Name = "compareWithPoint" };

            var comparisonNodeLess = new CompareNode() { Name = "comparisonNodeLess" };
            var comparisonNodeMore = new CompareNode() { Name = "comparisonNodeMore" };
            var comparisonNodeEquals = new CompareNode() { Name = "comparisonNodeEquals" };

            xPoint.ConnectTo(comparisonNodeLess);
            xPoint.ConnectTo(comparisonNodeMore);
            xPoint.ConnectTo(comparisonNodeEquals);

            compareWithPoint.ConnectTo(comparisonNodeLess.CompareWith);
            compareWithPoint.ConnectTo(comparisonNodeMore.CompareWith);
            compareWithPoint.ConnectTo(comparisonNodeEquals.CompareWith);

            new OutPoint(new CompareColor(CompareType.Less)) { Name = "CompareType.Less" }.ConnectTo(comparisonNodeLess.CompareOperation);
            new OutPoint(new CompareColor(CompareType.More)) { Name = "CompareType.More" }.ConnectTo(comparisonNodeMore.CompareOperation);
            new OutPoint(new CompareColor(CompareType.Equal)) { Name = "CompareType.Equal" }.ConnectTo(comparisonNodeEquals.CompareOperation);

            var mix1 = new MixNode() { Name = "mix1" };
            var mix2 = new MixNode() { Name = "mix2" };
            var mix3 = new MixNode() { Name = "mix3" };

            comparisonNodeLess.ConnectTo(mix1);
            comparisonNodeMore.ConnectTo(mix2);
            comparisonNodeEquals.ConnectTo(mix3);

            new OutPoint(new StringColor("Less")) { Name = "LessString" }.ConnectTo(mix1);
            new OutPoint(new StringColor("More")) { Name = "MoreString" }.ConnectTo(mix2);
            new OutPoint(new StringColor("Equals")) { Name = "EqualsString" }.ConnectTo(mix3);
            //comparisonNodeEquals.AddOutConnection().ConnectTo(new OutPoint(new StringColor("Equal")));

            //both x and y will recieve the same result in this text, so add just one of them
            graph.Add(xPoint);
            string res = "";
            graph.OnFinish += delegate(GraphResult result)
                {
                    GraphPath path = result[xPoint];
                    if (path != null)
                    {
                        //sum is list of transitions of first value — result is the last one.
                        res = path.LastColor.ToStringDemuxed();
                    }

                };
            graph.Start();
            return res;
        }
        static int Sum(int x, int y)
        {
            var xNode = new InPoint(new IntColor(x));
            var yNode = new InPoint(new IntColor(y));
            int res = 0;
            var summator = new Summator();

            //connect x and y to summato
            xNode.ConnectTo(summator);
            yNode.ConnectTo(summator);

            //both x and y will recieve the same result in this text, so add just one of them
            graph.Add(xNode);
            graph.OnFinish += delegate(GraphResult result)
                {
                    GraphPath path = result[xNode];
                    if (path != null)
                    {
                        var color = path.LastColor as IntColor;
                        if (color != null)
                            res = color.Value;
                        //sum is list of transitions of first value — result is the last one.
                       
                    }

                };

            graph.Start();
            return res;
        }
        static void Test()
        {
            //Calculate x+y
            var summ = new Summator();
            var summ2 = new Summator();
            var summ3 = new Summator();
            var str = new Stringer();

            var one = new InPoint(new IntColor(1));
            var one2 = new OutPoint(new IntColor(1));
            var two = new InPoint(new IntColor(2));
            var three = new OutPoint(new IntColor(3));
            var four = new OutPoint(new IntColor(5));
            var mix1 = new MixNode();

            one.ConnectTo(summ);
            two.ConnectTo(summ);

            one2.ConnectTo(summ).ConnectTo(summ2);
            three.ConnectTo(summ2).ConnectTo(str);

            four.ConnectTo(summ2);
            summ.ConnectTo(summ3);
            summ2.ConnectTo(summ3);
            summ3.ConnectTo(str);
            new OutPoint(new IntColor(6)).ConnectTo(str);
            //str.ConnectTo(mix1);
            // new OutPoint(new StringColor("abba")).ConnectTo(mix1);/**/
            // graph.Add(new ColorableClass[] { one, two, one2, three });
            graph.Add(new ConnectionPoint[] { one, two, one2, three, four });
            graph.OnFinish += OnFinish;
            graph.StartAsync();
        }

        private static void OnFinish(GraphResult result)
        {
            foreach (KeyValuePair<ConnectionPoint, GraphPath>  pair in result)
            {
                var inPoint = pair.Key;
                var path = pair.Value;
                Write(inPoint.GetCurrentColor());
                var first = true;

                foreach (var color in path.ColorPath)
                {
                    Write(first ? ": " : ", ");
                    first = false;

                    var first2 = true;
                    foreach (var c in color.DeMix())
                    {

                        Write(first2 ? "<" : ", ");
                        first2 = false;
                        Write(c);
                    }
                    Write(">");
                }
                Write("\n");
            }
            if (counter == 0)
            {
                counter++;
                //graph.ClearGraph();
                //graph.Start();
            }
        }
    }
}
