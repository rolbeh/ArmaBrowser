﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ArmaBrowser.Shader
{
    static class Global
    {
        public static Uri MakePackUri(string relativeFile)
        {
            string uriString = "pack://application:,,,/" + AssemblyShortName + ";component/Shader/" + relativeFile;
            return new Uri(uriString);
        }

        private static string AssemblyShortName
        {
            get
            {
                if (_assemblyShortName == null)
                {
                    Assembly a = typeof(Global).Assembly;

                    // Pull out the short name.
                    _assemblyShortName = a.ToString().Split(',')[0];
                }

                return _assemblyShortName;
            }
        }

        private static string _assemblyShortName;
    }
}