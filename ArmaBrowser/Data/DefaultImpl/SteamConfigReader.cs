/*
 * https://developer.valvesoftware.com/wiki/Steam_Web_API
 * 
 * This is a quick and dirty implementation
 */

using System;
using System.IO;
using System.Text;
using System.Xml;
using JetBrains.Annotations;

namespace ArmaBrowser.Data.DefaultImpl
{
    internal class SteamConfigReader : IDisposable
    {
        public enum ElementType
        {
            Array,
            Attribute
        }

        private readonly Stream _file;
        private bool _disposed;

        public SteamConfigReader(string filepath)
        {
            _file = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }

        #region IDisposable

        public void Dispose()
        {
            if (_disposed)
            {
                _disposed = true;
                _file.Dispose();
            }
        }

        #endregion IDisposable

        ~SteamConfigReader()
        {
            Dispose();
        }

        public string GetValueOf(string firstPart)
        {
            if (firstPart == null) throw new ArgumentNullException(nameof(firstPart));

            _file.Position = 0;

            using (var reader = new StreamReader(_file, Encoding.ASCII, false, 1024*1024, true))
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();

                    if (line != null && line.StartsWith(firstPart))
                    {
                        line = line.TrimStart();
                        return
                            line.Split(new[] {'\t'}, StringSplitOptions.RemoveEmptyEntries)[1].Replace("\"",
                                string.Empty).Replace(@"\\", @"\");
                    }
                }
                return string.Empty;
            }
        }

        [NotNull]
        public XmlDocument ToXml()
        {
            var doc = new XmlDocument();
            var xmlDeclaration = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
            var root = doc.DocumentElement;
            doc.InsertBefore(xmlDeclaration, root);
            using (var reader = new StreamReader(_file, Encoding.ASCII, false, 1024*1024, true))
            {
                root = ReadNextElements(reader, doc);
                doc.AppendChild(root);
            }


            return doc;
        }

        private XmlElement ReadNextElements(TextReader reader, XmlNode parent)
        {
            var doc = parent.OwnerDocument ?? (XmlDocument) parent;
            var charbuffer = new char[1];
            if (reader.Read(charbuffer, 0, 1) == 0) return null;
            while (charbuffer[0] == 9 /*Tabulator*/
                   || charbuffer[0] == ' '
                   || char.IsControl(charbuffer[0])
                )
            {
                if (reader.Read(charbuffer, 0, 1) == 0) break;
            }
            var token = charbuffer[0];
            if (token == '"')
            {
                var s = ReadUntilNextQuota(reader);
                var element = doc.CreateElement("Node");
                if (element.OwnerDocument != null)
                {
                    var att = element.OwnerDocument.CreateAttribute("Name");
                    att.Value = s;
                    element.Attributes.Append(att);

                    while (ReadElementContent(reader, element))
                    {
                    }
                }
                return element;
            }

            throw new Exception("unexpect char '" + token + "'");
        }

        private bool ReadElementContent([NotNull] TextReader reader, [NotNull] XmlElement element)
        {
            var charbuffer = new char[1];
            do
            {
                if (reader.Read(charbuffer, 0, 1) == 0) return false;
            } while (charbuffer[0] == ' ' || char.IsControl(charbuffer[0]));

            var token = charbuffer[0];
            if (token == '"')
            {
                var s = ReadUntilNextQuota(reader);
                if (element.OwnerDocument != null)
                    element.AppendChild(element.OwnerDocument.CreateTextNode(s));
                return true;
            }

            if (token == '{')
            {
                while (ReadNextElement(reader, element))
                {
                }
                return true;
            }
            if (token == '}')
            {
                return false;
            }

            throw new Exception("unexpect char '" + token + "'");
        }

        private bool ReadNextElement([NotNull] TextReader reader,[NotNull] XmlElement element)
        {
            var token = MoveToNextStop(reader);
            if (token == '}') return false;
            if (token == 0) return false;
            if (token != '"') throw new Exception("unexpect char '" + token + "'");

            var s = ReadUntilNextQuota(reader);
            if (element.OwnerDocument == null) throw new InvalidOperationException("OwnerDocument is null!");

            var child = element.OwnerDocument.CreateElement("Node");
            var att = element.OwnerDocument.CreateAttribute("Name");
            att.Value = s;
            child.Attributes.Append(att);
            element.AppendChild(child);
            return ReadElementContent(reader, child);
        }

        private char MoveToNextStop(TextReader reader)
        {
            var charbuffer = new char[1];
            do
            {
                if (reader.Read(charbuffer, 0, 1) == 0) return (char) 0;
            } while (!(charbuffer[0] == '"' || charbuffer[0] == '}'));
            return charbuffer[0];
        }

        private string ReadUntilNextQuota(TextReader reader)
        {
            var sb = new StringBuilder(255);
            var charbuffer = new char[1];
            if (reader.Read(charbuffer, 0, 1) == 0) return null;
            while (charbuffer[0] != '"')
            {
                sb.Append(charbuffer[0]);
                if (reader.Read(charbuffer, 0, 1) == 0) break;
            }
            return sb.ToString();
        }
    }
}