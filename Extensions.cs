using FormsByAir.SDK.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace FormsByAir.Model
{
    public static class Extensions
    {
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

        public static List<List<Element>> FlattenWithRepeaters(this Element root)
        {
            var line = new List<Element>();
            var lines = new List<List<Element>>();
            root.FlattenWithRepeatersInternal(line, lines);
            return lines;
        }

        private static void FlattenWithRepeatersInternal(this Element root, List<Element> line, List<List<Element>> lines)
        {
            var isLeaf = true;
            var elements = new List<Element>();

            foreach (var element in root.DocumentElements)
            {
                elements.Add(element);
            }
            foreach (var element in root.DocumentElements.Where(a => a.DocumentElements != null).ToList())
            {
                if (element.ElementType != ElementType.Condition || string.Equals(root.DocumentValue, element.Visibility, StringComparison.OrdinalIgnoreCase))
                {
                    isLeaf = false;
                    line = line.Concat(elements).ToList();
                    FlattenWithRepeatersInternal(element, line, lines);
                }
            }
            if (isLeaf)
            {
                var newLine = line.Concat(elements).ToList();
                lines.Add(newLine);
            }
        }

        public static string Left(this string text, int length)
        {
            return text == null ? null : text.Substring(0, Math.Min(length, text.Length));
        }

        public static T ParseTags<T>(this Schema document, string note)
        {
            return (T)Convert.ChangeType(document.Form.Flatten().ParseTags<string>(note), typeof(T));
        }

        public static T ParseTags<T>(this List<Element> elements, string note, bool format = true)
        {
            var taglist = Regex.Matches(note, "\\<\\<[0-9a-zA-Z]+\\>\\>", RegexOptions.IgnoreCase);

            foreach (var tag in taglist)
            {
                var documentElementValue = "";
                var tagName = tag.ToString().TrimStart('<', '<').TrimEnd('>', '>');
                var element = elements.Where(a => a.AutofillKey != null && a.AutofillKey.Equals(tagName, System.StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

                if (element != null)
                {
                    documentElementValue = element.DocumentValue ?? "";

                    if (element.Type == "boolean")
                    {
                        documentElementValue = documentElementValue == "true" ? "Yes" : "No";
                    }
                    else if (element.Type == "nameValueList" && !string.IsNullOrEmpty(documentElementValue) && format)
                    {
                        documentElementValue = element.SimpleType.Values.Single(a => a.Value == documentElementValue).Name;
                    }
                    else if (element.Type == "date" && !string.IsNullOrEmpty(documentElementValue))
                    {
                        documentElementValue = DateTime.Parse(documentElementValue).ToString("dd-MMM-yyyy");
                    }
                    else if (element.Type == "currency" && !string.IsNullOrEmpty(documentElementValue) && format)
                    {
                        documentElementValue = Convert.ToDecimal(documentElementValue).ToString("C");
                    }
                    else if (element.Type == "formula" && !string.IsNullOrEmpty(documentElementValue) && format)
                    {
                        float n;
                        if (float.TryParse(documentElementValue, out n))
                        {
                            documentElementValue = Convert.ToDecimal(documentElementValue).ToString("C");
                        }
                    }
                }
                note = Regex.Replace(note, Regex.Escape(tag.ToString()), documentElementValue, RegexOptions.IgnoreCase);
            }
            return (T)Convert.ChangeType(note, typeof(T));
        }

        public static void SetTags(this Schema document, string note, string value, bool readOnly = false)
        {
            var elements = document.Form.Flatten().Where(a => a.AutofillKey != null && a.AutofillKey.Equals(note, System.StringComparison.OrdinalIgnoreCase)).ToList();
            foreach (var element in elements)
            {
                element.DocumentValue = value;
                element.ReadOnly = readOnly;
            }
        }

    }
}
