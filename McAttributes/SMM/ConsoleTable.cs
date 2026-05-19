using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace SMM {
    public class ConsoleTable {
        public string[] Headers { get; private set; }
        public int MaxColumnWidth { get; private set; } = 30;

        public ConsoleTable(string[] headers, int maxColumnWidth = 30) {
            Headers = headers;
            MaxColumnWidth = maxColumnWidth;
        }

        public ConsoleTable() {
            Headers = Array.Empty<string>();
        }

        public ConsoleTable(int maxColumnWidth) {
            MaxColumnWidth = maxColumnWidth;
        }

        public void InferHeaders(object Item) {
            Headers = Item.GetType().GetProperties().Select(p => p.Name).ToArray();
        }

        public void DumpTable<T>(IEnumerable<T> items) {
            Headers = typeof(T).GetProperties().Select(p => p.Name).ToArray();
            DumpTable(items.Cast<object>());
        }

        private void _dump(Action<string> action, IEnumerable<object> items) {
            if (Headers.Length == 0 && items.Any()) {
                InferHeaders(items.First());
            }
            // Compute column widths based on headers and data
            Dictionary<string, int> columnWidths = Headers.ToDictionary(h => h, h => h.Length);
            foreach (var h in Headers) {
                foreach (var item in items.Take(Math.Min(10, items.Count()))) {
                    var value = Extensions.FormatProperty(item.GetType().GetProperty(h)?.GetValue(item));
                    if (value.Length > columnWidths[h]) {
                        columnWidths[h] = value.Length;
                    }
                }
            }

            // Print headers
            string headerLine = string.Join(" | ", Headers.Select(h => h.PadRight(columnWidths[h])));
            action(headerLine);
            var headerSegments = Headers.Take(1).Select(h => new string('-', columnWidths[h] + 1)).Concat(Headers.Skip(1).Select(h => new string('-', columnWidths[h] + 2)));
            action(string.Join("+", headerSegments)); // Simple separator the length of all columns plus the separators
            // Print rows
            foreach (var item in items) {
                var values = Headers.Select(h => Extensions.FormatProperty(item.GetType().GetProperty(h)?.GetValue(item) ?? String.Empty).PadRight(columnWidths[h]));
                action(string.Join(" | ", values));
            }
        }

        public void DumpTable(IEnumerable<object> items) {
            _dump(Console.WriteLine, items);
        }

        public string FormatTable(int Width, IEnumerable<object> items) {
            var sb = new StringBuilder();
            _dump((x) => { sb.AppendLine(x); }, items);
            return sb.ToString();
        }

        public string FormatHtmlTable(IEnumerable<object> items) {
            var sb = new StringBuilder();
            sb.AppendLine("<table>");
            sb.AppendLine("  <tr>");
            foreach (var header in Headers) {
                sb.AppendLine($"    <th>{header}</th>");
            }
            sb.AppendLine("  </tr>");
            foreach (var item in items) {
                sb.AppendLine("  <tr>");
                foreach (var header in Headers) {
                    var value = Extensions.FormatProperty(item.GetType().GetProperty(header)?.GetValue(item));
                    sb.AppendLine($"    <td>{value}</td>");
                }
                sb.AppendLine("  </tr>");
            }
            sb.AppendLine("</table>");
            return sb.ToString();
        }

        public static string FormatHtmlList(IEnumerable<object> items, string style) {
            var sb = new StringBuilder();
            sb.AppendLine($"<ul{(string.IsNullOrEmpty(style) ? "" : $" style=\"{style}\"")}>");
            foreach (var item in items) {
                sb.AppendLine("  <li>");
                sb.AppendLine("    <ul>");
                var properties = item.GetType().GetProperties();
                foreach (var p in properties) {
                    var value = Extensions.FormatProperty(p.GetValue(item));
                    sb.AppendLine($"      <li><strong>{p.Name}:</strong> {value}</li>");
                }
                sb.AppendLine("    </ul>");
                sb.AppendLine("  </li>");
            }
            sb.AppendLine("</ul>");
            return sb.ToString();
        }

        public void DumpList(IEnumerable<object> items) {
            // Format each item as a key-value list rather than a table. This is more for debugging than anything else.
            Console.WriteLine(FormatList(items));
        }

        public static string FormatList(object items) {
            return Extensions.ToKvText(items);
        }

        public static string FormatList(IEnumerable<object> items) {
            // Format each item as a key-value list rather than a table. This is more for debugging than anything else.
            var sb = new StringBuilder();
            foreach (var item in items) {
                sb.AppendLine(Extensions.ToKvText(item));
                sb.AppendLine(new string('-', 40)); // Separator between items
            }
            return sb.ToString();
        }

        public static ConsoleTable FromItems<T>(IEnumerable<T> items, int maxColumnWidth = 30) {
            var table = new ConsoleTable(maxColumnWidth: maxColumnWidth);
            if (items.Any()) {
                table.InferHeaders(items.First());
            }
            return table;
        }
    }

    public static partial class Extensions {
        public static object Dump(this object thing, params string[]? msgs) {
            if (msgs != null && msgs.Length > 0) {
                Console.WriteLine("> {0}\n", string.Join(", ", msgs));
            }

            var thingType = thing.GetType();
            var isEnumerable = thingType.GetInterface("IEnumerable");

            if (thing is String) {
                Console.WriteLine(thing);
            }
            else if (thing is Expression ex) {
                Console.WriteLine(ex.ToString());
            }
            else if (isEnumerable != null) {
                var thingEnumerable = (System.Collections.IEnumerable)thing;
                var count = 0;
                foreach (var _ in thingEnumerable) count++;

                if ((count < 1)) goto end;


                var thingEnum = thingEnumerable.GetEnumerator();
                if (!thingEnum.MoveNext()) goto end;

                var table = ConsoleTable.FromItems(thingEnumerable.Cast<object>());
                table.DumpTable(thingEnumerable.Cast<object>());
            }
            else {
                Console.WriteLine(ToKvText(thing));
            }

        end:
            return thing;
        }

        public static string ToKvText(this object entry) {
            if (entry == null) return string.Empty;

            StringBuilder sb = new StringBuilder();

            var properties = entry.GetType().GetProperties();
            var longestPropertyLength = properties.Select(x => x.Name.Length).Max();
            int indentation = longestPropertyLength + 3;

            foreach (var p in properties) {
                sb.Append(p.Name.PadRight(longestPropertyLength));
                sb.Append(" : ");
                bool multiLine = false;
                var thing = p.GetValue(entry);
                if (thing is string) {
                    foreach (var value in (thing ?? "NULL").ToString().Split("\n")) {
                        var strValue = FormatProperty((value) ?? "NULL");
                        if (multiLine) {
                            sb.AppendLine((strValue).PadLeft(indentation + strValue.Length));
                        }
                        else {
                            sb.AppendLine((strValue).PadRight(longestPropertyLength));
                            multiLine = true;
                        }
                    }
                }
                else {
                    var value = FormatProperty(thing);
                    sb.AppendLine((value).PadLeft(value.Length));
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Format properties for being printed inside a table. For array objects with fewer than 4
        /// elements, wrap em in square brackets and comma delimit them. 4 or more, take the first
        /// three that way then append ellipsis and the number of elements. This is more for
        /// debugging than anything else.
        /// </summary>
        /// <param name="Property"></param>
        /// <returns></returns>
        internal static string FormatProperty(dynamic Property) {
            if (Property == null) return "*NULL";

            if (Property is String s) return s;

            if (Property.GetType().GetInterface("IEnumerable") != null) {
                if (Enumerable.Count(Property) <= 5) {
                    return $"[{String.Join(", ", Property)}]";
                }
                else {
                    var elems = new List<string>();
                    var count = 0;
                    foreach (var el in Property) {
                        count++;
                        elems.Add(el.ToString());
                        if (count >= 3) break;
                    }

                    return $"[{String.Join(", ", elems)}...({Enumerable.Count(Property)})]";
                }
            }

            return Property?.ToString();
        }
    }
}
