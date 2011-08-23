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
using System.Diagnostics;

namespace DeepZoomView
{
    public class Stacking
    {
        public Dictionary<int, List<int>> groups = new Dictionary<int, List<int>>();
        public Dictionary<int, int> invertedGroups = new Dictionary<int, int>();

        /// <summary>
        /// Creates Stacks of similar images. Sets instance vars "groups" and "invertedGroups"
        /// </summary>
        /// <param name="o">Input</param>
        /// <returns>Group ID (<0) -> ImageIds</returns>
        public Dictionary<int, List<int>> MakeStacks(Dictionary<int, DateTime> o)
        {
            // sortes everyimage by date taken
            List<KeyValuePair<int, DateTime>> sortedTimes = o.OrderBy(kv => kv.Value).ToList();

            int currentKey = 0;

            KeyValuePair<int, DateTime> last = new KeyValuePair<int,DateTime>(-1,new DateTime(0));
            int delta = 4; // Seconds
            Boolean isInGroup = false;
            // for each image, compares with previous
            foreach (KeyValuePair<int, DateTime> pair in sortedTimes)
            {
                if (pair.Value.Year != 1 && last.Value.AddSeconds(delta).CompareTo(pair.Value) >= 0)
                {
                    if (!isInGroup)
                    {
                        isInGroup = true;
                        currentKey--;
                        groups.Add(currentKey, new List<int>());
                        groups[currentKey].Add(last.Key);
                        invertedGroups[last.Key] = currentKey;
                    } // if previous was added to a group, this belongs to that group
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
