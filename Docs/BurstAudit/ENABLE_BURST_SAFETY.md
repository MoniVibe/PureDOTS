# Enabling Burst Safety Checks

## Method 1: Via Unity Editor Menu (Recommended)

1. Open Unity Editor with the PureDOTS project
2. Go to menu: **PureDOTS > Enable Burst Safety Checks**
3. Check the console for confirmation message

## Method 2: Via Unity's Built-in Burst Menu

1. Open Unity Editor
2. Go to menu: **Jobs > Burst > Enable Safety Checks > Force On**
3. This enables safety checks and forces them on even for jobs with `DisableSafetyChecks = true`

## Method 3: Programmatically (via Script)

The script `PureDOTS/Assets/Editor/EnableBurstSafetyChecks.cs` has been created. You can:

1. **Execute via menu**: Menu > PureDOTS > Enable Burst Safety Checks
2. **Execute via code**: Call `PureDOTS.Editor.EnableBurstSafetyChecks.EnableSafetyChecks()`

## What This Does

- Enables Burst compilation if not already enabled
- Sets `BurstEditorOptions.EnableBurstSafetyChecks = true`
- Sets `BurstEditorOptions.ForceEnableBurstSafetyChecks = true`
- Triggers recompilation of all Burst-compiled code

## Verification

After enabling, check:
1. Console for confirmation message: `[Burst Safety] ✓ Enabled Burst safety checks (Force On mode).`
2. Menu > Jobs > Burst > Enable Safety Checks should show "Force On" as checked
3. Any Burst compilation errors will appear in the console

## Notes

- Safety checks add runtime overhead but catch container index out of bounds and job dependency violations
- Force On mode ensures safety checks run even for jobs that explicitly disable them
- This setting persists across Unity sessions (stored in EditorPrefs)













