using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Collections.Generic;
using System.Linq;

namespace DeepZoomView
{
    public class Stacking
    {
        public Dictionary<int, List<int>> groups = new Dictionary<int, List<int>>();
        public Dictionary<int, int> invertedGroups = new Dictionary<int, int>();

        
        public Dictionary<int, List<int>> MakeStacks(OrganizableByDate o)
        {
            List<KeyValuePair<int, DateTime>> sortedTimes = o.invertedData.OrderBy(kv => kv.Value).ToList();

            int currentKey = 0;

            KeyValuePair<int, DateTime> last = new KeyValuePair<int,DateTime>(-1,new DateTime(0));
            int delta = 2; // Seconds
            Boolean isInGroup = false;
            foreach (KeyValuePair<int, DateTime> pair in sortedTimes)
            {
                if (last.Value.AddSeconds(delta).CompareTo(pair.Value) >= 0)
                {
                    if (!isInGroup)
                    {
                        isInGroup = true;
                        currentKey--;
                        groups.Add(currentKey, new List<int>());
                        groups[currentKey].Add(last.Key);
                        invertedGroups[last.Key] = currentKey;
                    }
                    groups[currentKey].Add(pair.Key);
                    invertedGroups[pair.Key] = currentKey;
                }
                else
                {
                    isInGroup = false;
                }
                last = pair;
            }
            return groups;
        }
    }
}
