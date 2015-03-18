using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ItsATrap
{
    public class MobHandler
    {
        internal static MobHandler Instance { get; private set; }

        private ListBox listBox;
        public SortedDictionary<string, int> trappableMobs;
        private string settingsFilePath;

        public MobHandler(ListBox listBox)
        {
            this.listBox = listBox;
            SettingsFilePath = "ItsATrap.txt";
            trappableMobs = new SortedDictionary<string, int>();
            readFile();
        }

        public string SettingsFilePath { get; set; }
		
        public List<int> getList()
        {
            List<int> list = new List<int>();

            foreach (KeyValuePair<string, int> pair in trappableMobs)
            {
                list.Add(pair.Value);
            }

            return list;
        }

        public void listAllItems()
        {
            foreach (KeyValuePair<string, int> pair in trappableMobs)
            {
                Console.WriteLine("{0} = {1}",
                pair.Value,
                pair.Key);
            }
        }

        public void addItem(string name, int id)
        {
            bool isAllReadyThere = trappableMobs.ContainsKey(name);
            trappableMobs[name] = id;

            if (!isAllReadyThere)
            {
                listBox.Items.Add(name);
            }

            saveFile();
        }

        public void deleteItem()
        {
            string key = listBox.SelectedItem.ToString();
            trappableMobs.Remove(key);
            listBox.Items.RemoveAt(listBox.SelectedIndex);
        }

        public int getMobId(string key)
        {
            return trappableMobs[key];
        }

        public void readFile()
        {
            trappableMobs.Clear();
            listBox.Items.Clear();

            try
            {
                string[] file = System.IO.File.ReadAllLines(SettingsFilePath);
                foreach (string line in file)
                {
                    string[] split = line.Split('|');
                    int mobId = 0;
                    string name = split[1];

                    try
                    {
                        mobId = int.Parse(split[0]);
                    }
                    catch (Exception)
                    {
                        //skip ahead
                        continue;
                    }

                    addItem(name, mobId);
                }
            }
            catch(Exception)
            {
                //ignore
            }
        }

        public void saveFile()
        {
            System.IO.StreamWriter file = new System.IO.StreamWriter(SettingsFilePath);
            foreach (KeyValuePair<string, int> pair in trappableMobs)
            {
                file.WriteLine("{0}|{1}",
                pair.Value,
                pair.Key);
            }
            file.Close();
        }
    }
}
