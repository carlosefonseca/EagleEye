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
using DeepZoomView.EECanvas;

namespace DeepZoomView
{
    public class Organizable
    {
        public readonly String Name;
        public Boolean isNumber = false;
        public Dictionary<string, List<int>> data;
        public Dictionary<int, string> invertedData;
        public Boolean AvailableForGroupping = true;
        public List<Group> ListOfGroups = null;
        public Dictionary<Object, List<int>> dataWithStacks;
        public Dictionary<int, List<int>> stacks;
        public Dictionary<int, List<CanvasItem>> stacksForCanvas;
        public Boolean hasStacks = false;

        public virtual List<int> Ids { get { return invertedData.Keys.ToList(); } }

        /// <summary>
        /// Total of Images
        /// </summary>
        public virtual int ItemCount
        {
            get
            {
                return invertedData.Count;
            }
        }

        /// <summary>
        /// Total of Groups
        /// </summary>
        public virtual int GroupCount
        {
            get
            {
                return data.Count;
            }
        }

        public Organizable(String name)
        {
            this.Name = name;
            data = new Dictionary<string, List<int>>();
            invertedData = new Dictionary<int, string>();
            dataWithStacks = new Dictionary<Object, List<int>>();
            stacksForCanvas = new Dictionary<int, List<CanvasItem>>();
        }

        public Boolean Import(String s) { return false; }


        public List<Group> GroupList()
        {
            if (ListOfGroups == null)
            {
                ListOfGroups = new List<Group>();
                List<KeyValuePair<string, List<int>>> gs = GetGroups();
                foreach (KeyValuePair<string, List<int>> kv in gs)
                {
                    Group g = new Group(kv.Key, kv.Value);
                    ListOfGroups.Add(g);
                }
            }
            return ListOfGroups;
        }

        public virtual List<KeyValuePair<string, List<int>>> GetGroups()
        {
            if (isNumber)
            {
                return (List<KeyValuePair<string, List<int>>>)data.OrderBy<KeyValuePair<string, List<int>>, int>(x => Convert.ToInt32(x.Key)).ToList();
            }
            else
            {
                return (List<KeyValuePair<string, List<int>>>)data.OrderBy<KeyValuePair<string, List<int>>, string>(x => x.Key).ToList();
            }
        }


        public virtual List<KeyValuePair<String, List<int>>> GetGroups(List<int> subset)
        {
            List<KeyValuePair<string, int>> filter = (List<KeyValuePair<string, int>>)subset.Join<int, KeyValuePair<int, string>, int, KeyValuePair<string, int>>(
                invertedData,
                id => id,
                data => data.Key,
                (id, data) => new KeyValuePair<string, int>(data.Value, id)
                ).ToList();

            ILookup<string, int> lookup = filter.ToLookup(kv => kv.Key, kv => kv.Value);

            List<KeyValuePair<string, List<int>>> outList = new List<KeyValuePair<string, List<int>>>();
            foreach (IGrouping<string, int> group in lookup)
            {
                outList.Add(new KeyValuePair<string, List<int>>(group.Key, group.ToList()));
            }
            return outList;
        }

        public virtual void Add(int k, string p)
        {
            if (!data.ContainsKey(p))
            {
                data.Add(p, new List<int>());
            }
            data[p].Add(k);

            invertedData.Add(k, p);
        }


        /// <summary>
        /// Given an image id, returns its value for this organizable
        /// </summary>
        /// <param name="k">The MSI-Id for the image</param>
        /// <returns></returns>
        public virtual string Id(int k)
        {
            if (invertedData.ContainsKey(k))
            {
                return invertedData[k];
            }
            else
            {
                return null;
            }
        }

        public virtual Boolean ContainsId(int k)
        {
            return invertedData.ContainsKey(k);
        }

        public override string ToString()
        {
            return this.Name;
        }
    }
}
