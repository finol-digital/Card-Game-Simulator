using System.Collections.Generic;
using System.Linq;

namespace CardGameDef
{
    public class CardSearchFilters
    {
		public string Id { get; set; } = "";

		public string Name { get; set; } = "";

		public string SetCode { get; set; } = "";

		public Dictionary<string, string> StringProperties { get; } = new Dictionary<string, string>();

		public Dictionary<string, int> IntMinProperties { get; } = new Dictionary<string, int>();

		public Dictionary<string, int> IntMaxProperties { get; } = new Dictionary<string, int>();

		public Dictionary<string, int> EnumProperties { get; } = new Dictionary<string, int>();

		public override string ToString()
		{
            string filters = string.Empty;
            if (!string.IsNullOrEmpty(Id))
                filters += "id:" + Id + "; ";
            if (!string.IsNullOrEmpty(SetCode))
                filters += "set:" + SetCode + "; ";
            foreach (PropertyDef property in CardGameManager.Current.CardProperties)
            {
                switch (property.Type)
                {
                    case PropertyType.ObjectEnum:
                    case PropertyType.ObjectEnumList:
                    case PropertyType.StringEnum:
                    case PropertyType.StringEnumList:
                        if (!EnumProperties.ContainsKey(property.Name))
                            break;
                        EnumDef enumDef = CardGameManager.Current.Enums.FirstOrDefault(def => def.Property.Equals(property.Name));
                        if (enumDef != null)
                            filters += property.Name + ":=" + EnumProperties[property.Name] + "; ";
                        break;
                    case PropertyType.Integer:
                        if (IntMinProperties.ContainsKey(property.Name))
                            filters += property.Name + ">=" + IntMinProperties[property.Name] + "; ";
                        if (IntMaxProperties.ContainsKey(property.Name))
                            filters += property.Name + "<=" + IntMaxProperties[property.Name] + "; ";
                        break;
                    case PropertyType.Object:
                    case PropertyType.ObjectList:
                    case PropertyType.Number:
                    case PropertyType.Boolean:
                    case PropertyType.StringList:
                    case PropertyType.EscapedString:
                    case PropertyType.String:
                    default:
                        if (StringProperties.ContainsKey(property.Name))
                            filters += property.Name + ":" + StringProperties[property.Name] + "; ";
                        break;
                }
            }
            return filters;
		}
    }
}