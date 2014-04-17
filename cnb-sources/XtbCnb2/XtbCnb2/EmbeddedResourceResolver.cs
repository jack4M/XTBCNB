using System;
using System.Xml;
using System.Reflection;
using System.IO;

namespace XtbCnb2
{
    public class EmbeddedResourceResolver : XmlUrlResolver
    {
        public override object GetEntity(Uri absoluteUri,
          string role, Type ofObjectToReturn)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            return assembly.GetManifestResourceStream(this.GetType(),
            Path.GetFileName(absoluteUri.AbsolutePath));
        }
    }
}