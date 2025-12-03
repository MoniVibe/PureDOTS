# PureDOTS Extension Requests

This directory contains extension requests from game teams (Space4X, Godgame, etc.) for PureDOTS framework features.

## How to Submit a Request

1. **Create a new markdown file** in this directory
   - Name format: `YYYY-MM-DD-{short-description}.md`
   - Example: `2025-01-27-custom-sensor-categories.md`

2. **Copy the template** from `TEMPLATE.md` and fill it out

3. **Commit and push** to the PureDOTS repository

4. **PureDOTS team** will review and implement approved requests

## Request Status

Requests are tracked with status labels:
- `[PENDING]` - Awaiting review
- `[APPROVED]` - Approved, awaiting implementation
- `[IN PROGRESS]` - Currently being implemented
- `[COMPLETED]` - Implemented and merged
- `[REJECTED]` - Not approved (with reason)
- `[DEFERRED]` - Approved but deferred to future milestone

## Request Lifecycle

1. Game team creates request document
2. PureDOTS team reviews for:
   - Game agnosticism (must be reusable)
   - Architectural fit
   - Implementation feasibility
3. If approved, request is prioritized and assigned
4. Implementation follows patterns in `PUREDOTS_INTEGRATION_SPEC.md`
5. Request marked as `[COMPLETED]` when merged

## See Also

- `Docs/PUREDOTS_INTEGRATION_SPEC.md` - Extension conventions and patterns
- `Docs/ORIENTATION_SUMMARY.md` - PureDOTS architecture overview

