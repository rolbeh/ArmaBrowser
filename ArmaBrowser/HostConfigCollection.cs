using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using ArmaBrowser.Properties;

namespace ArmaBrowser
{
    [Serializable]
    public class HostConfig
    {
        public string EndPoint { get; set; }

        public string PossibleAddons { get; set; }

        internal string Key => string.Format("{0}", EndPoint);
    }

    [Serializable]
    public class HostConfigCollection : IEnumerable<HostConfig>
    {
        private static HostConfigCollection _default;

        private readonly ArrayList _employees = new ArrayList();

        public static HostConfigCollection Default
        {
            get
            {
                if (_default != null) return _default;

                if (string.IsNullOrWhiteSpace(Settings.Default.HostConfigs))
                    _default = new HostConfigCollection();
                else
                    try
                    {
                        var serializer = new XmlSerializer(typeof(HostConfigCollection));
                        using (TextReader textreader = new StringReader(Settings.Default.HostConfigs))
                        {
                            _default = (HostConfigCollection) serializer.Deserialize(textreader);
                        }
                    }
                    catch
                    {
                        _default = new HostConfigCollection();
                    }

                return _default;
            }
        }

        public HostConfig this[int index]
        {
            get => (HostConfig) _employees[index];
            set => _employees[index] = value;
        }

        public IEnumerator<HostConfig> GetEnumerator()
        {
            return _employees.Cast<HostConfig>().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _employees.GetEnumerator();
        }

        public void Add(HostConfig element)
        {
            _employees.Add(element);
        }

        public void Clear()
        {
            _employees.Clear();
        }

        public int IndexOf(HostConfig element)
        {
            return _employees.IndexOf(element);
        }

        public void Remove(HostConfig element)
        {
            _employees.Remove(element);
        }

        public void RemoveAt(int index)
        {
            _employees.RemoveAt(index);
        }
    }
}