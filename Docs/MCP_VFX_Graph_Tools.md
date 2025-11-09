# MCP VFX Graph Tools Documentation

This document describes the MCP tools available for manipulating Unity Visual Effect (VFX) Graphs programmatically.

## Overview

The VFX Graph tools allow you to:
- Discover available node types (contexts, operators, parameters)
- Add, remove, and move nodes
- Connect and disconnect nodes
- Modify node properties
- Inspect graph structure

All tools use reflection to access Unity's internal VFX Graph editor APIs, making them compatible across Unity versions while maintaining safety.

## Tool Reference

### `list_graph_variants`

Lists available VFX graph node types (variants) that can be added to graphs.

**Parameters:**
- `category` (optional): Filter by category ("Context", "Operator", "Parameter")
- `search` (optional): Search term to filter variants by name or identifier
- `limit` (optional): Maximum number of variants to return (default: 500)

**Example:**
```json
{
  "category": "Operator",
  "search": "multiply",
  "limit": 50
}
```

**Response:**
```json
{
  "success": true,
  "message": "Found 15 VFX variants",
  "data": {
    "count": 15,
    "variants": [
      {
        "identifier": "Multiply",
        "name": "Multiply",
        "category": "Operator",
        "modelType": "UnityEditor.VFX.VFXOperator",
        "synonyms": ["mult", "mul"]
      }
    ]
  }
}
```

### `add_node_to_graph`

Adds a node to a VFX graph at a specified position.

**Parameters:**
- `graph_path` (required): Path to the VFX graph asset (e.g., "Assets/VFX/MyEffect.vfx")
- `node_type` (required): Node type identifier (use `list_graph_variants` to discover)
- `position_x` (required): X position for the node
- `position_y` (required): Y position for the node
- `properties` (optional): Dictionary of initial property values

**Example:**
```json
{
  "graph_path": "Assets/VFX/MyEffect.vfx",
  "node_type": "Multiply",
  "position_x": 100.0,
  "position_y": 200.0,
  "properties": {
    "value": 2.0
  }
}
```

**Response:**
```json
{
  "success": true,
  "message": "Node Multiply added to graph",
  "data": {
    "graphPath": "Assets/VFX/MyEffect.vfx",
    "node": {
      "id": 12345,
      "name": "Multiply",
      "type": "UnityEditor.VFX.VFXOperator",
      "position": { "x": 100.0, "y": 200.0 }
    }
  }
}
```

### `remove_node_from_graph`

Removes a node from a VFX graph.

**Parameters:**
- `graph_path` (required): Path to the VFX graph asset
- `node_id` (required): Node instance ID (from `get_graph_structure` or `add_node_to_graph`)

**Example:**
```json
{
  "graph_path": "Assets/VFX/MyEffect.vfx",
  "node_id": 12345
}
```

### `move_graph_node`

Moves a node to a new position in the graph.

**Parameters:**
- `graph_path` (required): Path to the VFX graph asset
- `node_id` (required): Node instance ID
- `position_x` (required): New X position
- `position_y` (required): New Y position

**Example:**
```json
{
  "graph_path": "Assets/VFX/MyEffect.vfx",
  "node_id": 12345,
  "position_x": 300.0,
  "position_y": 400.0
}
```

### `connect_graph_nodes`

Creates a connection between two nodes in a VFX graph.

**Parameters:**
- `graph_path` (required): Path to the VFX graph asset
- `source_node_id` (required): Source node instance ID
- `source_port` (required): Output port name or index on source node
- `target_node_id` (required): Target node instance ID
- `target_port` (required): Input port name or index on target node

**Note:** Ports can be specified by name (e.g., "value") or by index (e.g., "0" for the first port).

**Example:**
```json
{
  "graph_path": "Assets/VFX/MyEffect.vfx",
  "source_node_id": 12345,
  "source_port": "output",
  "target_node_id": 12346,
  "target_port": "input"
}
```

### `disconnect_graph_nodes`

Removes a connection between two nodes.

**Parameters:**
- `graph_path` (required): Path to the VFX graph asset
- `source_node_id` (required): Source node instance ID
- `source_port` (required): Output port name or index
- `target_node_id` (required): Target node instance ID
- `target_port` (required): Input port name or index

**Example:**
```json
{
  "graph_path": "Assets/VFX/MyEffect.vfx",
  "source_node_id": 12345,
  "source_port": "output",
  "target_node_id": 12346,
  "target_port": "input"
}
```

### `set_graph_node_property`

Sets a property value on a node.

**Parameters:**
- `graph_path` (required): Path to the VFX graph asset
- `node_id` (required): Node instance ID
- `property_name` (required): Property name to set
- `property_value` (required): Property value (will be converted to appropriate type)

**Supported Types:**
- `int`, `float`, `bool`, `string`
- `Vector2`: `{"x": 1.0, "y": 2.0}`
- `Vector3`: `{"x": 1.0, "y": 2.0, "z": 3.0}`

**Example:**
```json
{
  "graph_path": "Assets/VFX/MyEffect.vfx",
  "node_id": 12345,
  "property_name": "value",
  "property_value": 5.0
}
```

### `get_graph_structure`

Retrieves the complete structure of a graph, including all nodes and connections.

**Parameters:**
- `graph_path` (required): Path to the VFX graph asset

**Response:**
```json
{
  "success": true,
  "data": {
    "graphPath": "Assets/VFX/MyEffect.vfx",
    "graphType": "VFXGraph",
    "structure": {
      "nodeCount": 5,
      "dataConnectionCount": 4,
      "flowConnectionCount": 2,
      "nodes": [
        {
          "id": 12345,
          "name": "Multiply",
          "type": "UnityEditor.VFX.VFXOperator",
          "position": { "x": 100.0, "y": 200.0 },
          "inputs": [
            { "name": "a", "path": "a", "direction": "input", "connected": true }
          ],
          "outputs": [
            { "name": "output", "path": "output", "direction": "output", "connected": true }
          ]
        }
      ],
      "dataConnections": [
        {
          "sourceNodeId": 12345,
          "sourcePort": "output",
          "targetNodeId": 12346,
          "targetPort": "input"
        }
      ],
      "flowConnections": []
    }
  }
}
```

## Workflow Examples

### Creating a Simple VFX Graph

1. **List available nodes:**
```json
{ "category": "Context", "limit": 10 }
```

2. **Add a spawn context:**
```json
{
  "graph_path": "Assets/VFX/MyEffect.vfx",
  "node_type": "Spawn",
  "position_x": 0,
  "position_y": 0
}
```

3. **Add an operator:**
```json
{
  "graph_path": "Assets/VFX/MyEffect.vfx",
  "node_type": "Multiply",
  "position_x": 200,
  "position_y": 0
}
```

4. **Connect them:**
```json
{
  "graph_path": "Assets/VFX/MyEffect.vfx",
  "source_node_id": <spawn_node_id>,
  "source_port": "output",
  "target_node_id": <multiply_node_id>,
  "target_port": "input"
}
```

5. **Inspect the result:**
```json
{ "graph_path": "Assets/VFX/MyEffect.vfx" }
```

## Error Handling

All tools return structured error responses:

```json
{
  "success": false,
  "message": "Node with id 12345 not found"
}
```

Common errors:
- **Graph not found**: Check the `graph_path` is correct
- **Node not found**: Use `get_graph_structure` to verify node IDs
- **Port not found**: Use `get_graph_structure` to see available ports
- **Type mismatch**: Check that property types match expected values

## Implementation Notes

- All graph modifications are automatically saved
- Tools use reflection to access Unity's internal VFX Graph APIs
- Node IDs are instance IDs from Unity's object system
- Ports can be referenced by name or index
- Duplicate node detection warns when nodes are placed very close together

## Advanced Tools

### Flow Context Management

#### `connect_flow_contexts`

Connects two VFX contexts via flow (spawn/init/update/output flow connections).

**Parameters:**
- `graph_path` (required): Path to the VFX graph asset
- `source_context_id` (required): Source context node ID
- `source_slot_index` (optional): Source flow slot index (default: 0)
- `target_context_id` (required): Target context node ID
- `target_slot_index` (optional): Target flow slot index (default: 0)

**Example:**
```json
{
  "graph_path": "Assets/VFX/MyEffect.vfx",
  "source_context_id": 12345,
  "target_context_id": 12346
}
```

#### `disconnect_flow_contexts`

Disconnects flow connections between contexts.

**Parameters:** Same as `connect_flow_contexts`

### Block Authoring

#### `list_context_blocks`

Lists all blocks in a VFX context (Initialize, Update, or Output).

**Parameters:**
- `graph_path` (required): Path to the VFX graph asset
- `context_id` (required): Context node ID

**Response:**
```json
{
  "success": true,
  "data": {
    "blockCount": 3,
    "blocks": [
      {
        "id": 78901,
        "name": "SetAttribute",
        "type": "UnityEditor.VFX.Block.SetAttribute",
        "index": 0
      }
    ]
  }
}
```

#### `add_block_to_context`

Adds a block to a VFX context.

**Parameters:**
- `graph_path` (required): Path to the VFX graph asset
- `context_id` (required): Context node ID
- `block_type` (required): Block type identifier (e.g., "SetAttribute", "Orient")
- `insert_index` (optional): Index to insert block at

#### `remove_block_from_context`

Removes a block from a context.

**Parameters:**
- `graph_path` (required): Path to the VFX graph asset
- `context_id` (required): Context node ID
- `block_id` (required): Block instance ID

#### `move_block_in_context`

Moves a block to a new position within a context.

**Parameters:**
- `graph_path` (required): Path to the VFX graph asset
- `context_id` (required): Context node ID
- `block_id` (required): Block instance ID
- `new_index` (required): New index position

### Discovery Tools

#### `describe_node_ports`

Get detailed information about all input and output ports on a node.

**Parameters:**
- `graph_path` (required): Path to the VFX graph asset
- `node_id` (required): Node instance ID

**Response:**
```json
{
  "success": true,
  "data": {
    "inputs": [
      {
        "index": 0,
        "name": "a",
        "path": "a",
        "direction": "input",
        "connected": false,
        "portType": "System.Single",
        "defaultValue": 0.0
      }
    ],
    "outputs": [
      {
        "index": 0,
        "name": "",
        "path": "",
        "direction": "output",
        "connected": true,
        "portType": "System.Single"
      }
    ]
  }
}
```

#### `describe_node_settings`

Get detailed information about all settings/properties on a node.

**Parameters:**
- `graph_path` (required): Path to the VFX graph asset
- `node_id` (required): Node instance ID

**Response:**
```json
{
  "success": true,
  "data": {
    "settings": [
      {
        "name": "value",
        "type": "System.Single",
        "canRead": true,
        "canWrite": true,
        "value": 1.0
      }
    ]
  }
}
```

### Slot and Parameter Management

#### `set_slot_value`

Set the default value of a slot (port) on a node without requiring a connection.

**Parameters:**
- `graph_path` (required): Path to the VFX graph asset
- `node_id` (required): Node instance ID
- `slot_name` (required): Slot name or path
- `slot_value` (required): Value to set (will be converted to appropriate type)

**Example:**
```json
{
  "graph_path": "Assets/VFX/MyEffect.vfx",
  "node_id": 12345,
  "slot_name": "a",
  "slot_value": 5.0
}
```

#### `list_exposed_parameters`

List all exposed parameters in a VFX graph.

**Parameters:**
- `graph_path` (required): Path to the VFX graph asset

**Response:**
```json
{
  "success": true,
  "data": {
    "parameterCount": 2,
    "parameters": [
      {
        "id": 45678,
        "name": "Intensity",
        "exposedName": "Intensity",
        "valueType": "System.Single",
        "value": 1.0
      }
    ]
  }
}
```

#### `create_exposed_parameter`

Create a new exposed parameter in a VFX graph.

**Parameters:**
- `graph_path` (required): Path to the VFX graph asset
- `parameter_name` (required): Parameter name
- `parameter_type` (required): Parameter type (e.g., "Float", "Vector3", "Texture2D")
- `default_value` (optional): Default value

**Example:**
```json
{
  "graph_path": "Assets/VFX/MyEffect.vfx",
  "parameter_name": "Intensity",
  "parameter_type": "Float",
  "default_value": 1.0
}
```

## Complete Tool List

### Basic Graph Operations
1. `get_graph_structure` - Get complete graph structure
2. `list_graph_variants` - List available node types
3. `add_node_to_graph` - Add nodes
4. `remove_node_from_graph` - Remove nodes
5. `move_graph_node` - Move nodes

### Connections
6. `connect_graph_nodes` - Connect data slots
7. `disconnect_graph_nodes` - Disconnect data slots
8. `connect_flow_contexts` - Connect flow contexts
9. `disconnect_flow_contexts` - Disconnect flow contexts

### Block Management
10. `list_context_blocks` - List blocks in context
11. `add_block_to_context` - Add block to context
12. `remove_block_from_context` - Remove block from context
13. `move_block_in_context` - Reorder blocks

### Discovery & Configuration
14. `describe_node_ports` - Get port information
15. `describe_node_settings` - Get settings information
16. `set_graph_node_property` - Set node properties
17. `set_slot_value` - Set slot default values

### Parameters
18. `list_exposed_parameters` - List exposed parameters
19. `create_exposed_parameter` - Create exposed parameter
20. `rename_exposed_parameter` - Rename exposed parameter
21. `delete_exposed_parameter` - Delete exposed parameter
22. `set_exposed_parameter_property` - Set parameter properties (category, tooltip, etc.)
23. `bind_parameter_to_object` - Bind parameter to GameObject property
24. `unbind_parameter` - Unbind parameter, reset to default

### Graph Lifecycle
25. `create_vfx_graph` - Create new VFX graph asset
26. `duplicate_graph` - Duplicate existing graph
27. `set_visual_effect_graph` - Assign graph to VisualEffect component
28. `list_graph_instances` - List all VisualEffect instances in scene

### Context & Node Management
29. `create_context` - Create new VFX context (Spawn, Initialize, etc.)
30. `duplicate_context` - Duplicate existing context
31. `toggle_context_enabled` - Enable/disable context
32. `duplicate_node` - Duplicate any node
33. `toggle_block_enabled` - Enable/disable block in context

### Connection & Metadata
34. `list_data_connections` - List all data connections in graph
35. `get_slot_metadata` - Get slot metadata (type, constraints, defaults)
36. `list_available_node_settings` - List all configurable settings on a node

### Scene & Runtime Integration
37. `spawn_visual_effect_prefab` - Spawn prefab with VisualEffect component
38. `swap_visual_effect_graph` - Swap graph on VisualEffect component
39. `play_effect` - Start effect playback
40. `stop_effect` - Stop effect playback
41. `send_vfx_event` - Send VFX event (Play, Stop, custom)
42. `set_vfx_parameter` - Set parameter value on VisualEffect component

### Diagnostics & Layout
43. `get_vfx_graph_diagnostics` - Get compilation errors/warnings and statistics
44. `export_graph_snapshot` - Export graph structure to JSON
45. `auto_layout_nodes` - Auto-arrange nodes in grid layout
46. `align_nodes` - Align multiple nodes horizontally or vertically

## New Tool Details

### Graph Lifecycle Tools

#### `create_vfx_graph`

Creates a new VFX graph asset at the specified path.

**Parameters:**
- `graph_path` (required): Path where the graph should be created (must end with .vfx)

**Example:**
```json
{
  "graph_path": "Assets/VFX/NewEffect.vfx"
}
```

#### `duplicate_graph`

Duplicates an existing VFX graph asset.

**Parameters:**
- `source_path` (required): Path to source graph
- `destination_path` (required): Path for duplicated graph

#### `set_visual_effect_graph`

Sets the VFX graph asset on a VisualEffect component in the scene.

**Parameters:**
- `gameobject_name` (required): GameObject name with VisualEffect component
- `graph_path` (required): Path to VFX graph asset

#### `list_graph_instances`

Lists all VisualEffect components in the active scene.

**Response:**
```json
{
  "success": true,
  "data": {
    "instanceCount": 3,
    "instances": [
      {
        "gameObjectName": "VFX_Explosion",
        "gameObjectPath": "VFX_Explosion",
        "graphPath": "Assets/VFX/Explosion.vfx",
        "graphName": "Explosion"
      }
    ]
  }
}
```

### Batch Graph Builders

#### `build_vfx_graph_tree`

Creates a starter VFX graph in a single command. The tool assembles a spawn → initialize → update → output flow, drops the contexts onto the canvas, connects them, and adds common blocks such as a constant spawn rate and basic initialize logic.

**Parameters:**
- `graph_path` (required): Path to the VFX graph asset to create or update
- `overwrite` (optional): Delete and recreate the asset if it already exists (default: `false`)
- `spawn_rate` (optional): Spawn rate to apply to the constant spawn block (default: `32.0`)

**Response:**
```json
{
  "success": true,
  "message": "Built VFX graph tree with 4 contexts",
  "data": {
    "graphPath": "Assets/VFX/StarterTree.vfx",
    "contexts": [
      {
        "role": "Spawn",
        "identifier": "VFXBasicSpawner",
        "contextId": 12345,
        "blocks": [
          {
            "identifier": "ConstantSpawnRate",
            "added": true,
            "message": "Added"
          }
        ]
      }
    ],
    "connections": [
      {
        "sourceContextId": 12345,
        "targetContextId": 12346,
        "connected": true,
        "message": "Connected"
      }
    ],
    "warnings": [
      "Block not found for context 'Initialize': No variant found matching keywords ..."
    ]
  }
}
```

Warnings are included for any optional blocks that could not be located on the current Unity version/package set; the base contexts and connections are still created successfully.

### Context & Node Management

#### `create_context`

Creates a new VFX context (Spawn, Initialize, Update, Output).

**Parameters:**
- `graph_path` (required): Path to graph asset
- `context_type` (required): Context type (e.g., "VFXBasicSpawner", "VFXBasicInitialize")
- `position_x` (required): X position
- `position_y` (required): Y position

#### `duplicate_context`

Duplicates an existing context.

**Parameters:**
- `graph_path` (required): Path to graph asset
- `context_id` (required): Context ID to duplicate
- `position_x` (optional): X position for duplicate (default: offset from original)
- `position_y` (optional): Y position for duplicate (default: offset from original)

#### `toggle_context_enabled`

Enables or disables a context.

**Parameters:**
- `graph_path` (required): Path to graph asset
- `context_id` (required): Context ID
- `enabled` (required): true to enable, false to disable

#### `duplicate_node`

Duplicates any node (operator or context).

**Parameters:**
- `graph_path` (required): Path to graph asset
- `node_id` (required): Node ID to duplicate
- `position_x` (optional): X position for duplicate
- `position_y` (optional): Y position for duplicate

#### `toggle_block_enabled`

Enables or disables a block within a context.

**Parameters:**
- `graph_path` (required): Path to graph asset
- `context_id` (required): Context ID
- `block_id` (required): Block ID
- `enabled` (required): true to enable, false to disable

### Connection & Metadata Tools

#### `list_data_connections`

Lists all data connections in a graph, optionally filtered by node.

**Parameters:**
- `graph_path` (required): Path to graph asset
- `node_id` (optional): Filter to show only connections involving this node

**Response:**
```json
{
  "success": true,
  "data": {
    "connectionCount": 5,
    "connections": [
      {
        "sourceNodeId": 12345,
        "sourcePortName": "output",
        "sourcePortPath": "output",
        "targetNodeId": 12346,
        "targetPortName": "input",
        "targetPortPath": "input",
        "connectionType": "data"
      }
    ]
  }
}
```

#### `get_slot_metadata`

Get detailed metadata about a slot including type, constraints, and default values.

**Parameters:**
- `graph_path` (required): Path to graph asset
- `node_id` (required): Node ID
- `slot_name` (required): Slot name or path

**Response:**
```json
{
  "success": true,
  "data": {
    "metadata": {
      "slotName": "a",
      "slotType": "UnityEditor.VFX.VFXSlot",
      "name": "a",
      "path": "a",
      "valueType": "System.Single",
      "minValue": 0.0,
      "maxValue": 10.0,
      "defaultValue": 1.0
    }
  }
}
```

#### `list_available_node_settings`

Lists all available settings/properties that can be configured on a node.

**Parameters:**
- `graph_path` (required): Path to graph asset
- `node_id` (required): Node ID

**Response:**
```json
{
  "success": true,
  "data": {
    "settingCount": 10,
    "settings": [
      {
        "name": "value",
        "type": "System.Single",
        "canRead": true,
        "canWrite": true,
        "isPublic": true,
        "currentValue": 2.5,
        "minValue": 0.0,
        "maxValue": 10.0
      }
    ]
  }
}
```

### Parameter Management Tools

#### `rename_exposed_parameter`

Renames an exposed parameter.

**Parameters:**
- `graph_path` (required): Path to graph asset
- `parameter_name` (required): Current parameter name
- `new_name` (required): New parameter name

#### `delete_exposed_parameter`

Deletes an exposed parameter from a graph.

**Parameters:**
- `graph_path` (required): Path to graph asset
- `parameter_name` (required): Parameter name to delete

#### `set_exposed_parameter_property`

Sets properties on exposed parameters (category, tooltip, exposedName, etc.).

**Parameters:**
- `graph_path` (required): Path to graph asset
- `parameter_name` (required): Parameter name
- `property_name` (required): Property to set (e.g., "exposedName", "category", "tooltip")
- `property_value` (required): Value to set

#### `bind_parameter_to_object`

Binds a VFX parameter to a property on a GameObject in the scene.

**Parameters:**
- `gameobject_name` (required): GameObject with VisualEffect component
- `parameter_name` (required): VFX parameter name
- `target_object_name` (required): Target GameObject
- `property_path` (required): Property path (e.g., "transform.position")

#### `unbind_parameter`

Unbinds a parameter, resetting it to its default value.

**Parameters:**
- `gameobject_name` (required): GameObject with VisualEffect component
- `parameter_name` (required): Parameter name to unbind

### Scene & Runtime Integration

#### `spawn_visual_effect_prefab`

Spawns a prefab with a VisualEffect component in the scene.

**Parameters:**
- `prefab_path` (required): Path to prefab asset
- `position_x` (optional): X position (default: 0)
- `position_y` (optional): Y position (default: 0)
- `position_z` (optional): Z position (default: 0)
- `parent_name` (optional): Parent GameObject name

#### `swap_visual_effect_graph`

Swaps the VFX graph asset on a VisualEffect component.

**Parameters:**
- `gameobject_name` (required): GameObject with VisualEffect component
- `graph_path` (required): Path to new VFX graph asset

#### `play_effect`

Starts playback of a visual effect.

**Parameters:**
- `gameobject_name` (required): GameObject with VisualEffect component

#### `stop_effect`

Stops playback of a visual effect.

**Parameters:**
- `gameobject_name` (required): GameObject with VisualEffect component

#### `send_vfx_event`

Sends a VFX event to a visual effect (e.g., "Play", "Stop", custom events).

**Parameters:**
- `gameobject_name` (required): GameObject with VisualEffect component
- `event_name` (required): VFX event name

#### `set_vfx_parameter`

Sets a parameter value on a VisualEffect component. Auto-detects type or can be specified.

**Parameters:**
- `gameobject_name` (required): GameObject with VisualEffect component
- `parameter_name` (required): VFX parameter name
- `parameter_value` (required): Value to set
- `parameter_type` (optional): Type hint (float, int, bool, vector2, vector3, vector4, color, texture2d)

**Supported Types:**
- `float`, `int`, `bool`
- `vector2`: `{"x": 1.0, "y": 2.0}`
- `vector3`: `{"x": 1.0, "y": 2.0, "z": 3.0}`
- `vector4`/`color`: `{"x": 1.0, "y": 2.0, "z": 3.0, "w": 4.0}` or `{"r": 1.0, "g": 0.5, "b": 0.0, "a": 1.0}`
- `texture2d`: Path to texture asset

### Diagnostics & Layout Tools

#### `get_vfx_graph_diagnostics`

Gets compilation errors, warnings, and graph statistics.

**Parameters:**
- `graph_path` (required): Path to graph asset

**Response:**
```json
{
  "success": true,
  "data": {
    "errorCount": 0,
    "warningCount": 2,
    "diagnostics": [
      {
        "type": "warning",
        "message": "Unused parameter detected",
        "line": 42
      }
    ],
    "statistics": {
      "nodeCount": 15,
      "contextCount": 4,
      "connectionCount": 12
    }
  }
}
```

#### `export_graph_snapshot`

Exports a snapshot of the graph structure to JSON for comparison or backup.

**Parameters:**
- `graph_path` (required): Path to graph asset
- `snapshot_path` (optional): Path for snapshot (default: graph_path with _snapshot.json)

**Response:**
```json
{
  "success": true,
  "data": {
    "graphPath": "Assets/VFX/MyEffect.vfx",
    "snapshotPath": "Assets/VFX/MyEffect_snapshot.json",
    "snapshotSize": 5432
  }
}
```

#### `auto_layout_nodes`

Automatically arranges nodes in a grid layout for better readability.

**Parameters:**
- `graph_path` (required): Path to graph asset
- `spacing_x` (optional): Horizontal spacing (default: 200)
- `spacing_y` (optional): Vertical spacing (default: 150)
- `start_x` (optional): Starting X position (default: 0)
- `start_y` (optional): Starting Y position (default: 0)

#### `align_nodes`

Aligns multiple nodes horizontally or vertically with specified spacing.

**Parameters:**
- `graph_path` (required): Path to graph asset
- `node_ids` (required): Array of node IDs to align
- `alignment` (optional): "horizontal" or "vertical" (default: "horizontal")
- `spacing` (optional): Spacing between nodes (default: 200)

## Enhanced `describe_node_ports`

The `describe_node_ports` tool now includes connection information:

**Response:**
```json
{
  "success": true,
  "data": {
    "inputs": [
      {
        "index": 0,
        "name": "a",
        "path": "a",
        "direction": "input",
        "connected": true,
        "portType": "System.Single",
        "defaultValue": 0.0,
        "connections": [
          {
            "sourceNodeId": 12345,
            "sourcePortName": "output",
            "sourcePortPath": "output",
            "connectionType": "data"
          }
        ]
      }
    ]
  }
}
```

## Core Unity Operations

### Core Asset Creation

#### `create_material`

Creates a new material asset with a specified shader.

**Parameters:**
- `material_path` (required): Path for material asset (e.g., "Assets/Materials/MyMaterial.mat")
- `shader_name` (optional): Shader name (default: "Standard")
- `replace_existing` (optional): Overwrite if material already exists (default: false)

**Example:**
```json
{
  "material_path": "Assets/Materials/MyMaterial.mat",
  "shader_name": "Standard",
  "replace_existing": false
}
```

#### `create_texture`

Creates a placeholder texture asset.

**Parameters:**
- `texture_path` (required): Path for texture asset
- `width` (optional): Texture width in pixels (default: 256)
- `height` (optional): Texture height in pixels (default: 256)
- `texture_format` (optional): Texture format (default: "RGBA32")
- `fill_color` (optional): Fill color as {r, g, b, a} (0-1, default: white)
- `replace_existing` (optional): Overwrite if texture already exists (default: false)

#### `create_mesh`

Creates a primitive mesh asset (plane, cube, sphere, cylinder).

**Parameters:**
- `mesh_path` (required): Path for mesh asset
- `mesh_type` (optional): Mesh type: "plane", "cube", "sphere", "cylinder" (default: "plane")
- `replace_existing` (optional): Overwrite if mesh already exists (default: false)

### Transform Utilities

#### `set_transform`

Sets transform properties (position, rotation, scale, parent) on a GameObject.

**Parameters:**
- `gameobject_name` (required): Name or instance ID of GameObject
- `position` (optional): Position as {x, y, z}
- `rotation` (optional): Rotation (Euler angles) as {x, y, z}
- `scale` (optional): Scale as {x, y, z}
- `parent_name` (optional): Name of parent GameObject (empty string to unparent)
- `search_method` (optional): Search method: "by_name", "by_id" (default: "by_name")

#### `get_transform`

Gets transform properties from a GameObject.

**Parameters:**
- `gameobject_name` (required): Name or instance ID of GameObject
- `search_method` (optional): Search method: "by_name", "by_id" (default: "by_name")

### Timeline Operations

#### `create_timeline_asset`

Creates a new Timeline asset.

**Parameters:**
- `timeline_path` (required): Path for timeline asset (e.g., "Assets/Timelines/MyTimeline.playable")
- `replace_existing` (optional): Overwrite if timeline already exists (default: false)

#### `add_timeline_track`

Adds a track to a Timeline asset.

**Parameters:**
- `timeline_path` (required): Path to timeline asset
- `track_type` (optional): Track type: "ActivationTrack", "AnimationTrack", "AudioTrack", "ControlTrack", "PlayableTrack" (default: "ActivationTrack")
- `track_name` (optional): Name for the track

#### `add_timeline_clip`

Adds a clip to a Timeline track.

**Parameters:**
- `timeline_path` (required): Path to timeline asset
- `track_index` (optional): Track index (0-based)
- `track_name` (optional): Track name
- `clip_asset_path` (optional): Path to clip asset (required for AudioTrack)
- `start_time` (optional): Clip start time in seconds (default: 0.0)
- `duration` (optional): Clip duration in seconds (default: 1.0)

#### `set_timeline_playhead`

Sets the playhead position on a PlayableDirector.

**Parameters:**
- `gameobject_name` (required): Name or instance ID of GameObject with PlayableDirector component
- `time` (optional): Time position in seconds (default: 0.0)
- `search_method` (optional): Search method: "by_name", "by_id" (default: "by_name")

### Audio Operations

#### `play_audio_clip`

Configures and plays an audio clip on an AudioSource component.

**Parameters:**
- `gameobject_name` (required): Name or instance ID of GameObject with AudioSource component
- `clip_path` (required): Path to AudioClip asset
- `volume` (optional): Volume (0-1, default: 1.0)
- `pitch` (optional): Pitch (-3 to 3, default: 1.0)
- `search_method` (optional): Search method: "by_name", "by_id" (default: "by_name")

#### `set_audio_source_property`

Sets properties on an AudioSource component.

**Parameters:**
- `gameobject_name` (required): Name or instance ID of GameObject with AudioSource component
- `volume` (optional): Volume (0-1)
- `pitch` (optional): Pitch (-3 to 3)
- `play_on_awake` (optional): Play on awake
- `loop` (optional): Loop audio
- `search_method` (optional): Search method: "by_name", "by_id" (default: "by_name")

### Project Automation

#### `enter_play_mode`

Enters Play mode in Unity Editor.

**Parameters:** None

#### `exit_play_mode`

Exits Play mode in Unity Editor.

**Parameters:** None

#### `refresh_assets`

Refreshes and saves all assets in the Unity project.

**Parameters:** None

#### `list_scripting_defines`

Lists scripting define symbols for a build target group.

**Parameters:**
- `target_group` (optional): Build target group (e.g., "Standalone", "Android", "iOS")

#### `add_scripting_define`

Adds a scripting define symbol to a build target group.

**Parameters:**
- `define` (required): Scripting define symbol to add
- `target_group` (optional): Build target group (e.g., "Standalone", "Android", "iOS")

#### `remove_scripting_define`

Removes a scripting define symbol from a build target group.

**Parameters:**
- `define` (required): Scripting define symbol to remove
- `target_group` (optional): Build target group (e.g., "Standalone", "Android", "iOS")

## Shader Graph Operations

### Graph Lifecycle

#### `create_shader_graph`

Creates a new Shader Graph or SubGraph asset.

**Parameters:**
- `graph_path` (required): Path for shader graph asset (e.g., "Assets/Shaders/MyShader.shadergraph")
- `is_subgraph` (optional): Create as SubGraph instead of ShaderGraph (default: false)
- `replace_existing` (optional): Overwrite if graph already exists (default: false)

#### `duplicate_shader_graph`

Duplicates a Shader Graph asset to a new path.

**Parameters:**
- `source_path` (required): Path to source shader graph asset
- `destination_path` (required): Path where duplicated graph should be created
- `allow_overwrite` (optional): Overwrite if destination already exists (default: false)

#### `get_shader_graph_structure`

Gets the structure of a Shader Graph (nodes, edges, properties).

**Parameters:**
- `graph_path` (required): Path to shader graph asset

**Response includes:**
- `nodeCount`: Number of nodes in the graph
- `nodes`: Array of node information (id, name, type, position)
- `edgeCount`: Number of connections
- `edges`: Array of edge information (source/target node IDs and slots)
- `propertyCount`: Number of blackboard properties
- `properties`: Array of property information

### Node & Property Operations

#### `add_node_to_shader_graph`

Adds a node to a Shader Graph.

**Parameters:**
- `graph_path` (required): Path to shader graph asset
- `node_type` (required): Node type (e.g., "Multiply", "Add", "SampleTexture2D")
- `position_x` (required): X position for the node
- `position_y` (required): Y position for the node

#### `connect_shader_graph_nodes`

Connects two nodes in a Shader Graph.

**Parameters:**
- `graph_path` (required): Path to shader graph asset
- `source_node_id` (required): Source node GUID
- `target_node_id` (required): Target node GUID
- `source_slot` (optional): Source slot name (default: "output")
- `target_slot` (optional): Target slot name (default: "input")

#### `add_shader_graph_property`

Adds a property to a Shader Graph blackboard.

**Parameters:**
- `graph_path` (required): Path to shader graph asset
- `property_name` (required): Name of the property
- `property_type` (optional): Property type: "Float", "Vector2", "Vector3", "Vector4", "Color", "Texture2D" (default: "Float")
- `default_value` (optional): Default value for the property

### Diagnostics

#### `get_shader_graph_diagnostics`

Gets compilation errors, warnings, and graph statistics for a Shader Graph.

**Parameters:**
- `graph_path` (required): Path to shader graph asset

**Response includes:**
- `errorCount`: Number of compilation errors
- `warningCount`: Number of compilation warnings
- `errors`: Array of error messages
- `warnings`: Array of warning messages
- `statistics`: Graph statistics (nodeCount, edgeCount, propertyCount)

## Troubleshooting

### Common Issues

#### "Node type not found" errors
- Use `list_graph_variants` to discover available node types
- Node types are case-sensitive and must match exactly
- Try using the `identifier` field from variant listings

#### "Port not found" errors
- Use `describe_node_ports` to list all available ports on a node
- Port names are case-sensitive
- You can use numeric indices (0, 1, 2...) as port identifiers
- Error messages now include lists of available ports when a port is not found
- Both `connect_graph_nodes` and `disconnect_graph_nodes` provide helpful error messages with available port suggestions

#### Duplicate node handling
- When adding nodes at the same or very close positions, the tool automatically detects duplicates
- Nodes are automatically repositioned to avoid overlap (with a 5-unit tolerance)
- The response includes a `warning` field and `positionAdjusted` flag when auto-adjustment occurs
- Check the response data to see the final position after adjustment
- The warning message includes the distance to the nearby node for reference

#### Connection failures
- Verify both nodes exist using `get_graph_structure`
- Check port compatibility - output and input types must match
- Use `describe_node_ports` to verify port names and types
- Error messages now include available ports when a port is not found

#### Graph not found errors
- Ensure the graph path is relative to the Assets folder (e.g., "Assets/VFX/MyGraph.vfx")
- Verify the graph asset exists in the project
- Check that the Visual Effect Graph package is installed

#### Reflection/assembly errors
- Ensure Unity Visual Effect Graph package is installed
- Check Unity Console for detailed error messages
- Try refreshing assets: `refresh_assets` tool
- Restart Unity Editor if assembly loading issues persist

### Best Practices

1. **Always check graph structure first**: Use `get_graph_structure` before making changes
2. **Use variant discovery**: Call `list_graph_variants` to find correct node type names
3. **Verify ports**: Use `describe_node_ports` before connecting nodes
4. **Check diagnostics**: Run `get_vfx_graph_diagnostics` after making changes
5. **Save frequently**: Tools automatically save using `SyncAndSave` helper for safe, guarded asset operations
6. **Handle duplicate nodes**: The `add_node_to_graph` tool automatically handles position conflicts, but check the response for warnings
7. **Use improved error messages**: When connection/disconnection fails, error messages now include available ports for easier debugging
8. **Property setting resilience**: The `set_graph_node_property` tool uses `SafePropertySet` with fallback options for better compatibility

### Testing

Run the comprehensive test suite from Unity: `Tools > MCP > Test All VFX Tools`

This will verify:
- Graph creation and structure retrieval
- Variant discovery
- Node addition with duplicate handling and auto-adjustment
- Connection with improved slot selection and error messages
- Disconnection with available port suggestions
- Node movement and property setting with fallback support
- Parameter management
- Diagnostics and instance listing

The test suite now includes:
- Duplicate node detection and handling
- Improved error messages with available port listings
- Property setting with `SafePropertySet` fallback
- All handlers using `SyncAndSave` for safe asset operations

## See Also

- `CUSTOM_TOOLS.md` - General MCP tool development guide
- `PureDOTS/Assets/Editor/MCP/Handlers/` - C# handler implementations
- `PureDOTS/Assets/Editor/MCP/Python/` - Python tool definitions
- `PureDOTS/Assets/Editor/MCP/Tests/TestAllVfxTools.cs` - Test harness

