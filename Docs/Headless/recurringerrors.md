# recurringerrors

- UTC: 2026-01-03T19:39:28Z
  - Request: 3ead62eb-aab5-4a38-a7f2-e4a93e733290
  - Desired build commit: origin/walktest-telemetry-oracle
  - Error: git checkout failed for space4x (origin/walktest-telemetry-oracle) pathspec not found
  - Mitigation: use a pin reachable in all requested repos; create matching branch refs in space4x/godgame or pin to origin/main
- UTC: 2026-01-03T20:41:35Z
  - Signature: telemetry bloat when PUREDOTS_TELEMETRY_LEVEL is Unspecified in headless and flags unset (events/metrics flood risk)
  - Repro: headless batch run with telemetry enabled, no PUREDOTS_TELEMETRY_LEVEL, no PUREDOTS_TELEMETRY_FLAGS
  - Mitigation: set PUREDOTS_TELEMETRY_LEVEL=summary (optionally PUREDOTS_TELEMETRY_FLAGS=metrics,frame)
  - Fix: TelemetryExportBootstrapSystem defaults to Summary in batch when level is Unspecified and flags unset (commit f0e2fa8)
  - Evidence (pre-fix): Space4x smoke summary 184,947,236 and 213,574,673 bytes on build_id 20260103_214440_1db54f9099 (cycle_telemetry_safe_20260103_222942); no telemetryTruncated marker
- UTC: 2026-01-03T21:41:29Z
  - Signature: ORACLE_KEYS_MISSING (telemetry.oracle.heartbeat, move.stuck_ticks, ai.idle_with_work_ratio absent)
  - Repro: Space4x smoke on build_id 20260103_233603_1db54f9099 and Godgame smoke on build_id 20260103_233651_a518b02816
  - Evidence: `/mnt/c/dev/Tri/.tri/state/runs/2026-01-03/cycle_oracle_fix3_20260103_233721/space4x/telemetry/space4x_smoke_oracle.ndjson` + excerpt `/mnt/c/dev/Tri/.tri/state/runs/2026-01-03/cycle_oracle_fix3_20260103_233721/space4x/oracle_missing_excerpt.txt`
  - Likely cause: TelemetryOracleAccumulatorSystem not emitting to telemetry buffer (heartbeat missing)
  - Mitigation: none yet (do not assume oracle keys exist; treat H-T02 as FAIL)
  - Fix attempts: emit metrics via stream buffer (commit 9c56043), run before export (commit d13436d), bind oracle state to stream entity (commit c7094cf)
