using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MCPForUnity.Editor.Data;
using MCPForUnity.Editor.Helpers;
using MCPForUnity.Editor.Tools;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace PureDOTS.Editor.Mcp
{
    [McpForUnityTool("generate_mcp_tool")]
    public static class McpToolGenerator
    {
        private const string DefaultPythonDirectory = "Assets/Editor/MCP/Python";
        private const string DefaultCSharpDirectory = "Assets/Editor/MCP/Handlers";
        private const string DefaultPythonToolsAssetPath = "Assets/Editor/MCP/McpPythonTools.asset";
        private const string DefaultNamespace = "PureDOTS.Editor.McpTools";

        public static object HandleCommand(JObject @params)
        {
            try
            {
                if (@params == null)
                {
                    return Response.Error("missing_params");
                }

                string toolId = @params.Value<string>("tool_id")?.Trim();
                if (string.IsNullOrEmpty(toolId))
                {
                    return Response.Error("tool_id_missing");
                }

                if (!IsValidToolId(toolId))
                {
                    return Response.Error("tool_id_invalid", new { toolId });
                }

                string description = @params.Value<string>("description")?.Trim();
                if (string.IsNullOrEmpty(description))
                {
                    description = $"Custom MCP tool {toolId}";
                }

                bool overwrite = @params.Value<bool?>("overwrite") ?? false;
                bool overwritePython = @params.Value<bool?>("overwrite_python") ?? overwrite;
                bool overwriteCSharp = @params.Value<bool?>("overwrite_csharp") ?? overwrite;
                bool autoAddPython = @params.Value<bool?>("add_to_python_tools_asset") ?? true;
                bool triggerSync = @params.Value<bool?>("trigger_sync") ?? false;

                string pythonDirectory = NormalizeDirectory(@params.Value<string>("python_directory")) ?? DefaultPythonDirectory;
                string pythonFileName = @params.Value<string>("python_file_name");
                string pythonPath = NormalizeAssetPath(@params.Value<string>("python_path")) ??
                                    CombineAssetPath(pythonDirectory, pythonFileName ?? $"{toolId}.py");
                if (!pythonPath.EndsWith(".py", StringComparison.OrdinalIgnoreCase))
                {
                    pythonPath += ".py";
                }

                string className = @params.Value<string>("class_name")?.Trim();
                if (string.IsNullOrEmpty(className))
                {
                    className = ToPascalCase(toolId) + "Tool";
                }
                else if (!IsValidIdentifier(className))
                {
                    return Response.Error("class_name_invalid", new { className });
                }

                string namespaceValue = @params.Value<string>("namespace")?.Trim();
                if (string.IsNullOrEmpty(namespaceValue))
                {
                    namespaceValue = DefaultNamespace;
                }
                else if (!IsValidNamespace(namespaceValue))
                {
                    return Response.Error("namespace_invalid", new { @namespace = namespaceValue });
                }

                string csharpDirectory = NormalizeDirectory(@params.Value<string>("csharp_directory")) ?? DefaultCSharpDirectory;
                string csharpFileName = @params.Value<string>("csharp_file_name");
                string csharpPath = NormalizeAssetPath(@params.Value<string>("csharp_path")) ??
                                    CombineAssetPath(csharpDirectory, csharpFileName ?? $"{className}.cs");
                if (!csharpPath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                {
                    csharpPath += ".cs";
                }

                string pythonToolsAssetPath =
                    NormalizeAssetPath(@params.Value<string>("python_tools_asset_path")) ?? DefaultPythonToolsAssetPath;

                var parameters = ParseParameters(@params);

                var pythonContent = GeneratePythonContent(toolId, description, parameters);
                var csharpContent = GenerateCSharpContent(toolId, className, namespaceValue, description, parameters);

                var pythonWrite = WriteTextAsset(pythonPath, pythonContent, overwritePython);
                if (!pythonWrite.Success)
                {
                    return Response.Error(pythonWrite.ErrorCode, pythonWrite.Payload);
                }

                var csharpWrite = WriteTextAsset(csharpPath, csharpContent, overwriteCSharp);
                if (!csharpWrite.Success)
                {
                    return Response.Error(csharpWrite.ErrorCode, csharpWrite.Payload);
                }

                var addedPythonFiles = Array.Empty<string>();
                if (autoAddPython)
                {
                    var assetResult = EnsurePythonToolsAsset(pythonToolsAssetPath, pythonPath);
                    if (!assetResult.Success)
                    {
                        return Response.Error(assetResult.ErrorCode, assetResult.Payload);
                    }

                    addedPythonFiles = assetResult.AddedFiles.ToArray();
                }

                AssetDatabase.ImportAsset(pythonPath, ImportAssetOptions.ForceUpdate);
                AssetDatabase.ImportAsset(csharpPath, ImportAssetOptions.ForceUpdate);
                AssetDatabase.SaveAssets();

                if (triggerSync)
                {
                    PythonToolSyncProcessor.SyncAllTools();
                }

                var summary = new
                {
                    toolId,
                    description,
                    pythonPath,
                    csharpPath,
                    className,
                    @namespace = namespaceValue,
                    pythonToolsAssetPath,
                    parameters = parameters.Select(p => p.ToSummary()).ToArray(),
                    addedPythonFiles
                };

                return Response.Success($"Generated MCP scaffolding for {toolId}.", summary);
            }
            catch (Exception ex)
            {
                return Response.Error(
                    "generate_mcp_tool_exception",
                    new { message = ex.Message, stack = ex.StackTrace });
            }
        }

        private static IReadOnlyList<ParameterSpec> ParseParameters(JObject @params)
        {
            if (!@params.TryGetValue("parameters", out JToken token) || token == null || token.Type == JTokenType.Null)
            {
                return Array.Empty<ParameterSpec>();
            }

            var result = new List<ParameterSpec>();
            if (token is JArray array)
            {
                foreach (JToken entry in array)
                {
                    if (entry is not JObject obj)
                    {
                        throw new ArgumentException("parameters entries must be objects");
                    }

                    result.Add(ParameterSpec.FromJson(obj));
                }
            }
            else if (token is JObject single)
            {
                result.Add(ParameterSpec.FromJson(single));
            }
            else
            {
                throw new ArgumentException("parameters must be array or object");
            }

            return result;
        }

        private static string GeneratePythonContent(
            string toolId,
            string description,
            IReadOnlyList<ParameterSpec> parameters)
        {
            var builder = new StringBuilder();
            builder.AppendLine("from typing import Annotated, Any");
            builder.AppendLine();
            builder.AppendLine("from fastmcp import Context");
            builder.AppendLine("from registry import mcp_for_unity_tool");
            builder.AppendLine("from unity_connection import send_command_with_retry");
            builder.AppendLine();
            builder.AppendLine();
            builder.AppendLine($"@mcp_for_unity_tool(description=\"{EscapePythonString(description)}\")");
            builder.AppendLine($"async def {toolId}(");
            builder.AppendLine("    ctx: Context,");

            foreach (ParameterSpec parameter in parameters)
            {
                builder.Append("    ");
                builder.Append(parameter.GetPythonSignature());
                builder.AppendLine(",");
            }

            builder.AppendLine(") -> dict[str, Any]:");
            builder.AppendLine($"    await ctx.info(f\"Invoking {toolId}\")");
            builder.AppendLine();
            builder.AppendLine("    params = {");
            foreach (ParameterSpec parameter in parameters)
            {
                builder.AppendLine($"        \"{parameter.JsonName}\": {parameter.PythonIdentifier},");
            }

            builder.AppendLine("    }");
            builder.AppendLine("    params = {k: v for k, v in params.items() if v is not None}");
            builder.AppendLine();
            builder.AppendLine($"    response = send_command_with_retry(\"{toolId}\", params)");
            builder.AppendLine("    if isinstance(response, dict):");
            builder.AppendLine("        return response");
            builder.AppendLine();
            builder.AppendLine("    return {\"success\": bool(response), \"payload\": str(response)}");

            return builder.ToString();
        }

        private static string GenerateCSharpContent(
            string toolId,
            string className,
            string namespaceValue,
            string description,
            IReadOnlyList<ParameterSpec> parameters)
        {
            var builder = new StringBuilder();
            builder.AppendLine("using System;");
            builder.AppendLine("using MCPForUnity.Editor.Helpers;");
            builder.AppendLine("using Newtonsoft.Json.Linq;");
            builder.AppendLine();
            builder.AppendLine($"namespace {namespaceValue}");
            builder.AppendLine("{");
            builder.AppendLine($"    /// <summary>");
            builder.AppendLine($"    /// {EscapeXmlComment(description)}");
            builder.AppendLine("    /// </summary>");
            builder.AppendLine($"    [McpForUnityTool(\"{toolId}\")]");
            builder.AppendLine($"    public static class {className}");
            builder.AppendLine("    {");
            builder.AppendLine("        public static object HandleCommand(JObject @params)");
            builder.AppendLine("        {");
            builder.AppendLine("            if (@params == null)");
            builder.AppendLine("            {");
            builder.AppendLine("                return Response.Error(\"missing_params\");");
            builder.AppendLine("            }");
            builder.AppendLine();

            foreach (ParameterSpec parameter in parameters)
            {
                foreach (string line in parameter.GetCSharpHandlingLines())
                {
                    builder.AppendLine(line);
                }

                builder.AppendLine();
            }

            builder.AppendLine("            try");
            builder.AppendLine("            {");
            builder.AppendLine("                // TODO: Implement Unity logic for this tool.");
            builder.AppendLine($"                return Response.Success(\"{toolId} executed successfully.\");");
            builder.AppendLine("            }");
            builder.AppendLine("            catch (Exception ex)");
            builder.AppendLine("            {");
            builder.AppendLine("                return Response.Error(");
            builder.AppendLine($"                    \"{toolId}_exception\",");
            builder.AppendLine("                    new { message = ex.Message, stack = ex.StackTrace });");
            builder.AppendLine("            }");
            builder.AppendLine("        }");
            builder.AppendLine("    }");
            builder.AppendLine("}");

            return builder.ToString();
        }

        private static FileWriteResult WriteTextAsset(string assetPath, string content, bool overwrite)
        {
            if (string.IsNullOrEmpty(assetPath) || !assetPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            {
                return FileWriteResult.Failed("asset_path_invalid", new { assetPath });
            }

            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string fullPath = Path.GetFullPath(Path.Combine(projectRoot, assetPath));

            Directory.CreateDirectory(Path.GetDirectoryName(fullPath) ?? projectRoot);

            bool exists = File.Exists(fullPath);
            if (exists)
            {
                string existing = File.ReadAllText(fullPath);
                if (existing == content)
                {
                    return FileWriteResult.Unchanged(assetPath);
                }

                if (!overwrite)
                {
                    return FileWriteResult.Failed("asset_exists", new { assetPath });
                }
            }

            File.WriteAllText(fullPath, content, new UTF8Encoding(false));
            return FileWriteResult.Written(assetPath, !exists);
        }

        private static AssetUpdateResult EnsurePythonToolsAsset(string assetPath, string pythonFilePath)
        {
            if (string.IsNullOrEmpty(assetPath) || !assetPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            {
                return AssetUpdateResult.Failed("python_asset_path_invalid", new { assetPath });
            }

            if (string.IsNullOrEmpty(pythonFilePath))
            {
                return AssetUpdateResult.Failed("python_file_path_missing", new { pythonFilePath });
            }

            string directory = Path.GetDirectoryName(assetPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var asset = AssetDatabase.LoadAssetAtPath<PythonToolsAsset>(assetPath);
            bool created = false;
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<PythonToolsAsset>();
                AssetDatabase.CreateAsset(asset, assetPath);
                created = true;
            }

            AssetDatabase.ImportAsset(pythonFilePath, ImportAssetOptions.ForceUpdate);
            var textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(pythonFilePath);
            if (textAsset == null)
            {
                return AssetUpdateResult.Failed("python_file_not_imported", new { pythonFilePath });
            }

            bool added = false;
            if (!asset.pythonFiles.Contains(textAsset))
            {
                asset.pythonFiles.Add(textAsset);
                added = true;
                EditorUtility.SetDirty(asset);
                AssetDatabase.SaveAssets();
            }

            return AssetUpdateResult.Successful(created, added ? new[] { pythonFilePath } : Array.Empty<string>());
        }

        private static string NormalizeDirectory(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var normalized = value.Replace("\\", "/").TrimEnd('/');
            if (!normalized.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            {
                normalized = "Assets/" + normalized.TrimStart('/');
            }

            return normalized;
        }

        private static string NormalizeAssetPath(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var normalized = value.Replace("\\", "/").Trim();
            if (!normalized.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            {
                normalized = "Assets/" + normalized.TrimStart('/');
            }

            return normalized;
        }

        private static string CombineAssetPath(string directory, string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                throw new ArgumentException("fileName cannot be empty", nameof(fileName));
            }

            directory ??= "Assets";
            directory = directory.TrimEnd('/');
            return $"{directory}/{fileName}";
        }

        private static bool IsValidToolId(string value)
        {
            return value.All(ch => char.IsLower(ch) || char.IsDigit(ch) || ch == '_' || ch == '-');
        }

        private static bool IsValidIdentifier(string value)
        {
            if (string.IsNullOrEmpty(value) || !(char.IsLetter(value[0]) || value[0] == '_'))
            {
                return false;
            }

            return value.All(ch => char.IsLetterOrDigit(ch) || ch == '_');
        }

        private static bool IsValidNamespace(string value)
        {
            var segments = value.Split('.');
            return segments.Length > 0 && segments.All(IsValidIdentifier);
        }

        private static string ToPascalCase(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            var segments = value.Replace('-', '_').Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
            var builder = new StringBuilder();
            foreach (string segment in segments)
            {
                if (segment.Length == 0)
                {
                    continue;
                }

                builder.Append(char.ToUpperInvariant(segment[0]));
                if (segment.Length > 1)
                {
                    builder.Append(segment.Substring(1));
                }
            }

            return builder.ToString();
        }

        private static string EscapePythonString(string value)
        {
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private static string EscapeXmlComment(string value)
        {
            return value.Replace("--", "- -");
        }

        private sealed class ParameterSpec
        {
            private ParameterSpec(
                string jsonName,
                string pythonIdentifier,
                string pythonAnnotation,
                string pythonDefaultSuffix,
                string description,
                string csharpVariableName,
                string csharpType,
                string csharpNullableType,
                bool required,
                bool usesNullableValue)
            {
                JsonName = jsonName;
                PythonIdentifier = pythonIdentifier;
                PythonAnnotation = pythonAnnotation;
                PythonDefaultSuffix = pythonDefaultSuffix;
                Description = description;
                CSharpVariableName = csharpVariableName;
                CSharpType = csharpType;
                CSharpNullableType = csharpNullableType;
                Required = required;
                UsesNullableValue = usesNullableValue;
            }

            public string JsonName { get; }
            public string PythonIdentifier { get; }
            public string PythonAnnotation { get; }
            public string PythonDefaultSuffix { get; }
            public string Description { get; }
            public string CSharpVariableName { get; }
            public string CSharpType { get; }
            public string CSharpNullableType { get; }
            public bool Required { get; }
            public bool UsesNullableValue { get; }

            public string GetPythonSignature()
            {
                string descriptionPart = string.IsNullOrEmpty(Description)
                    ? string.Empty
                    : $", \"{EscapePythonString(Description)}\"";
                return $"{PythonIdentifier}: Annotated[{PythonAnnotation}{descriptionPart}]{PythonDefaultSuffix}";
            }

            public IEnumerable<string> GetCSharpHandlingLines()
            {
                string indent = "            ";

                if (CSharpType == "string")
                {
                    yield return $"{indent}string {CSharpVariableName} = @params.Value<string>(\"{JsonName}\");";
                    if (Required)
                    {
                        yield return $"{indent}if (string.IsNullOrWhiteSpace({CSharpVariableName}))";
                        yield return $"{indent}{{";
                        yield return $"{indent}    return Response.Error(\"{JsonName}_required\");";
                        yield return $"{indent}}}";
                    }
                    yield break;
                }

                if (UsesNullableValue)
                {
                    string tempName = CSharpVariableName + "Value";
                    yield return $"{indent}{CSharpNullableType} {tempName} = @params.Value<{CSharpNullableType}>(\"{JsonName}\");";
                    if (Required)
                    {
                        yield return $"{indent}if (!{tempName}.HasValue)";
                        yield return $"{indent}{{";
                        yield return $"{indent}    return Response.Error(\"{JsonName}_required\");";
                        yield return $"{indent}}}";
                        yield return $"{indent}{CSharpType} {CSharpVariableName} = {tempName}.Value;";
                    }
                    else
                    {
                        yield return $"{indent}{CSharpNullableType} {CSharpVariableName} = {tempName};";
                    }
                    yield break;
                }

                string tokenName = CSharpVariableName + "Token";
                yield return $"{indent}{CSharpType} {tokenName} = @params[\"{JsonName}\"];";
                if (Required)
                {
                    yield return $"{indent}if ({tokenName} == null)";
                    yield return $"{indent}{{";
                    yield return $"{indent}    return Response.Error(\"{JsonName}_required\");";
                    yield return $"{indent}}}";
                }

                yield return $"{indent}{CSharpType} {CSharpVariableName} = {tokenName};";
            }

            public object ToSummary()
            {
                return new
                {
                    jsonName = JsonName,
                    required = Required,
                    pythonAnnotation = PythonAnnotation,
                    csharpType = CSharpType
                };
            }

            public static ParameterSpec FromJson(JObject obj)
            {
                string name = obj.Value<string>("name") ?? obj.Value<string>("id") ?? obj.Value<string>("json") ?? string.Empty;
                name = name.Trim();
                if (string.IsNullOrEmpty(name))
                {
                    throw new ArgumentException("parameter name is required");
                }

                if (!IsValidParameterName(name))
                {
                    throw new ArgumentException($"invalid parameter name '{name}'");
                }

                string pythonIdentifier = name;
                string pythonType = (obj.Value<string>("python_type") ?? obj.Value<string>("type") ?? "str").Trim().ToLowerInvariant();
                bool required = obj.Value<bool?>("required") ?? true;
                string description = obj.Value<string>("description")?.Trim() ?? string.Empty;
                string pythonAnnotation;
                switch (pythonType)
                {
                    case "str":
                    case "string":
                        pythonAnnotation = "str";
                        break;
                    case "int":
                        pythonAnnotation = "int";
                        break;
                    case "float":
                    case "double":
                        pythonAnnotation = "float";
                        break;
                    case "bool":
                        pythonAnnotation = "bool";
                        break;
                    case "dict":
                        pythonAnnotation = "dict[str, Any]";
                        break;
                    default:
                        pythonAnnotation = "Any";
                        break;
                }

                string pythonDefaultSuffix = string.Empty;
                string defaultExpression = obj.Value<string>("python_default") ?? obj.Value<string>("default_value");
                if (!required)
                {
                    if (!string.IsNullOrEmpty(defaultExpression))
                    {
                        pythonDefaultSuffix = $" = {defaultExpression}";
                    }
                    else
                    {
                        pythonDefaultSuffix = " = None";
                        pythonAnnotation += " | None";
                    }
                }

                string csharpType = (obj.Value<string>("csharp_type") ?? string.Empty).Trim();
                if (string.IsNullOrEmpty(csharpType))
                {
                    switch (pythonType)
                    {
                        case "str":
                        case "string":
                            csharpType = "string";
                            break;
                        case "int":
                            csharpType = "int";
                            break;
                        case "float":
                        case "double":
                            csharpType = "double";
                            break;
                        case "bool":
                            csharpType = "bool";
                            break;
                        default:
                            csharpType = "JToken";
                            break;
                    }
                }

                string csharpVariableName = ToCamelCase(name);
                string csharpNullableType;
                switch (csharpType)
                {
                    case "int":
                        csharpNullableType = "int?";
                        break;
                    case "double":
                        csharpNullableType = "double?";
                        break;
                    case "float":
                        csharpNullableType = "double?";
                        break;
                    case "bool":
                        csharpNullableType = "bool?";
                        break;
                    case "long":
                        csharpNullableType = "long?";
                        break;
                    default:
                        csharpNullableType = csharpType;
                        break;
                }

                bool usesNullableValue = csharpType == "int" ||
                                         csharpType == "double" ||
                                         csharpType == "bool" ||
                                         csharpType == "long";

                return new ParameterSpec(
                    name,
                    pythonIdentifier,
                    pythonAnnotation,
                    pythonDefaultSuffix,
                    description,
                    csharpVariableName,
                    csharpType,
                    csharpNullableType,
                    required,
                    usesNullableValue);
            }

            private static bool IsValidParameterName(string value)
            {
                if (string.IsNullOrEmpty(value) || !(char.IsLetter(value[0]) || value[0] == '_'))
                {
                    return false;
                }

                return value.All(ch => char.IsLetterOrDigit(ch) || ch == '_');
            }

            private static string ToCamelCase(string value)
            {
                if (string.IsNullOrEmpty(value))
                {
                    return value;
                }

                if (!value.Contains('_') && !value.Contains('-'))
                {
                    return char.ToLowerInvariant(value[0]) + value.Substring(1);
                }

                return string.Concat(value.Replace('-', '_')
                    .Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select((segment, index) =>
                        index == 0
                            ? segment.ToLowerInvariant()
                            : char.ToUpperInvariant(segment[0]) + segment.Substring(1)));
            }
        }

        private struct FileWriteResult
        {
            private FileWriteResult(bool success, bool created, string assetPath, string errorCode, object payload)
            {
                Success = success;
                Created = created;
                AssetPath = assetPath;
                ErrorCode = errorCode;
                Payload = payload;
            }

            public bool Success { get; }
            public bool Created { get; }
            public string AssetPath { get; }
            public string ErrorCode { get; }
            public object Payload { get; }

            public static FileWriteResult Written(string assetPath, bool created)
            {
                return new FileWriteResult(true, created, assetPath, null, null);
            }

            public static FileWriteResult Unchanged(string assetPath)
            {
                return new FileWriteResult(true, false, assetPath, null, null);
            }

            public static FileWriteResult Failed(string errorCode, object payload)
            {
                return new FileWriteResult(false, false, null, errorCode, payload);
            }
        }

        private struct AssetUpdateResult
        {
            private AssetUpdateResult(bool success, bool created, IReadOnlyCollection<string> addedFiles, string errorCode, object payload)
            {
                Success = success;
                Created = created;
                AddedFiles = addedFiles;
                ErrorCode = errorCode;
                Payload = payload;
            }

            public bool Success { get; }
            public bool Created { get; }
            public IReadOnlyCollection<string> AddedFiles { get; }
            public string ErrorCode { get; }
            public object Payload { get; }

            public static AssetUpdateResult Successful(bool created, IReadOnlyCollection<string> addedFiles)
            {
                return new AssetUpdateResult(true, created, addedFiles, null, null);
            }

            public static AssetUpdateResult Failed(string errorCode, object payload)
            {
                return new AssetUpdateResult(false, false, Array.Empty<string>(), errorCode, payload);
            }
        }
    }
}
