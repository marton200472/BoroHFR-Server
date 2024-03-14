using System.IO;
using System.Security.Cryptography.Xml;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace BoroHFR.Configuration;

public class AppSettingsDocument
{
    private JsonNode? _doc;
    private string _path;

    public JsonNode? this[string key] { get => GetValue(key); set => SetValue(key, value); }

    public AppSettingsDocument()
    { _path = "appsettings.json"; }

    public AppSettingsDocument(string path)
    {
        _path = path;
        if (!File.Exists(path))
        {
            _doc = new JsonObject();
            return;
        }
        using FileStream file = new(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        _doc = JsonNode.Parse(file,
            new() { PropertyNameCaseInsensitive = true },
            new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip });

    }

    private JsonNode? GetValue(string key)
    {
        return GetParentNode(key, false, out string targetProperty)?[targetProperty];
    }


    private void SetValue(string key, JsonNode? value)
    {
        GetParentNode(key, true, out string? targetProperty)![targetProperty] = value;
    }

    private JsonNode? GetParentNode(string key, bool create, out string targetProperty)
    {
        JsonNode? node;
        string[] props = key.Split(':');
        targetProperty = props[^1];
        _doc ??= new JsonObject();
        JsonNode parent = _doc.Root;
        for (int i = 0; i < props.Length - 1; i++)
        {
            node = parent[props[i]];
            if (node is null)
            {
                if (create)
                {
                    node = new JsonObject();
                    parent[props[i]] = node;
                }
                else
                {
                    return null;
                }
            }
            parent = node;
        }
        return parent;
    }

    public void Save()
    {
        using FileStream file = new(_path, FileMode.Create, FileAccess.Write, FileShare.None);
        using Utf8JsonWriter jsonWriter = new(file, new JsonWriterOptions() { Indented = true });
        _doc ??= new JsonObject();
        _doc.WriteTo(jsonWriter);
    }

}