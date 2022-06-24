using NCalc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
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
            var attribute = entity.Attributes?.FirstOrDefault(a => a.Name == name);
            if (attribute != null)
            {
                return attribute.Value;
            }
            return null;
        }

        public static Attribute GetAttribute(this Attribute attribute, string name)
        {
            return attribute.Attributes?.FirstOrDefault(a => a.Name == name);
        }

        public static Attribute GetAttribute(this Entity entity, string name)
        {
            return entity.Attributes?.FirstOrDefault(a => a.Name == name);
        }

        public static List<Attribute> GetAttributes(this Entity entity, string name)
        {
            return entity.Attributes?.Where(a => a.Name == name).ToList() ?? new List<Attribute>();
        }

        public static Entity GetEntity(this Entity entity, string name)
        {
            return entity.Entities?.FirstOrDefault(a => a.Name == name);
        }

        public static Entity GetEntity(this List<Entity> entities, string name)
        {
            return entities.FirstOrDefault(a => a.Name == name);
        }

        public static List<Entity> GetEntities(this Entity entity, string name)
        {
            return entity.Entities?.Where(a => a.Name == name).ToList() ?? new List<Entity>();
        }

        public static List<Element> Flatten(this Element root, bool includeEmptyRepeaters = false, bool ignoreConditional = false)
        {
            var line = new List<Element>();
            root.FlattenInternal(line, includeEmptyRepeaters, ignoreConditional);
            return line;
        }

        private static void FlattenInternal(this Element root, List<Element> line, bool includeEmptyRepeaters = false, bool ignoreConditional = false)
        {
            if (root.DocumentElements != null)
            {
                foreach (var element in root.DocumentElements)
                {
                    line.Add(element);
                    if (element.ElementType != ElementType.Condition || ignoreConditional || string.Equals(root.DocumentValue, element.Visibility, StringComparison.OrdinalIgnoreCase))
                    {
                        FlattenInternal(element, line, includeEmptyRepeaters, ignoreConditional);
                    }
                }
            }
            if (includeEmptyRepeaters && root.Elements != null && root.Elements.Any() && root.Elements[0].AllowMany && (root.DocumentElements == null || !root.DocumentElements.Any()))
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
            if (root.Entities != null)
            {
                foreach (var entity in root.Entities)
                {
                    line.Add(entity);
                    FlattenInternal(entity, line);
                }
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
            if (!result.Any() & root.Parent != null && !root.Flatten(true, true).Any(a => a.AutofillKey != null && a.AutofillKey.Equals(tagName, StringComparison.OrdinalIgnoreCase)))
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
                if (!string.IsNullOrEmpty(root.DocumentElements.FirstOrDefault()?.LinkedRepeater))
                {
                    var linkedRepeaterParents = root.GetElementsByTagRecursive(root.DocumentElements.First().LinkedRepeater);
                    if (linkedRepeaterParents.Any())
                    {
                        foreach (var child in root.DocumentElements)
                        {
                            child.LinkedRepeaterParent = linkedRepeaterParents[root.DocumentElements.IndexOf(child)];
                        }
                    }
                }
            }
        }

        public static string JsonToText(this string text)
        {
            if (text.StartsWith("{"))
            {
                return string.Join(" ", ((JObject)JsonConvert.DeserializeObject(text)).Children().Cast<JProperty>().Select(jp => jp.Value));
            }
            return text;
        }

        public static string EmptyToNull(this string text)
        {
            return string.IsNullOrEmpty(text) ? null : text;
        }

        public static string Left(this string text, int length)
        {
            return text?.Substring(0, Math.Min(length, text.Length));
        }

        public static string Right(this string text, int length)
        {
            return text == null ? null : length >= text.Length ? text : text.Substring(text.Length - length);
        }

        public static bool EvaluateFilter(this Element element, string filter, bool format = false)
        {
            try
            {
                var e = new Expression(element.ParseTags(filter.FixSingleQuotes(), format, escapeSingleQuotes: true, escapeSlashes: true).CleanForExpression());
                var result = (bool)e.Evaluate();
                return result;
            }
            catch (Exception ex)
            {
                ex.Data.Add("Filter", filter);
                throw;
            }
        }

        public static T EvaluateExpression<T>(this Element form, string expression, bool format = false)
        {
            try
            {
                var e = new Expression(form.ParseTags(expression.FixSingleQuotes(), format, escapeSingleQuotes: true, escapeSlashes: true).CleanForExpression());
                var result = e.Evaluate();
                return (T)Convert.ChangeType(result, typeof(T));
            }
            catch (Exception ex)
            {
                ex.Data.Add("Expression", expression);
                throw;
            }
        }

        public static bool BlockApproval(this Element root)
        {
            return root.Flatten().Any(a => a.ElementType == ElementType.Workflow && a.Required);
        }

        public static T EvaluateExpression<T>(this Schema document, string expression, bool format = false)
        {
            try
            {
                var e = new Expression(document.Form.ParseTags(expression.FixSingleQuotes().ParseSchemaTags(document), format, escapeSingleQuotes: true, escapeSlashes: true).CleanForExpression());
                var result = e.Evaluate();
                return (T)Convert.ChangeType(result, typeof(T));
            }
            catch (Exception ex)
            {
                ex.Data.Add("Expression", expression);
                throw;
            }
        }

        public static dynamic ToObject(this Entity entity)
        {
            var documentObject = new ExpandoObject();
            BuildEntityObject(entity, documentObject);
            return documentObject;
        }

        private static void BuildEntityObject(Entity entity, IDictionary<string, object> item)
        {
            if (entity.Attributes != null)
            {
                foreach (var attribute in entity.Attributes)
                {
                    if (attribute.Setter == "float")
                    {
                        if (string.IsNullOrEmpty(attribute.Value))
                        {
                            item.Add(attribute.Name, null);
                        }
                        else
                        {
                            item.Add(attribute.Name, float.Parse(attribute.Value));
                        }
                    }
                    else
                    {
                        item.Add(attribute.Name, attribute.Value);
                    }
                }
            }

            if (entity.Entities != null)
            {
                foreach (var child in entity.Entities.Where(a => string.IsNullOrEmpty(a.Setter)))
                {
                    var group = new ExpandoObject();
                    BuildEntityObject(child, group);
                    item.Add(child.Name, group);
                }

                var arrays = entity.Entities.Where(a => a.Setter == "array").Select(b => b.Name).Distinct();

                foreach (var array in arrays)
                {
                    var list = new List<ExpandoObject>();
                    foreach (var child in entity.Entities.Where(a => a.Name == array))
                    {
                        var group = new ExpandoObject();
                        BuildEntityObject(child, group);
                        list.Add(group);
                    }
                    item.Add(array, list);
                }
            }
        }

        public static Entity ToMap(this Schema schema)
        {
            var map = new Entity { Attributes = new List<Attribute>() };
            foreach (var element in schema.Form.Elements)
            {
                BuildEntity(element, map);
            }
            return map;
        }

        private static void BuildEntity(Element element, Entity entity, string path = null)
        {
            if (element.ElementType == ElementType.Question)
            {
                if (element.Elements != null && element.Elements.Any())
                {
                    entity.Attributes.Add(new Attribute { Name = element.AutofillKey ?? element.Name, Value = path + (element.AutofillKey ?? element.Name) + ".@value" });
                    foreach (var childElement in element.Elements)
                    {
                        BuildEntity(childElement, entity, path + (element.AutofillKey ?? element.Name) + ".");
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(element.AutofillKey) && entity.Attributes.Any(a => a.Name == element.AutofillKey))
                    {
                        entity.Attributes.Add(new Attribute { Name = element.AutofillKey + element.Name, Value = path + (element.AutofillKey + element.Name) });
                    }
                    else
                    {
                        entity.Attributes.Add(new Attribute { Name = element.AutofillKey ?? element.Name, Value = path + (element.AutofillKey ?? element.Name) });
                    }
                }
            }
            else if (element.Elements != null && element.Elements.FirstOrDefault() != null && element.Elements.FirstOrDefault().AllowMany)
            {
                var repeater = element.Elements.FirstOrDefault();
                var child = new Entity { Name = repeater.AutofillKey ?? repeater.Name, ForEach = path + (element.AutofillKey ?? element.Name), Attributes = new List<Attribute>() };
                if (element.Elements != null)
                {
                    foreach (var childElement in element.Elements)
                    {
                        BuildEntity(childElement, child);
                    }
                }
                if (entity.Entities == null)
                {
                    entity.Entities = new List<Entity>();
                }
                entity.Entities.Add(child);
            }
            else
            {
                foreach (var childElement in element.Elements)
                {
                    BuildEntity(childElement, entity, path);
                }
            }
        }

        public static dynamic ToObject(this Schema document, bool flatten = false, bool ignoreConditional = false)
        {
            var documentObject = new ExpandoObject();
            foreach (var element in document.Form.DocumentElements)
            {
                BuildObject(element, documentObject, flatten, ignoreConditional);
            }
            return documentObject;
        }

        private static void BuildObject(Element element, IDictionary<string, object> item, bool flatten = false, bool ignoreConditional = false, Element parent = null)
        {
            if (element.ElementType == ElementType.Question)
            {
                if (element.DocumentElements != null && element.DocumentElements.Any())
                {
                    var group = new ExpandoObject();
                    (group as IDictionary<string, object>).Add("@value", element.DocumentValue);
                    foreach (var childElement in element.DocumentElements)
                    {
                        BuildObject(childElement, group, flatten, ignoreConditional, element);
                    }
                    try
                    {
                        item.Add(element.AutofillKey ?? element.Name, group);
                    }
                    catch (ArgumentException)
                    {
                        item.Add(element.AutofillKey + element.Name, group);
                    }
                }
                else if (!flatten || (!string.IsNullOrEmpty(element.DocumentValue) && element.Type != "signature" && element.Type != "diagram"))
                {
                    try
                    {
                        item.Add(element.AutofillKey ?? element.Name, element.DocumentValue);
                    }
                    catch (ArgumentException)
                    {
                        item.Add(element.AutofillKey + element.Name, element.DocumentValue);
                    }
                }
            }
            else if (element.Elements != null && element.Elements.FirstOrDefault() != null && element.Elements.FirstOrDefault().AllowMany)
            {
                var list = new List<ExpandoObject>();
                if (element.DocumentElements != null)
                {
                    foreach (var childElement in element.DocumentElements)
                    {
                        var child = new ExpandoObject();
                        BuildObject(childElement, child, flatten, ignoreConditional, element);
                        list.Add(child);
                    }
                }
                item.Add(element.AutofillKey ?? element.Name, list);
            }
            else if (element.ElementType != ElementType.Condition || ignoreConditional || string.Equals(parent.DocumentValue, element.Visibility, StringComparison.OrdinalIgnoreCase))
            {
                if (flatten)
                {
                    if (element.ElementType == ElementType.ValidationService)
                    {
                        var group = new ExpandoObject();
                        (group as IDictionary<string, object>).Add("@value", element.DocumentValue);
                        (group as IDictionary<string, object>).Add("@message", element.SectionValidationMessage);
                        (group as IDictionary<string, object>).Add("@datetime", element.SectionValidationDateTime);
                        (group as IDictionary<string, object>).Add("@reference", element.SectionValidationReference);
                        item.Add(element.AutofillKey ?? element.Name, group);
                    }
                    foreach (var childElement in element.DocumentElements)
                    {
                        BuildObject(childElement, item, flatten, ignoreConditional, element);
                    }
                }
                else
                {
                    var group = new ExpandoObject();
                    if (element.ElementType == ElementType.ValidationService)
                    {
                        (group as IDictionary<string, object>).Add("@value", element.DocumentValue);
                        (group as IDictionary<string, object>).Add("@message", element.SectionValidationMessage);
                        (group as IDictionary<string, object>).Add("@datetime", element.SectionValidationDateTime);
                        (group as IDictionary<string, object>).Add("@reference", element.SectionValidationReference);
                    }
                    if (element.DocumentElements != null)
                    {
                        foreach (var childElement in element.DocumentElements)
                        {
                            BuildObject(childElement, group, flatten, ignoreConditional, element);
                        }
                    }
                    item.Add(element.AutofillKey ?? element.Name, group);
                }
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
                            var e = new Expression(repeater.ParseTags(filter.FixSingleQuotes(), format, escapeSingleQuotes: true, escapeSlashes: true).CleanForExpression());
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
                        try
                        {
                            attribute.Value = root.ParseTags(attribute.Value, format);
                        }
                        catch (Exception ex)
                        {
                            ex.Data.Add("Attribute", attribute.Name);
                            throw;
                        }
                        if (!string.IsNullOrEmpty(attribute.Filter))
                        {
                            var e = new Expression(root.ParseTags(attribute.Filter.FixSingleQuotes(), format, escapeSingleQuotes: true, escapeSlashes: true).CleanForExpression());
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
                                var e = new Expression(repeater.ParseTags(child.Filter.FixSingleQuotes(), format, escapeSingleQuotes: true, escapeSlashes: true).CleanForExpression());
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
                            var e = new Expression(root.ParseTags(child.Filter.FixSingleQuotes(), format, escapeSingleQuotes: true, escapeSlashes: true).CleanForExpression());
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

        public static string ParseTags(this Element root, string note, bool format = true, bool preflattened = false, string separator = " ", bool escapeDoubleQuotes = false, bool escapeSingleQuotes = false, bool join = false, bool escapeXML = false, bool htmlOutput = false, bool escapeSlashes = false)
        {
            if (string.IsNullOrEmpty(note))
            {
                return string.Empty;
            }
            var elements = preflattened ? root.DocumentElements : root.Flatten();
            var taglist = Regex.Matches(note, @"(\<\<\<\[((.|\n)*?)\]\>\>\>|\<\<\[((.|\n)*?)\]\>\>|\<\<(.*?)\>\>)", RegexOptions.IgnoreCase);

            foreach (var tag in taglist)
            {
                var documentElementValue = "";
                var tagName = tag.ToString().TrimStart('<', '<').TrimEnd('>', '>');
                var leadingSeparator = false;

                if (tagName.StartsWith("[ForEach:"))
                {
                    tagName = tagName.Remove(tagName.Length - 1).Substring(9);
                    var repeaterName = tagName.IndexOf(":") > -1 ? tagName.Substring(0, tagName.IndexOf(":")) : tagName;
                    var repeaterTag = tagName.IndexOf(":") > -1 ? tagName.Substring(tagName.IndexOf(":") + 1) : "<<[This]>>";
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
                            var line = repeater.ParseTags(repeaterTag, format, preflattened, separator, escapeDoubleQuotes, escapeSingleQuotes, escapeXML: escapeXML, htmlOutput: htmlOutput);
                            documentElementValue += line + separator;
                        }
                    }

                    if (string.IsNullOrEmpty(documentElementValue))
                    {
                        leadingSeparator = false;
                    }
                }
                else if (tagName.StartsWith("[First:"))
                {
                    tagName = tagName.Remove(tagName.Length - 1).Substring(7);
                    var repeaterName = tagName.IndexOf(":") > -1 ? tagName.Substring(0, tagName.IndexOf(":")) : tagName;
                    var repeaterTag = tagName.IndexOf(":") > -1 ? tagName.Substring(tagName.IndexOf(":") + 1) : "<<[This]>>";
                    var filter = "";

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
                            var line = repeater.ParseTags(repeaterTag, format, preflattened, separator, escapeDoubleQuotes, escapeSingleQuotes, escapeXML: escapeXML, htmlOutput: htmlOutput);
                            documentElementValue += line;
                            break;
                        }
                    }
                }
                else if (tagName.StartsWith("[ElementAt("))
                {
                    tagName = tagName.Remove(tagName.Length - 1).Substring(11);
                    var repeaterIndex = int.Parse(tagName.Substring(0, tagName.IndexOf(")")));
                    tagName = tagName.Substring(tagName.IndexOf(")") + 2);
                    var repeaterName = tagName.IndexOf(":") > -1 ? tagName.Substring(0, tagName.IndexOf(":")) : tagName;
                    var repeaterTag = tagName.IndexOf(":") > -1 ? tagName.Substring(tagName.IndexOf(":") + 1) : "<<[This]>>";
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
                            if (count == repeaterIndex)
                            {
                                var line = repeater.ParseTags(repeaterTag, format, preflattened, separator, escapeDoubleQuotes, escapeSingleQuotes, escapeXML: escapeXML, htmlOutput: htmlOutput);
                                documentElementValue += line;
                                break;
                            }
                            count++;
                        }
                    }
                }
                else if (tagName.StartsWith("[Condition:"))
                {
                    tagName = tagName.Remove(tagName.Length - 1).Substring(11);
                    var tagParsed = tagName;
                    var nestedTagList = Regex.Matches(tagName, @"(\<\<\[((.|\n)*?)\]\>\>)", RegexOptions.IgnoreCase);
                    if (nestedTagList.Count > 0)
                    {
                        foreach (var nestedTag in nestedTagList)
                        {
                            tagParsed = tagParsed.ReplaceFirst(nestedTag.ToString(), "");
                        }
                    }

                    var repeaterTag = tagParsed.Substring(tagParsed.IndexOf(":") + 1);
                    var filter = tagName.Substring(0, tagName.Length - repeaterTag.Length - 1);

                    if (root.EvaluateFilter(filter))
                    {
                        var line = root.ParseTags(repeaterTag, format, preflattened, separator, escapeDoubleQuotes, escapeSingleQuotes, escapeXML: escapeXML, htmlOutput: htmlOutput);
                        documentElementValue += line;
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
                    var filter = "";

                    if (repeaterName.IndexOf("{") != -1)
                    {
                        filter = repeaterName.Split("{}".ToCharArray())[1];
                        repeaterName = repeaterName.Replace("{" + filter + "}", "").Trim();
                    }

                    var repeaters = root.GetElementsByTagRecursive(repeaterName);

                    documentElementValue = "false";

                    foreach (var repeater in repeaters)
                    {
                        if (string.IsNullOrEmpty(filter) || repeater.EvaluateFilter(filter))
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
                        try
                        {
                            if (htmlOutput)
                            {
                                expression = expression.Replace("&lt;", "<").Replace("&gt;", ">");
                            }
                            var e = new Expression(root.ParseTags(expression.FixSingleQuotes().Replace("@Count", count.ToString()), format, preflattened, separator, escapeSingleQuotes: true, escapeSlashes: true, escapeXML: escapeXML).CleanForExpression());
                            documentElementValue = e.Evaluate().ToString();
                            if (htmlOutput)
                            {
                                documentElementValue = documentElementValue.Replace("<", "&lt;").Replace(">", "&gt;");
                            }
                        }
                        catch (Exception ex)
                        {
                            ex.Data.Add("Expression", expression);
                            throw;
                        }
                    }
                }
                else if (tagName.StartsWith("[Expression:"))
                {
                    var expression = tagName.Substring(12).TrimEnd(']');
                    try
                    {
                        var e = new Expression(root.ParseTags(expression.FixSingleQuotes(), format, preflattened, separator, escapeSingleQuotes: true, escapeSlashes: true, escapeXML: escapeXML).CleanForExpression());
                        documentElementValue = e.Evaluate().ToString();
                    }
                    catch (Exception ex)
                    {
                        ex.Data.Add("Expression", expression);
                        throw;
                    }
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
                    documentElementValue = root.ParseTags(joinTag, format, preflattened, separator, escapeDoubleQuotes, escapeSingleQuotes, true, escapeXML);
                    if (string.IsNullOrEmpty(documentElementValue))
                    {
                        leadingSeparator = false;
                    }
                }
                else if (tagName == "[Stage]")
                {
                    documentElementValue = elements.Last(a => a.ElementType == ElementType.Section && a.Completed == "true").AutofillKey;
                }
                else if (tagName.StartsWith("[") && tagName != "[This]")
                {
                    //skip other system tags
                    continue;
                }
                else if (tagName.IndexOf("].") > 0 && tagName.Split('[')[0].IndexOf(".") < 0)
                {
                    string[] parts = tagName.Split("[]".ToCharArray());
                    var repeaterName = parts[0];
                    var repeaterIndex = int.Parse(parts[1]);
                    var repeaterTag = "<<" + parts[2].TrimStart('.') + ">>";
                    var repeaters = root.GetElementsByTagRecursive(repeaterName);
                    if (repeaterIndex < repeaters.Count)
                    {
                        documentElementValue = repeaters[repeaterIndex].ParseTags(repeaterTag, format, preflattened, separator);
                    }
                }
                else
                {
                    var tagFormat = "";
                    var tagProperty = "";
                    if (tagName.IndexOf("|") != -1)
                    {
                        tagFormat = tagName.Split('|')[1];
                        tagName = tagName.Split('|')[0];
                    }
                    else if (tagName.IndexOf(".") != -1)
                    {
                        tagProperty = tagName.Substring(tagName.IndexOf(".") + 1);
                        if (tagProperty.StartsWith("[SectionValidationData."))
                        {
                            tagProperty = tagProperty.Replace("[SectionValidationData.", "").TrimEnd(']');
                        }
                        tagName = tagName.Substring(0, tagName.IndexOf("."));
                    }

                    var matches = tagName == "[This]" || (root.AllowMany && root.AutofillKey != null && root.AutofillKey.Equals(tagName, StringComparison.OrdinalIgnoreCase)) ? new List<Element> { root } : elements.Where(a => a.AutofillKey != null && a.AutofillKey.Equals(tagName, StringComparison.OrdinalIgnoreCase)).ToList();

                    if (matches.Any())
                    {
                        foreach (var element in matches)
                        {
                            var value = element.DocumentValue ?? "";

                            try
                            {
                                if (tagProperty == "[SectionValidationMessage]")
                                {
                                    value = element.SectionValidationMessage;
                                }
                                else if (tagProperty == "[SectionValidationReference]")
                                {
                                    value = element.SectionValidationReference;
                                }
                                else if (tagProperty == "[SectionValidationDateTime]")
                                {
                                    value = element.SectionValidationDateTime;
                                }
                                else if (!string.IsNullOrEmpty(tagProperty) && element.Token != null)
                                {
                                    value = element.Token.SelectToken(tagProperty).Value<string>() ?? "";
                                }

                                if (element.Type == "boolean" && tagProperty != "Value" && (format || !escapeXML))
                                {
                                    value = value == "true" ? "Yes" : "No";
                                }
                                else if (element.Type == "option" && tagProperty != "Value")
                                {
                                    value = value == "true" ? element.Prompt : "";
                                }
                                else if (element.Type == "nameValueList" && !string.IsNullOrEmpty(value) && ((format && tagProperty != "Value") || tagProperty == "Name"))
                                {
                                    value = element.SimpleType.Values.Single(a => a.Value == value).Name;
                                }
                                else if (element.Type == "date" && !string.IsNullOrEmpty(value) && (format || !string.IsNullOrEmpty(tagFormat)))
                                {
                                    value = DateTime.Parse(value).ToString(!string.IsNullOrEmpty(tagFormat) ? tagFormat : element.Format == "year" ? "yyyy" : element.Format == "month" ? "MMM-yyyy" : "dd-MMM-yyyy");
                                }
                                else if (element.Type == "time" && !string.IsNullOrEmpty(value) && (format || !string.IsNullOrEmpty(tagFormat)))
                                {
                                    value = DateTime.Parse(value).ToString(!string.IsNullOrEmpty(tagFormat) ? tagFormat : element.Format == "12" ? "hh:mm tt" : "HH:mm");
                                }
                                else if (element.Type == "currency" && !string.IsNullOrEmpty(value) && (format || !string.IsNullOrEmpty(tagFormat)))
                                {
                                    value = decimal.Parse(value, NumberStyles.Float).ToString(!string.IsNullOrEmpty(tagFormat) ? tagFormat : "C" + element.Decimals);
                                }
                                else if (element.Type == "number" && !string.IsNullOrEmpty(value) && (format || !string.IsNullOrEmpty(tagFormat)))
                                {
                                    value = decimal.Parse(value, NumberStyles.Float).ToString(!string.IsNullOrEmpty(tagFormat) ? tagFormat : "N" + (string.IsNullOrEmpty(element.Decimals) ? "0" : element.Decimals));
                                }
                                else if (element.Type == "slider" && !string.IsNullOrEmpty(value) && (format || !string.IsNullOrEmpty(tagFormat)))
                                {
                                    value = decimal.Parse(value, NumberStyles.Float).ToString(!string.IsNullOrEmpty(tagFormat) ? tagFormat : "N" + (float.Parse(element.Step) < 1 ? "1" : "0"));
                                }
                                else if (element.Type == "percent" && !string.IsNullOrEmpty(value) && (format || !string.IsNullOrEmpty(tagFormat)))
                                {
                                    value = decimal.Parse(value, NumberStyles.Float).ToString(!string.IsNullOrEmpty(tagFormat) ? tagFormat : "P" + element.Decimals);
                                }
                                else if (element.Type == "formula" && !string.IsNullOrEmpty(value) && (format || !string.IsNullOrEmpty(tagFormat)))
                                {
                                    if (element.Format == "date")
                                    {
                                        value = DateTime.Parse(value).ToString(!string.IsNullOrEmpty(tagFormat) ? tagFormat : element.Format == "year" ? "yyyy" : element.Format == "month" ? "MMM-yyyy" : "dd-MMM-yyyy");
                                    }
                                    if (element.Format == "number")
                                    {
                                        value = decimal.Parse(value, NumberStyles.Float).ToString(!string.IsNullOrEmpty(tagFormat) ? tagFormat : "N" + (string.IsNullOrEmpty(element.Decimals) ? "0" : element.Decimals));
                                    }
                                    if (element.Format == "percent")
                                    {
                                        value = decimal.Parse(value, NumberStyles.Float).ToString(!string.IsNullOrEmpty(tagFormat) ? tagFormat : "P" + element.Decimals);
                                    }
                                    else if (element.Format == "currency" || string.IsNullOrEmpty(element.Format) && Decimal.TryParse(value, out decimal n))
                                    {
                                        value = decimal.Parse(value, NumberStyles.Float).ToString(!string.IsNullOrEmpty(tagFormat) ? tagFormat : "C" + element.Decimals);
                                    }
                                }
                                else if (element.Type == "nzird" && !string.IsNullOrEmpty(value) && tagFormat != "nzird" && (!format || tagFormat == "0"))
                                {
                                    value = value.Replace("-", "");
                                }
                                else if (element.Type == "nzbank" && !string.IsNullOrEmpty(value) && tagFormat == "0")  //TODO: change this to respect 'format' like nzird
                                {
                                    value = value.Replace("-", "");
                                }
                                else if (!string.IsNullOrEmpty(element.Decimals) && !string.IsNullOrEmpty(value) && (element.Type == "percent" || element.Format == "percent"))
                                {
                                    value = decimal.Parse(value, NumberStyles.Float).ToString("F" + (int.Parse(element.Decimals) + 2));
                                }
                                else if (!string.IsNullOrEmpty(element.Decimals) && !string.IsNullOrEmpty(value))
                                {
                                    value = decimal.Parse(value, NumberStyles.Float).ToString("F" + element.Decimals);
                                }
                                if (escapeSlashes)
                                {
                                    value = value.Replace("\\", "\\\\");
                                }
                                if (escapeDoubleQuotes)
                                {
                                    value = value.Replace("\"", "\"\"");
                                }
                                if (escapeSingleQuotes)
                                {
                                    value = value.Replace("'", "\\'");
                                }
                                if (escapeXML)
                                {
                                    value = System.Security.SecurityElement.Escape(value);
                                }
                                if (htmlOutput)
                                {
                                    if (element.Type == "string")
                                    {
                                        value = value.Replace("<", "&lt;").Replace(">", "&gt;");
                                    }
                                    else if (element.Type != "dataService")
                                    {
                                        value = value.Replace("\n", "<br>");
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                ex.Data.Add("Value", value);
                                throw;
                            }

                            if (!string.IsNullOrEmpty(value))
                            {
                                documentElementValue += value + separator;
                            }
                        }
                    }
                    else if ((root.LinkedRepeaterParent != null || root.Parent != null) && !root.Flatten(true, true).Any(a => a.AutofillKey != null && a.AutofillKey.Equals(tagName, StringComparison.OrdinalIgnoreCase)))
                    {
                        //escalate to parent scope if parent exists and element doesn't exist at current level behind conditional path or empty repeater
                        documentElementValue = (root.LinkedRepeaterParent ?? root.Parent).ParseTags(string.Format("<<{0}>>", !string.IsNullOrEmpty(tagFormat) ? string.Format("{0}|{1}", tagName, tagFormat) : !string.IsNullOrEmpty(tagProperty) ? string.Format("{0}.{1}", tagName, tagProperty) : tagName), format, preflattened, separator, escapeDoubleQuotes, escapeSingleQuotes, escapeXML: escapeXML, htmlOutput: htmlOutput);
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

        public static string RemoveInvalidChars(this string filename)
        {
            return string.Concat(filename.Split(Path.GetInvalidFileNameChars()));
        }

        public static string MakeValidFileName(this string name)
        {
            return name.RemoveInvalidChars().RemoveDiacritics().Replace("–", "-").Replace("’", "'").Left(240).Trim();
        }

        public static string RemoveDiacritics(this string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            text = text.Normalize(NormalizationForm.FormD);
            var chars = text.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray();
            return new string(chars).Normalize(NormalizationForm.FormC);
        }

        public static string ParseSchemaTags(this string template, Schema document)
        {
            template = Regex.Replace(template, "<<\\[DocumentFormVersion\\]>>", document.Version.ToString(), RegexOptions.IgnoreCase);
            return template;
        }
        
        private static string CleanForExpression(this string input)
        {
            return input.Replace("\t", "").Replace("\n", "");
        }

        private static string FixSingleQuotes(this string input)
        {
            return input.Replace("‘", "'").Replace("’", "'");
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
                var elements = root.Flatten(ignoreConditional: true).Where(a => (a.AutofillKey + a.Name).Equals(tag.Tag, StringComparison.OrdinalIgnoreCase)
                                            || a.Name.Equals(tag.Tag, StringComparison.OrdinalIgnoreCase)
                                            || a.AutofillKey != null && a.AutofillKey.Equals(tag.Tag, StringComparison.OrdinalIgnoreCase)).ToList();

                foreach (var element in elements)
                {
                    element.DocumentValue = tag.Data;
                }
            }
        }

    }
}
