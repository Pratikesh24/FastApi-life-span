using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Tekla.Structures.Geometry3d;
using Tekla.Structures.Model;

class Program
{
    private const string JsonOutputFileName = "tekla_database_structure.json";
    private const string CsvOutputFileName = "tekla_database_structure.csv";
    private const string TextOutputFileName = "tekla_database_structure.txt";

    static void Main()
    {
        var model = new Model();

        if (!model.GetConnectionStatus())
        {
            Console.WriteLine("Tekla not connected");
            return;
        }

        string outputDirectory = Environment.CurrentDirectory;
        ExtractDatabaseStructure(model, outputDirectory);

        Console.WriteLine("Database structure extracted successfully.");
        Console.WriteLine(Path.Combine(outputDirectory, JsonOutputFileName));
        Console.WriteLine(Path.Combine(outputDirectory, CsvOutputFileName));
        Console.WriteLine(Path.Combine(outputDirectory, TextOutputFileName));
    }

    private static void ExtractDatabaseStructure(Model model, string outputDirectory)
    {
        ModelObjectEnumerator.AutoFetch = true;

        var info = model.GetInfo();
        var selector = model.GetModelObjectSelector();
        var objects = selector.GetAllObjects();
        var countsByType = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var parts = new List<PartRecord>();

        while (objects.MoveNext())
        {
            var modelObject = objects.Current as ModelObject;
            if (modelObject == null)
            {
                continue;
            }

            string objectType = modelObject.GetType().Name;
            Increment(countsByType, objectType);

            var part = modelObject as Part;
            if (part != null)
            {
                parts.Add(BuildPartRecord(part, objectType));
            }
        }

        WriteJson(outputDirectory, info, countsByType, parts);
        WriteCsv(outputDirectory, parts);
        WriteText(outputDirectory, info, countsByType, parts);
    }

    private static void WriteJson(string outputDirectory, ModelInfo info, Dictionary<string, int> countsByType, List<PartRecord> parts)
    {
        var json = new StringBuilder();
        json.AppendLine("{");
        AppendProperty(json, "extractedAt", DateTime.Now.ToString("o", CultureInfo.InvariantCulture), 1, true);
        json.AppendLine(Indent(1) + "\"model\": {");
        AppendProperty(json, "name", info.ModelName, 2, true);
        AppendProperty(json, "path", info.ModelPath, 2, true);
        AppendProperty(json, "currentPhase", Convert.ToString(info.CurrentPhase, CultureInfo.InvariantCulture), 2, false);
        json.AppendLine(Indent(1) + "},");
        json.AppendLine(Indent(1) + "\"summary\": {");
        AppendProperty(json, "totalObjects", Sum(countsByType), 2, true);
        AppendProperty(json, "totalParts", parts.Count, 2, false);
        json.AppendLine(Indent(1) + "},");
        json.AppendLine(Indent(1) + "\"objectCounts\": {");
        AppendCounts(json, countsByType, 2);
        json.AppendLine(Indent(1) + "},");
        json.AppendLine(Indent(1) + "\"partSchema\": {");
        AppendProperty(json, "id", "Tekla runtime identifier", 2, true);
        AppendProperty(json, "guid", "Tekla object GUID", 2, true);
        AppendProperty(json, "type", "Tekla Open API object class", 2, true);
        AppendProperty(json, "name", "Part name", 2, true);
        AppendProperty(json, "profile", "Profile string", 2, true);
        AppendProperty(json, "material", "Material string", 2, true);
        AppendProperty(json, "class", "Part class", 2, true);
        AppendProperty(json, "finish", "Part finish", 2, true);
        AppendProperty(json, "startPoint", "Beam start point, when the part exposes one", 2, true);
        AppendProperty(json, "endPoint", "Beam end point, when the part exposes one", 2, false);
        json.AppendLine(Indent(1) + "},");
        json.AppendLine(Indent(1) + "\"parts\": [");

        for (int i = 0; i < parts.Count; i++)
        {
            json.Append(BuildPartJson(parts[i]));
            json.AppendLine(i == parts.Count - 1 ? string.Empty : ",");
        }

        json.AppendLine(Indent(1) + "]");
        json.AppendLine("}");

        string outputPath = Path.Combine(outputDirectory, JsonOutputFileName);
        File.WriteAllText(outputPath, json.ToString(), Encoding.UTF8);
    }

    private static void WriteCsv(string outputDirectory, List<PartRecord> parts)
    {
        var csv = new StringBuilder();
        csv.AppendLine("id,guid,type,name,profile,material,class,finish,start_x,start_y,start_z,end_x,end_y,end_z");

        foreach (var part in parts)
        {
            csv.Append(Csv(part.Id));
            csv.Append(",");
            csv.Append(Csv(part.Guid));
            csv.Append(",");
            csv.Append(Csv(part.Type));
            csv.Append(",");
            csv.Append(Csv(part.Name));
            csv.Append(",");
            csv.Append(Csv(part.Profile));
            csv.Append(",");
            csv.Append(Csv(part.Material));
            csv.Append(",");
            csv.Append(Csv(part.Class));
            csv.Append(",");
            csv.Append(Csv(part.Finish));
            csv.Append(",");
            csv.Append(Csv(part.StartX));
            csv.Append(",");
            csv.Append(Csv(part.StartY));
            csv.Append(",");
            csv.Append(Csv(part.StartZ));
            csv.Append(",");
            csv.Append(Csv(part.EndX));
            csv.Append(",");
            csv.Append(Csv(part.EndY));
            csv.Append(",");
            csv.AppendLine(Csv(part.EndZ));
        }

        string outputPath = Path.Combine(outputDirectory, CsvOutputFileName);
        File.WriteAllText(outputPath, csv.ToString(), Encoding.UTF8);
    }

    private static void WriteText(string outputDirectory, ModelInfo info, Dictionary<string, int> countsByType, List<PartRecord> parts)
    {
        var text = new StringBuilder();
        text.AppendLine("Tekla Database Structure");
        text.AppendLine("========================");
        text.AppendLine("Extracted At: " + DateTime.Now.ToString("o", CultureInfo.InvariantCulture));
        text.AppendLine("Model Name: " + info.ModelName);
        text.AppendLine("Model Path: " + info.ModelPath);
        text.AppendLine("Current Phase: " + Convert.ToString(info.CurrentPhase, CultureInfo.InvariantCulture));
        text.AppendLine();
        text.AppendLine("Summary");
        text.AppendLine("-------");
        text.AppendLine("Total Objects: " + Sum(countsByType).ToString(CultureInfo.InvariantCulture));
        text.AppendLine("Total Parts: " + parts.Count.ToString(CultureInfo.InvariantCulture));
        text.AppendLine();
        text.AppendLine("Object Counts");
        text.AppendLine("-------------");

        foreach (var entry in countsByType)
        {
            text.AppendLine(entry.Key + ": " + entry.Value.ToString(CultureInfo.InvariantCulture));
        }

        text.AppendLine();
        text.AppendLine("Parts");
        text.AppendLine("-----");

        foreach (var part in parts)
        {
            text.AppendLine(
                part.Id + " | " +
                part.Guid + " | " +
                part.Type + " | " +
                part.Name + " | " +
                part.Profile + " | " +
                part.Material + " | Class " +
                part.Class + " | Start (" +
                part.StartX + ", " + part.StartY + ", " + part.StartZ + ") | End (" +
                part.EndX + ", " + part.EndY + ", " + part.EndZ + ")");
        }

        string outputPath = Path.Combine(outputDirectory, TextOutputFileName);
        File.WriteAllText(outputPath, text.ToString(), Encoding.UTF8);
    }

    private static PartRecord BuildPartRecord(Part part, string objectType)
    {
        var record = new PartRecord
        {
            Id = part.Identifier.ID.ToString(CultureInfo.InvariantCulture),
            Guid = Convert.ToString(part.Identifier.GUID, CultureInfo.InvariantCulture),
            Type = objectType,
            Name = part.Name,
            Profile = SafeProfile(part),
            Material = SafeMaterial(part),
            Class = part.Class,
            Finish = part.Finish
        };

        var beam = part as Beam;
        if (beam != null)
        {
            record.StartX = FormatDouble(beam.StartPoint.X);
            record.StartY = FormatDouble(beam.StartPoint.Y);
            record.StartZ = FormatDouble(beam.StartPoint.Z);
            record.EndX = FormatDouble(beam.EndPoint.X);
            record.EndY = FormatDouble(beam.EndPoint.Y);
            record.EndZ = FormatDouble(beam.EndPoint.Z);
        }

        return record;
    }

    private static string BuildPartJson(PartRecord part)
    {
        var json = new StringBuilder();
        json.AppendLine(Indent(2) + "{");
        AppendProperty(json, "id", part.Id, 3, true);
        AppendProperty(json, "guid", part.Guid, 3, true);
        AppendProperty(json, "type", part.Type, 3, true);
        AppendProperty(json, "name", part.Name, 3, true);
        AppendProperty(json, "profile", part.Profile, 3, true);
        AppendProperty(json, "material", part.Material, 3, true);
        AppendProperty(json, "class", part.Class, 3, true);
        AppendProperty(json, "finish", part.Finish, 3, true);

        if (!string.IsNullOrEmpty(part.StartX))
        {
            AppendPoint(json, "startPoint", part.StartX, part.StartY, part.StartZ, 3, true);
            AppendPoint(json, "endPoint", part.EndX, part.EndY, part.EndZ, 3, false);
        }
        else
        {
            AppendNull(json, "startPoint", 3, true);
            AppendNull(json, "endPoint", 3, false);
        }

        json.Append(Indent(2) + "}");
        return json.ToString();
    }

    private static string SafeProfile(Part part)
    {
        return part.Profile == null ? string.Empty : part.Profile.ProfileString;
    }

    private static string SafeMaterial(Part part)
    {
        return part.Material == null ? string.Empty : part.Material.MaterialString;
    }

    private static void Increment(Dictionary<string, int> counts, string key)
    {
        int current;
        counts.TryGetValue(key, out current);
        counts[key] = current + 1;
    }

    private static int Sum(Dictionary<string, int> counts)
    {
        int total = 0;
        foreach (int count in counts.Values)
        {
            total += count;
        }

        return total;
    }

    private static void AppendCounts(StringBuilder json, Dictionary<string, int> counts, int level)
    {
        int index = 0;
        foreach (var entry in counts)
        {
            index++;
            AppendProperty(json, entry.Key, entry.Value, level, index < counts.Count);
        }
    }

    private static void AppendProperty(StringBuilder json, string name, string value, int level, bool comma)
    {
        json.Append(Indent(level));
        json.Append("\"");
        json.Append(Escape(name));
        json.Append("\": \"");
        json.Append(Escape(value));
        json.Append("\"");
        json.AppendLine(comma ? "," : string.Empty);
    }

    private static void AppendProperty(StringBuilder json, string name, int value, int level, bool comma)
    {
        json.Append(Indent(level));
        json.Append("\"");
        json.Append(Escape(name));
        json.Append("\": ");
        json.Append(value.ToString(CultureInfo.InvariantCulture));
        json.AppendLine(comma ? "," : string.Empty);
    }

    private static void AppendPoint(StringBuilder json, string name, string x, string y, string z, int level, bool comma)
    {
        json.Append(Indent(level));
        json.Append("\"");
        json.Append(Escape(name));
        json.Append("\": { ");
        json.Append("\"x\": ");
        json.Append(x);
        json.Append(", \"y\": ");
        json.Append(y);
        json.Append(", \"z\": ");
        json.Append(z);
        json.Append(" }");
        json.AppendLine(comma ? "," : string.Empty);
    }

    private static void AppendNull(StringBuilder json, string name, int level, bool comma)
    {
        json.Append(Indent(level));
        json.Append("\"");
        json.Append(Escape(name));
        json.Append("\": null");
        json.AppendLine(comma ? "," : string.Empty);
    }

    private static string FormatDouble(double value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private static string Indent(int level)
    {
        return new string(' ', level * 2);
    }

    private static string Escape(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\r", "\\r")
            .Replace("\n", "\\n")
            .Replace("\t", "\\t");
    }

    private static string Csv(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        bool mustQuote = value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0;
        string escaped = value.Replace("\"", "\"\"");
        return mustQuote ? "\"" + escaped + "\"" : escaped;
    }

    private class PartRecord
    {
        public string Id;
        public string Guid;
        public string Type;
        public string Name;
        public string Profile;
        public string Material;
        public string Class;
        public string Finish;
        public string StartX;
        public string StartY;
        public string StartZ;
        public string EndX;
        public string EndY;
        public string EndZ;
    }
}
