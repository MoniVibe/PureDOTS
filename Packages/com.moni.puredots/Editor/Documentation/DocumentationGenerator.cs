using System;
using System.IO;
using System.Linq;
using System.Text;
using PureDOTS.Editor.Reflection;
using PureDOTS.Runtime.Reflection;
using UnityEditor;
using UnityEngine;

namespace PureDOTS.Editor.Documentation
{
    /// <summary>
    /// Generates markdown documentation from Type Reflection Index.
    /// Run via menu: PureDOTS/Generate Documentation
    /// </summary>
    public static class DocumentationGenerator
    {
        private const string OutputDirectory = "Packages/com.moni.puredots/Docs/Generated";

        [MenuItem("PureDOTS/Generate Documentation")]
        public static void GenerateDocumentation()
        {
            try
            {
                // Ensure reflection index is generated first
                TypeReflectionIndexGenerator.GenerateIndex();

                // Load the index
                if (!TypeReflectionIndex.Load())
                {
                    Debug.LogError("Failed to load Type Reflection Index. Run PureDOTS/Generate Type Reflection Index first.");
                    return;
                }

                var outputPath = Path.Combine(Application.dataPath, "..", OutputDirectory);
                if (!Directory.Exists(outputPath))
                {
                    Directory.CreateDirectory(outputPath);
                }

                // Generate Components.md
                GenerateComponentsDocumentation(outputPath);

                // Generate Buffers.md
                GenerateBuffersDocumentation(outputPath);

                // Generate Systems.md
                GenerateSystemsDocumentation(outputPath);

                Debug.Log($"Documentation generated in {OutputDirectory}");
                AssetDatabase.Refresh();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to generate documentation: {ex}");
            }
        }

        private static void GenerateComponentsDocumentation(string outputPath)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# ECS Components Reference");
            sb.AppendLine();
            sb.AppendLine("Auto-generated from Type Reflection Index.");
            sb.AppendLine();
            sb.AppendLine("| Component | Namespace | Fields |");
            sb.AppendLine("|-----------|-----------|--------|");

            var components = TypeReflectionIndex.GetComponents()
                .OrderBy(c => c.Namespace)
                .ThenBy(c => c.Name);

            foreach (var component in components)
            {
                var fieldCount = component.Fields?.Count ?? 0;
                sb.AppendLine($"| `{component.Name}` | `{component.Namespace}` | {fieldCount} |");
            }

            // Detailed component information
            sb.AppendLine();
            sb.AppendLine("## Component Details");
            sb.AppendLine();

            foreach (var component in components)
            {
                sb.AppendLine($"### {component.Name}");
                sb.AppendLine();
                sb.AppendLine($"**Namespace:** `{component.Namespace}`");
                sb.AppendLine($"**Full Name:** `{component.FullName}`");
                sb.AppendLine();

                if (component.Fields != null && component.Fields.Count > 0)
                {
                    sb.AppendLine("**Fields:**");
                    sb.AppendLine();
                    sb.AppendLine("| Field | Type |");
                    sb.AppendLine("|-------|------|");

                    foreach (var field in component.Fields)
                    {
                        sb.AppendLine($"| `{field.Name}` | `{field.Type}` |");
                    }
                }

                sb.AppendLine();
            }

            File.WriteAllText(Path.Combine(outputPath, "Components.md"), sb.ToString());
        }

        private static void GenerateBuffersDocumentation(string outputPath)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# ECS Buffers Reference");
            sb.AppendLine();
            sb.AppendLine("Auto-generated from Type Reflection Index.");
            sb.AppendLine();
            sb.AppendLine("| Buffer | Namespace | Capacity | Fields |");
            sb.AppendLine("|--------|-----------|----------|--------|");

            var buffers = TypeReflectionIndex.GetBuffers()
                .OrderBy(b => b.Namespace)
                .ThenBy(b => b.Name);

            foreach (var buffer in buffers)
            {
                var fieldCount = buffer.Fields?.Count ?? 0;
                var capacity = buffer.InternalBufferCapacity > 0 ? buffer.InternalBufferCapacity.ToString() : "Dynamic";
                sb.AppendLine($"| `{buffer.Name}` | `{buffer.Namespace}` | {capacity} | {fieldCount} |");
            }

            // Detailed buffer information
            sb.AppendLine();
            sb.AppendLine("## Buffer Details");
            sb.AppendLine();

            foreach (var buffer in buffers)
            {
                sb.AppendLine($"### {buffer.Name}");
                sb.AppendLine();
                sb.AppendLine($"**Namespace:** `{buffer.Namespace}`");
                sb.AppendLine($"**Full Name:** `{buffer.FullName}`");
                if (buffer.InternalBufferCapacity > 0)
                {
                    sb.AppendLine($"**Internal Capacity:** {buffer.InternalBufferCapacity}");
                }
                sb.AppendLine();

                if (buffer.Fields != null && buffer.Fields.Count > 0)
                {
                    sb.AppendLine("**Fields:**");
                    sb.AppendLine();
                    sb.AppendLine("| Field | Type |");
                    sb.AppendLine("|-------|------|");

                    foreach (var field in buffer.Fields)
                    {
                        sb.AppendLine($"| `{field.Name}` | `{field.Type}` |");
                    }
                }

                sb.AppendLine();
            }

            File.WriteAllText(Path.Combine(outputPath, "Buffers.md"), sb.ToString());
        }

        private static void GenerateSystemsDocumentation(string outputPath)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# ECS Systems Reference");
            sb.AppendLine();
            sb.AppendLine("Auto-generated from Type Reflection Index.");
            sb.AppendLine();
            sb.AppendLine("| System | Namespace | Group | Burst |");
            sb.AppendLine("|--------|-----------|-------|-------|");

            var systems = TypeReflectionIndex.GetSystems()
                .OrderBy(s => s.UpdateInGroup ?? "")
                .ThenBy(s => s.Namespace)
                .ThenBy(s => s.Name);

            foreach (var system in systems)
            {
                var group = system.UpdateInGroup ?? "Unknown";
                var burst = system.IsBurstCompiled ? "Yes" : "No";
                sb.AppendLine($"| `{system.Name}` | `{system.Namespace}` | `{group}` | {burst} |");
            }

            // Detailed system information
            sb.AppendLine();
            sb.AppendLine("## System Details");
            sb.AppendLine();

            foreach (var system in systems)
            {
                sb.AppendLine($"### {system.Name}");
                sb.AppendLine();
                sb.AppendLine($"**Namespace:** `{system.Namespace}`");
                sb.AppendLine($"**Full Name:** `{system.FullName}`");
                
                if (!string.IsNullOrEmpty(system.UpdateInGroup))
                {
                    sb.AppendLine($"**Update Group:** `{system.UpdateInGroup}`");
                }

                if (system.OrderFirst)
                {
                    sb.AppendLine("**Order:** First");
                }
                else if (system.OrderLast)
                {
                    sb.AppendLine("**Order:** Last");
                }

                if (!string.IsNullOrEmpty(system.UpdateAfter))
                {
                    sb.AppendLine($"**Update After:** `{system.UpdateAfter}`");
                }

                if (!string.IsNullOrEmpty(system.UpdateBefore))
                {
                    sb.AppendLine($"**Update Before:** `{system.UpdateBefore}`");
                }

                if (system.IsBurstCompiled)
                {
                    sb.AppendLine("**Burst Compiled:** Yes");
                }

                sb.AppendLine();
            }

            File.WriteAllText(Path.Combine(outputPath, "Systems.md"), sb.ToString());
        }
    }
}

