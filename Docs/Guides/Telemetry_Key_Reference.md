# Telemetry Key Reference (Agent D)

Standard keys to keep metrics consistent across emitters. Use these when adding metrics to `TelemetryStream` (via TelemetryHub or direct buffer writes).

## Core Runtime
- `system.total_count` (count)
- `system.total_cost_ms` (duration_ms)
- `system.update_ms` (duration_ms) — per-system update time
- `system.gc_gen0/1/2` (count)
- `system.alloc_bytes` (bytes)

## Caching
- `cache.lookups` (count)
- `cache.hits` (count)
- `cache.misses` (count)
- `cache.hit_rate` (ratio)

## Tick Domains
- `domain.cognitive_ticks` (count)
- `domain.economy_ticks` (count)
- `domain.cognitive_skipped` (count)
- `domain.economy_skipped` (count)

## Event Queue
- `event.processed` (count)
- `event.dropped` (count)

## Streaming
- `streaming.cells_active` (count)
- `streaming.cells_serialized` (count)
- `streaming.agents_active` (count)
- `streaming.agents_serialized` (count)

## Telemetry Pipeline
- `telemetry.frames_written` (count)
- `telemetry.metrics_written` (count)

## Guidance
- Prefer `TelemetryHub.Enqueue` (main-thread) or `TelemetryHub.AsParallelWriter()` (jobs) to emit metrics.
- Keep key casing/lowercase and dot-separated segments.
- Always set `TelemetryMetricUnit` appropriately (`Count`, `DurationMilliseconds`, `Bytes`, `Ratio`, `None`).
- Ensure emitters dispose of any temporary native allocations used for metrics.
