# Physics Validation Scenarios

The playmode suite now includes lightweight physics checks inspired by Unity’s DOTS Physics samples. They serve as guardrails while we stabilise the configurable physics backend called out in the roadmap.

## Tests

- `PhysicsValidationTests.FallingSphereSettlesAboveGround`
  - Builds a minimal Unity Physics world (Build → Step → Export systems) and drops a dynamic sphere on a static box.
  - Confirms the final contact height remains within tolerance, exercising collider authoring equivalence to the `BoxCollider`/`SphereCollider` samples.

- `PhysicsValidationTests.RuntimeColliderScaleChangeIsDeterministic`
  - Adjusts the sphere radius mid-simulation, mirroring the “Change Collider Size” sample.
  - Verifies that re-baking the collider blob and mass yields identical end-state positions across repeated runs.

Both tests run under fixed 60 Hz timesteps, apply default Unity Physics material properties, and execute inside `SimulationSystemGroup` together with our rewind-friendly guard (Record phase only). They can be extended with additional sample-inspired cases (motors, joints, runtime filter swaps) as the roadmap unlocks each physics feature slice.

