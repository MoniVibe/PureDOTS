# Unity MCP Relay for WSL

The Unity MCP bridge binds to `127.0.0.1` inside the Windows editor
process. When the Codex CLI runs inside WSL it cannot reach that socket
directly, so MCP tools report “No Unity Editor instances found”.

This guide describes how to expose the Windows-only bridge through a
lightweight TCP relay and how to point the MCP server at it.

## 1. Start the Windows relay

Run the helper script **from the Windows side** (PowerShell 7+):

```powershell
pwsh -ExecutionPolicy Bypass `
    -File .\MCPutils\Scripts\wsl_unity_mcp_relay.ps1 `
    -ListenPort 6510 `
    -TargetPort 6400
```

- `ListenPort` is the port WSL will connect to (pick any >1024).
- `TargetPort` is the Unity MCP bridge port (default 6400).

Leave this window running; the relay prints a line whenever it proxies a
connection.

## 2. Tell the MCP server where to look

The Codex CLI now sets the following environment variables (see
`~/.codex/config.toml`):

```
UNITY_MCP_REGISTRY_DIR=/mnt/c/Users/<user>/.unity-mcp
UNITY_MCP_RELAY_PORT=6510
```

If you launch the server outside Codex, export the same values so
`PortDiscovery` reads Unity’s Windows registry files and targets the
relay port:

```bash
export UNITY_MCP_REGISTRY_DIR=/mnt/c/Users/<user>/.unity-mcp
export UNITY_MCP_RELAY_PORT=6510
```

The server auto-detects the Windows host IP (via `/etc/resolv.conf`) so
no extra host setting is required, but you can override it with
`UNITY_MCP_HOST=<ip>` if needed.

## 3. Connecting from WSL

Once the relay is running:

1. Restart the Codex CLI (or re-run `codex mcp reload unity_mcp`) so the
   updated config takes effect.
2. Verify the relay port is reachable:

   ```bash
   python3 - <<'PY'
   import socket
   import os

   host = open("/etc/resolv.conf").read().split("nameserver")[1].split()[0]
   s = socket.create_connection((host, 6510), timeout=1)
   print("Relay reachable")
   s.close()
   PY
   ```

3. Call `list_mcp_resources` or `unity://instances` — the connected Unity
   editor should now appear in-tool.

If Unity restarts and picks a new bridge port, rerun the PowerShell
relay with `-TargetPort <newPort>` (the value is written to
`C:\Users\<user>\.unity-mcp\unity-mcp-status-*.json`).
