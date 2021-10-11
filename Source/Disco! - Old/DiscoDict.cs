using System.Collections.Generic;
using System.Xml;

namespace Disco
{
    public class DiscoDict : Dictionary<string, string>
    {
        public void LoadDataFromXmlCustom(XmlNode node)
        {
            foreach (var child in node.ChildNodes)
            {
                var item = child as XmlNode;
                string key = item.Name.Trim();
                if (!this.ContainsKey(key))
                {
                    this.Add(key, item.InnerText.Trim());
                }
                else
                {
                    this[key] = item.InnerText.Trim();
                    Core.Error($"Duplicate input key '{key}'.");
                }
            }
        }
    }
}
