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

        public static Element GetMappedElement(this Schema document, string valueName, XDocument map)
        {
            var mapElements = map.Root.Descendants().Where(a => (a.Value == valueName && !a.HasElements) || (a.Attribute("value") != null && a.Attribute("value").Value == valueName)).ToList();

            if (mapElements != null && mapElements.Any())
            {
                foreach (var mapElement in mapElements)
                {
                    var elements = document.Form.Flatten().Where(a => a.Name == mapElement.Name.LocalName);
                    if (elements.Any())
                    {
                        return elements.First();
                    }
                }
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

        public static Attribute GetAttribute(this Attribute attribute, string name)
        {
            return attribute.Attributes.FirstOrDefault(a => a.Name == name);
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

        public static List<Element> Flatten(this Element root, bool includeEmptyRepeaters = false, bool ignoreConditional = false)
        {
            var line = new List<Element>();
            root.FlattenInternal(line, includeEmptyRepeaters, ignoreConditional);
            return line;
        }

        private static void FlattenInternal(this Element root, List<Element> line, bool includeEmptyRepeaters = false, bool ignoreConditional = false)
        {
            foreach (var element in root.DocumentElements)
            {
                line.Add(element);
            }
            foreach (var element in root.DocumentElements.Where(a => a.DocumentElements != null).ToList())
            {
                if (element.ElementType != ElementType.Condition || ignoreConditional || string.Equals(root.DocumentValue, element.Visibility, StringComparison.OrdinalIgnoreCase))
                {
                    FlattenInternal(element, line, includeEmptyRepeaters, ignoreConditional);
                }
            }
            if (includeEmptyRepeaters && root.Elements != null && root.Elements.Any() && root.Elements[0].AllowMany && !root.DocumentElements.Any())
            {
                line.Add(root.Elements[0]);
                if (root.Elements[0].DocumentElements != null)
                {
                    FlattenInternal(root.Elements[0], line, includeEmptyRepeaters, ignoreConditional);
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
            if (!result.Any())
            {
                if (root.Flatten(true).Where(a => a.AutofillKey != null && a.AutofillKey.Equals(tagName, System.StringComparison.OrdinalIgnoreCase)).Any())
                {
                    return new List<Element>();
                }
            }
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

        public static string Right(this string text, int length)
        {
            return text == null ? null : length >= text.Length ? text : text.Substring(text.Length - length);
        }

        public static bool EvaluateFilter(this Element element, string filter)
        {
            var e = new Expression(element.ParseTags(filter, escapeSingleQuotes: true));
            var result = (bool)e.Evaluate();
            return result;
        }

        public static T EvaluateExpression<T>(this Element form, string expression, bool format = false)
        {
            var e = new Expression(form.ParseTags(expression, format, escapeSingleQuotes: true));
            var result = e.Evaluate();
            return (T)Convert.ChangeType(result, typeof(T));
        }

        public static T EvaluateExpression<T>(this Schema document, string expression, bool format = false)
        {
            var e = new Expression(document.Form.ParseTags(expression, format, escapeSingleQuotes: true));
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
            if (!string.IsNullOrEmpty(entity.Id))
            {
                entity.Id = root.ParseTags(entity.Id, format);
            }

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
                            var e = new Expression(repeater.ParseTags(filter, format, escapeSingleQuotes: true));
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
                            var e = new Expression(root.ParseTags(attribute.Filter, format, escapeSingleQuotes: true));
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
                                var e = new Expression(repeater.ParseTags(child.Filter, format, escapeSingleQuotes: true));
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
                            var e = new Expression(root.ParseTags(child.Filter, format, escapeSingleQuotes: true));
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

        public static string ParseTags(this Element root, string note, bool format = true, bool preflattened = false, string separator = " ", bool escapeDoubleQuotes = false, bool escapeSingleQuotes = false, bool join = false)
        {
            if (string.IsNullOrEmpty(note))
            {
                return string.Empty;
            }
            var elements = preflattened ? root.DocumentElements : root.Flatten();
            var taglist = Regex.Matches(note, @"(\<\<\[((.|\n)*?)\]\>\>|\<\<(.*?)\>\>)", RegexOptions.IgnoreCase);

            foreach (var tag in taglist)
            {
                var documentElementValue = "";
                var tagName = tag.ToString().TrimStart('<', '<').TrimEnd('>', '>');
                var leadingSeparator = false;

                if (tagName.StartsWith("[ForEach:"))
                {
                    tagName = tagName.Remove(tagName.Length - 1).Substring(9);
                    var repeaterName = tagName.Substring(0, tagName.IndexOf(":"));
                    var repeaterTag = tagName.Substring(tagName.IndexOf(":") + 1);
                    var filter = "";

                    if (repeaterName.IndexOf("[") != -1)
                    {
                        leadingSeparator = repeaterName.IndexOf("[") == 0;
                        separator = repeaterName.Split("[]".ToCharArray())[1];
                        repeaterName = repeaterName.Replace("[" + separator + "]", "").Trim();
                        separator = Regex.Unescape(separator);
                    }

                    if (repeaterName.IndexOf("{") != -1)
                    {
                        filter = repeaterName.Split("{}".ToCharArray())[1];
                        repeaterName = repeaterName.Replace("{" + filter + "}", "").Trim();
                    }

                    var repeaters = root.GetElementsByTagRecursive(repeaterName);

                    foreach (var repeater in repeaters)
                    {
                        if (string.IsNullOrEmpty(filter) || repeater.EvaluateFilter(filter))
                        {
                            var line = repeater.ParseTags(repeaterTag, format, preflattened, separator, escapeDoubleQuotes, escapeSingleQuotes);
                            documentElementValue += line + separator;
                        }
                    }

                    if (string.IsNullOrEmpty(documentElementValue))
                    {
                        leadingSeparator = false;
                    }
                }
                else if (tagName.StartsWith("[All:"))
                {
                    var repeaterName = tagName.Remove(tagName.Length - 1).Substring(5);
                    var filter = repeaterName.Split("{}".ToCharArray())[1];
                    repeaterName = repeaterName.Replace("{" + filter + "}", "").Trim();

                    var repeaters = root.GetElementsByTagRecursive(repeaterName);

                    documentElementValue = "true";

                    foreach (var repeater in repeaters)
                    {
                        if (!repeater.EvaluateFilter(filter))
                        {
                            documentElementValue = "false";
                            break;
                        }
                    }
                }
                else if (tagName.StartsWith("[Any:"))
                {
                    var repeaterName = tagName.Remove(tagName.Length - 1).Substring(5);
                    var filter = repeaterName.Split("{}".ToCharArray())[1];
                    repeaterName = repeaterName.Replace("{" + filter + "}", "").Trim();

                    var repeaters = root.GetElementsByTagRecursive(repeaterName);

                    documentElementValue = "false";

                    foreach (var repeater in repeaters)
                    {
                        if (repeater.EvaluateFilter(filter))
                        {
                            documentElementValue = "true";
                            break;
                        }
                    }
                }
                else if (tagName.StartsWith("[Count:"))
                {
                    tagName = tagName.Remove(tagName.Length - 1).Substring(7);
                    var repeaterName = tagName.IndexOf(":") > -1 ? tagName.Substring(0, tagName.IndexOf(":")) : tagName;
                    var expression = tagName.IndexOf(":") > -1 ? tagName.Substring(tagName.IndexOf(":") + 1) : null;
                    var filter = "";

                    if (repeaterName.IndexOf("{") != -1)
                    {
                        filter = repeaterName.Split("{}".ToCharArray())[1];
                        repeaterName = repeaterName.Replace("{" + filter + "}", "").Trim();
                    }

                    var repeaters = root.GetElementsByTagRecursive(repeaterName);
                    var count = 0;

                    foreach (var repeater in repeaters)
                    {
                        if (string.IsNullOrEmpty(filter) || repeater.EvaluateFilter(filter))
                        {
                            count++;
                        }
                    }

                    if (string.IsNullOrEmpty(expression))
                    {
                        documentElementValue = count.ToString();
                    }
                    else
                    {
                        var e = new Expression(root.ParseTags(expression.Replace("@Count", count.ToString()), format, preflattened, separator, escapeSingleQuotes: true));
                        documentElementValue = (string)e.Evaluate();
                    }
                }
                else if (tagName.StartsWith("[Expression:"))
                {
                    var expression = tagName.Substring(12).TrimEnd(']');
                    var e = new Expression(root.ParseTags(expression, format, preflattened, separator, escapeSingleQuotes: true));
                    documentElementValue = (string)e.Evaluate();
                }
                else if (tagName.StartsWith("[Join:"))
                {
                    var joinTag = tagName.Remove(tagName.Length - 1).Substring(6);
                    if (joinTag.IndexOf("[") != -1)
                    {
                        leadingSeparator = joinTag.IndexOf("[") == 0;
                        separator = joinTag.Split("[]".ToCharArray())[1];
                        joinTag = joinTag.Replace("[" + separator + "]", "").Trim();
                        separator = Regex.Unescape(separator);
                    }
                    documentElementValue = root.ParseTags(joinTag, format, preflattened, separator, escapeDoubleQuotes, escapeSingleQuotes, join: true);
                    if (string.IsNullOrEmpty(documentElementValue))
                    {
                        leadingSeparator = false;
                    }
                }
                else if (tagName.StartsWith("["))
                {
                    //skip other system tags
                    continue;
                }
                else if (tagName.Contains("]."))
                {
                    string[] parts = tagName.Split("[].".ToCharArray());
                    var repeaterName = parts[0];
                    var repaterIndex = Convert.ToInt16(parts[1]);
                    var repeaterTag = "<<" + parts[3] + ">>";
                    var repeaters = root.GetElementsByTagRecursive(repeaterName);
                    if (repaterIndex < repeaters.Count)
                    {
                        documentElementValue = repeaters[repaterIndex].ParseTags(repeaterTag, format, preflattened, separator);
                    }
                }
                else
                {
                    var tagFormat = "";
                    if (tagName.IndexOf("|") != -1)
                    {
                        tagFormat = tagName.Split('|')[1];
                        tagName = tagName.Split('|')[0];
                    }

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
                                value = DateTime.Parse(value).ToString(!string.IsNullOrEmpty(tagFormat) ? tagFormat : element.Format == "year" ? "yyyy" : element.Format == "month" ? "MMM-yyyy" : "dd-MMM-yyyy");
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
                                value = Convert.ToDecimal(value).ToString("P" + element.Decimals);
                            }
                            else if ((element.Type == "formula" || element.Type == "slider") && !string.IsNullOrEmpty(value) && format)
                            {
                                decimal n;
                                if (element.Format == "number")
                                {
                                    value = Convert.ToDecimal(value).ToString("N0");
                                }
                                if (element.Format == "percent")
                                {
                                    value = Convert.ToDecimal(value).ToString("P" + element.Decimals);
                                }
                                else if (element.Format == "currency" || string.IsNullOrEmpty(element.Format) && Decimal.TryParse(value, out n))
                                {
                                    value = Convert.ToDecimal(value).ToString("C" + element.Decimals);
                                }
                            }
                            if (escapeDoubleQuotes)
                            {
                                value = value.Replace("\"", "\"\"");
                            }
                            if (escapeSingleQuotes)
                            {
                                value = value.Replace("'", "\\'").Replace("\n", "");
                            }
                            documentElementValue += value + separator;
                        }
                    }
                    else if (root.Parent != null && !root.Flatten(true, true).Any(a => a.AutofillKey != null && a.AutofillKey.Equals(tagName, StringComparison.OrdinalIgnoreCase)))
                    {
                        //escalate to parent scope if parent exists and element doesn't exist at current level behind conditional path or empty repeater
                        documentElementValue = root.Parent.ParseTags(string.Format("<<{0}>>", tagName), format);
                    }
                }
                if (!string.IsNullOrEmpty(separator) && documentElementValue.EndsWith(separator))
                {
                    documentElementValue = documentElementValue.Remove(documentElementValue.Length - separator.Length);
                }
                documentElementValue = documentElementValue.Trim();
                if (!string.IsNullOrEmpty(separator) && leadingSeparator)
                {
                    documentElementValue = separator + documentElementValue;
                }
                if (join && !string.IsNullOrEmpty(documentElementValue))
                {
                    documentElementValue += separator;
                }
                note = note.ReplaceFirst(tag.ToString(), documentElementValue);
            }
            return note;
        }

        public static string ReplaceFirst(this string text, string search, string replace)
        {
            int pos = text.IndexOf(search, StringComparison.OrdinalIgnoreCase);
            if (pos < 0)
            {
                return text;
            }
            return text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
        }

        public static string ParseDocumentDateTag(this string template, string tagName, DateTime value, string timeZoneId)
        {
            var taglist = Regex.Matches(template, "<<\\[" + tagName + "(.*?)\\]>>", RegexOptions.IgnoreCase);
            if (taglist.Count > 0)
            {
                var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                var documentDateTime = TimeZoneInfo.ConvertTimeFromUtc(value, timeZoneInfo);
                foreach (var tag in taglist)
                {
                    var filter = tag.ToString().Replace("<<[" + tagName, "").Replace("]>>", "").Trim();
                    if (!string.IsNullOrEmpty(filter))
                    {
                        filter = filter.TrimStart('|').Trim().Trim('\'');
                    }
                    else
                    {
                        filter = "dd-MMM-yyyy";
                    }
                    template = template.ReplaceFirst(tag.ToString(), documentDateTime.ToString(filter));
                }
            }
            return template;
        }

        private static void GetRepeaterParents(this Element root, List<Element> line, string tagName)
        {
            if (root.Elements != null && root.Elements.Any() && root.Elements[0].AllowMany && root.Elements[0].AutofillKey != null && root.Elements[0].AutofillKey.Equals(tagName, StringComparison.OrdinalIgnoreCase))
            {
                line.Add(root);
            }
            foreach (var element in root.DocumentElements.Where(a => a.DocumentElements != null).ToList())
            {
                if (element.ElementType != ElementType.Condition || string.Equals(root.DocumentValue, element.Visibility, StringComparison.OrdinalIgnoreCase))
                {
                    GetRepeaterParents(element, line, tagName);
                }
            }
        }

        public static void SetTags(this Element root, TagData tag)
        {
            if (tag.Tags != null && tag.Tags.Any())
            {
                var repeaterParents = new List<Element>();
                root.GetRepeaterParents(repeaterParents, tag.Tag);
                foreach (var repeaterParent in repeaterParents)
                {
                    var repeater = JsonConvert.DeserializeObject<Element>(JsonConvert.SerializeObject(repeaterParent.Elements[0]));
                    foreach (var child in tag.Tags)
                    {
                        repeater.SetTags(child);
                    }
                    repeaterParent.DocumentElements.Remove(repeaterParent.Elements[0]);
                    repeaterParent.DocumentElements.Add(repeater);
                }
            }
            else if (!string.IsNullOrEmpty(tag.Data))
            {
                var elements = root.Flatten(ignoreConditional: true).Where(a => a.AutofillKey != null && a.AutofillKey.Equals(tag.Tag, StringComparison.OrdinalIgnoreCase)).ToList();
                foreach (var element in elements)
                {
                    element.DocumentValue = tag.Data;
                }
            }
        }

    }
}
