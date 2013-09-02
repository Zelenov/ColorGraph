using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ColorGraph
{
    public interface IBlack
    {
        
    }

    

    public class Color : IComparable<Color>,ICloneable
    {
        internal class Anchor : Color
        {
            public Anchor()
            {
                _prevColor = this;
                _nextColor = this;
            }
        }
        private Color _nextColor;
        
        private Color _prevColor;
        public Color()
        {
            if (!(this is Anchor))
            {
                _nextColor = new Anchor();
                _prevColor = _nextColor;
                _nextColor._prevColor = this;
                _prevColor._nextColor = this;
            }
        }

        public bool Mixed { get { return _prevColor is Anchor && _nextColor is Anchor; } }
        /*public void Sort()
        {
          
            Color first = GetFirstColor();
            while (first._nextColor != null)
            {
                Color min = first;
                Color current = first._nextColor;
                while (current != null)
                {
                    if (current.CompareTo(min) < 0)
                    {
                        min = current;
                    }
                }
                if (min == first)
                    first = first._nextColor;
                else
                {
                    min.Delete();
                    if (first._prevColor != null)
                        first._prevColor.Mix(min);
                    else
                    {
                        min._nextColor = first;
                        first._prevColor = min;
                    }
                }
            }
        }
      /*  private Color GetLessOrEuqalColor(Color color)
        {
            Color res = this;
            int cmpResult = res.CompareTo(color);
            while (res._nextColor != null)
                res = res._nextColor;
            return res;
        }*/


        private Color GetFirstColor()
        {
            var anchor = _prevColor;
            while (!(anchor is Anchor))
                anchor = anchor._prevColor;
            return anchor._nextColor;
        }
        private void AddAfter(Color color)
        {
            var cpy = color.Clone() as Color;
            if (cpy != null)
            {
                cpy._nextColor = _nextColor;
                cpy._prevColor = this;
                _nextColor._prevColor = cpy;
                _nextColor = cpy;
            }

        }
        public Color Mix(Color addColor)
        {
            Color i = GetFirstColor();
            Color j = addColor.GetFirstColor();
            while (!(i is Anchor) && !(j is Anchor))
            {
                int cmpResult = Math.Sign(i.CompareTo(j));
                switch (cmpResult)
                {
                    case -1: //father is less than son
                        {
                            //add son
                            i = i._nextColor;
                            break;
                        }
                    case 1: //father is greater than son
                        {
                            //add father
                            
                            i._prevColor.AddAfter(j);
                            j = j._nextColor;
                            break;
                        }
                    case 0: //equal
                        {
                            //add anybody
                            
                            j = j._nextColor;
                            i = i._nextColor;
                            break;
                        }
                }
            }
            if (i is Anchor) // father's line is over
                i._prevColor.AddAfter(j);
            return GetFirstColor(); 
        }

        public List<Color> DeMix()
        {
            List<Color> res = new List<Color>();
            Color current = GetFirstColor();
            while (!(current is Anchor))
            {
                res.Add(current);
                current = current._nextColor;
            }
            return res;
        }

        public string ToStringDemuxed()
        {
            var first2 = true;
            var builder = new StringBuilder();
            var c = GetFirstColor();
            while (!(c is Anchor))
            {
                if (!first2)
                    builder.Append(", ");
                first2 = false;
                builder.Append(c);
                c = c._nextColor;
            }
            return builder.ToString();
        }
        public virtual int CompareTo(Color other)
        {
            if (other == null)
                return 1;
            return String.Compare(GetType().Name, other.GetType().Name, StringComparison.Ordinal);
        }


        public bool Contains(Color color)
        {
            if (color == null)
                return false;
              var i = GetFirstColor();
              var j = color.GetFirstColor();

              while (!(i is Anchor) && !(j is Anchor))
              {
                  int cmpResult = Math.Sign(i.CompareTo(j));
                  switch (cmpResult)
                  {
                      case -1: //father is less than son
                          {
                              //add son
                              i = i._nextColor;
                              break;
                          }
                      case 1: //father is greater than son
                          {
                              return false;
                          }
                      case 0: //equal
                          {
                              //add anybody
                              j = j._nextColor;
                              i = i._nextColor;
                              break;
                          }
                  }
              }
              if (j is Anchor) // son's line is over
                  return true;
              //if (i is Anchor) // father's line is over
                  return false;
          }
        
        public static bool NullOrBlack(Color result)
        {
            return result == null || result is Black;
        }

        public static Color Mix(IEnumerable<Color> colors)
        {
           
            if (colors == null)
                return null;
            var realColors = colors.Where(color => color != null).ToList();
            if (!realColors.Any())
            {
                return null;
            }
            var enumerator = realColors.GetEnumerator();
            enumerator.MoveNext();
            if (enumerator.Current == null)
                return null;
            Color firstColor = (enumerator.Current.CloneContent());
            while (enumerator.MoveNext())
            {
                firstColor=firstColor.Mix(enumerator.Current);
            }
            return firstColor;
        }

        protected virtual Color CloneContent()
        {
            return new Color();
        }

        public virtual object Clone()
        {
            var i = GetFirstColor();

            Color first = new Anchor(); //first anchor
            var currrent = first;
            Color last = first; //last anchor
            while (!(i is Anchor))
            {
                var icpy = i.CloneContent();
                icpy._prevColor = currrent;
                currrent._nextColor = icpy;
                currrent = icpy;
                i = i._nextColor;
            }
            last._prevColor = currrent;
            currrent._nextColor = last;
            return first._nextColor;
        }
    }
    
}
