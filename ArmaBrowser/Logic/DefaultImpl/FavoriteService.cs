using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

namespace ArmaBrowser.Logic
{
    internal sealed class FavoriteService
    {
        private const string Filename = "favorites";
        private readonly AppPathService _appPathService;
        private readonly string _filename;

        public FavoriteService()
        {
            _appPathService = ServiceHub.Instance.GetService<AppPathService>();
            _filename = Path.Combine(_appPathService.UserSettingsPath, Filename);
        }

        public void Add(params IServerItem[] items)
        {
            HashSet<EndPoint> endPoints = GetEntries();
            items.Each(i => endPoints.Add(new IPEndPoint(i.Host, i.QueryPort)));
            SaveEntries(endPoints);
        }

        public void Remove(params IServerItem[] items)
        {
            HashSet<EndPoint> endPoints = GetEntries();
            items.Each(i => endPoints.Remove(new IPEndPoint(i.Host, i.QueryPort)));
            SaveEntries(endPoints);
        }

        public IServerItem[] Get()
        {
            HashSet<EndPoint> endPoints = GetEntries();
            return endPoints.OfType<IPEndPoint>()
                .Select(e => (IServerItem) new ServerItem {Host = e.Address, QueryPort = e.Port, IsFavorite = true})
                .ToArray();
        }

        #region private

        private HashSet<EndPoint> GetEntries()
        {
            var result = new HashSet<EndPoint>();
            lock (this)
            {
                var path = _appPathService.UserSettingsPath;
                Directory.CreateDirectory(_appPathService.UserSettingsPath);
                if (!File.Exists(_filename)) return new HashSet<EndPoint>();

                using (var streamReader = File.OpenText(Path.Combine(path, Filename)))
                {
                    while (!streamReader.EndOfStream)
                        try
                        {
                            var line = streamReader.ReadLine();
                            if (line != null)
                            {
                                var pos = line.LastIndexOf(":", StringComparison.Ordinal);
                                var ipEndPoint = new IPEndPoint(IPAddress.Parse(line.Substring(0, pos)),
                                    int.Parse(line.Substring(pos + 1)));
                                result.Add(ipEndPoint);
                            }
                        }
                        catch
                        {
                            // ignore
                        }
                }
            }

            return result;
        }

        private void SaveEntries(IEnumerable<EndPoint> entries)
        {
            lock (this)
            {
                string path = _appPathService.UserSettingsPath;
                Directory.CreateDirectory(_appPathService.UserSettingsPath);
                using (var streamWriter = File.CreateText(Path.Combine(path, Filename)))
                {
                    streamWriter.BaseStream.Position = 0;
                    foreach (var endPoint in entries) streamWriter.WriteLine(endPoint.ToString());
                    streamWriter.BaseStream.SetLength(streamWriter.BaseStream.Length);
                }
            }
        }

        #endregion
    }
}