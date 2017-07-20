using NCalc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace FormsByAir.SDK.Model
{
    public static class Extensions
    {
        public static string GetMappedValue(this Schema document, string valueName, XDocument map)
        {
            string value = null;
            var mapElements = map.Root.Descendants().Where(a => (a.Value == valueName && !a.HasElements) || (a.Attribute("value") != null && a.Attribute("value").Value == valueName)).ToList();

            if (mapElements != null && mapElements.Any())
            {
                foreach (var mapElement in mapElements)
                {
                    var elements = document.Form.Flatten().Where(a => a.Name == mapElement.Name.LocalName);
                    if (elements.Any())
                    {
                        if (value == null)
                        {
                            value = elements.First().DocumentValue;
                        }
                        else
                        {
                            value += " " + elements.First().DocumentValue;
                        }
                    }
                }
                return value;
            }
            return null;
        }

        public static List<string> GetMappedValues(this Schema document, string valueName, XDocument map)
        {
            int records = 0;
            var mapElements = map.Root.Descendants().Where(a => (a.Value == valueName && !a.HasElements) || (a.Attribute("value") != null && a.Attribute("value").Value == valueName)).ToList();

            if (mapElements != null && mapElements.Any())
            {
                var mapElement = mapElements.First();
                records = document.Form.Flatten().Where(a => a.Name == mapElement.Name.LocalName).Count();
            }
            else
            {
                return new List<string>();
            }

            string[] values = new string[records];

            foreach (var mapElement in mapElements)
            {
                var elements = document.Form.Flatten().Where(a => a.Name == mapElement.Name.LocalName);
                for (int i = 0; i < records; i++)
                {
                    if (values[i] == null)
                    {
                        values[i] = elements.ElementAt(i).DocumentValue;
                    }
                    else
                    {
                        values[i] += " " + elements.ElementAt(i).DocumentValue;
                    }
                }
            }
            return values.ToList();
        }

        public static string Value(this Entity entity, string name)
        {
            var attribute = entity.Attributes.FirstOrDefault(a => a.Name == name);
            if (attribute != null)
            {
                return attribute.Value;
            }
            return null;
        }

        public static Attribute GetAttribute(this Entity entity, string name)
        {
            return entity.Attributes.FirstOrDefault(a => a.Name == name);
        }

        public static Entity GetEntity(this Entity entity, string name)
        {
            return entity.Entities.FirstOrDefault(a => a.Name == name);
        }

        public static Entity GetEntity(this List<Entity> entities, string name)
        {
            return entities.FirstOrDefault(a => a.Name == name);
        }

        public static List<Entity> GetEntities(this Entity entity, string name)
        {
            return entity.Entities.Where(a => a.Name == name).ToList();
        }

        public static List<Element> Flatten(this Element root)
        {
            var line = new List<Element>();
            root.FlattenInternal(line);
            return line;
        }

        private static void FlattenInternal(this Element root, List<Element> line)
        {
            foreach (var element in root.DocumentElements)
            {
                line.Add(element);
            }
            foreach (var element in root.DocumentElements.Where(a => a.DocumentElements != null).ToList())
            {
                if (element.ElementType != ElementType.Condition || string.Equals(root.DocumentValue, element.Visibility, StringComparison.OrdinalIgnoreCase))
                {
                    FlattenInternal(element, line);
                }
            }
        }

        public static List<Entity> Flatten(this Entity root)
        {
            var line = new List<Entity>();
            root.FlattenInternal(line);
            return line;
        }

        private static void FlattenInternal(this Entity root, List<Entity> line)
        {
            foreach (var entity in root.Entities)
            {
                line.Add(entity);
            }
            foreach (var entity in root.Entities.Where(a => a.Entities != null).ToList())
            {
                FlattenInternal(entity, line);
            }
        }

        public static List<Element> GetElementsByTagRecursive(this Element root, string tagName)
        {
            var result = root.Flatten().Where(a => a.AutofillKey != null && a.AutofillKey.Equals(tagName, System.StringComparison.OrdinalIgnoreCase)).ToList();
            if (!result.Any() & root.Parent != null)
            {
                return root.Parent.GetElementsByTagRecursive(tagName);
            }
            return result;
        }

        public static void SetParent(this Element root, Element parent = null)
        {
            if (root.DocumentElements != null && parent != null)
            {
                root.Parent = parent;
            }
            if (root.DocumentElements != null)
            {
                foreach (var child in root.DocumentElements)
                {
                    child.SetParent(root);
                }
            }
        }

        public static string Left(this string text, int length)
        {
            return text == null ? null : text.Substring(0, Math.Min(length, text.Length));
        }

        public static T EvaluateExpression<T>(this Schema document, string expression, bool format = false)
        {
            var e = new Expression(document.Form.ParseTags(expression, format));
            var result = e.Evaluate();
            return (T)Convert.ChangeType(result, typeof(T));
        }

        public static dynamic ToObject(this Schema document)
        {
            var documentObject = new ExpandoObject();
            foreach (var element in document.Form.DocumentElements)
            {
                BuildObject(element, documentObject);
            }
            return documentObject;
        }

        private static void BuildObject(Element element, IDictionary<string, object> item)
        {
            if (element.ElementType == ElementType.Question)
            {
                item.Add(element.AutofillKey ?? element.Name, element.DocumentValue);
            }
            else if (element.Elements != null && element.Elements.FirstOrDefault() != null && element.Elements.FirstOrDefault().AllowMany)
            {
                var list = new List<ExpandoObject>();
                if (element.DocumentElements != null)
                {
                    foreach (var childElement in element.DocumentElements)
                    {
                        var child = new ExpandoObject();
                        BuildObject(childElement, child);
                        list.Add(child);
                    }
                }
                item.Add(element.AutofillKey ?? element.Name, list);
            }
            else
            {
                var group = new ExpandoObject();
                if (element.DocumentElements != null)
                {
                    foreach (var childElement in element.DocumentElements)
                    {
                        BuildObject(childElement, group);
                    }
                }
                item.Add(element.AutofillKey ?? element.Name, group);
            }
        }

        public static void ParseTags(this Element root, Entity entity, bool format = false)
        {
            if (entity.Attributes != null)
            {
                foreach (var attribute in entity.Attributes)
                {
                    if (!string.IsNullOrEmpty(attribute.ForEach))
                    {
                        var repeaters = root.GetElementsByTagRecursive(attribute.ForEach);
                        var found = false;
                        var index = 1;  //zero-based index would be confusing for users
                        foreach (var repeater in repeaters)
                        {
                            var filter = attribute.Filter.Replace("<<[Index]>>", index.ToString());
                            var e = new Expression(repeater.ParseTags(filter, format));
                            var result = (bool)e.Evaluate();
                            if (result)
                            {
                                attribute.Value = repeater.ParseTags(attribute.Value, format);
                                found = true;
                                break;
                            }
                            index++;
                        }
                        if (!found)
                        {
                            attribute.Value = null;
                        }
                    }
                    else
                    {
                        attribute.Value = root.ParseTags(attribute.Value, format);
                        if (!string.IsNullOrEmpty(attribute.Filter))
                        {
                            var e = new Expression(root.ParseTags(attribute.Filter, format));
                            var result = (bool)e.Evaluate();
                            if (!result)
                            {
                                attribute.Value = null;
                            }
                        }

                    }
                }
                entity.Attributes.RemoveAll(a => a.Value == null);
            }

            if (entity.Entities != null)
            {
                var count = entity.Entities.Count;
                for (var i = 0; i < count; i++)
                {
                    var child = entity.Entities[i];
                    if (!string.IsNullOrEmpty(child.ForEach))
                    {
                        var newChildSource = JsonConvert.SerializeObject(child);
                        var repeaters = root.GetElementsByTagRecursive(child.ForEach);
                        foreach (var repeater in repeaters)
                        {
                            if (!string.IsNullOrEmpty(child.Filter))
                            {
                                var e = new Expression(repeater.ParseTags(child.Filter, format));
                                var result = (bool)e.Evaluate();
                                if (!result)
                                {
                                    continue;
                                }
                            }
                            var newChild = JsonConvert.DeserializeObject<Entity>(newChildSource);
                            newChild.ForEach = null;
                            newChild.Filter = null;
                            repeater.ParseTags(newChild, format);
                            entity.Entities.Add(newChild);
                        }
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(child.Filter))
                        {
                            var e = new Expression(root.ParseTags(child.Filter, format));
                            var result = (bool)e.Evaluate();
                            if (result)
                            {
                                child.Filter = null;
                            }
                            else
                            {
                                continue;
                            }
                        }
                        root.ParseTags(child, format);
                    }
                }
                entity.Entities.RemoveAll(a => !string.IsNullOrEmpty(a.Filter));
                entity.Entities.RemoveAll(a => !string.IsNullOrEmpty(a.ForEach));
            }
        }

        public static string ParseTags(this Element root, string note, bool format = true, bool preflattened = false, string separator = " ", bool escapeQuotes = false)
        {
            if (string.IsNullOrEmpty(note))
            {
                return string.Empty;
            }
            var elements = preflattened ? root.DocumentElements : root.Flatten();
            var taglist = Regex.Matches(note, @"(\<\<\[(.*?)\]\>\>|\<\<(.*?)\>\>)", RegexOptions.IgnoreCase);

            foreach (var tag in taglist)
            {
                var documentElementValue = "";
                var tagName = tag.ToString().TrimStart('<', '<').TrimEnd('>', '>');

                if (tagName.StartsWith("[ForEach:"))
                {                    
                    tagName = tagName.TrimStart('[', 'F', 'o', 'r', 'E', 'a', 'c', 'h', ':').TrimEnd(']');
                    var repeaterName = tagName.Substring(0, tagName.IndexOf(":"));
                    var repeaterTag = tagName.Substring(tagName.IndexOf(":") + 1);
                    
                    if (repeaterName.IndexOf("[") != -1)
                    {
                        separator = repeaterName.Substring(repeaterName.IndexOf("[") + 1).TrimEnd(']');
                        repeaterName = repeaterName.Substring(0, repeaterName.IndexOf("["));                        
                    }

                    var repeaters = root.GetElementsByTagRecursive(repeaterName);

                    foreach (var repeater in repeaters)
                    {
                        var line = repeater.ParseTags(repeaterTag, format, preflattened, separator);
                        documentElementValue += line + separator;
                    }
                }
                else if (tagName.StartsWith("[Expression:"))
                {
                    var expression = tagName.TrimStart('[', 'E', 'x', 'p', 'r', 'e', 's', 's', 'i', 'o', 'n', ':').TrimEnd(']');
                    var e = new Expression(root.ParseTags(expression, format, preflattened, separator));
                    documentElementValue = (string)e.Evaluate();
                }
                else if (tagName.StartsWith("["))
                {
                    //skip other system tags
                    continue;
                }
                else
                {
                    var matches = elements.Where(a => a.AutofillKey != null && a.AutofillKey.Equals(tagName, StringComparison.OrdinalIgnoreCase)).ToList();

                    if (matches.Any())
                    {
                        foreach (var element in matches)
                        {
                            var value = element.DocumentValue ?? "";

                            if (element.Type == "boolean")
                            {
                                value = value == "true" ? "Yes" : "No";
                            }
                            else if (element.Type == "option")
                            {
                                value = value == "true" ? element.Prompt : "";
                            }
                            else if (element.Type == "nameValueList" && !string.IsNullOrEmpty(value) && format)
                            {
                                value = element.SimpleType.Values.Single(a => a.Value == value).Name;
                            }
                            else if (element.Type == "date" && !string.IsNullOrEmpty(value))
                            {
                                value = DateTime.Parse(value).ToString(element.Format == "year" ? "yyyy" : element.Format == "month" ? "MMM-yyyy" : "dd-MMM-yyyy");
                            }
                            else if (element.Type == "currency" && !string.IsNullOrEmpty(value) && format)
                            {
                                value = Convert.ToDecimal(value).ToString("C" + element.Decimals);
                            }
                            else if (element.Type == "number" && !string.IsNullOrEmpty(value) && format)
                            {
                                value = Convert.ToDecimal(value).ToString("N0");
                            }
                            else if (element.Type == "percent" && !string.IsNullOrEmpty(value) && format)
                            {
                                value = Convert.ToDecimal(value).ToString("P");
                            }
                            else if ((element.Type == "formula" || element.Type == "slider") && !string.IsNullOrEmpty(value) && format)
                            {
                                float n;
                                if (element.Format == "number")
                                {
                                    value = Convert.ToDecimal(value).ToString("N0");
                                }
                                if (element.Format == "percent")
                                {
                                    value = Convert.ToDecimal(value).ToString("P");
                                }
                                else if (element.Format == "currency" || string.IsNullOrEmpty(element.Format) && float.TryParse(value, out n))
                                {
                                    value = Convert.ToDecimal(value).ToString("C" + element.Decimals);
                                }
                            }
                            documentElementValue += value + separator;
                        }
                    }
                    else if (root.Parent != null)
                    {
                        documentElementValue = root.Parent.ParseTags(string.Format("<<{0}>>", tagName), format);
                    }
                }
                if (documentElementValue.EndsWith(separator))
                {
                    documentElementValue = documentElementValue.Remove(documentElementValue.Length - separator.Length);
                }
                documentElementValue = documentElementValue.Trim();
                if (escapeQuotes)
                {
                    documentElementValue = documentElementValue.Replace("\"", "\"\"");
                }
                note = Regex.Replace(note, Regex.Escape(tag.ToString()), documentElementValue, RegexOptions.IgnoreCase);
            }
            return note;
        }

        public static void SetTags(this Schema document, string note, string value)
        {
            var elements = document.Form.Flatten().Where(a => a.AutofillKey != null && a.AutofillKey.Equals(note, System.StringComparison.OrdinalIgnoreCase)).ToList();
            foreach (var element in elements)
            {
                element.DocumentValue = value;
            }
        }

    }
}
